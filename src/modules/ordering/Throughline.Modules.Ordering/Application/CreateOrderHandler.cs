using Throughline.Common.Results;
using Throughline.Modules.Billing.Domain.Models;
using Throughline.Modules.Ordering.Application.Models;
using Throughline.Modules.Ordering.Domain;
using Throughline.Modules.Ordering.Domain.Orders;

namespace Throughline.Modules.Ordering.Application;

public sealed class CreateOrderHandler : ICreateOrderHandler
{
    private readonly CreateOrderCommandValidator _commandValidator;
    private readonly IOrdersRepository _ordersRepository;

    public CreateOrderHandler(
        IOrdersRepository ordersRepository,
        CreateOrderCommandValidator commandValidator)
    {
        _ordersRepository = ordersRepository;
        _commandValidator = commandValidator;
    }

    public async Task<Result<OrderModel>> CreateOrderAsync(
        CreateOrderCommand command, CancellationToken cancellationToken = default)
    {
        var validationResult = _commandValidator.ValidateCommand(command);

        if (!validationResult.Succeeded)
            return Validation(validationResult.Errors);

        var orderExists = await _ordersRepository.OrderExistsFor(
            command.MerchantId, command.ReferenceNumber, cancellationToken);

        if (orderExists)
            return
                Result<OrderModel>.Conflict(
                    $"An order exists for merchant #{command.MerchantId} with reference #{command.ReferenceNumber}");

        var postalCode = new PostalCode(command.PostalCode);
        var destination = new StreetAddress(
            command.StreetAddressOne,
            command.StreetAddressTwo,
            command.City,
            command.State,
            postalCode);

        var order = new Order(
            new OrderId(),
            command.MerchantId,
            command.PurchaseOrderNumber,
            command.ReferenceNumber,
            destination,
            command.Items.Select(i => new OrderLine(new SkuCode(i.Sku), i.Quantity)));

        await _ordersRepository.SaveOrderAsync(order, cancellationToken);

        return OrderModel.FromOrder(order);
    }

    private static Result<OrderModel> Validation(IEnumerable<Error> errors)
    {
        return Result<OrderModel>.Validation(errors);
    }
}