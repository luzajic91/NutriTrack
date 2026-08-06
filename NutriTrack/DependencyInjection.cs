using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NutriTrack.Shared.Auth;
using NutriTrack.Shared.Email;
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

                // ExceptionHandlingMiddleware is registered before UseAuthentication, so it
                // never sees these. Without this the framework returns an empty body and the
                // client has no code to act on.
                options.Events = new JwtBearerEvents
                {
                    OnChallenge = async ctx =>
                    {
                        ctx.HandleResponse();
                        await ctx.Response.WriteErrorAsync(
                            StatusCodes.Status401Unauthorized,
                            "Your session has expired. Please sign in again.",
                            "auth.token_invalid");
                    },
                    OnForbidden = async ctx =>
                        await ctx.Response.WriteErrorAsync(
                            StatusCodes.Status403Forbidden,
                            "You do not have permission to perform this action.",
                            "auth.forbidden")
                };
            });

        services.AddHttpContextAccessor();
        services.AddAuthorization();

        services.AddScoped<JwtTokenService>();
        services.AddScoped<CurrentUserService>();
        services.AddScoped<NutritionQueryService>();

        // email
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));
        services.AddScoped<IEmailSender, SmtpEmailSender>();

        // validators
        services.AddScoped<RegisterValidator>();
        services.AddScoped<LoginValidator>();
        services.AddScoped<RefreshTokenValidator>();
        services.AddScoped<RevokeTokenValidator>();
        services.AddScoped<ConfirmEmailValidator>();
        services.AddScoped<ResendConfirmationValidator>();
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
