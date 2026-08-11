namespace Throughline.Modules.Ordering.Domain.Models;

public sealed class ZoneSurCharge
{
    public ZoneSurCharge(
        int merchantId,
        PostalCode startCode,
        PostalCode endCode,
        Money surcharge)
    {
        ArgumentNullException.ThrowIfNull(startCode);
        ArgumentNullException.ThrowIfNull(endCode);
        ArgumentNullException.ThrowIfNull(surcharge);

        if (endCode < startCode)
            throw new ArgumentException("startCode must preceded or equal endCode");

        MerchantId = merchantId;
        StartCode = startCode;
        EndCode = endCode;
        Surcharge = surcharge;
    }

    public int MerchantId { get; }
    public PostalCode StartCode { get; }
    public PostalCode EndCode { get; }
    public Money Surcharge { get; }

    public bool Includes(PostalCode postalCode)
    {
        ArgumentNullException.ThrowIfNull(postalCode);

        return postalCode >= StartCode && postalCode <= EndCode;
    }
}