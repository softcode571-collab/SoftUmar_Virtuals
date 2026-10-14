using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoftUmar_Virtuals.Application.Features.Auth.UserLogOut
{
    public class LogoutRequestDTO
    {
        public required string RefreshToken { get; set; }
    }
}
