using Throughline.Common.Results;
using Throughline.Modules.Ordering.Application.Models;
using Throughline.Modules.Ordering.Domain;
using Throughline.Modules.Ordering.Domain.Orders;
using Throughline.Modules.Ordering.Infrastructure.Orders;

namespace Throughline.Modules.Ordering.Application.CreateOrder;

internal sealed class CreateOrderHandler
{
    private readonly OrdersRepository _ordersRepository;

    public CreateOrderHandler(
        OrdersRepository ordersRepository)
    {
        _ordersRepository = ordersRepository;
    }

    public async Task<Result<OrderModel>> CreateOrderAsync(
        CreateOrderCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var commandResult = command.Validate();

        if (!commandResult.Succeeded)
            return Validation(commandResult.Errors);

        var postalResult = PostalCode.Create(command.PostalCode);

        if (!postalResult.Succeeded)
            return Validation(postalResult.Errors);

        var addressResult = StreetAddress.Create(
            command.StreetAddressOne,
            command.StreetAddressTwo,
            command.City,
            command.State,
            postalResult.Value);

        if (!addressResult.Succeeded)
            return Validation(addressResult.Errors);

        var orderExists = await _ordersRepository.OrderExistsFor(
            command.OwnerId, command.ReferenceNumber, cancellationToken);

        if (orderExists)
            return
                Result<OrderModel>.Conflict(
                    $"An order exists for owner #{command.OwnerId} with reference #{command.ReferenceNumber}");

        var orderResult = Order.Create(
            new OrderId(),
            command.OwnerId,
            command.PurchaseOrderNumber,
            command.ReferenceNumber,
            addressResult.Value,
            command.Items.Select(i => new OrderLine(new SkuCode(i.Sku), i.Quantity)));

        if (!orderResult.Succeeded)
            return Validation(orderResult.Errors);

        await _ordersRepository.SaveOrderAsync(orderResult.Value, cancellationToken);

        return OrderModel.FromOrder(orderResult.Value);
    }

    private static Result<OrderModel> Validation(IEnumerable<Error> errors)
    {
        return Result<OrderModel>.Validation(errors);
    }
}