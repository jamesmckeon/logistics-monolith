using Microsoft.EntityFrameworkCore;
using Throughline.Common.Models;
using Throughline.Common.Results;
using Throughline.Modules.Inventory.Domain.Allocation;
using Throughline.Modules.Inventory.Domain.Common;
using Throughline.Modules.Inventory.Domain.Inventory;
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

    public async Task<Result<OrderAllocation>> AllocateOrderAsync(OrderAllocation order, CancellationToken token)
    {
        if (order.Allocating)
            return Result<OrderAllocation>.Conflict(
                $"Order id {order.Id} is currently allocating");

        // if order isn't allocatable there's nothing to do
        if (!order.IsAllocatable)
            return order;

        order.SetAllocating(AppDateTime.Now);

        try
        {
            await _unitOfWork.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            return Result<OrderAllocation>.Conflict(
                $"Order id {order.Id} is currently allocating");
        }

        var unallocatedLines = order.OrderLines.Where(w => w.IsAllocatable);

        foreach (var line in unallocatedLines)
        {
            var receipts = await _inventoryRespository.GetAvailableInventoryBySkuIdAsync(line.SkuId, token);
            _orderlineAllocationService.AllocateOrderLine(order.Id, line, receipts);

            // if the owner doesn't allow for partial allocations and this allocation run produced
            // at least one partially allocated line, it shouldn't be committed

            if (!_allocationSpec.IsSatisfiedBy(order))
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