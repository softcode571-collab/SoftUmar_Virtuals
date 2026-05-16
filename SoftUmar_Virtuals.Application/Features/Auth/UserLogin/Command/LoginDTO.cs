using System;
using System.Collections.Generic;
using System.Text;

namespace SoftUmar_Virtuals.Application.Features.Auth.UserLogin.Command
{
    public class LoginDTO
    {
        public required string Email { get; set; }
        public required string Password { get; set; }
    }
}
