using NutriTrack.Api;
using NutriTrack.Api.Cors;
using Microsoft.AspNetCore.OpenApi;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog reads its sinks/levels from the "Serilog" config section. The file sink
// is configured in appsettings.json; a Console sink is layered in for Development.
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

builder.Services.AddControllers();

// FluentValidation in the feature services is the server-side authority; the *Request
// models carry DataAnnotations only for the Blazor client. Suppress the automatic
// ModelState 400 so validation failures flow through ExceptionHandlingMiddleware
// and keep the { "error": ... } response shape the web client parses.
builder.Services.Configure<ApiBehaviorOptions>(o => o.SuppressModelStateInvalidFilter = true);

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info.Title = "NutriTrack API";
        document.Info.Version = "v1";
        document.Info.Description = "Nutrition tracking REST API";
        return Task.CompletedTask;
    });
});

builder.Services.AddNutriTrackCors(builder.Configuration);

builder.Services.AddCore(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.MapScalarApiReference(options =>
    {
        options
            .WithTitle("NutriTrack API")
            .WithTheme(ScalarTheme.Purple)
            .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });
}

app.UseCors();

// Correlation id must run first so every downstream log line carries it.
app.UseMiddleware<CorrelationIdMiddleware>();

// One structured summary line per request (method, path, status, elapsed, user).
app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        if (httpContext.Items.TryGetValue(CorrelationIdMiddleware.HeaderName, out var correlationId))
            diagnosticContext.Set("CorrelationId", correlationId);

        var userId = httpContext.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userId))
            diagnosticContext.Set("UserId", userId);
    };
});

app.UseMiddleware<ExceptionHandlingMiddleware>();

// Ahead of authentication: every rate-limited endpoint is anonymous, so abusive traffic is
// rejected without paying for JWT validation. Sits after the correlation-id and request-log
// middleware so a 429 is logged and carries X-Correlation-ID like any other response.
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

try
{
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "NutriTrack API terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}