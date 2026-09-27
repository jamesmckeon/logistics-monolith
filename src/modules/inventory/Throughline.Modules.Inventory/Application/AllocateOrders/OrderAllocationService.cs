using Microsoft.EntityFrameworkCore;
using Throughline.Common.Models;
using Throughline.Modules.Inventory.Application.Models;
using Throughline.Modules.Inventory.Domain.Allocation;
using Throughline.Modules.Inventory.Domain.Inventory;
using Throughline.Modules.Inventory.Infrastructure.Common;

namespace Throughline.Modules.Inventory.Application.AllocateOrders;

/// <summary>
///     Allocates a confirmed order's outstanding demand against available inventory, subject to the
///     owner's allocation-completeness policy
/// </summary>
internal sealed class OrderAllocationService : IOrderAllocationService
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


    public async Task<AllocatedOrder> AllocateOrderAsync(
        OrderAllocation order,
        AllocationPolicies policy,
        CancellationToken token)
    {
        if (order.Allocating)
            return AllocatedOrder.Failed(
                order.Id, AllocationError.OrderAllocating(order.Id));

        order.StartAllocating(AppDateTime.Now);

        try
        {
            await _unitOfWork.SaveChangesAsync(token);
        }
        catch (DbUpdateConcurrencyException)
        {
            return AllocatedOrder.Failed(
                order.Id, AllocationError.OrderAllocating(order.Id));
        }

        var transaction = await _unitOfWork.BeginTransactionAsync(token);

        var receipts = await _inventoryRespository.GetAvailableInventoryAsync(
            order.UnallocatedLines.Select(ul => ul.SkuId), token);

        var canSatisfyPolicy = _allocationService.CanSatisfyPolicyWithCurrentReceipts(
            order, receipts, policy);

        if (!canSatisfyPolicy) return await ConstructResponseAsync(order, receipts, token);

        foreach (var line in order.UnallocatedLines)
            _allocationService.AllocateOrderLine(order.Id, line, receipts);

        order.StopAllocating(AppDateTime.Now);

        await _unitOfWork.SaveChangesAsync(token);
        await transaction.CommitAsync(token);

        return await ConstructResponseAsync(order, receipts, token);
    }

    private async Task<AllocatedOrder> ConstructResponseAsync(
        OrderAllocation order, IEnumerable<SkuReceipt> receipts, CancellationToken token)
    {
        var shortedSkus = _allocationService.GetShortedSkus(order, receipts)
            .ToArray();
        var newStatus = _allocationService.DeriveStatusFromShortages(order, shortedSkus);
        var allSkus = await _inventoryRespository.GetSkusByIdAsync(
            shortedSkus.Select(s => s.SkuId), token);

        return ToAllocatedOrder(
            order.Id,
            newStatus,
            shortedSkus.Select(sh => new SkuCodeShortage(
                allSkus.Single(s => s.Id == sh.SkuId).Code, sh.QuantityRequested, sh.QuantityAllocated,
                sh.QuantityOutstanding)));
    }

    private static AllocatedOrder ToAllocatedOrder(
        Guid orderId, AllocationStatuses statuses, IEnumerable<SkuCodeShortage> shortages)
    {
        if (statuses == AllocationStatuses.Allocated)
            return AllocatedOrder.FullyAllocated(orderId);

        if (statuses == AllocationStatuses.PartiallyAllocated)
            return AllocatedOrder.PartiallyAllocated(orderId, shortages);

        return AllocatedOrder.AllShort(orderId, shortages);
    }
}