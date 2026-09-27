using Microsoft.AspNetCore.Http;

namespace RadiumServer.Structs;

public static class EpicError
{
    public static async Task WriteErrorAsync(
        HttpContext context,
        string errorCode,
        string errorMessage,
        object? messageVars = null,
        int numericErrorCode = 1004,
        string? error = null,
        int statusCode = 404)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;
        context.Response.Headers["X-Epic-Error-Name"] = errorCode;
        context.Response.Headers["X-Epic-Error-Code"] = numericErrorCode.ToString();

        var payload = new
        {
            errorCode,
            errorMessage,
            messageVars,
            numericErrorCode,
            originatingService = "any",
            intent = "prod",
            error_description = errorMessage,
            error
        };

        await context.Response.WriteAsJsonAsync(payload);
    }
}
