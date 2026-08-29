using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Planara.Common.Database.Domain;
using Planara.Common.Enums;
using Planara.Common.Kafka;
using Planara.Common.Kafka.Messages.Privacy;
using Planara.Kafka.Configurations;
using Planara.Privacy.Workers;

namespace Planara.Privacy.Tests.Api;

public class OutboxPublisherTests : BaseApiTest
{
    public OutboxPublisherTests(ApiTestWebAppFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task ConsentGrantedPublisher_PublishOnce_SendsMessage_AndMarksProcessed()
    {
        await DbTestUtils.ResetPrivacyDbAsync(Context);

        var consentId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        Context.OutboxMessages.Add(new OutboxMessage
        {
            TopicKey = KafkaTopicKeys.ConsentGranted,
            Type = nameof(ConsentGrantedMessage),
            Key = userId.ToString(),
            PayloadJson = JsonSerializer.Serialize(
                new ConsentGrantedMessage
                {
                    ConsentId = consentId,
                    RequestId = Guid.NewGuid(),
                    UserId = userId,
                    Type = ConsentType.PersonalData,
                    ConsentVersionId = Guid.NewGuid(),
                    GivenAt = DateTime.UtcNow
                },
                KafkaJson.SerializerOptions)
        });

        await Context.SaveChangesAsync();

        using var scope = Factory.Services.CreateScope();

        var publisher = scope.ServiceProvider
            .GetRequiredService<ConsentGrantedOutboxPublisher>();

        var fake = scope.ServiceProvider
            .GetRequiredService<
                FakeKafkaProducer<ConsentGrantedMessage>>();

        fake.Reset();

        await publisher.PublishOnce(
            CancellationToken.None);

        fake.Sent.Should().HaveCount(1);

        fake.Sent[0].TopicKey.Should().Be(
            KafkaTopicKeys.ConsentGranted);

        fake.Sent[0].Msg.ConsentId.Should().Be(
            consentId);

        Context.ChangeTracker.Clear();

        var row = await Context.OutboxMessages
            .AsNoTracking()
            .SingleAsync();

        row.ProcessedAt.Should().NotBeNull();
        row.LastError.Should().BeNull();
        row.LockedUntil.Should().BeNull();
        row.LockedBy.Should().BeNull();
    }

    [Fact]
    public async Task ConsentRevokedPublisher_PublishOnce_SendsMessage_AndMarksProcessed()
    {
        await DbTestUtils.ResetPrivacyDbAsync(Context);

        var consentId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        Context.OutboxMessages.Add(new OutboxMessage
        {
            TopicKey = KafkaTopicKeys.ConsentRevoked,
            Type = nameof(ConsentRevokedMessage),
            Key = userId.ToString(),
            PayloadJson = JsonSerializer.Serialize(
                new ConsentRevokedMessage
                {
                    ConsentId = consentId,
                    UserId = userId,
                    Type = ConsentType.PersonalData,
                    ConsentVersionId = Guid.NewGuid(),
                    RevokedAt = DateTime.UtcNow
                },
                KafkaJson.SerializerOptions)
        });

        await Context.SaveChangesAsync();

        using var scope = Factory.Services.CreateScope();

        var publisher = scope.ServiceProvider
            .GetRequiredService<ConsentRevokedOutboxPublisher>();

        var fake = scope.ServiceProvider
            .GetRequiredService<
                FakeKafkaProducer<ConsentRevokedMessage>>();

        fake.Reset();

        await publisher.PublishOnce(
            CancellationToken.None);

        fake.Sent.Should().HaveCount(1);

        fake.Sent[0].TopicKey.Should().Be(
            KafkaTopicKeys.ConsentRevoked);

        fake.Sent[0].Msg.ConsentId.Should().Be(
            consentId);

        Context.ChangeTracker.Clear();

        var row = await Context.OutboxMessages
            .AsNoTracking()
            .SingleAsync();

        row.ProcessedAt.Should().NotBeNull();
        row.LastError.Should().BeNull();
        row.LockedUntil.Should().BeNull();
    }

    [Fact]
    public async Task ConsentGrantedPublisher_IgnoresConsentRevokedMessages()
    {
        await DbTestUtils.ResetPrivacyDbAsync(Context);

        Context.OutboxMessages.Add(new OutboxMessage
        {
            TopicKey = KafkaTopicKeys.ConsentRevoked,
            Type = nameof(ConsentRevokedMessage),
            Key = UserId.ToString(),
            PayloadJson = JsonSerializer.Serialize(
                new ConsentRevokedMessage
                {
                    ConsentId = Guid.NewGuid(),
                    UserId = UserId,
                    Type = ConsentType.PersonalData,
                    ConsentVersionId = Guid.NewGuid(),
                    RevokedAt = DateTime.UtcNow
                },
                KafkaJson.SerializerOptions)
        });

        await Context.SaveChangesAsync();

        using var scope = Factory.Services.CreateScope();

        var publisher = scope.ServiceProvider
            .GetRequiredService<ConsentGrantedOutboxPublisher>();

        var fake = scope.ServiceProvider
            .GetRequiredService<
                FakeKafkaProducer<ConsentGrantedMessage>>();

        fake.Reset();

        await publisher.PublishOnce(
            CancellationToken.None);

        fake.Sent.Should().BeEmpty();

        Context.ChangeTracker.Clear();

        var row = await Context.OutboxMessages
            .AsNoTracking()
            .SingleAsync();

        row.ProcessedAt.Should().BeNull();
    }
}