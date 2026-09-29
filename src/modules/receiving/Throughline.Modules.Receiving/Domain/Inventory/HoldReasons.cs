namespace Throughline.Modules.Receiving.Domain.Inventory;

internal enum HoldReasons
{
    Damaged,
    Expired,

    /// <summary>
    ///     The SKU is known, but condition, labeling, seals or other observable details make it unsuitable for allocation.
    /// </summary>
    QualityConcern,

    /// <summary>
    ///     The inventory is identifiable, but the client-owner must decide whether it can be
    ///     used, returned or otherwise dispositioned.
    /// </summary>
    OwnerReview
}