using Microsoft.EntityFrameworkCore;
using Throughline.Modules.Inventory.Application.Models;
using Throughline.Modules.Inventory.Infrastructure.Orders;

namespace Throughline.Modules.Inventory.Application.Queries;

internal sealed class GetOrderQuery
{
    private readonly OrderAllocationDbContext _dbContext;

    public GetOrderQuery(OrderAllocationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<OrderAllocationModel?> GetOrderByIdAsync(Guid orderId, CancellationToken token)
    {
        var rows = await _dbContext.Database
            .SqlQuery<OrderLineRow>(
                $"""
                 SELECT o.order_id           AS "OrderId",
                        o.owner_id           AS "OwnerId",
                        o.allocation_status  AS "OrderStatus",
                        s.code               AS "SkuCode",
                        l.quantity_requested AS "QuantityRequested",
                        l.quantity_allocated AS "QuantityAllocated",
                        l.allocation_status  AS "LineStatus"
                 FROM inventory.order_allocations o
                 JOIN inventory.orderline_allocations l ON l.order_id = o.order_id
                 JOIN inventory.skus s ON s.sku_id = l.sku_id
                 WHERE o.order_id = {orderId}
                 """)
            .ToListAsync(token);

        if (rows.Count == 0)
            return null;

        var header = rows[0];

        var lines = rows
            .Select(r =>
                new OrderLineAllocationModel(r.SkuCode, r.QuantityRequested, r.QuantityAllocated, r.LineStatus))
            .ToList()
            .AsReadOnly();

        return new OrderAllocationModel(header.OwnerId, header.OrderId, header.OrderStatus, lines);
    }

    // Flat projection of one order line joined to its SKU; column aliases match these names.
    private sealed record OrderLineRow(
        Guid OrderId,
        int OwnerId,
        string OrderStatus,
        string SkuCode,
        int QuantityRequested,
        int QuantityAllocated,
        string LineStatus);
}