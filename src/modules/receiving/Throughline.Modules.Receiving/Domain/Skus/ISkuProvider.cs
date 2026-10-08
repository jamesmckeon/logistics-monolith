using Throughline.Modules.Receiving.Domain.Common;

namespace Throughline.Modules.Receiving.Domain.Skus;

internal interface ISkuProvider
{
    Task<IReadOnlyCollection<OwnerSku>> GetSkusByOwnerSkuCodeAsync(
        int ownerId, IEnumerable<UpperCaseString> skuCodes, CancellationToken token);
}