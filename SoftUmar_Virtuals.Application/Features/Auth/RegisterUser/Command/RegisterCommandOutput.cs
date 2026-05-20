using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoftUmar_Virtuals.Application.Features.Auth.RegisterUser.Command
{
    public class RegisterCommandOutput
    {
        public Guid UserId { get; set; }
        public string? Message { get; set; }

    }
}
