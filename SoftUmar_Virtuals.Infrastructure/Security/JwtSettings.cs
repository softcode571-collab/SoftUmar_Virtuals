using System;
using System.Collections.Generic;
using System.Text;

namespace SoftUmar_Virtuals.Infrastructure.Security
{
    public class JwtSettings
    {
        public required string Key { get; set; }

        public required string Issuer { get; set; }

        public required string Audience { get; set; }

        public int AccessTokenExpiryMinutes { get; set; }

        public int RefreshTokenExpiryDays { get; set; }
    }
}
