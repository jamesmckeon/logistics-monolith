using Microsoft.EntityFrameworkCore;
using Throughline.Modules.Ordering.Application.Models;
using Throughline.Modules.Ordering.Infrastructure.Orders;

namespace Throughline.Modules.Ordering.Application.Queries;

internal sealed class GetOrderByIdQuery
{
    private readonly OrdersDbContext _dbContext;

    public GetOrderByIdQuery(OrdersDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<OrderModel?> GetOrderByIdAsync(Guid orderId, int ownerId, CancellationToken token)
    {
        var order = await _dbContext.Orders
            .Where(o => o.OrderId == orderId && o.OwnerId == ownerId) // scope!
            .Select(o => new OrderModel(
                o.OrderId,
                o.OwnerId,
                o.PurchaseOrderNumber,
                o.ReferenceNumber,
                new DestinationModel(o.StreetAddressOne, o.StreetAddressTwo, o.City, o.State, o.Zipcode),
                o.OrderLines.Select(l => new OrderLineModel(l.SkuCode, l.Quantity)).ToList().AsReadOnly()))
            .AsNoTracking()
            .SingleOrDefaultAsync(token);

        return order;
    }
}