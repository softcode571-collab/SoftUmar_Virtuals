using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Mvc;
using SoftUmar_Virtuals.Application.Features.Auth.RegisterUser.Command;
using SoftUmar_Virtuals.Application.Features.Auth.UserLogin.Command;
using SoftUmar_Virtuals.Application.Features.Auth.UserLogOut;
using SoftUmar_Virtuals.Application.Features.Auth.UserLogOut.Command;

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


        [HttpPost("logout")]
        [Authorize] // requires a valid access token to call this at all
        public async Task<IActionResult> Logout([FromBody] LogoutRequestDTO dto)
        {
            var result = await sender.Send(new LogOutCommand(dto.RefreshToken));

            if (result.IsFailed)
                return BadRequest(new { message = result.Errors.First().Message });

            return Ok(result.Value); // returns { message: "Logged out." }
        }
    }
}
