using System.Text;
using FireArtTestTask.Application.Abstractions;
using FireArtTestTask.Infrastructure.Authentication;
using FireArtTestTask.Infrastructure.Configuration;
using FireArtTestTask.Infrastructure.Email;
using FireArtTestTask.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace FireArtTestTask.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Database
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrEmpty(connectionString))
        {
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase("FireArtDev"));
        }
        else
        {
            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(connectionString));
        }

        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        // JWT Settings
        var jwtSection = configuration.GetSection("Jwt");
        services.Configure<JwtSettings>(jwtSection);
        var jwtSettings = jwtSection.Get<JwtSettings>()!;

        // Authentication
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
                ValidIssuer = jwtSettings.Issuer,
                ValidAudience = jwtSettings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret))
            };
        });

        // Services
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IEmailService, EmailService>();

        return services;
    }
}
