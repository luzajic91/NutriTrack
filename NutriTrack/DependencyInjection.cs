using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NutriTrack.Api.RateLimiting;
using NutriTrack.Shared.Auth;
using NutriTrack.Shared.Caching;
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

        // The signing key must never live in a tracked file: this repo is public, and anyone
        // holding the key can mint a valid token for any user and role. It comes from user
        // secrets in development and from the environment in deployment (see README).
        var jwtSecret = configuration["Jwt:Secret"];
        if (string.IsNullOrWhiteSpace(jwtSecret))
            throw new InvalidOperationException(
                "Jwt:Secret is not configured; set it in user secrets.");

        // HMAC-SHA256 requires a 256-bit key. Checking here rather than letting
        // JwtTokenService fail on the first signature turns a broken login into a startup error.
        if (Encoding.UTF8.GetByteCount(jwtSecret) < 32)
            throw new InvalidOperationException(
                "Jwt:Secret must be at least 32 bytes long for HMAC-SHA256.");

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

        // email. Validated on start because registration swallows delivery failures by
        // design, so a misconfigured sender is otherwise invisible until someone reads
        // the log. A merge once dropped these sections and nothing complained for weeks.
        services.AddOptions<EmailOptions>()
            .Bind(configuration.GetSection(EmailOptions.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.Host), "Email:Host is not configured.")
            .Validate(o => !string.IsNullOrWhiteSpace(o.FromAddress),
                "Email:FromAddress is not configured; set it in user secrets.")
            .Validate(o => string.IsNullOrEmpty(o.User) || !string.IsNullOrEmpty(o.Password),
                "Email:Password is required whenever Email:User is set; set it in user secrets.")
            .ValidateOnStart();
        services.AddScoped<IEmailSender, SmtpEmailSender>();

        // Confirmation links are built from this, and a relative link is useless in an
        // email, so refuse to start rather than send unclickable mail.
        if (string.IsNullOrWhiteSpace(configuration["App:ClientBaseUrl"]))
            throw new InvalidOperationException("App:ClientBaseUrl is not configured.");

        // rate limiting (applied per-action on AuthController)
        services.AddNutriTrackRateLimiting(configuration);

        // caching. Only seed data that every user shares is cached: the lookup tables and the
        // food catalog. Per-user reads (meal history, summaries) are left uncached so there is
        // no way for one user's data to be served to another, and nothing to invalidate on write.
        services.AddHybridCache();
        services.AddScoped<ReferenceDataCache>();

        // validators
        services.AddScoped<RegisterValidator>();
        services.AddScoped<LoginValidator>();
        services.AddScoped<RefreshTokenValidator>();
        services.AddScoped<RevokeTokenValidator>();
        services.AddScoped<ConfirmEmailValidator>();
        services.AddScoped<ResendConfirmationValidator>();
        services.AddScoped<SearchFoodsValidator>();
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
