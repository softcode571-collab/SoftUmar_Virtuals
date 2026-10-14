using Microsoft.AspNetCore.Http;
using SoftUmar_Virtuals.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace SoftUmar_Virtuals.Infrastructure.Security
{
    public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
    {
        private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

        public Guid? UserId
        {
            get
            {
                var value = User?.FindFirstValue(ClaimTypes.NameIdentifier);
                return Guid.TryParse(value, out var id) ? id : null;
            }
        }

        public string? Email => User?.FindFirstValue(ClaimTypes.Email);
        public string? Role => User?.FindFirstValue(ClaimTypes.Role);
        public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;
    }
}
