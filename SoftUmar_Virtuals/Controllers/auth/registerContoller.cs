using MediatR;
using Microsoft.AspNetCore.Mvc;
using SoftUmar_Virtuals.Application.Features.Auth.RegisterUser.Command;
using SoftUmar_Virtuals.Application.Features.Auth.UserLogin.Command;

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


        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginCommand command)
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
