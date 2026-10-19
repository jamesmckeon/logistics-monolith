using System.Net;
using Moq;
using Throughline.Simulator.Configuration;
using Throughline.Simulator.Orders;

namespace Throughline.Simulator.Tests.Orders;

[Category("Unit")]
public sealed class OrderGeneratorTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 19, 15, 0, 0, TimeSpan.Zero);

    private static readonly SkuCatalog Catalog = new(
        Enumerable.Range(1, 200).Select(n => $"HFO-{n:D6}").ToList(), 0.2, 0.8);

    private static readonly SkuCatalog OtherCatalog = new(
        Enumerable.Range(1, 50).Select(n => $"HFO-NEW-{n:D3}").ToList(), 0.2, 0.8);

    private Mock<TimeProvider> _time = null!;

    [SetUp]
    public void SetUp()
    {
        _time = new Mock<TimeProvider>(MockBehavior.Strict);
    }

    #region Next

    [Test]
    public void Next_NoFaults_ReturnsNewOrderOfDistinctCatalogSkus()
    {
        GivenNow();
        var sut = CreateSut(new FaultRates());

        var actual = sut.Next();

        Assert.Multiple(() =>
        {
            Assert.That(actual.Kind, Is.EqualTo(OrderKind.New));
            Assert.That(actual.ExpectedStatus, Is.EqualTo(HttpStatusCode.Created));
            Assert.That(actual.Request.Items, Is.Not.Empty);
            Assert.That(actual.Request.Items.Select(i => i.Sku), Is.Unique.And.SubsetOf(Catalog.Skus));
            Assert.That(actual.Request.Items.Select(i => i.Quantity), Is.All.GreaterThan(0));
            Assert.That(actual.Request.PurchaseOrderNumber, Is.EqualTo($"PO-{actual.Request.ReferenceNumber}"));
        });
    }

    [Test]
    public void Next_ThousandNewOrders_ReferenceNumbersAreUnique()
    {
        GivenNow();
        var sut = CreateSut(new FaultRates());

        var references = Enumerable.Range(0, 1000).Select(_ => sut.Next().Request.ReferenceNumber);

        Assert.That(references, Is.Unique);
    }

    [Test]
    public void Next_UnknownSkuEveryTime_ExactlyOneSkuIsNotInCatalog()
    {
        GivenNow();
        var sut = CreateSut(new FaultRates { UnknownSku = 1 });

        var actual = sut.Next();

        Assert.Multiple(() =>
        {
            Assert.That(actual.Kind, Is.EqualTo(OrderKind.UnknownSku));
            Assert.That(actual.ExpectedStatus, Is.EqualTo(HttpStatusCode.Created));
            Assert.That(actual.Request.Items.Count(i => !Catalog.Contains(i.Sku)), Is.EqualTo(1));
        });
    }

    [Test]
    public void Next_MissingFieldEveryTime_RequiredFieldBlankOrNoItems()
    {
        GivenNow();
        var sut = CreateSut(new FaultRates { MissingRequiredField = 1 });

        var actual = sut.Next();

        var request = actual.Request;
        var somethingMissing = request.PurchaseOrderNumber == "" || request.StreetAddressOne == "" ||
                               request.City == "" || request.PostalCode == "" || request.Items.Count == 0;
        Assert.Multiple(() =>
        {
            Assert.That(actual.Kind, Is.EqualTo(OrderKind.MissingRequiredField));
            Assert.That(actual.ExpectedStatus, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(somethingMissing, Is.True);
        });
    }

    [Test]
    public void Next_InvalidQuantityEveryTime_ExactlyOneLineQuantityNotPositive()
    {
        GivenNow();
        var sut = CreateSut(new FaultRates { InvalidQuantity = 1 });

        var actual = sut.Next();

        Assert.Multiple(() =>
        {
            Assert.That(actual.Kind, Is.EqualTo(OrderKind.InvalidQuantity));
            Assert.That(actual.ExpectedStatus, Is.EqualTo(HttpStatusCode.Created));
            Assert.That(actual.Request.Items.Count(i => i.Quantity <= 0), Is.EqualTo(1));
        });
    }

    [Test]
    public void Next_ResendEveryTimeButNothingAccepted_ReturnsNewOrder()
    {
        GivenNow();
        var sut = CreateSut(new FaultRates { IdenticalResend = 1 });

        var actual = sut.Next();

        Assert.That(actual.Kind, Is.EqualTo(OrderKind.New));
    }

    [Test]
    public void Next_IdenticalResendAfterAccepted_ReturnsAcceptedRequest()
    {
        GivenNow();
        var sut = CreateSut(new FaultRates { IdenticalResend = 1 });
        var accepted = sut.Next().Request;
        sut.RecordAccepted(accepted);

        var actual = sut.Next();

        Assert.Multiple(() =>
        {
            Assert.That(actual.Kind, Is.EqualTo(OrderKind.IdenticalResend));
            Assert.That(actual.ExpectedStatus, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(actual.Request, Is.SameAs(accepted));
        });
    }

    [Test]
    public void Next_ChangedResendAfterAccepted_SameReferenceWithDifferentItems()
    {
        GivenNow();
        var sut = CreateSut(new FaultRates { ChangedResend = 1 });
        var accepted = sut.Next().Request;
        sut.RecordAccepted(accepted);

        var actual = sut.Next();

        Assert.Multiple(() =>
        {
            Assert.That(actual.Kind, Is.EqualTo(OrderKind.ChangedResend));
            Assert.That(actual.ExpectedStatus, Is.EqualTo(HttpStatusCode.Conflict));
            Assert.That(actual.Request.ReferenceNumber, Is.EqualTo(accepted.ReferenceNumber));
            Assert.That(actual.Request.Items[0].Quantity, Is.EqualTo(accepted.Items[0].Quantity + 1));
        });
    }

    [Test]
    public void Next_QuarterUnknownSkuQuarterMissingField_KindsOccurAtThoseRates()
    {
        GivenNow();
        var sut = CreateSut(new FaultRates { UnknownSku = 0.25, MissingRequiredField = 0.25 });

        var kinds = Enumerable.Range(0, 8000).Select(_ => sut.Next().Kind).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(kinds.Count(k => k == OrderKind.UnknownSku) / 8000.0, Is.EqualTo(0.25).Within(0.02));
            Assert.That(kinds.Count(k => k == OrderKind.MissingRequiredField) / 8000.0, Is.EqualTo(0.25).Within(0.02));
            Assert.That(kinds.Count(k => k == OrderKind.New) / 8000.0, Is.EqualTo(0.5).Within(0.02));
        });
    }

    #endregion

    [Test]
    public void UseCatalog_ReloadedCatalog_NewOrdersUseIt()
    {
        GivenNow();
        var sut = CreateSut(new FaultRates());

        sut.UseCatalog(OtherCatalog);

        Assert.That(sut.Next().Request.Items.Select(i => i.Sku), Is.SubsetOf(OtherCatalog.Skus));
    }

    private void GivenNow()
    {
        _time.Setup(t => t.GetUtcNow()).Returns(Now);
    }

    private OrderGenerator CreateSut(FaultRates faults)
    {
        var profile = new CustomerProfile
        {
            ReferencePrefix = "HFO",
            LinesPerOrder = new SizeProfile { P50 = 2, P95 = 6 },
            QuantityPerLine = new SizeProfile { P50 = 1, P95 = 4 },
            Faults = faults
        };

        return new OrderGenerator(profile, Catalog, new Random(17), _time.Object);
    }
}
