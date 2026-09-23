using Microsoft.EntityFrameworkCore;
using Throughline.Common.Models;
using Throughline.Modules.Inventory.Application.Models;
using Throughline.Modules.Inventory.Domain.Allocation;
using Throughline.Modules.Inventory.Domain.Common;
using Throughline.Modules.Inventory.Domain.Inventory;
using Throughline.Modules.Inventory.Domain.Skus;
using Throughline.Modules.Inventory.Infrastructure.Common;

namespace Throughline.Modules.Inventory.Application.AllocateOrders;

internal sealed class OrderAllocationService
{
    private readonly ISpecification<OrderAllocation> _allocationSpec;
    private readonly IInventoryRepository _inventoryRespository;
    private readonly ILogger<OrderAllocationService> _logger;
    private readonly IOrderAllocationRepository _orderAllocationRepository;
    private readonly IOrderlineAllocationService _orderlineAllocationService;
    private readonly IUnitOfWork _unitOfWork;

    public OrderAllocationService(
        IOrderAllocationRepository repository,
        IUnitOfWork unitOfWork,
        IOrderlineAllocationService orderlineAllocationService,
        IInventoryRepository inventoryRepository,
        ISpecification<OrderAllocation> allocationSpec,
        ILogger<OrderAllocationService> logger)
    {
        _orderAllocationRepository = repository;
        _unitOfWork = unitOfWork;
        _orderlineAllocationService = orderlineAllocationService;
        _inventoryRespository = inventoryRepository;
        _allocationSpec = allocationSpec;
        _logger = logger;
    }

    public async Task<AllocatedOrder> AllocateOrderAsync(OrderAllocation order, CancellationToken token)
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
            _orderlineAllocationService.AllocateOrderLine(order.Id, line, receipts);

        // capture result before changes are rolled back due to
        // owner's allocation policy
        var result = ToAllocatedOrder(order, skus);

        if (!_allocationSpec.IsSatisfiedBy(order))
            await transaction.RollbackAsync(token);
        else
            await transaction.CommitAsync(token);

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
                allSkus.Single(sk => sk.Id == s.Id).Code,
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