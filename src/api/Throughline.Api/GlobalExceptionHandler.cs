using Microsoft.AspNetCore.Diagnostics;

namespace Throughline.Api;

internal sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context, Exception exception, CancellationToken ct)
    {
        logger.LogError(exception, "Unhandled exception for {Method} {Path}",
            context.Request.Method, context.Request.Path);

        await Results.Problem(statusCode: StatusCodes.Status500InternalServerError)
            .ExecuteAsync(context);
        return true;
    }
}