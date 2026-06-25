using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoftUmar_Virtuals.Domain.Entities
{
    public class Login
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public string Email { get; set; } = default!;

        public DateTime LoginTime { get; set; }

        public DateTime? LogoutTime { get; set; }

        public bool IsActive { get; set; }
        
        public string RefreshToken { get; set; }

        public DateTime? RefreshTokenExpiryTime { get; set; }
    }
}
