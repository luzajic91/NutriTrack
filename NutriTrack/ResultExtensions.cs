using NutriTrack.Shared.Models.Common;

namespace NutriTrack.Api;

/// <summary>
/// Translates a <see cref="Result"/> / <see cref="Result{T}"/> into an HTTP response.
/// This is the only place that knows how an <see cref="ErrorType"/> maps to a status code —
/// <c>NutriTrack.Shared</c> stays transport-agnostic.
/// </summary>
public static class ResultExtensions
{
    public static IActionResult ToActionResult<T>(this Result<T> result, ControllerBase controller) =>
        result.IsSuccess
            ? controller.Ok(result.Value)
            : controller.ToErrorResult(result.Error!);

    public static IActionResult ToActionResult(this Result result, ControllerBase controller) =>
        result.IsSuccess
            ? controller.NoContent()
            : controller.ToErrorResult(result.Error!);

    /// <summary>
    /// For actions whose success path does more than return the value — writing a cookie, or
    /// responding with a different shape than the service produced. Error mapping stays here so
    /// there is still one place that turns an <see cref="ErrorType"/> into a status code.
    /// </summary>
    public static IActionResult ToActionResult<T>(
        this Result<T> result, ControllerBase controller, Func<T, IActionResult> onSuccess) =>
        result.IsSuccess
            ? onSuccess(result.Value)
            : controller.ToErrorResult(result.Error!);

    private static IActionResult ToErrorResult(this ControllerBase controller, Error error) =>
        controller.StatusCode(
            error.Type.ToStatusCode(),
            new ApiErrorResponse(error.Message, error.Code, controller.HttpContext.GetCorrelationId()));

    /// <summary>
    /// Writes the standard error body directly to the response, for the paths that have no
    /// <see cref="ControllerBase"/> to hand: middleware and JWT bearer events.
    /// </summary>
    public static async Task WriteErrorAsync(
        this HttpResponse response, int statusCode, string message, string code)
    {
        response.ContentType = "application/json";
        response.StatusCode = statusCode;

        var body = System.Text.Json.JsonSerializer.Serialize(
            new ApiErrorResponse(message, code, response.HttpContext.GetCorrelationId()));
        await response.WriteAsync(body);
    }

    public static int ToStatusCode(this ErrorType type) => type switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        _ => StatusCodes.Status500InternalServerError
    };
}
