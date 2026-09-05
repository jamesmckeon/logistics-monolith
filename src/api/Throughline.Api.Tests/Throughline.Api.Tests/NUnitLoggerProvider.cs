using Microsoft.Extensions.Logging;

namespace Throughline.Api.Tests;

// Routes the in-memory host's logs to NUnit's output so failures (e.g. an unhandled 500)
// are visible in the test runner. Test infrastructure only — not app observability.
internal sealed class NUnitLoggerProvider : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName) => new NUnitLogger(categoryName);

    public void Dispose()
    {
    }

    private sealed class NUnitLogger(string category) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter)
        {
            // TestContext.Progress writes immediately (unbuffered), so output survives a hang/crash.
            TestContext.Progress.WriteLine($"[{logLevel}] {category}: {formatter(state, exception)}");

            if (exception is not null)
                TestContext.Progress.WriteLine(exception);
        }
    }
}
