using Microsoft.EntityFrameworkCore;
using Throughline.Common.Models;
using Throughline.Common.Results;
using Throughline.Modules.Inventory.Domain.Allocation;
using Throughline.Modules.Inventory.Domain.Inventory;
using Throughline.Modules.Inventory.Infrastructure.Common;

namespace Throughline.Modules.Inventory.Application.AllocateOrders;

internal sealed class OrderAllocationService
{
    private readonly IInventoryRespository _inventoryRespository;
    private readonly IOrderAllocationRepository _orderAllocationRepository;
    private readonly IOrderlineAllocationService _orderlineAllocationService;
    private readonly IUnitOfWork _unitOfWork;

    public OrderAllocationService(
        IOrderAllocationRepository repository,
        IUnitOfWork unitOfWork,
        IOrderlineAllocationService orderlineAllocationService,
        IInventoryRespository inventoryRepository)
    {
        _orderAllocationRepository = repository;
        _unitOfWork = unitOfWork;
        _orderlineAllocationService = orderlineAllocationService;
        _inventoryRespository = inventoryRepository;
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
            var receipts = await _inventoryRespository.GetAvailableInventoryBySkuAsync(line.Sku, token);
            var lineResult = _orderlineAllocationService.AllocateOrderLine(line, receipts);

            if (lineResult.AllocatedQuantityChanged)
            {
                order.UpdateLine(lineResult.OrderLine.Sku, lineResult.QuantityAllocated, AppDateTime.Now);

                // TODO record inventoryallocation 

                await _unitOfWork.SaveChangesAsync();
            }
        }


        throw new NotImplementedException();
    }
}