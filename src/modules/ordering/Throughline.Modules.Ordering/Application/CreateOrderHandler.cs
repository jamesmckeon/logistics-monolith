using Throughline.Common.Results;
using Throughline.Modules.Billing.Domain.Models;
using Throughline.Modules.Ordering.Application.Models;
using Throughline.Modules.Ordering.Domain;
using Throughline.Modules.Ordering.Domain.Orders;

namespace Throughline.Modules.Ordering.Application;

public sealed class CreateOrderHandler : ICreateOrderHandler
{
    private readonly IOrdersRepository _ordersRepository;

    public CreateOrderHandler(
        IOrdersRepository ordersRepository)
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
            command.MerchantId, command.ReferenceNumber, cancellationToken);

        if (orderExists)
            return
                Result<OrderModel>.Conflict(
                    $"An order exists for merchant #{command.MerchantId} with reference #{command.ReferenceNumber}");

        var orderResult = Order.Create(
            new OrderId(),
            command.MerchantId,
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