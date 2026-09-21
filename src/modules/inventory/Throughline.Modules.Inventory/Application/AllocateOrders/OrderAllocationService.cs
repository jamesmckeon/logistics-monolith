using Microsoft.EntityFrameworkCore;
using Throughline.Common.Models;
using Throughline.Common.Results;
using Throughline.Modules.Inventory.Domain.Allocation;
using Throughline.Modules.Inventory.Domain.Inventory;
using Throughline.Modules.Inventory.Domain.Owners;
using Throughline.Modules.Inventory.Infrastructure.Common;

namespace Throughline.Modules.Inventory.Application.AllocateOrders;

internal sealed class OrderAllocationService
{
    private readonly IInventoryRepository _inventoryRespository;
    private readonly ILogger<OrderAllocationService> _logger;
    private readonly IOrderAllocationRepository _orderAllocationRepository;
    private readonly IOrderlineAllocationService _orderlineAllocationService;
    private readonly IOwnerProvider _ownerProvider;
    private readonly IUnitOfWork _unitOfWork;

    public OrderAllocationService(
        IOrderAllocationRepository repository,
        IUnitOfWork unitOfWork,
        IOrderlineAllocationService orderlineAllocationService,
        IInventoryRepository inventoryRepository,
        IOwnerProvider ownerProvider,
        ILogger<OrderAllocationService> logger)
    {
        _orderAllocationRepository = repository;
        _unitOfWork = unitOfWork;
        _orderlineAllocationService = orderlineAllocationService;
        _inventoryRespository = inventoryRepository;
        _ownerProvider = ownerProvider;
        _logger = logger;
    }

    public async Task<Result<OrderAllocation>> AllocateOrderAsync(Guid orderId, CancellationToken token)
    {
        var order = await _orderAllocationRepository.GetByOrderId(orderId, token);

        if (order == null)
            return Result<OrderAllocation>.NotFound(
                $"Confirmed order for order id {orderId} not found");

        if (order.Allocating)
            return Result<OrderAllocation>.Conflict(
                $"Order id {orderId} is currently allocating");

        // if order isn't allocatable there's nothing to do
        if (!order.IsAllocatable)
            return order;

        var owner = await _ownerProvider.GetOwnerByIdAsync(order.OwnerId);
        if (owner == null)
            return Result<OrderAllocation>.NotFound(
                $"No owner found for id {order.OwnerId}");

        order.SetAllocating(AppDateTime.Now);

        try
        {
            await _unitOfWork.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            return Result<OrderAllocation>.Conflict(
                $"Order id {orderId} is currently allocating");
        }

        var unallocatedLines = order.OrderLines.Where(w => w.IsAllocatable);

        foreach (var line in unallocatedLines)
        {
            var receipts = await _inventoryRespository.GetAvailableInventoryBySkuIdAsync(line.SkuId, token);
            _orderlineAllocationService.AllocateOrderLine(order.Id, line, receipts);

            // if the owner doesn't allow for partial allocations and this allocation run produced
            // at least one partially allocated line, it shouldn't be committed

            if (line.AllocationStatus != AllocationStatus.Allocated &&
                owner.AllocationPolicy == AllocationPolicies.Partial)
            {
                _logger.LogWarning("Canceling allocation for owner id {OwnerId} because the " +
                                   "owner's configured allocation policy doesnt allow for partial allocation",
                    order.OwnerId);
                throw new NotImplementedException("how to rollback changes to order lines?");
                // then early return
            }

            await _unitOfWork.SaveChangesAsync();
        }

// set allocating to false and update order allocation status
        throw new NotImplementedException();
    }
}