using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoftUmar_Virtuals.Application.Features.Admin.Pricing.Command;

namespace SoftUmar_Virtuals.Api.Controllers.Admin
{
    public class UpdatePricingRequestDTO
    {
        public decimal UsdToNgnRate { get; set; }
        public decimal MarkupPercentage { get; set; }
    }

    [ApiController]
    [Route("api/admin/pricing")]
    [Authorize(Policy = "AdminOnly")] // reuses your existing policy from
                                      // Program.cs - only users with
                                      // Role == "Admin" can touch this
    public class AdminPricingController(IMediator mediator) : ControllerBase
    {
        [HttpPut]
        public async Task<IActionResult> UpdatePricing([FromBody] UpdatePricingRequestDTO dto)
        {
            var result = await mediator.Send(new UpdatePricingSettingsCommand(
                dto.UsdToNgnRate, dto.MarkupPercentage));

            if (result.IsFailed)
                return BadRequest(new { message = result.Errors.First().Message });

            return Ok(new { message = "Pricing updated." });
        }
    }
}