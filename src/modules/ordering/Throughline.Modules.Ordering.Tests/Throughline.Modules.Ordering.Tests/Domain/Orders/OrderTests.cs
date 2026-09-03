using Throughline.Common.Results;
using Throughline.Modules.Ordering.Domain;
using Throughline.Modules.Ordering.Domain.Orders;

namespace Throughline.Modules.Ordering.Tests.Domain.Orders;

[Category("Unit")]
public sealed class OrderTests
{
    private const int OwnerId = 42;
    private static readonly OrderId Id = new();

    private static OrderLine Line(string sku, int quantity = 1)
    {
        return new OrderLine(new SkuCode(sku), quantity);
    }

    private static StreetAddress Destination()
    {
        return new StreetAddress("1 Main St", "Apt 2", "Boston", "MA", new PostalCode("05001"));
    }

    private static Result<Order> WhenCreated(
        int ownerId = OwnerId,
        string purchaseOrderNumber = "PO-1001",
        string referenceNumber = "REF-1001",
        StreetAddress? destination = null,
        IEnumerable<OrderLine>? orderLines = null)
    {
        return Order.Create(
            Id,
            ownerId,
            purchaseOrderNumber,
            referenceNumber,
            destination ?? Destination(),
            orderLines ?? [Line("SKU-1"), Line("SKU-2")]);
    }

    #region Create

    [Test]
    public void Create_ValidInputs_ReturnsSuccessWithPopulatedOrder()
    {
        var destination = Destination();
        var lines = new[] { Line("SKU-1"), Line("SKU-2") };

        var result = WhenCreated(destination: destination, orderLines: lines);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Value!.Id, Is.EqualTo(Id));
            Assert.That(result.Value.OwnerId, Is.EqualTo(OwnerId));
            Assert.That(result.Value.Destination, Is.EqualTo(destination));
            Assert.That(result.Value.OrderLines, Is.EqualTo(lines));
        });
    }

    [Test]
    public void Create_PurchaseOrderAndReferenceNumbersHaveWhitespace_TrimsThem()
    {
        var result = WhenCreated(
            purchaseOrderNumber: "  PO-1001  ",
            referenceNumber: "  REF-1001  ");

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Value!.PurchaseOrderNumber, Is.EqualTo("PO-1001"));
            Assert.That(result.Value.ReferenceNumber, Is.EqualTo("REF-1001"));
        });
    }

    [Test]
    public void Create_DuplicateSkuCodes_ReturnsValidationFailure()
    {
        var result = WhenCreated(orderLines: [Line("SKU-1"), Line("SKU-1")]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.ErrorType, Is.EqualTo(ErrorType.Validation));
            Assert.That(
                result.Errors.Single().Description,
                Is.EqualTo("An order cannot have more than one line with the same sku code"));
        });
    }

    [Test]
    public void Create_NoOrderLines_ReturnsValidationFailure()
    {
        var result = WhenCreated(orderLines: []);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.ErrorType, Is.EqualTo(ErrorType.Validation));
            Assert.That(
                result.Errors.Single().Description,
                Is.EqualTo("An order must have at least one line"));
        });
    }

    [Test]
    public void Create_MultipleDistinctSkuCodes_ReturnsSuccess()
    {
        var result = WhenCreated(orderLines: [Line("SKU-1"), Line("SKU-2"), Line("SKU-3")]);

        Assert.That(result.Succeeded, Is.True);
    }

    [Test]
    public void Create_NullOrderId_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => Order.Create(
            null!, OwnerId, "PO-1001", "REF-1001", Destination(), [Line("SKU-1")]));
    }

    [Test]
    public void Create_NullDestination_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => Order.Create(
            Id, OwnerId, "PO-1001", "REF-1001", null!, [Line("SKU-1")]));
    }

    [Test]
    public void Create_NullOrderLines_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => Order.Create(
            Id, OwnerId, "PO-1001", "REF-1001", Destination(), null!));
    }

    [Test]
    public void Create_NullPurchaseOrderNumber_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => Order.Create(
            Id, OwnerId, null!, "REF-1001", Destination(), [Line("SKU-1")]));
    }

    [Test]
    public void Create_NullReferenceNumber_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => Order.Create(
            Id, OwnerId, "PO-1001", null!, Destination(), [Line("SKU-1")]));
    }

    #endregion
}