using MediatR;
using Microsoft.AspNetCore.Mvc;
using SoftUmar_Virtuals.Application.Features.Auth.RegisterUser.Command;

namespace SoftUmar_Virtuals.Api.Controllers.auth
{
    [ApiController]
    [Route("api/[controller]")]
    
    public class registerContoller(ISender sender) : ControllerBase
    {

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterCommand command)
        {
            var result = await sender.Send(command);
            if (result.IsSuccess)
            {
                return Ok(result.Value);
            }
            return BadRequest(result.Errors.Select(e => e.Message));
        }
    }
}
