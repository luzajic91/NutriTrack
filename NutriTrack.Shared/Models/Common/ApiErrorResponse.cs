using System.Text.Json.Serialization;

namespace NutriTrack.Shared.Models.Common;

/// <summary>
/// The single error body shape every non-success response uses, whatever produced it:
/// a failed <see cref="NutriTrack.Shared.Common.Result"/>, an unhandled exception, or a
/// JWT bearer challenge. Shared with the Blazor client so both ends agree on the contract.
/// </summary>
/// <param name="Error">Human-readable text, safe to display. Do not match on it.</param>
/// <param name="Code">Stable machine-readable identifier. Match on this.</param>
/// <param name="CorrelationId">Ties the response to the server logs for this request.</param>
public sealed record ApiErrorResponse(
    [property: JsonPropertyName("error")] string Error,
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("correlationId")] string? CorrelationId);
