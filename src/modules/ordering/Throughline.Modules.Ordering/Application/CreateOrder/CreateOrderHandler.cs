using Microsoft.Extensions.Logging;
using Throughline.Common.Results;
using Throughline.Modules.Ordering.Application.Models;
using Throughline.Modules.Ordering.Domain;
using Throughline.Modules.Ordering.Domain.Orders;
using Throughline.Modules.Ordering.Infrastructure.Orders;

namespace Throughline.Modules.Ordering.Application.CreateOrder;

internal sealed class CreateOrderHandler
{
    private readonly ILogger<CreateOrderHandler> _logger;
    private readonly OrdersRepository _ordersRepository;

    public CreateOrderHandler(
        OrdersRepository ordersRepository,
        ILogger<CreateOrderHandler> logger)
    {
        _ordersRepository = ordersRepository;
        _logger = logger;
    }

    public async Task<Result<OrderModel>> CreateOrderAsync(
        int ownerId,
        CreateOrderCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var commandResult = command.Validate();

        if (!commandResult.Succeeded)
            return RejectInvalid(commandResult.Errors, command, ownerId);

        var postalResult = PostalCode.Create(command.PostalCode);

        if (!postalResult.Succeeded)
            return RejectInvalid(postalResult.Errors, command, ownerId);

        var addressResult = StreetAddress.Create(
            command.StreetAddressOne,
            command.StreetAddressTwo,
            command.City,
            command.State,
            postalResult.Value);

        if (!addressResult.Succeeded)
            return RejectInvalid(addressResult.Errors, command, ownerId);

        var orderExists = await _ordersRepository.OrderExistsFor(
            ownerId, command.ReferenceNumber, cancellationToken);

        if (orderExists)
            return
                Result<OrderModel>.Conflict(
                    $"An order exists for owner #{ownerId} with reference #{command.ReferenceNumber}");

        var orderResult = Order.Create(
            new OrderId(),
            ownerId,
            command.PurchaseOrderNumber,
            command.ReferenceNumber,
            addressResult.Value,
            command.Items.Select(i => new OrderLine(new SkuCode(i.Sku), i.Quantity)));

        if (!orderResult.Succeeded)
            return RejectInvalid(orderResult.Errors, command, ownerId);

        await _ordersRepository.SaveOrderAsync(orderResult.Value, cancellationToken);

        return OrderModel.FromOrder(orderResult.Value);
    }

    private Result<OrderModel> RejectInvalid(IEnumerable<Error> errors, CreateOrderCommand command, int ownerId)
    {
        _logger.LogWarning("Create order request for owner id {@OwnerId}, PO # {@PoNumber}, " +
                           "ref #{@refNumber} rejected as invalid: {@errors}",
            ownerId, command.PurchaseOrderNumber, command.ReferenceNumber, errors);
        return Result<OrderModel>.Validation(errors);
    }
}