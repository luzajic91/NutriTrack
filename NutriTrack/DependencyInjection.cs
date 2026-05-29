using System.Data;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NutriTrack.Shared.Auth;
using NutriTrack.Shared.Persistence;

namespace NutriTrack.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddCore(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<NutriTrackDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("Default")));

        var jwtSecret = configuration["Jwt:Secret"]
            ?? throw new InvalidOperationException("Jwt:Secret is not configured.");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration["Jwt:Issuer"],
                    ValidAudience = configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtSecret))
                };
            });

        services.AddHttpContextAccessor();
        services.AddAuthorization();

        services.AddScoped<IDbConnection>(_ =>
            new SqlConnection(configuration.GetConnectionString("Default")));

        services.AddScoped<JwtTokenService>();
        services.AddScoped<CurrentUserService>();
        services.AddScoped<NutritionQueryService>();

        // validators
        services.AddScoped<RegisterValidator>();
        services.AddScoped<LoginValidator>();
        services.AddScoped<RefreshTokenValidator>();
        services.AddScoped<RevokeTokenValidator>();
        services.AddScoped<LogMealValidator>();
        services.AddScoped<CreateRecipeValidator>();
        services.AddScoped<UpdateUserPreferencesValidator>();

        // feature services
        services.AddScoped<AuthService>();
        services.AddScoped<FoodCatalogService>();
        services.AddScoped<MealLoggingService>();
        services.AddScoped<RecipeService>();
        services.AddScoped<UserPreferencesService>();

        return services;
    }
}
