using System.Runtime.CompilerServices;
using Throughline.Common.Models;

namespace Throughline.Modules.Receiving.Common.Exceptions;

internal sealed class FutureDateException : Exception
{
    public FutureDateException(string message, string? paramName) : base(message)
    {
        ParamName = paramName;
    }

    public string? ParamName { get; }

    public static void ThrowIfPastNow(AppDateTime value,
        [CallerArgumentExpression(nameof(value))]
        string? paramName = null)
    {
        if (value > AppDateTime.Now)
        {
            throw new FutureDateException("value must be prior to the current date & time", paramName);
        }
    }
}