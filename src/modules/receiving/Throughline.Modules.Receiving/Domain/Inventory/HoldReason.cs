using Throughline.Common.Models;
using Throughline.Modules.Receiving.Domain.Common;

namespace Throughline.Modules.Receiving.Domain.Inventory;

internal sealed class HoldReason : ValueObject
{
    private HoldReason(UpperCaseString reasonCode, bool isActive)
    {
        ReasonCode = reasonCode;
        IsActive = isActive;
    }

    public UpperCaseString ReasonCode { get; }
    public bool IsActive { get; }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return ReasonCode;
    }

    public override string ToString() => ReasonCode.Value;
}