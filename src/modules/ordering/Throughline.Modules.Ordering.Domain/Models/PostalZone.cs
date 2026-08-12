namespace Throughline.Modules.Ordering.Domain.Models;

public sealed class PostalZone
{
    public PostalCode StartCode { get; }
    public PostalCode EndCode { get; }

    public PostalZone(PostalCode startCode, PostalCode endCode)
    {
        ArgumentNullException.ThrowIfNull(startCode);
        ArgumentNullException.ThrowIfNull(endCode);

        StartCode = startCode;
        EndCode = endCode;
    }

    public bool Includes(PostalCode postalCode)
    {
        throw new NotImplementedException();
    }
}