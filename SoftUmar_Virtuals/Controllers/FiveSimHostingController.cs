using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoftUmar_Virtuals.Application.Features.Activation.Command;
using SoftUmar_Virtuals.Application.Features.Activation.Query;
using SoftUmar_Virtuals.Application.Interfaces;

namespace SoftUmar_Virtuals.API.Controllers
{
    public class BuyHostingRequestDTO
    {
        public required string Country { get; set; }
        public required string Duration { get; set; } // e.g. "1month"
        public string Operator { get; set; } = "any";
    }

    [ApiController]
    [Route("api/hosting")]
    [Authorize]
    public class HostingController(
        IMediator mediator,
        ICurrentUserService currentUser) : ControllerBase
    {
        [HttpGet("durations")]
        [AllowAnonymous]
        public async Task<IActionResult> GetDurations([FromQuery] string country, [FromQuery] string @operator = "any")
        {
            var result = await mediator.Send(new GetHostingDurationsQuery(country, @operator));

            if (result.IsFailed)
                return BadRequest(new { message = result.Errors.First().Message });

            return Ok(result.Value);
        }

        [HttpPost("buy")]
        public async Task<IActionResult> Buy([FromBody] BuyHostingRequestDTO dto)
        {
            if (currentUser.UserId is not Guid userId)
                return Unauthorized();

            var result = await mediator.Send(new BuyHostingCommand(
                userId, dto.Country, dto.Duration, dto.Operator));

            if (result.IsFailed)
                return BadRequest(new { message = result.Errors.First().Message });

            return Ok(result.Value);
        }

        [HttpGet("{id}/inbox")]
        public async Task<IActionResult> GetInbox(Guid id)
        {
            if (currentUser.UserId is not Guid userId)
                return Unauthorized();

            var result = await mediator.Send(new GetHostingInboxQuery(userId, id));

            if (result.IsFailed)
                return BadRequest(new { message = result.Errors.First().Message });

            return Ok(result.Value);
        }
    }
}