using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoftUmar_Virtuals.Application.Features.Activation.Command;
using SoftUmar_Virtuals.Application.Features.Activation.Query;
using SoftUmar_Virtuals.Application.Features.FiveSim.Activation.Command;
using SoftUmar_Virtuals.Application.Features.FiveSim.Activation.Query;
using SoftUmar_Virtuals.Application.Interfaces;


namespace SoftUmar_Virtuals.API.Controllers
{


    [ApiController]
    [Route("api/activation")]
    [Authorize]
    public class ActivationController(IMediator mediator, ICurrentUserService currentUser) : ControllerBase
    {

        [HttpGet("services")]
        [AllowAnonymous]
        public async Task<IActionResult> GetServices([FromQuery] string country, [FromQuery] string @operator = "any")
        {
            var result = await mediator.Send(new GetAvailableServicesQuery(country, @operator));

            if (result.IsFailed)
                return BadRequest(new { message = result.Errors.First().Message });

            return Ok(result.Value);
        }

        [HttpGet("countries")]
        [AllowAnonymous]
        public async Task<IActionResult> GetCountries()
        {
            var result = await mediator.Send(new GetCountriesQuery());

            if (result.IsFailed)
                return BadRequest(new { message = result.Errors.First().Message });

            return Ok(result.Value);
        }


        [HttpPost("buy")]
        public async Task<IActionResult> Buy([FromBody] BuyActivationRequestDTO dto)
        {
            if (currentUser.UserId is not Guid userId)
                return Unauthorized();

            var result = await mediator.Send(new BuyActivationCommand(
                userId, dto.Country, dto.Product, dto.Operator));

            if (result.IsFailed)
                return BadRequest(new { message = result.Errors.First().Message });

            return Ok(result.Value);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Check(Guid id)
        {
            if (currentUser.UserId is not Guid userId)
                return Unauthorized();

            var result = await mediator.Send(new CheckActivationQuery(userId, id));

            if (result.IsFailed)
                return NotFound(new { message = result.Errors.First().Message });

            return Ok(result.Value);
        }

        [HttpPost("cancel")]
        public async Task<IActionResult> Cancel([FromBody] Guid activationId)
        {
            if (currentUser.UserId is not Guid userId)
                return Unauthorized();

            var result = await mediator.Send(new CancelActivationCommand(userId, activationId));

            if (result.IsFailed)
                return BadRequest(new { message = result.Errors.First().Message });

            return Ok(new { message = "Activation canceled and refunded." });
        }
    }
}