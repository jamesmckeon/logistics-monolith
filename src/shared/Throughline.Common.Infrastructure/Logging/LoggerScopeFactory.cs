using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Throughline.Common.Infrastructure.Logging;

public class LoggerScopeFactory
{
    /// <summary>
    ///     Adds OwnerId to the request span and logging scope
    /// </summary>
    public static IDisposable? OwnerScope(ILogger logger, int ownerId)
    {
        // owner attribution on the per-request span the ASP.NET Core instrumentation already emits
        Activity.Current?.SetTag("owner_id", ownerId);

        // owner on every log record in the request
        return logger.BeginScope("Owner {OwnerId}", ownerId);
    }
}