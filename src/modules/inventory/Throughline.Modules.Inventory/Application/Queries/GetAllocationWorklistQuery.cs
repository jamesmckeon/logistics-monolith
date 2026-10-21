using Throughline.Common.Results;
using Throughline.Modules.Inventory.Application.Models;
using Throughline.Modules.Inventory.Domain.Allocation;
using Throughline.Modules.Inventory.Domain.Inventory;

namespace Throughline.Modules.Inventory.Application.Queries;

internal sealed class GetAllocationWorklistQuery
{
    public const int DefaultBatchSize = 25;
    public const int MaxBatchSize = 500;

    private readonly IInventoryRepository _inventoryRepository;
    private readonly IOrderAllocationRepository _orderAllocationRepository;

    public GetAllocationWorklistQuery(
        IOrderAllocationRepository orderAllocationRepository,
        IInventoryRepository inventoryRepository)
    {
        _orderAllocationRepository = orderAllocationRepository;
        _inventoryRepository = inventoryRepository;
    }

    /// <summary>
    ///     Returns one of the owner's allocation worklists: up to <paramref name="batchSize" /> of its orders on
    ///     <paramref name="worklist" />, as returned by <see cref="IOrderAllocationRepository.GetWorklistAsync" />,
    ///     in that order. Each order includes all of its lines, with each line's SKU code and quantity still to
    ///     allocate.
    /// </summary>
    /// <param name="worklist">Unreleased or backordered orders</param>
    /// <param name="batchSize">
    ///     Maximum number of orders to return, from 1 to <see cref="MaxBatchSize" />;
    ///     <see cref="DefaultBatchSize" /> when null
    /// </param>
    /// <returns>
    ///     The worklist's orders, earliest accepted first, or none when the list is empty; or a
    ///     validation failure, without reading any orders, when <paramref name="batchSize" /> is outside 1 to
    ///     <see cref="MaxBatchSize" />
    /// </returns>
    public async Task<Result<IReadOnlyCollection<OrderAllocationModel>>> GetWorklistAsync(
        int ownerId, AllocationWorklist worklist, int? batchSize, CancellationToken token)
    {
        var size = batchSize ?? DefaultBatchSize;

        if (size is < 1 or > MaxBatchSize)
            return Result<IReadOnlyCollection<OrderAllocationModel>>.Validation($"batchSize must be from 1 to {MaxBatchSize}");

        var orders = await _orderAllocationRepository.GetWorklistAsync(ownerId, worklist, size, token);

        // one SKU lookup for the whole batch
        var skus = await _inventoryRepository.GetSkusByIdAsync(
            orders.SelectMany(o => o.OrderLines).Select(l => l.SkuId).Distinct(), token);
        var skuCodes = skus.ToDictionary(s => s.Id.Value, s => s.Code);

        var models = orders
            .Select(o => new OrderAllocationModel(
                o.OwnerId,
                o.Id,
                o.OrderCreatedOn.Value,
                o.AllocationStatus.ToString(),
                o.OrderLines
                    .Select(l => new OrderLineAllocationModel(
                        skuCodes[l.SkuId.Value],
                        l.QuantityRequested,
                        l.QuantityAllocated,
                        l.QuantityShort,
                        l.AllocationStatus.ToString()))
                    .ToList()
                    .AsReadOnly()))
            .ToList()
            .AsReadOnly();

        return models;
    }
}
