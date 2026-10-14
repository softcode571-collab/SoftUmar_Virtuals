using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SoftUmar_Virtuals.Application.Interfaces;
using SoftUmar_Virtuals.Infrastructure.Services.OTP.OtpReset;
using SoftUmar_Virtuals.Infrastructure.Services.OTP.OtpVerification;

namespace SoftUmar_Virtuals.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OtpController : ControllerBase
    {
        private readonly IOtpService _otpService;
        private readonly ISender _sender;

        public OtpController(IOtpService otpService, ISender sender)
        {
            _otpService = otpService;
            _sender = sender;
        }

        [HttpGet("generate")]
        public IActionResult GenerateOtp()
        {
            var otp = _otpService.GenerateOtp();

            return Ok(new
            {
                success = true,
                message = "OTP generated successfully",
                otp
            });
        }

        [HttpPost("otpVerification")]
        public async Task<IActionResult> OtpVerification(OtpVerificationCommand command)
        {
            var result = await _sender.Send(command);
            if (result.IsSuccess)
            {
                return Ok(result.Value);
            }
            return BadRequest(result.Errors.Select(e => e.Message));

        }

        [HttpPost("otpReset")]
        public async Task<IActionResult> OtpReset(OtpResetCommand command)
        {
            var result = await _sender.Send(command);
            if (result.IsSuccess)
            {
                return Ok(result.Value);
            }
            return BadRequest(result.Errors.Select(e => e.Message));

        }
    }
}
