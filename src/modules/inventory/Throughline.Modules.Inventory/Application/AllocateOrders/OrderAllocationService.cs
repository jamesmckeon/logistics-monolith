using Microsoft.EntityFrameworkCore;
using Throughline.Common.Models;
using Throughline.Common.Results;
using Throughline.Modules.Inventory.Domain.Allocation;
using Throughline.Modules.Inventory.Infrastructure.Common;

namespace Throughline.Modules.Inventory.Application.AllocateOrders;

internal sealed class OrderAllocationService
{
    private readonly IOrderAllocationRepository _orderAllocationRepository;
    private readonly IUnitOfWork _unitOfWork;

    public OrderAllocationService(IOrderAllocationRepository repository, IUnitOfWork unitOfWork)
    {
        _orderAllocationRepository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<OrderAllocation>> AllocateOrderAsync(Guid orderId, CancellationToken token)
    {
        var order = await _orderAllocationRepository.GetByOrderId(orderId, token);

        if (order == null)
            return Result<OrderAllocation>.NotFound(
                $"Confirmed order for order id {orderId} not found");

        if (order.AllocationStatus == OrderAllocationStatus.Allocating)
            return Result<OrderAllocation>.Conflict(
                $"Order id {orderId} is currently allocating");

        order.SetAllocating(AppDateTime.Now);

        try
        {
            await _unitOfWork.SaveChangesAsync();
            throw new NotImplementedException();
        }

        catch (DbUpdateConcurrencyException ex)
        {
            return Result<OrderAllocation>.Conflict(
                $"Order id {orderId} is currently allocating");
        }


        // if order status is allocating, return failure
        // if order is already alloated, return failure
        // if order is partially or un-alloated, execute
        // allocation logic

        throw new NotImplementedException();
    }
}