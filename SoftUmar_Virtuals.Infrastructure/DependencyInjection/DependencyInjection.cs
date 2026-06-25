using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SoftUmar_Virtuals.Application.Interfaces;
using SoftUmar_Virtuals.Domain.Entities;
using SoftUmar_Virtuals.Infrastructure.Persistence.Repositories;
using SoftUmar_Virtuals.Infrastructure.Security;
using SoftUmar_Virtuals.Infrastructure.Services.Logger;
using SoftUmar_Virtuals.Infrastructure.Services.Logger.Slack;
using StackExchange.Redis;
using VirtualNumber.Infrastructure.Persistence.Repositories;

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
            services.AddScoped<IRepository<User>, Repository<User>>();
            services.AddScoped<IRepository<Login>, Repository<Login>>();
            
            services.AddScoped<IRepository<Wallets>, Repository<Wallets>>();
            services.AddScoped<IRepository<AuditLogs>, Repository<AuditLogs>>();
            services.AddScoped<ILoggerService, LoggerService>();
            services.AddScoped<ISlackLogger, SlackLogger>();

            return services;
        }
    }
}
