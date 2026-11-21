using Throughline.Common.Results;
using Throughline.Modules.Ordering.Application.CreateOrder;

namespace Throughline.Modules.Ordering.Tests.Application.CreateOrder;

[Category("Unit")]
public sealed class CreateOrderCommandTests
{
    private static readonly CreateOrderCommandItem[] OneItem = [new("HF-10442", 6)];

    private static CreateOrderCommand Given(
        string purchaseOrderNumber = "PO-48213",
        string referenceNumber = "REF-0001931",
        string streetAddressOne = "1 Main St",
        string? streetAddressTwo = "Suite 200",
        string city = "Boston",
        string state = "MA",
        string postalCode = "02110",
        IEnumerable<CreateOrderCommandItem>? items = null)
    {
        return new CreateOrderCommand(
            purchaseOrderNumber,
            referenceNumber,
            streetAddressOne,
            streetAddressTwo,
            city,
            state,
            postalCode,
            items ?? OneItem);
    }

    private static void AssertRequiredError(Result result, string fieldName)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.ErrorType, Is.EqualTo(ErrorType.Validation));
            Assert.That(result.Errors, Is.EqualTo(new[] { FieldError.IsRequired(fieldName) }));
        });
    }

    #region Validate

    [Test]
    public void Validate_AllFieldsPresent_Succeeds()
    {
        var sut = Given();

        var result = sut.Validate();

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.ErrorType, Is.Null);
            Assert.That(result.Errors, Is.Empty);
        });
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Validate_StreetAddressTwoMissing_Succeeds(string? streetAddressTwo)
    {
        var sut = Given(streetAddressTwo: streetAddressTwo);

        var result = sut.Validate();

        Assert.That(result.Succeeded, Is.True);
    }

    [TestCase("")]
    [TestCase("   ")]
    public void Validate_PurchaseOrderNumberMissing_ReturnsRequiredError(string purchaseOrderNumber)
    {
        var sut = Given(purchaseOrderNumber: purchaseOrderNumber);

        var result = sut.Validate();

        AssertRequiredError(result, "PurchaseOrderNumber");
    }

    [TestCase("")]
    [TestCase("   ")]
    public void Validate_ReferenceNumberMissing_ReturnsRequiredError(string referenceNumber)
    {
        var sut = Given(referenceNumber: referenceNumber);

        var result = sut.Validate();

        AssertRequiredError(result, "ReferenceNumber");
    }

    [TestCase("")]
    [TestCase("   ")]
    public void Validate_StreetAddressOneMissing_ReturnsRequiredError(string streetAddressOne)
    {
        var sut = Given(streetAddressOne: streetAddressOne);

        var result = sut.Validate();

        AssertRequiredError(result, "StreetAddressOne");
    }

    [TestCase("")]
    [TestCase("   ")]
    public void Validate_CityMissing_ReturnsRequiredError(string city)
    {
        var sut = Given(city: city);

        var result = sut.Validate();

        AssertRequiredError(result, "City");
    }

    [TestCase("")]
    [TestCase("   ")]
    public void Validate_StateMissing_ReturnsRequiredError(string state)
    {
        var sut = Given(state: state);

        var result = sut.Validate();

        AssertRequiredError(result, "State");
    }

    [TestCase("")]
    [TestCase("   ")]
    public void Validate_PostalCodeMissing_ReturnsRequiredError(string postalCode)
    {
        var sut = Given(postalCode: postalCode);

        var result = sut.Validate();

        AssertRequiredError(result, "PostalCode");
    }

    [Test]
    public void Validate_ItemsEmpty_ReturnsRequiredError()
    {
        var sut = Given(items: []);

        var result = sut.Validate();

        AssertRequiredError(result, "Items");
    }

    [Test]
    public void Validate_EveryRequiredFieldMissing_ReturnsEveryRequiredError()
    {
        var sut = Given("", "", "", null, "", "", "", []);

        var result = sut.Validate();

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.ErrorType, Is.EqualTo(ErrorType.Validation));
            Assert.That(result.Errors, Is.EquivalentTo(new[]
            {
                FieldError.IsRequired("PurchaseOrderNumber"),
                FieldError.IsRequired("ReferenceNumber"),
                FieldError.IsRequired("StreetAddressOne"),
                FieldError.IsRequired("City"),
                FieldError.IsRequired("State"),
                FieldError.IsRequired("PostalCode"),
                FieldError.IsRequired("Items")
            }));
        });
    }

    #endregion
}
