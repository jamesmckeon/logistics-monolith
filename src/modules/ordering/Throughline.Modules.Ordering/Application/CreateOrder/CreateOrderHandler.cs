using Microsoft.Extensions.Logging;
using Throughline.Common.Results;
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

    public async Task<Result<CreateOrderResult>> CreateOrderAsync(
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

        var contentResult = OrderContent.Create(
            command.PurchaseOrderNumber,
            addressResult.Value,
            command.Items.Select(i => new OrderLine(new SkuCode(i.Sku), i.Quantity)));

        if (!contentResult.Succeeded)
            return RejectInvalid(contentResult.Errors, command, ownerId);

        var ownerReference = new OwnerReferenceNumber(ownerId, command.ReferenceNumber);
        var existingOrder = await _ordersRepository.GetOrderByOwnerReference(
            ownerReference,
            cancellationToken);

        if (existingOrder != null)
        {
            _logger.LogInformation("Order found for ref #{@RefNumber}, owner id {@OwnerId}",
                command.ReferenceNumber, ownerId);

            if (!existingOrder.Content.Equals(contentResult.Value))
            {
                _logger.LogInformation("Content of existing order id {@OrderId} differs from request",
                    existingOrder.Id);
                return Result<CreateOrderResult>.Conflict(
                    $"Reference #{command.ReferenceNumber} already identifies an order with different contents.");
            }

            return new CreateOrderResult(false, existingOrder.Id.Value, ownerId, command.ReferenceNumber);
        }

        var order = new Order(new OrderId(), ownerReference, contentResult.Value);

        _logger.LogInformation(
            "Order #{@OrderNumber} created for owner id {@OwnerId}, PO #{@PoNumber}, ref #{@RefNumber}",
            order.Id, ownerId, command.PurchaseOrderNumber, command.ReferenceNumber);

        var newOrderId = await _ordersRepository.SaveOrderAsync(order, cancellationToken);

        return new CreateOrderResult(true, newOrderId, ownerId, command.ReferenceNumber);
    }

    private Result<CreateOrderResult> RejectInvalid(IEnumerable<Error> errors, CreateOrderCommand command, int ownerId)
    {
        _logger.LogInformation("Create order request for owner id {@OwnerId}, PO # {@PoNumber}, " +
                               "ref #{@refNumber} rejected as invalid: {@errors}",
            ownerId, command.PurchaseOrderNumber, command.ReferenceNumber, errors);
        return Result<CreateOrderResult>.Validation(errors);
    }
}