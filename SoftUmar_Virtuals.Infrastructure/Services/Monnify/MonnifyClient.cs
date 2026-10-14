using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace SoftUmar_Virtuals.Infrastructure.Services.Monnify
{
    // Register: builder.Services.AddHttpClient<MonnifyClient>(client =>
    //     client.BaseAddress = new Uri(builder.Configuration["Monnify:BaseUrl"]!));
    public class MonnifyClient(HttpClient httpClient, IOptions<MonnifySettings> options)
    {
        private readonly MonnifySettings _settings = options.Value;

        // Simple in-memory token cache. Monnify tokens last ~1hr. Not
        // thread-safe against a burst of simultaneous first-callers (each
        // would fetch its own token) - fine at your current scale.
        private string? _cachedToken;
        private DateTime _tokenExpiresAt = DateTime.MinValue;

        private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
        {
            if (_cachedToken != null && DateTime.UtcNow < _tokenExpiresAt.AddSeconds(-60))
                return _cachedToken;

            var credentials = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{_settings.ApiKey}:{_settings.SecretKey}"));

            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/login");
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
            request.Content = new StringContent("{}", Encoding.UTF8, "application/json");

            using var response = await httpClient.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
                throw new MonnifyApiException($"Monnify auth failed: {response.StatusCode} - {body}");

            var authResponse = JsonSerializer.Deserialize<MonnifyAuthResponse>(body, JsonOptions)
                ?? throw new MonnifyApiException("Monnify auth response could not be parsed.");

            _cachedToken = authResponse.ResponseBody.AccessToken;
            _tokenExpiresAt = DateTime.UtcNow.AddSeconds(authResponse.ResponseBody.ExpiresIn);

            return _cachedToken;
        }

        public async Task<MonnifyReservedAccountResult> CreateReservedAccountAsync(
            Guid userId,
            string customerEmail,
            string customerName,
            string? bvn,
            string? nin,
            CancellationToken cancellationToken)
        {
            var token = await GetAccessTokenAsync(cancellationToken);

            // Dictionary, not anonymous object, so a null bvn/nin is OMITTED
            // from the JSON entirely rather than sent as "bvn": null.
            var payload = new Dictionary<string, object?>
            {
                ["accountReference"] = userId.ToString(),
                ["accountName"] = customerName,
                ["currencyCode"] = "NGN",
                ["contractCode"] = _settings.ContractCode,
                ["customerEmail"] = customerEmail,
                ["customerName"] = customerName,
                ["getAllAvailableBanks"] = false,
            };

            if (!string.IsNullOrWhiteSpace(bvn)) payload["bvn"] = bvn;
            if (!string.IsNullOrWhiteSpace(nin)) payload["nin"] = nin;

            using var request = new HttpRequestMessage(
                HttpMethod.Post, "/api/v2/bank-transfer/reserved-accounts");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Content = new StringContent(
                JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json");

            using var response = await httpClient.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
                throw new MonnifyApiException($"Reserved account creation failed: {response.StatusCode} - {body}");

            var parsed = JsonSerializer.Deserialize<MonnifyReservedAccountResponse>(body, JsonOptions)
                ?? throw new MonnifyApiException("Reserved account response could not be parsed.");

            if (!parsed.RequestSuccessful || parsed.ResponseBody == null)
                throw new MonnifyApiException($"Monnify returned failure: {parsed.ResponseMessage}");

            var account = parsed.ResponseBody.Accounts.FirstOrDefault()
                ?? throw new MonnifyApiException("No account returned in Monnify response.");

            return new MonnifyReservedAccountResult(
                AccountReference: parsed.ResponseBody.AccountReference,
                AccountNumber: account.AccountNumber,
                BankName: account.BankName);
        }

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };
    }

    public record MonnifyReservedAccountResult(string AccountReference, string AccountNumber, string BankName);

    public class MonnifyApiException(string message) : Exception(message);

    internal class MonnifyAuthResponse
    {
        [JsonPropertyName("responseBody")]
        public required MonnifyAuthResponseBody ResponseBody { get; set; }
    }

    internal class MonnifyAuthResponseBody
    {
        [JsonPropertyName("accessToken")]
        public required string AccessToken { get; set; }

        [JsonPropertyName("expiresIn")]
        public int ExpiresIn { get; set; }
    }

    internal class MonnifyReservedAccountResponse
    {
        [JsonPropertyName("requestSuccessful")]
        public bool RequestSuccessful { get; set; }

        [JsonPropertyName("responseMessage")]
        public string? ResponseMessage { get; set; }

        [JsonPropertyName("responseBody")]
        public MonnifyReservedAccountResponseBody? ResponseBody { get; set; }
    }

    internal class MonnifyReservedAccountResponseBody
    {
        [JsonPropertyName("accountReference")]
        public required string AccountReference { get; set; }

        [JsonPropertyName("accounts")]
        public List<MonnifyBankAccount> Accounts { get; set; } = [];
    }

    internal class MonnifyBankAccount
    {
        [JsonPropertyName("bankName")]
        public required string BankName { get; set; }

        [JsonPropertyName("accountNumber")]
        public required string AccountNumber { get; set; }
    }
}