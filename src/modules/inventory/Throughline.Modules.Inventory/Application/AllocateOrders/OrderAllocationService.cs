using Microsoft.EntityFrameworkCore;
using Throughline.Common.Models;
using Throughline.Modules.Inventory.Application.Models;
using Throughline.Modules.Inventory.Domain.Allocation;
using Throughline.Modules.Inventory.Domain.Inventory;
using Throughline.Modules.Inventory.Domain.Skus;
using Throughline.Modules.Inventory.Infrastructure.Common;

namespace Throughline.Modules.Inventory.Application.AllocateOrders;

/// <summary>
///     Allocates a confirmed order's outstanding demand against available inventory, subject to the
///     owner's allocation-completeness policy
/// </summary>
internal sealed class OrderAllocationService
{
    private readonly IAllocationService _allocationService;
    private readonly IInventoryRepository _inventoryRespository;
    private readonly ILogger<OrderAllocationService> _logger;
    private readonly IOrderAllocationRepository _orderAllocationRepository;
    private readonly IUnitOfWork _unitOfWork;

    public OrderAllocationService(
        IOrderAllocationRepository repository,
        IUnitOfWork unitOfWork,
        IAllocationService allocationService,
        IInventoryRepository inventoryRepository,
        ILogger<OrderAllocationService> logger)
    {
        _orderAllocationRepository = repository;
        _unitOfWork = unitOfWork;
        _allocationService = allocationService;
        _inventoryRespository = inventoryRepository;
        _logger = logger;
    }

    /// <summary>
    ///     Allocates only the order's outstanding lines; existing allocations are preserved. New allocations
    ///     are committed only if <paramref name="allocationSpec" /> is satisfied, otherwise none are.
    /// </summary>
    /// <param name="order">The order to allocate.</param>
    /// <param name="allocationSpec">The owner's allocation-completeness policy.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>
    ///     <c>fullyAllocated</c> if nothing is outstanding (no allocation is attempted when the order is already
    ///     fully allocated); <c>partiallyAllocated</c> or <c>notAllocated</c> with a shortage per short line; or
    ///     <c>failed</c> with <c>ORDER_ALLOCATING</c> if another request is already allocating the order.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="order" /> or <paramref name="allocationSpec" /> is null.</exception>
    public async Task<AllocatedOrder> AllocateOrderAsync(
        OrderAllocation order,
        CancellationToken token)
    {
        if (order.Allocating)
            return AllocatedOrder.Failed(
                order.Id, AllocationError.OrderAllocating(order.Id));

        // if order isn't allocatable there's nothing to do
        if (!order.IsAllocatable)
            return AllocatedOrder.FullyAllocated(order.Id);

        order.SetAllocating(AppDateTime.Now);

        try
        {
            await _unitOfWork.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return AllocatedOrder.Failed(
                order.Id, AllocationError.OrderAllocating(order.Id));
        }

        var skus = await _inventoryRespository.GetSkusByIdAsync(
            order.OrderLines.Select(ol => ol.SkuId), token);

        var transaction = await _unitOfWork.BeginTransactionAsync(token);

        var unallocatedLines = order.OrderLines.Where(w => w.IsAllocatable)
            .ToList();
        var receipts =
            await _inventoryRespository.GetAvailableInventoryAsync(unallocatedLines.Select(ul => ul.SkuId), token);

        foreach (var line in unallocatedLines)
            _allocationService.AllocateOrderLine(order.Id, line, receipts);

        // capture result before changes are rolled back due to
        // owner's allocation policy
        var result = ToAllocatedOrder(order, skus);

        throw new NotImplementedException();

        order.StopAllocating(AppDateTime.Now);

        await _unitOfWork.SaveChangesAsync();

        return result;
    }

    private static AllocatedOrder ToAllocatedOrder(OrderAllocation order, IEnumerable<Sku> allSkus)
    {
        if (order.AllocationStatus == AllocationStatus.Allocated)
            return AllocatedOrder.FullyAllocated(order.Id);

        var shortages = order.OrderLines.Where(ol => ol.IsAllocatable)
            .Select(s => new AllocationShortage(
                allSkus.Single(sk => sk.Id == s.SkuId).Code,
                s.QuantityRequested,
                s.QuantityAllocated,
                s.QuantityShort));

        switch (order.AllocationStatus)
        {
            case AllocationStatus.Allocated:
                return AllocatedOrder.FullyAllocated(order.Id);
            case AllocationStatus.PartiallyAllocated:
                return AllocatedOrder.PartiallyAllocated(order.Id, shortages);
            default:
                return AllocatedOrder.AllShort(order.Id, shortages);
        }
    }
}