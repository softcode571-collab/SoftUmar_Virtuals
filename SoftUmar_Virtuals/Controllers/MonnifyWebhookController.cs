using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SoftUmar_Virtuals.Infrastructure.Services.Monnify;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SoftUmar_Virtuals.Api.Controllers
{
    [ApiController]
    [Route("api/webhooks/monnify")]
    // NO [Authorize] here - Monnify's servers call this, not a logged-in
    // user, so there's no JWT to check. Signature verification below is
    // what proves the request is genuinely from Monnify instead of
    // [Authorize]'s job of proving who a logged-in user is.
    public class MonnifyWebhookController(
         IMediator mediator,
         IOptions<MonnifySettings> settings,
         ILogger<MonnifyWebhookController> logger) : ControllerBase
    {
        private readonly MonnifySettings _settings = settings.Value;

        [HttpPost("MonnifyWebHook")]
        public async Task<IActionResult> Handle()
        {
            // Read the RAW body as text. This is critical: signature
            // verification has to hash the EXACT bytes Monnify sent, not a
            // re-serialized version of a parsed object (which could have
            // different spacing/field order and produce a different hash).
            string rawBody;
            using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
            {
                rawBody = await reader.ReadToEndAsync();
            }

            if (!Request.Headers.TryGetValue("monnify-signature", out var providedSignature))
            {
                logger.LogWarning("Monnify webhook received with no signature header.");
                return Unauthorized();
            }

            var computedSignature = ComputeHmacSha512(rawBody, _settings.SecretKey);

            // FixedTimeEquals instead of == : same timing-attack reasoning as
            // the password check earlier in this project - a normal string
            // comparison returns faster the earlier two strings differ,
            // which could theoretically let an attacker guess the correct
            // signature byte-by-byte by measuring response times. Comparing
            // the hashes' raw bytes in constant time closes that gap.
            var providedBytes = Convert.FromHexString(providedSignature.ToString());
            var computedBytes = Convert.FromHexString(computedSignature);

            if (providedBytes.Length != computedBytes.Length ||
                !CryptographicOperations.FixedTimeEquals(providedBytes, computedBytes))
            {
                logger.LogWarning("Monnify webhook signature verification FAILED - possible forged request.");
                return Unauthorized();
            }

            MonnifyWebhookPayload? payload;
            try
            {
                payload = JsonSerializer.Deserialize<MonnifyWebhookPayload>(rawBody,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (JsonException ex)
            {
                logger.LogWarning(ex, "Monnify webhook payload could not be parsed.");
                return BadRequest();
            }

            if (payload?.EventData == null)
            {
                logger.LogWarning("Monnify webhook payload had no eventData.");
                return BadRequest();
            }

            // Only handle the event type that means "money arrived". Monnify
            // sends other event types too (e.g. refund events) - ignore
            // anything that isn't this, rather than trying to process it
            // as a credit.
            if (payload.EventType != "SUCCESSFUL_TRANSACTION")
            {
                logger.LogInformation("Monnify webhook event type '{EventType}' ignored.", payload.EventType);
                return Ok(); // still 200 - acknowledge receipt even for
                             // event types we don't act on, so Monnify
                             // doesn't keep retrying something we're
                             // deliberately ignoring
            }

            var result = await mediator.Send(new CreditWalletFromWebhookCommand(payload.EventData));

            // ALWAYS return 200 here if we got this far, even if the
            // command internally failed (e.g. no matching wallet). Returning
            // a non-200 tells Monnify to RETRY the webhook - which is only
            // useful for genuinely transient failures, not "no wallet
            // exists for this account number" (retrying won't fix that).
            // The failure is already logged inside the handler for you to
            // investigate manually.
            return Ok();
        }

        private static string ComputeHmacSha512(string payload, string secretKey)
        {
            using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(secretKey));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            return Convert.ToHexString(hash).ToLowerInvariant();
        }
    }
}
