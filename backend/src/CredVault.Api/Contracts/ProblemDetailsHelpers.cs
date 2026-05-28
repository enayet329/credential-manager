using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace CredVault.Api.Contracts;

/// <summary>Constants and helpers for RFC 7807 ProblemDetails responses.</summary>
public static class ProblemDetailsHelpers
{
    /// <summary>Extension key used to signal MFA step-up is required.</summary>
    public const string CodeExtension = "code";

    /// <summary>Code value used by sensitive endpoints when MFA step-up is missing.</summary>
    public const string StepUpRequiredCode = "step_up_required";

    /// <summary>Builds a <see cref="ValidationProblemDetails"/> response from per-field errors.</summary>
    public static ValidationProblem ValidationFailed(IDictionary<string, string[]> errors) =>
        TypedResults.ValidationProblem(
            errors,
            title: "One or more validation errors occurred.",
            type: "https://tools.ietf.org/html/rfc7807");

    /// <summary>Builds a 403 ProblemDetails with the <c>step_up_required</c> extension code.</summary>
    public static ProblemHttpResult StepUpRequired() => TypedResults.Problem(
        title: "Multi-factor step-up required.",
        statusCode: StatusCodes.Status403Forbidden,
        detail: "Submit a valid MFA code to POST /auth/step-up and retry with the returned X-Step-Up header.",
        extensions: new Dictionary<string, object?> { [CodeExtension] = StepUpRequiredCode });

    /// <summary>Builds a 409 ProblemDetails for conflict conditions.</summary>
    public static ProblemHttpResult Conflict(string detail) => TypedResults.Problem(
        title: "Conflict",
        statusCode: StatusCodes.Status409Conflict,
        detail: detail);

    /// <summary>Builds a 404 ProblemDetails.</summary>
    public static ProblemHttpResult NotFound(string detail) => TypedResults.Problem(
        title: "Not found",
        statusCode: StatusCodes.Status404NotFound,
        detail: detail);

    /// <summary>Builds a 400 ProblemDetails for general bad-request scenarios that aren't field-level.</summary>
    public static ProblemHttpResult BadRequest(string detail) => TypedResults.Problem(
        title: "Bad request",
        statusCode: StatusCodes.Status400BadRequest,
        detail: detail);

    /// <summary>Builds a 403 ProblemDetails with the supplied extension code.</summary>
    public static ProblemHttpResult Forbidden(string detail, string? code = null)
    {
        var extensions = code is null
            ? null
            : new Dictionary<string, object?> { [CodeExtension] = code };
        return TypedResults.Problem(
            title: "Forbidden",
            statusCode: StatusCodes.Status403Forbidden,
            detail: detail,
            extensions: extensions);
    }
}
