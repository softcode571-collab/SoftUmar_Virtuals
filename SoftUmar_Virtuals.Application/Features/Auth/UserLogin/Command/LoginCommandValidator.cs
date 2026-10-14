using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace SoftUmar_Virtuals.Application.Features.Auth.UserLogin.Command;

    public class LoginCommandValidator
    {
        public class LoginDTOValidator : AbstractValidator<LoginDTO>
        {
            public LoginDTOValidator()
            {
                RuleFor(x => x.Email)
                    .NotEmpty().WithMessage("Email is required.")
                    .EmailAddress().WithMessage("Email format is invalid.");

                RuleFor(x => x.Password)
                    .NotEmpty().WithMessage("Password is required.");
                // Deliberately NOT validating password length/complexity here —
                // that belongs on Register, not Login. A login attempt should be
                // rejected as "wrong password" by BCrypt.Verify, not pre-judged
                // by a rule that might reject a legitimately shorter old password.
            }
        }
    }