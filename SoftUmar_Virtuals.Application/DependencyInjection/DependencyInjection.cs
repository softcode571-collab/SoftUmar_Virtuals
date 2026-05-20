using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using SoftUmar_Virtuals.Application.Behaviors;
using SoftUmar_Virtuals.Application.Features.Auth.RegisterUser.Command;
using SoftUmar_Virtuals.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace SoftUmar_Virtuals.Application.DependencyInjection
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(
            this IServiceCollection services)
        {
            services.AddMediatR(typeof(DependencyInjection).Assembly);

           

            services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

           

            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(RequestValidationBehaviour<,>));
            services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
            
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));


            return services;

            
        }
    }
}
