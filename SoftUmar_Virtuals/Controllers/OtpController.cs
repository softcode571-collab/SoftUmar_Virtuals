using MediatR;
using Microsoft.AspNetCore.Mvc;
using SoftUmar_Virtuals.Application.Features.OTP.OtpVerification;
using SoftUmar_Virtuals.Application.Interfaces;


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
        public async Task<IActionResult> OtpVerification(VerifyEmailCommand command)
        {
            var result = await _sender.Send(command);
            if (result.IsSuccess)
            {
                return Ok(result);
            }
            return BadRequest(result.Errors.Select(e => e.Message));

        }

        //[HttpPost("otpReset")]
        //public async Task<IActionResult> OtpReset(OtpResetCommand command)
        //{
        //    var result = await _sender.Send(command);
        //    if (result.IsSuccess)
        //    {
        //        return Ok(result.Value);
        //    }
        //    return BadRequest(result.Errors.Select(e => e.Message));

        //}
    }
}
