using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using SoftUmar_Virtuals.Application.Interfaces;
using SoftUmar_Virtuals.Infrastructure.Security;
using SoftUmar_Virtuals.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace SoftUmar_Virtuals.Infrastructure.DependencyInjection
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services,
        IConfiguration configuration)
        {
            var connStr = configuration.GetConnectionString("VirtualNumber");
            services.AddDbContextFactory<ApplicationDbContextFactory>(options =>
                options.UseSqlServer(connStr, sqlOpt =>
                    sqlOpt.EnableRetryOnFailure())
            );

            services.AddSingleton<IConnectionMultiplexer>(sp =>
            {
                var config = sp.GetRequiredService<IConfiguration>();

                var redisConnection = config["RedisConnect:Redis"]
                    ?? throw new Exception("Redis connection string is missing");

                return ConnectionMultiplexer.Connect(redisConnection);
            });


            services.AddScoped<IJwtService, JwtService>();

            return services;
        }
    }
}
