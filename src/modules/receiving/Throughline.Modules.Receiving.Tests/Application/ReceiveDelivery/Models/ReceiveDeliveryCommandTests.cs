using Throughline.Common.Results;
using Throughline.Modules.Receiving.Application.ReceiveDelivery.Models;
using Throughline.Modules.Receiving.Domain.Inventory;

namespace Throughline.Modules.Receiving.Tests.Application.ReceiveDelivery.Models;

[Category("Unit")]
public sealed class ReceiveDeliveryCommandTests
{
    // A pallet with nothing wrong with it; tests vary one thing at a time with `with`
    private static readonly SubmittedPallet ValidPallet = new("LPN-1", "SKU-1", 10, "DOCK-01", null, null, null);

    // A command with nothing wrong with it; tests vary one thing at a time with `with`
    private static readonly ReceiveDeliveryCommand ValidCommand = new(
        Guid.CreateVersion7(), Guid.CreateVersion7(), "DEL-1", "BOL-1", "ABCD", "TRL-1", "CNT-1", "Acme Shipping",
        [ValidPallet]);

    private static void AssertValidationErrors(Result result, params FieldError[] expected)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.ErrorType, Is.EqualTo(ErrorType.Validation));
            Assert.That(result.Errors, Is.EqualTo(expected));
        });
    }

    #region Validate

    [Test]
    public void Validate_ValidCommand_Succeeds()
    {
        var result = ValidCommand.Validate();

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Errors, Is.Empty);
        });
    }

    [Test]
    public void Validate_EmptyReceiptId_ReturnsRequiredError()
    {
        var command = ValidCommand with { ReceiptId = Guid.Empty };

        var result = command.Validate();

        AssertValidationErrors(result, new FieldError("ReceiptId is required", "ReceiptId"));
    }

    [Test]
    public void Validate_EmptyOperatorId_ReturnsRequiredError()
    {
        var command = ValidCommand with { OperatorId = Guid.Empty };

        var result = command.Validate();

        AssertValidationErrors(result, new FieldError("OperatorId is required", "OperatorId"));
    }

    [TestCase("")]
    [TestCase("   ")]
    public void Validate_BlankDeliveryReference_ReturnsRequiredError(string deliveryReference)
    {
        var command = ValidCommand with { DeliveryReference = deliveryReference };

        var result = command.Validate();

        AssertValidationErrors(result, new FieldError("DeliveryReference is required", "DeliveryReference"));
    }

    [TestCase("TRL-1", "")]
    [TestCase("", "CNT-1")]
    public void Validate_OnlyOneOfTrailerOrContainer_Succeeds(string trailerNumber, string containerNumber)
    {
        var command = ValidCommand with { TrailerNumber = trailerNumber, ContainerNumber = containerNumber };

        var result = command.Validate();

        Assert.That(result.Succeeded, Is.True);
    }

    [TestCase("", "")]
    [TestCase("   ", "   ")]
    public void Validate_TrailerAndContainerBlank_ReturnsError(string trailerNumber, string containerNumber)
    {
        var command = ValidCommand with { TrailerNumber = trailerNumber, ContainerNumber = containerNumber };

        var result = command.Validate();

        AssertValidationErrors(result,
            new FieldError("Either ContainerNumber or TrailerNumber is required", "TrailerNumber"),
            new FieldError("Either ContainerNumber or TrailerNumber is required", "ContainerNumber"));
    }

    [Test]
    public void Validate_LowercaseScac_Succeeds()
    {
        var command = ValidCommand with { CarrierScac = "abcd" };

        var result = command.Validate();

        Assert.That(result.Succeeded, Is.True);
    }

    [Test]
    public void Validate_PaddedScac_Succeeds()
    {
        var command = ValidCommand with { CarrierScac = " ABCD " };

        var result = command.Validate();

        Assert.That(result.Succeeded, Is.True);
    }

    [TestCase("ABC")]
    [TestCase("ABCDE")]
    [TestCase("AB1D")]
    public void Validate_ScacNotFourLetters_ReturnsCarrierScacError(string carrierScac)
    {
        var command = ValidCommand with { CarrierScac = carrierScac };

        var result = command.Validate();

        AssertValidationErrors(result, new FieldError("value must be 4 alpha characters", "CarrierScac"));
    }

    [Test]
    public void Validate_NoPallets_ReturnsError()
    {
        var command = ValidCommand with { Pallets = [] };

        var result = command.Validate();

        AssertValidationErrors(result, new FieldError("Pallets must contain at least one item", "Pallets"));
    }

    [TestCase("LPN-1", "LPN-1")]
    [TestCase("LPN-1", "lpn-1")]
    [TestCase("LPN-1", " LPN-1 ")]
    public void Validate_DuplicateLpns_ReturnsError(string firstLpn, string secondLpn)
    {
        var command = ValidCommand with
        {
            Pallets = [ValidPallet with { Lpn = firstLpn }, ValidPallet with { Lpn = secondLpn }]
        };

        var result = command.Validate();

        AssertValidationErrors(result, new FieldError("Pallets must contain unique LPNs", "Pallets"));
    }

    [TestCase("")]
    [TestCase("   ")]
    public void Validate_PalletWithBlankSku_ReturnsError(string sku)
    {
        var command = ValidCommand with { Pallets = [ValidPallet, ValidPallet with { Lpn = "LPN-2", Sku = sku }] };

        var result = command.Validate();

        AssertValidationErrors(result, new FieldError("Each pallet must have a non-blank sku", "Pallets"));
    }

    [TestCase("")]
    [TestCase("   ")]
    public void Validate_PalletWithBlankLpn_ReturnsError(string lpn)
    {
        var command = ValidCommand with { Pallets = [ValidPallet, ValidPallet with { Lpn = lpn }] };

        var result = command.Validate();

        AssertValidationErrors(result, new FieldError("Each pallet must have a non-blank lpn", "Pallets"));
    }

    [Test]
    public void Validate_SeveralPalletsWithBlankLpn_ReturnsOnlyBlankLpnError()
    {
        var command = ValidCommand with { Pallets = [ValidPallet with { Lpn = "" }, ValidPallet with { Lpn = "   " }] };

        var result = command.Validate();

        AssertValidationErrors(result, new FieldError("Each pallet must have a non-blank lpn", "Pallets"));
    }

    [TestCase("")]
    [TestCase("   ")]
    public void Validate_PalletWithBlankLocationId_ReturnsError(string locationId)
    {
        var command = ValidCommand with
        {
            Pallets = [ValidPallet, ValidPallet with { Lpn = "LPN-2", LocationId = locationId }]
        };

        var result = command.Validate();

        AssertValidationErrors(result, new FieldError("Each pallet must have a non-blank location ID", "Pallets"));
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void Validate_PalletWithQuantityNotPositive_ReturnsError(int quantity)
    {
        var command = ValidCommand with
        {
            Pallets = [ValidPallet, ValidPallet with { Lpn = "LPN-2", Quantity = quantity }]
        };

        var result = command.Validate();

        AssertValidationErrors(result,
            new FieldError("Each pallet must have quantity greater than zero", "Pallets"));
    }

    [Test]
    public void Validate_SeveralInvalidFields_ReturnsEveryError()
    {
        var command = ValidCommand with
        {
            DeliveryReference = "",
            TrailerNumber = "",
            ContainerNumber = "",
            CarrierScac = "AB1D",
            Pallets = [ValidPallet with { Sku = "", Quantity = 0 }]
        };

        var result = command.Validate();

        AssertValidationErrors(result,
            new FieldError("DeliveryReference is required", "DeliveryReference"),
            new FieldError("Either ContainerNumber or TrailerNumber is required", "TrailerNumber"),
            new FieldError("Either ContainerNumber or TrailerNumber is required", "ContainerNumber"),
            new FieldError("value must be 4 alpha characters", "CarrierScac"),
            new FieldError("Each pallet must have a non-blank sku", "Pallets"),
            new FieldError("Each pallet must have quantity greater than zero", "Pallets"));
    }

    #endregion
}