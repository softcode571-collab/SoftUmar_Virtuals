using System;
using System.Collections.Generic;
using System.Text;

namespace SoftUmar_Virtuals.Application.Features.Auth.RegisterUser.Command
{
    public class RegisterUserDTO
    {
        public required string FirstName { get; set; }

        public required string LastName { get; set; }

        public string? MiddleName { get; set; }

        public required string Email { get; set; }

        public required string Password { get; set; }

        public required string ConfirmPassword { get; set; }

        public required string PhoneNumber { get; set; }

        public required string Country { get; set; }

        public required string ReferralCode { get; set; }
    }
}
