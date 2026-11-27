using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using TripService.Infrastructure.Authentication;
using TripService.Infrastructure.Options;

namespace TripService.Api.Extensions;

public static class JwtExtensions
{
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtSection = configuration.GetSection(
            JwtOptions.SectionName);

        services.Configure<JwtOptions>(jwtSection);

        services.AddScoped<JwtTokenService>();

        var jwtSettings = jwtSection.Get<JwtOptions>()
            ?? throw new InvalidOperationException(
                "Не настроена секция Jwt.");

        if (string.IsNullOrWhiteSpace(jwtSettings.Issuer) ||
            string.IsNullOrWhiteSpace(jwtSettings.Audience) ||
            jwtSettings.ExpirationMinutes <= 0)
        {
            throw new InvalidOperationException(
                "Некорректные настройки JWT.");
        }

        var keyBytes = Convert.FromBase64String(
            jwtSettings.SigningKeyBase64);

        if (keyBytes.Length < 32)
        {
            throw new InvalidOperationException(
                "Ключ JWT должен содержать минимум 32 байта.");
        }

        services
            .AddAuthentication(
                JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;

                options.TokenValidationParameters =
                    new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = jwtSettings.Issuer,

                        ValidateAudience = true,
                        ValidAudience = jwtSettings.Audience,

                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.Zero,

                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey =
                            new SymmetricSecurityKey(keyBytes)
                    };
            });

        services.AddAuthorization();

        return services;
    }
}