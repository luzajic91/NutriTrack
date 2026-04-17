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

        // register Dispatcher
        services.AddScoped<Dispatcher>();

        // scan assembly and register all handlers
        var assembly = typeof(DependencyInjection).Assembly;

        var handlerTypes = assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType &&
                            i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>))
                .Select(i => new { Implementation = t, Interface = i }));

        foreach (var handler in handlerTypes)
            services.AddScoped(handler.Interface, handler.Implementation);

        // register behaviors — order matters, first registered = outermost wrapper
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        // register validators
        var validatorTypes = assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType &&
                            i.GetGenericTypeDefinition() == typeof(IValidator<>))
                .Select(i => new { Implementation = t, Interface = i }));

        foreach (var validator in validatorTypes)
            services.AddScoped(validator.Interface, validator.Implementation);

        return services;
    }
}