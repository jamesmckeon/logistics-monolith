namespace Throughline.Common.Results;

public class Error : IEquatable<Error>
{
    protected Error(ErrorType errorType, string description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        ErrorType = errorType;
        Description = description;
    }

    public ErrorType ErrorType { get; }
    public string? Description { get; }

    public bool Equals(Error? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return ErrorType == other.ErrorType && Description == other.Description;
    }

    public static Error Validation(string description)
    {
        return new Error(ErrorType.Validation, description);
    }

    public static Error Unavailable(string description)
    {
        return new Error(ErrorType.Unavailable, description);
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as Error);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine((int)ErrorType, Description);
    }

    public static bool operator ==(Error? left, Error? right)
    {
        if (left is null && right is null)
            return true;

        if (left is null || right is null)
            return false;

        return left.Equals(right);
    }

    public static bool operator !=(Error? left, Error? right)
    {
        return !(left == right);
    }
}