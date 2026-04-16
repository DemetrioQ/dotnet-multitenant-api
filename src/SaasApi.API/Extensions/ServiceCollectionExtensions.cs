using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SaasApi.API.Middleware;
using SaasApi.Application.Common.Behaviors;
using SaasApi.Application.Common.Interfaces;
using SaasApi.Domain.Interfaces;
using SaasApi.Infrastructure.Persistence;
using SaasApi.Application.Common.Interfaces;
using SaasApi.Infrastructure.Repositories;
using SaasApi.Infrastructure.Services;
using System.Security.Claims;
using System.Text;

namespace SaasApi.API.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(
            typeof(SaasApi.Application.Common.Behaviors.ValidationBehavior<,>).Assembly));

        services.AddValidatorsFromAssembly(
            typeof(SaasApi.Application.Common.Behaviors.ValidationBehavior<,>).Assembly);

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();

        return services;
    }

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<AppDbContext>(opts =>
            opts.UseSqlServer(config.GetConnectionString("DefaultConnection")));

        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<ICurrentTenantService, CurrentTenantService>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IEmailService, EmailService>();

        return services;
    }

    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration config)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(opts =>
            {
                opts.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = config["Jwt:Issuer"],
                    ValidAudience = config["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(config["Jwt:Secret"]!)),
                    RoleClaimType = ClaimTypes.Role,
                };
            });

        return services;
    }
}
