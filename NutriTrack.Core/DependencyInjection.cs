namespace NutriTrack.Core;

public static class DependencyInjection
{
    public static IServiceCollection AddCore(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<NutriTrackDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("Default")));

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
                        Encoding.UTF8.GetBytes(configuration["Jwt:Secret"]!))
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
        services.AddScoped<Features.Identity.RegisterValidator>();
        services.AddScoped<Features.Identity.LoginValidator>();
        services.AddScoped<Features.Identity.RefreshTokenValidator>();
        services.AddScoped<Features.Identity.RevokeTokenValidator>();
        services.AddScoped<Features.MealLogging.LogMealValidator>();
        services.AddScoped<Features.Recipes.CreateRecipeValidator>();

        // feature services
        services.AddScoped<Features.Identity.AuthService>();
        services.AddScoped<Features.FoodCatalog.FoodCatalogService>();
        services.AddScoped<Features.MealLogging.MealLoggingService>();
        services.AddScoped<Features.Recipes.RecipeService>();

        return services;
    }
}