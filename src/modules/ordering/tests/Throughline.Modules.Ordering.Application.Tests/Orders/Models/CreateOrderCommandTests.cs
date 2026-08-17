using Throughline.Common.Results;
using Throughline.Modules.Ordering.Application.Orders.Models;

namespace Throughline.Modules.Ordering.Application.Tests.Orders.Models;

[Category("Unit")]
public sealed class CreateOrderCommandTests
{
    private static Result<CreateOrderCommand> Create(
        int merchantId = 42,
        string purchaseOrderNumber = " PO-1 ",
        string streetAddressOne = " 1 Main St ",
        string streetAddressTwo = " Apt 2",
        string city = " Springfield ",
        string state = " IL ",
        string postalCode = " 10000 ",
        IEnumerable<(string Sku, int Quantity)>? items = null,
        string referenceNumber = " REF-1 ")
    {
        return CreateOrderCommand.Create(
            merchantId,
            purchaseOrderNumber,
            streetAddressOne,
            streetAddressTwo,
            city,
            state,
            postalCode,
            items ?? [("SKU-1", 2)],
            referenceNumber);
    }

    private static void AssertSingleValidationError(Result<CreateOrderCommand> result, string expectedDescription)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Value, Is.Null);
            Assert.That(result.Errors.Single().Description, Is.EqualTo(expectedDescription));
            Assert.That(result.Errors.Single().ErrorType, Is.EqualTo(ErrorType.Validation));
        });
    }

    #region Create

    [Test]
    public void Create_ValidInput_SucceedsWithSuppliedValues()
    {
        var result = Create();

        Assert.That(result.Succeeded, Is.True);
        var command = result.Value!;
        Assert.Multiple(() =>
        {
            Assert.That(command.MerchantId, Is.EqualTo(42));
            Assert.That(command.PurchaseOrderNumber, Is.EqualTo("PO-1"));
            Assert.That(command.ReferenceNumber, Is.EqualTo("REF-1"));
            Assert.That(command.StreetAddressOne, Is.EqualTo("1 Main St"));
            Assert.That(command.StreetAddressTwo, Is.EqualTo("Apt 2"));
            Assert.That(command.City, Is.EqualTo("Springfield"));
            Assert.That(command.State, Is.EqualTo("IL"));
            Assert.That(command.PostalCode, Is.EqualTo("10000"));
            Assert.That(command.Items.Single().Sku, Is.EqualTo("SKU-1"));
            Assert.That(command.Items.Single().Quantity, Is.EqualTo(2));
        });
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Create_MissingPurchaseOrderNumber_FailsValidation(string? purchaseOrderNumber)
    {
        var result = Create(purchaseOrderNumber: purchaseOrderNumber!);

        AssertSingleValidationError(result, "purchaseOrderNumber is required");
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Create_MissingReferenceNumber_FailsValidation(string? referenceNumber)
    {
        var result = Create(referenceNumber: referenceNumber!);

        AssertSingleValidationError(result, "referenceNumber is required");
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Create_MissingCity_FailsValidation(string? city)
    {
        var result = Create(city: city!);

        AssertSingleValidationError(result, "city is required");
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Create_MissingState_FailsValidation(string? state)
    {
        var result = Create(state: state!);

        AssertSingleValidationError(result, "state is required");
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Create_MissingPostalCode_FailsValidation(string? postalCode)
    {
        var result = Create(postalCode: postalCode!);

        AssertSingleValidationError(result, "postalCode is required");
    }


    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Create_ItemWithBlankSku_FailsValidation(string? sku)
    {
        var result = Create(items: [(sku!, 1)]);

        AssertSingleValidationError(result, "each item must have a sku");
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void Create_ItemWithNonPositiveQuantity_FailsValidation(int quantity)
    {
        var result = Create(items: [("SKU-1", quantity)]);

        AssertSingleValidationError(result, "each item must have a quantity greater than 0");
    }

    [Test]
    public void Create_MultipleInvalidFields_AccumulatesAllErrors()
    {
        var result = Create(purchaseOrderNumber: "", state: "", items: [("SKU-1", 0)]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Value, Is.Null);
            Assert.That(result.Errors.Select(e => e.Description), Is.EquivalentTo(new[]
            {
                "purchaseOrderNumber is required",
                "state is required",
                "each item must have a quantity greater than 0"
            }));
            Assert.That(result.Errors.Select(e => e.ErrorType), Is.All.EqualTo(ErrorType.Validation));
        });
    }

    [Test]
    public void Create_EmptyItems_FailsValidation()
    {
        var result = Create(items: Array.Empty<(string, int)>());

        AssertSingleValidationError(result, "items must contain at least one item");
    }

    [Test]
    public void Create_NullItems_FailsValidation()
    {
        var result = CreateOrderCommand.Create(
            42, "PO-1", "1 Main St", "Apt 2", "Springfield", "IL", "10000", null!, "REF-1");

        AssertSingleValidationError(result, "items is required");
    }

    #endregion
}