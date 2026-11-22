using Microsoft.AspNetCore.Diagnostics;

namespace Throughline.Api;

internal sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context, Exception exception, CancellationToken ct)
    {
        // A request ASP.NET Core couldn't read, such as a query value that isn't a number, is the caller's
        // error: it keeps its own status code (400) instead of becoming a 500
        if (exception is BadHttpRequestException badRequest)
        {
            logger.LogWarning(exception, "Bad request for {Method} {Path}",
                context.Request.Method, context.Request.Path);

            await Results.Problem(statusCode: badRequest.StatusCode, detail: badRequest.Message)
                .ExecuteAsync(context);
            return true;
        }

        logger.LogError(exception, "Unhandled exception for {Method} {Path}",
            context.Request.Method, context.Request.Path);

        await Results.Problem(statusCode: StatusCodes.Status500InternalServerError)
            .ExecuteAsync(context);
        return true;
    }
}
