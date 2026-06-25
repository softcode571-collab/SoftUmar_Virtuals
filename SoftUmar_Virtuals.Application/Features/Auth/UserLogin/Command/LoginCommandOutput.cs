using System;
using System.Collections.Generic;
using System.Text;

namespace SoftUmar_Virtuals.Application.Features.Auth.UserLogin.Command
{
    public class LoginCommandOutput
    {
        public Guid UserId { get; set; }

        public string FullName { get; set; }

        public string Email { get; set; }

        public string AccessToken { get; set; }

        public string RefreshToken { get; set; }

        public DateTime RefreshTokenExpiry { get; set; }
    }
}
