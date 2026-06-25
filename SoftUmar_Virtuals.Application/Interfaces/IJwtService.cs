using SoftUmar_Virtuals.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace SoftUmar_Virtuals.Application.Interfaces
{
    public interface IJwtService
    {
        //string GenerateAccessToken(User user);
        Task<string> GenerateAccessToken(User user);

        string GenerateRefreshToken();
    }
}
