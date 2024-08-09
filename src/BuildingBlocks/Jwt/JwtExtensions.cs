namespace BuildingBlocks.Jwt;

using BuildingBlocks.Web;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Duende.IdentityServer.EntityFramework.Entities;
using Microsoft.IdentityModel.Tokens;

public static class JwtExtensions
{
    public static IServiceCollection AddJwt(this IServiceCollection services)
    {
        // Retrieve JWT options from configuration
        var jwtOptions = services.GetOptions<JwtBearerOptions>("Jwt");

        // Configure Authentication with JWT Bearer
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddCookie(cfg => cfg.SlidingExpiration = true)
        .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            options.Authority = jwtOptions.Authority;
            options.RequireHttpsMetadata = jwtOptions.RequireHttpsMetadata;
            options.MetadataAddress = jwtOptions.MetadataAddress;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = false,
                ClockSkew = TimeSpan.FromSeconds(5) // Reduce clock skew to 5 seconds
            };
        });

        // Configure Authorization policies if Audience is specified
        if (!string.IsNullOrEmpty(jwtOptions.Audience))
        {
            services.AddAuthorization(options =>
            {
                options.AddPolicy(nameof(ApiScope), policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.RequireClaim("scope", jwtOptions.Audience);
                });
            });
        }

        return services;
    }
}
