using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using HrDemo.Application.Common.Interfaces;
using HrDemo.Application.Abstractions.DateTime;
using HrDemo.Application.Abstractions.Identity;
using HrDemo.Application.Abstractions.Events;
using HrDemo.Application.Abstractions.Authentication;
using HrDemo.Infrastructure.Identity;
using HrDemo.Infrastructure.Persistence;
using HrDemo.Infrastructure.Persistence.Interceptors;
using HrDemo.Infrastructure.Services;

namespace HrDemo.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDataProtection();
        services.AddHttpContextAccessor();

        // Register Clocks, Identity, Events, JWT
        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        services.AddScoped<IUserManager, UserManagerService>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

        // Register Interceptor
        services.AddScoped<AuditableAndDomainEventsInterceptor>();

        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            var interceptor = sp.GetRequiredService<AuditableAndDomainEventsInterceptor>();
            
            if (!string.IsNullOrEmpty(connectionString))
            {
                options.UseSqlServer(connectionString);
            }

            options.AddInterceptors(interceptor);
        });

        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

        // Register Identity Core
        services.AddIdentityCore<ApplicationUser>(options =>
        {
            // Lockout options
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;
        })
        .AddRoles<ApplicationRole>()
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        // Add RoleManager support
        services.AddScoped<RoleManager<ApplicationRole>>();

        // Bind and register JWT configurations
        var jwtSection = configuration.GetSection(JwtOptions.SectionName);
        services.Configure<JwtOptions>(jwtSection);
        var jwtOptions = new JwtOptions();
        jwtSection.Bind(jwtOptions);

        var key = Encoding.UTF8.GetBytes(jwtOptions.SigningKey);

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtOptions.Issuer,
                ValidAudience = jwtOptions.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ClockSkew = TimeSpan.Zero
            };
        });

        return services;
    }
}
