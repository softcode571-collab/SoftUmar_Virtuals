using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoftUmar_Virtuals.Application.Features.Auth.RegisterUser.Command
{
    public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
    {
        public RegisterCommandValidator() 
        {
            RuleFor(x => x.register.FirstName)
            .NotEmpty().WithMessage("First name is required")
            .MinimumLength(2).WithMessage("First name must be at least 2 characters")
            .MaximumLength(50);

            RuleFor(x => x.register.LastName)
                .NotEmpty().WithMessage("Last name is required")
                .MinimumLength(2)
                .MaximumLength(50);

            RuleFor(x => x.register.Email)
                .NotEmpty().WithMessage("Email is required")
                .EmailAddress().WithMessage("Invalid email format");

            RuleFor(x => x.register.Password)
                .NotEmpty().WithMessage("Password is required")
                .MinimumLength(6).WithMessage("Password must be at least 6 characters")
                .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter")
                .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter")
                .Matches(@"[0-9]").WithMessage("Password must contain at least one number");

            RuleFor(x => x.register.ConfirmPassword)
                .NotEmpty().WithMessage("Confirm password is required")
                .Equal(x => x.register.Password).WithMessage("Passwords do not match");

            RuleFor(x => x.register.PhoneNumber)
                .NotEmpty().WithMessage("Phone number is required")
                .Matches(@"^\+?[0-9]{10,15}$")
                .WithMessage("Invalid phone number format");

            RuleFor(x => x.register.Country)
                .NotEmpty().WithMessage("Country is required")
                .MaximumLength(100);

            RuleFor(x => x.register.ReferralCode)
                .NotEmpty().WithMessage("Referral code is required")
                .MaximumLength(30);
        }
    }

    
}
