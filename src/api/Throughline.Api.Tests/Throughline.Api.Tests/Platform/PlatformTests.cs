using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Throughline.Api.Tests.Common;
using Throughline.Api.Tests.Common.Auth;
using Throughline.Common.Presentation.Auth;
using Throughline.Modules.Ordering.Contracts.Events;
using Throughline.Modules.Ordering.Contracts.Models;
using Throughline.Modules.Platform.Application.Models;
using Throughline.Modules.Platform.Application.Queries;
using Throughline.Modules.Platform.Presentation;
using Wolverine;
using Wolverine.Runtime;
using Wolverine.Runtime.Serialization;

namespace Throughline.Api.Tests.Platform;

[Category("Integration")]
internal sealed class PlatformTests : IntegrationTestsBase
{
    private const string DeadLettersUri = PlatformExtensions.PlatformRoute + "/dead-letters";

    private static readonly DateTimeOffset SentAt = new(2025, 11, 21, 15, 40, 3, TimeSpan.Zero);

    // Platform has no schema of its own; the factory builds Wolverine's before the host starts
    protected override Task MigrateAsync() => Task.CompletedTask;

    [Test]
    public async Task GetDeadLetters_DeadLetterStored_ReturnsItWithItsMessage()
    {
        var deadLetterId = await InsertDeadLetterAsync(OrderConfirmed(2));
        using var request = CreateRequest(DeadLettersUri, ThroughlineRoles.ApplicationSupport);

        var response = await _client.SendAsync(request);
        var list = await response.Content.ReadFromJsonAsync<DeadLetterListModel>();

        var model = list?.DeadLetters.Single(d => d.DeadLetterId == deadLetterId);

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(model?.SentAt, Is.EqualTo(SentAt));
            Assert.That(model?.Module, Is.EqualTo(GetDeadLettersQuery.DefaultModule));
            Assert.That(model?.MessageType, Is.EqualTo(typeof(OrderConfirmedIntegrationEvent).FullName));
            Assert.That(model?.ExceptionType, Is.EqualTo("Throughline.Common.Events.MissingDependencyException"));
            Assert.That(model?.ReplayQueued, Is.False);
            Assert.That(model?.Message?.GetProperty("ownerId").GetInt32(), Is.EqualTo(2));
        });
    }

    [Test]
    public async Task GetDeadLetters_OwnerIdGiven_ReturnsOnlyThatOwnersDeadLetters()
    {
        var ownerTwo = await InsertDeadLetterAsync(OrderConfirmed(2));
        var ownerThree = await InsertDeadLetterAsync(OrderConfirmed(3));
        using var request = CreateRequest($"{DeadLettersUri}?ownerId=2", ThroughlineRoles.ApplicationSupport);

        var response = await _client.SendAsync(request);
        var list = await response.Content.ReadFromJsonAsync<DeadLetterListModel>();

        var ids = list?.DeadLetters.Select(d => d.DeadLetterId).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(ids, Does.Contain(ownerTwo));
            Assert.That(ids, Does.Not.Contain(ownerThree));
        });
    }

    [Test]
    public async Task GetDeadLetters_NoToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync(DeadLettersUri);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task GetDeadLetters_NotInApplicationSupportRole_ReturnsForbidden()
    {
        // Passes the owner check (header and matching owner role), so only the missing ApplicationSupport role
        // can be what forbids it
        using var request = CreateRequest(HttpMethod.Get, DeadLettersUri, 1, ThroughlineRoles.OutboundPlanning,
            ThroughlineRoles.OwnerRole(1));

        var response = await _client.SendAsync(request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task GetDeadLetters_OwnerIdNotANumber_ReturnsProblemDetails()
    {
        using var request = CreateRequest($"{DeadLettersUri}?ownerId=abc", ThroughlineRoles.ApplicationSupport);

        var response = await _client.SendAsync(request);
        var details = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(details?.Status, Is.EqualTo(StatusCodes.Status400BadRequest));
        });
    }

    [Test]
    public async Task GetDeadLetter_DeadLetterStored_ReturnsIt()
    {
        var deadLetterId = await InsertDeadLetterAsync(OrderConfirmed(2));
        using var request = CreateRequest($"{DeadLettersUri}/{deadLetterId}", ThroughlineRoles.ApplicationSupport);

        var response = await _client.SendAsync(request);
        var model = await response.Content.ReadFromJsonAsync<DeadLetterModel>();

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(model?.DeadLetterId, Is.EqualTo(deadLetterId));
        });
    }

    [Test]
    public async Task GetDeadLetter_NoDeadLetterWithId_ReturnsNotFound()
    {
        using var request = CreateRequest($"{DeadLettersUri}/{Guid.CreateVersion7()}",
            ThroughlineRoles.ApplicationSupport);

        var response = await _client.SendAsync(request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    /// <summary>
    ///     Builds a GET request with a signed token for a caller holding the given roles. Platform's endpoints
    ///     aren't owner-scoped, so the request names no owner.
    /// </summary>
    private static HttpRequestMessage CreateRequest(string uri, params string[] roles)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", TestTokens.Create(roles));

        return request;
    }

    private static OrderConfirmedIntegrationEvent OrderConfirmed(int ownerId) =>
        new(ownerId, Guid.CreateVersion7(), SentAt, [new OrderLineEventModel("HFO-UNKNOWN-17", 1)], TestTokens.CallerId);

    /// <summary>
    ///     Writes a dead letter the way Wolverine stores one: the envelope, with the message serialized by
    ///     Wolverine's default serializer, serialized into <c>body</c>
    /// </summary>
    /// <returns>The dead letter's ID</returns>
    private async Task<Guid> InsertDeadLetterAsync(object message)
    {
        var serializer = _testFactory.Services.GetRequiredService<IWolverineRuntime>().Options.DefaultSerializer;
        var messageType = message.GetType().FullName
                          ?? throw new InvalidOperationException("The message type has no name.");

        var envelope = new Envelope(message)
        {
            Id = Guid.CreateVersion7(),
            MessageType = messageType,
            ContentType = serializer.ContentType,
            SentAt = SentAt,
            Source = "throughline-api"
        };
        envelope.Data = serializer.Write(envelope);

        var connectionString = _testFactory.Services.GetRequiredService<IConfiguration>()
                                   .GetConnectionString("Throughline")
                               ?? throw new InvalidOperationException("No Throughline connection string.");

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            INSERT INTO wolverine.wolverine_dead_letters
                (id, execution_time, body, message_type, received_at, source, exception_type, exception_message,
                 sent_at, replayable)
            VALUES (@id, NULL, @body, @messageType, @receivedAt, @source, @exceptionType, @exceptionMessage,
                    @sentAt, FALSE)
            """, connection);
        command.Parameters.AddWithValue("id", envelope.Id);
        command.Parameters.AddWithValue("body", EnvelopeSerializer.Serialize(envelope));
        command.Parameters.AddWithValue("messageType", messageType);
        command.Parameters.AddWithValue("receivedAt", "local://inventory/");
        command.Parameters.AddWithValue("source", "throughline-api");
        command.Parameters.AddWithValue("exceptionType", "Throughline.Common.Events.MissingDependencyException");
        command.Parameters.AddWithValue("exceptionMessage", "The following skus were not found: HFO-UNKNOWN-17");
        command.Parameters.AddWithValue("sentAt", SentAt);
        await command.ExecuteNonQueryAsync();

        return envelope.Id;
    }
}
