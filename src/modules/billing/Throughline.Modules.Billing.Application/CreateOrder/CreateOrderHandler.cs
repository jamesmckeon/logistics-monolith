using Throughline.Modules.Billing.Application.Orders.Models;
using Throughline.Modules.Billing.Domain.Models;
using Throughline.Modules.Billing.Domain.OrderEstimates;
using Throughline.Modules.Billing.Domain.Orders;

namespace Throughline.Modules.Billing.Application.CreateOrder;

public sealed class CreateOrderHandler : ICreateOrderHandler
{
    private readonly IOrderEstimateService _estimateService;
    private readonly IOrdersRepository _ordersRepository;
    private readonly IOrderEstimateRequestBuilder _requestBuilder;

    public CreateOrderHandler(
        IOrderEstimateRequestBuilder requestBuilder,
        IOrderEstimateService estimateService,
        IOrdersRepository ordersRepository)
    {
        _requestBuilder = requestBuilder;
        _estimateService = estimateService;
        _ordersRepository = ordersRepository;
    }

    public async Task<Result<OrderModel>> CreateOrderAsync(
        CreateOrderCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var orderExists = await _ordersRepository.OrderExistsFor(
            command.MerchantId, command.ReferenceNumber, cancellationToken);

        if (orderExists)
            return
                Result<OrderModel>.Conflict(
                    $"An order exists for merchant #{command.MerchantId} with reference #{command.ReferenceNumber}");


        var destination = CreateAddress(command);

        if (!destination.Succeeded)
            return Validation(destination.Errors);

        var requestResult = await _requestBuilder.CreateRequestAsync(command, cancellationToken);

        if (!requestResult.Succeeded)
            return Validation(requestResult.Errors);

        var estimate = _estimateService.GetEstimate(requestResult.Value);

        var order = Order.FromOrderEstimate(
            new OrderId(),
            estimate,
            command.MerchantId,
            command.PurchaseOrderNumber,
            command.ReferenceNumber,
            destination.Value);

        await _ordersRepository.SaveOrderAsync(order, cancellationToken);

        return OrderModel.FromOrder(order);
    }

    private static Result<StreetAddress> CreateAddress(CreateOrderCommand command)
    {
        var stateResult = AddressState.Create(command.State);
        var zipResult = PostalCode.Create(command.PostalCode);

        if (!stateResult.Succeeded || !zipResult.Succeeded)
            return Result<StreetAddress>.Validation(stateResult.Errors.AsEnumerable().Concat(
                zipResult.Errors));


        return StreetAddress.Create(
            command.StreetAddressOne,
            command.StreetAddressTwo,
            command.City,
            stateResult.Value,
            zipResult.Value);
    }

    private static Result<OrderModel> Validation(IEnumerable<Error> errors)
    {
        return Result<OrderModel>.Validation(errors);
    }
}