namespace SoftUmar_Virtuals.Application.Features.OTP.OtpVerification;

public class OtpVerificationCommandOutput
{
    public string? Email { get; set; }
    public bool IsVerified { get; set; }
    public string? Message { get; set; }
}
