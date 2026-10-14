using BCrypt.Net;
using FluentResults;

using MediatR;
using Microsoft.Extensions.Configuration;
using SoftUmar_Virtuals.Application.Interfaces;

using SoftUmar_Virtuals.Domain.Entities;
using SoftUmar_Virtuals.Infrastructure.Services.OTP.OtpVerification;

namespace SoftUmar_Virtuals.Infrastructure.Services.OTP.OtpReset;

public record OtpResetCommand(string Email)
    : IRequest<Result<OtpVerificationCommandOutput>>;

public class OtpResetCommandHandler(
    IConfiguration config,
    IEmailService emailService,
    IOtpService otpService,
    IRepository<Otp> otpRepository)
    : IRequestHandler<OtpResetCommand, Result<OtpVerificationCommandOutput>>
{
    public async Task<Result<OtpVerificationCommandOutput>> Handle(
        OtpResetCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            
            var existingOtp = await otpRepository.GetSingleAsync(
                x => x.Email == request.Email && !x.IsUsed);

            // DELETE OLD OTP
            if (existingOtp != null)
            {
                await otpRepository.DeleteAsync(existingOtp);
            }

            // GENERATE OTP, hased, expiry
            var otpLength = int.Parse(config["OTP:Length"]!);

            var generatedOtp = otpService.GenerateOtp();

            
            var hashedOtp = BCrypt.Net.BCrypt.HashPassword(generatedOtp);

            
            var expiryMinutes = int.Parse(config["OTP:ExpiryMinutes"]!);

           
            var emailSubject = "Email Verification OTP";

            var emailContent =
                $"Your OTP for email verification is: {generatedOtp}. " +
                $"It will expire in {expiryMinutes} minutes.";

            
            await emailService.SendEmailWithFallback(
                request.Email,
                emailSubject,
                emailContent);

            
            var otp = new Otp
            {
                UserId = Guid.NewGuid(),
                Email = request.Email,
                Code = hashedOtp,
                ExpiryTime = DateTime.UtcNow.AddMinutes(expiryMinutes),
                IsUsed = false,
                AttemptCount = 0,
                CreatedAt = DateTime.UtcNow
            };

            await otpRepository.AddAsync(otp, cancellationToken);

            await otpRepository.SaveChanges(cancellationToken);

            return Result.Ok(new OtpVerificationCommandOutput
            {
                Message = "OTP sent successfully. Please check your email."
            });
        }
        catch (Exception ex)
        {
            return Result.Fail(
                $"Failed to send OTP: {ex.Message}");
        }
    }
}