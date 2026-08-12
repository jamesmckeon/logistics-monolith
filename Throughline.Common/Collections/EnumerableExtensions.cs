using System.Runtime.CompilerServices;

namespace Throughline.Common.Collections;

public static class EnumerableExtensions
{
    /// <summary>
    ///     Materializes <paramref name="source" /> to an array, guaranteeing it is non-null and non-empty.
    ///     The sequence is enumerated exactly once, so a deferred/<c>yield</c> source is safe to pass.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="source" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException"><paramref name="source" /> contains no elements.</exception>
    public static T[] ToNonEmptyArray<T>(
        this IEnumerable<T>? source,
        [CallerArgumentExpression(nameof(source))] string? paramName = null)
    {
        ArgumentNullException.ThrowIfNull(source, paramName);

        var array = source.ToArray();

        if (array.Length == 0)
            throw new ArgumentException($"'{paramName}' must contain at least one item.", paramName);

        return array;
    }
}
