namespace NutriTrack.Api;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        var (statusCode, message, code) = ex switch
        {
            NotFoundException e => (StatusCodes.Status404NotFound, e.Message, "error.not_found"),
            ForbiddenException e => (StatusCodes.Status403Forbidden, e.Message, "error.forbidden"),
            FluentValidation.ValidationException e => (StatusCodes.Status400BadRequest,
                string.Join(", ", e.Errors.Select(x => x.ErrorMessage)), "error.validation"),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.",
                "error.unexpected")
        };

        // Expected client errors (4xx) are logged at Warning without a stack trace;
        // only genuinely unexpected failures (5xx) are Errors with the full exception.
        if (statusCode >= StatusCodes.Status500InternalServerError)
            _logger.LogError(ex, "Unhandled exception for {Method} {Path}",
                context.Request.Method, context.Request.Path);
        else
            _logger.LogWarning("{StatusCode} for {Method} {Path}: {Message}",
                statusCode, context.Request.Method, context.Request.Path, message);

        await context.Response.WriteErrorAsync(statusCode, message, code);
    }
}
