using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Planara.Common.Enums;
using Planara.Common.Kafka;
using Planara.Common.Kafka.Messages.Privacy;
using Planara.Privacy.Data.Enums;
using Planara.Privacy.Workers;

namespace Planara.Privacy.Tests.Api;

public class KafkaConsumerTests : BaseApiTest
{
    public KafkaConsumerTests(ApiTestWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task ConsentGrantRequested_Success_CreatesConsent_AndGrantedOutboxMessage()
    {
        await DbTestUtils.ResetPrivacyDbAsync(Context);

        var version = PrivacyTestData.PublishedVersion();

        Context.ConsentVersions.Add(version);
        await Context.SaveChangesAsync();

        using var scope = Factory.Services.CreateScope();
        var fake = scope.ServiceProvider.GetRequiredService<FakeKafkaConsumer<ConsentGrantRequestedMessage>>();

        fake.Reset();

        var registrationId = Guid.NewGuid();
        var requestId = Guid.NewGuid();

        fake.Enqueue(new ConsentGrantRequestedMessage
        {
            RequestId = requestId,
            RegistrationId = registrationId,
            UserId = null,
            Type = ConsentType.PersonalData,
            ConsentVersionId = version.Id,
            GivenAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(2),
            IpAddress = "127.0.0.1",
            UserAgent = "Planara.Tests"
        });

        var worker = scope.ServiceProvider.GetRequiredService<ConsentGrantRequestedKafkaConsumerWorker>();

        await worker.ConsumeOnce(CancellationToken.None);

        Context.ChangeTracker.Clear();

        var consent = await Context.UserConsents.AsNoTracking().SingleAsync();

        consent.GrantRequestId.Should().Be(requestId);
        consent.RegistrationId.Should().Be(registrationId);
        consent.UserId.Should().BeNull();
        consent.ConsentVersionId.Should().Be(version.Id);
        consent.IpAddress.Should().Be("127.0.0.1");
        consent.UserAgent.Should().Be("Planara.Tests");
        consent.ExpiresAt.Should().NotBeNull();
        
        consent.UpdatedAt.Should().BeAfter(DateTime.UnixEpoch);

        var outbox = await Context.OutboxMessages.AsNoTracking().SingleAsync();

        outbox.TopicKey.Should().Be(KafkaTopicKeys.ConsentGranted);
        outbox.Type.Should().Be(nameof(ConsentGrantedMessage));
        outbox.Key.Should().Be(registrationId.ToString());
        fake.Committed.Should().HaveCount(1);
    }

    [Fact]
    public async Task ConsentGrantRequested_WhenDeliveredTwice_IsIdempotent()
    {
        await DbTestUtils.ResetPrivacyDbAsync(Context);

        var version = PrivacyTestData.PublishedVersion();

        Context.ConsentVersions.Add(version);
        await Context.SaveChangesAsync();

        using var scope = Factory.Services.CreateScope();

        var fake = scope.ServiceProvider.GetRequiredService<FakeKafkaConsumer<ConsentGrantRequestedMessage>>();
        fake.Reset();

        var message = new ConsentGrantRequestedMessage
        {
            RequestId = Guid.NewGuid(),
            RegistrationId = Guid.NewGuid(),
            Type = ConsentType.PersonalData,
            ConsentVersionId = version.Id,
            GivenAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(2)
        };

        fake.Enqueue(message);
        fake.Enqueue(message);

        var worker = scope.ServiceProvider.GetRequiredService<ConsentGrantRequestedKafkaConsumerWorker>();

        await worker.ConsumeOnce(CancellationToken.None);
        await worker.ConsumeOnce(CancellationToken.None);

        Context.ChangeTracker.Clear();

        (await Context.UserConsents.CountAsync()).Should().Be(1);
        (await Context.OutboxMessages.CountAsync()).Should().Be(1);
        fake.Committed.Should().HaveCount(2);
    }

    [Fact]
    public async Task ConsentGrantRequested_WhenVersionDoesNotExist_DoesNotCommitMessage()
    {
        await DbTestUtils.ResetPrivacyDbAsync(Context);

        using var scope = Factory.Services.CreateScope();

        var fake = scope.ServiceProvider.GetRequiredService<FakeKafkaConsumer<ConsentGrantRequestedMessage>>();
        fake.Reset();

        fake.Enqueue(new ConsentGrantRequestedMessage
        {
            RequestId = Guid.NewGuid(),
            RegistrationId = Guid.NewGuid(),
            Type = ConsentType.PersonalData,
            ConsentVersionId = Guid.NewGuid(),
            GivenAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(2)
        });

        var worker = scope.ServiceProvider.GetRequiredService<ConsentGrantRequestedKafkaConsumerWorker>();

        var action = async () => await worker.ConsumeOnce(CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>();
        fake.Committed.Should().BeEmpty();
        (await Context.UserConsents.CountAsync()).Should().Be(0);
    }
    
    [Fact]
    public async Task ConsentGrantRequested_ForUser_Success_CreatesPermanentConsent()
    {
        await DbTestUtils.ResetPrivacyDbAsync(Context);
    
        var version = PrivacyTestData.PublishedVersion();
    
        Context.ConsentVersions.Add(version);
        await Context.SaveChangesAsync();
    
        using var scope = Factory.Services.CreateScope();
    
        var fake = scope.ServiceProvider.GetRequiredService<FakeKafkaConsumer<ConsentGrantRequestedMessage>>();
        fake.Reset();
    
        var userId = Guid.NewGuid();
    
        fake.Enqueue(new ConsentGrantRequestedMessage
        {
            RequestId = Guid.NewGuid(),
            RegistrationId = null,
            UserId = userId,
            Type = ConsentType.PersonalData,
            ConsentVersionId = version.Id,
            GivenAt = DateTime.UtcNow,
            ExpiresAt = null,
            IpAddress = "127.0.0.1",
            UserAgent = "Planara.Tests"
        });
    
        var worker = scope.ServiceProvider.GetRequiredService<ConsentGrantRequestedKafkaConsumerWorker>();
    
        await worker.ConsumeOnce(CancellationToken.None);
    
        Context.ChangeTracker.Clear();
    
        var consent = await Context.UserConsents.AsNoTracking().SingleAsync();
    
        consent.UserId.Should().Be(userId);
        consent.RegistrationId.Should().BeNull();
        consent.ExpiresAt.Should().BeNull();
    
        var outbox = await Context.OutboxMessages.AsNoTracking().SingleAsync();
        outbox.Key.Should().Be(userId.ToString());
        fake.Committed.Should().HaveCount(1);
    }
    
    [Fact]
    public async Task ConsentGrantRequested_WhenSubjectIsMissing_DoesNotCommitMessage()
    {
        await DbTestUtils.ResetPrivacyDbAsync(Context);
    
        var version = PrivacyTestData.PublishedVersion();
    
        Context.ConsentVersions.Add(version);
        await Context.SaveChangesAsync();
    
        using var scope = Factory.Services.CreateScope();
    
        var fake = scope.ServiceProvider.GetRequiredService<FakeKafkaConsumer<ConsentGrantRequestedMessage>>();
    
        fake.Reset();
    
        fake.Enqueue(new ConsentGrantRequestedMessage
        {
            RequestId = Guid.NewGuid(),
            RegistrationId = null,
            UserId = null,
            Type = ConsentType.PersonalData,
            ConsentVersionId = version.Id,
            GivenAt = DateTime.UtcNow,
            ExpiresAt = null
        });
    
        var worker = scope.ServiceProvider.GetRequiredService<ConsentGrantRequestedKafkaConsumerWorker>();
    
        var action = async () => await worker.ConsumeOnce(CancellationToken.None);
    
        await action.Should().ThrowAsync<InvalidOperationException>();
        fake.Committed.Should().BeEmpty();
        (await Context.UserConsents.CountAsync()).Should().Be(0);
        (await Context.OutboxMessages.CountAsync()).Should().Be(0);
    }
    
    [Fact]
    public async Task ConsentGrantRequested_WhenBothSubjectsAreSpecified_DoesNotCommitMessage()
    {
        await DbTestUtils.ResetPrivacyDbAsync(Context);
    
        var version = PrivacyTestData.PublishedVersion();
    
        Context.ConsentVersions.Add(version);
        await Context.SaveChangesAsync();
    
        using var scope = Factory.Services.CreateScope();
    
        var fake = scope.ServiceProvider.GetRequiredService<FakeKafkaConsumer<ConsentGrantRequestedMessage>>();
        fake.Reset();
    
        fake.Enqueue(new ConsentGrantRequestedMessage
        {
            RequestId = Guid.NewGuid(),
            RegistrationId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Type = ConsentType.PersonalData,
            ConsentVersionId = version.Id,
            GivenAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(2)
        });
    
        var worker = scope.ServiceProvider.GetRequiredService<ConsentGrantRequestedKafkaConsumerWorker>();
    
        var action = async () => await worker.ConsumeOnce(CancellationToken.None);
    
        await action.Should().ThrowAsync<InvalidOperationException>();
        fake.Committed.Should().BeEmpty();
        (await Context.UserConsents.CountAsync()).Should().Be(0);
    }
    
    [Fact]
    public async Task ConsentGrantRequested_ForRegistrationWithoutExpiration_DoesNotCommitMessage()
    {
        await DbTestUtils.ResetPrivacyDbAsync(Context);
    
        var version = PrivacyTestData.PublishedVersion();
    
        Context.ConsentVersions.Add(version);
        await Context.SaveChangesAsync();
    
        using var scope = Factory.Services.CreateScope();
    
        var fake = scope.ServiceProvider.GetRequiredService<FakeKafkaConsumer<ConsentGrantRequestedMessage>>();
    
        fake.Reset();
    
        fake.Enqueue(new ConsentGrantRequestedMessage
        {
            RequestId = Guid.NewGuid(),
            RegistrationId = Guid.NewGuid(),
            UserId = null,
            Type = ConsentType.PersonalData,
            ConsentVersionId = version.Id,
            GivenAt = DateTime.UtcNow,
            ExpiresAt = null
        });
    
        var worker = scope.ServiceProvider.GetRequiredService<ConsentGrantRequestedKafkaConsumerWorker>();
    
        var action = async () => await worker.ConsumeOnce(CancellationToken.None);
    
        await action.Should().ThrowAsync<InvalidOperationException>();
        fake.Committed.Should().BeEmpty();
        (await Context.UserConsents.CountAsync()).Should().Be(0);
    }
    
    [Fact]
    public async Task ConsentGrantRequested_ForUserWithExpiration_DoesNotCommitMessage()
    {
        await DbTestUtils.ResetPrivacyDbAsync(Context);
    
        var version = PrivacyTestData.PublishedVersion();
    
        Context.ConsentVersions.Add(version);
        await Context.SaveChangesAsync();
    
        using var scope = Factory.Services.CreateScope();
    
        var fake = scope.ServiceProvider.GetRequiredService<FakeKafkaConsumer<ConsentGrantRequestedMessage>>();
        fake.Reset();
    
        fake.Enqueue(new ConsentGrantRequestedMessage
        {
            RequestId = Guid.NewGuid(),
            RegistrationId = null,
            UserId = Guid.NewGuid(),
            Type = ConsentType.PersonalData,
            ConsentVersionId = version.Id,
            GivenAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(2)
        });
    
        var worker = scope.ServiceProvider.GetRequiredService<ConsentGrantRequestedKafkaConsumerWorker>();
    
        var action = async () => await worker.ConsumeOnce(CancellationToken.None);
        await action.Should().ThrowAsync<InvalidOperationException>();
    
        fake.Committed.Should().BeEmpty();
    
        (await Context.UserConsents.CountAsync()).Should().Be(0);
    }
    
    [Fact]
    public async Task ConsentGrantRequested_WhenVersionIsNotPublished_DoesNotCommitMessage()
    {
        await DbTestUtils.ResetPrivacyDbAsync(Context);
    
        var version = PrivacyTestData.PublishedVersion();
    
        version.Status = ConsentVersionStatus.Draft;
        version.PublishedAt = null;
    
        Context.ConsentVersions.Add(version);
        await Context.SaveChangesAsync();
    
        using var scope = Factory.Services.CreateScope();
    
        var fake = scope.ServiceProvider.GetRequiredService<FakeKafkaConsumer<ConsentGrantRequestedMessage>>();
    
        fake.Reset();
    
        fake.Enqueue(new ConsentGrantRequestedMessage
        {
            RequestId = Guid.NewGuid(),
            RegistrationId = Guid.NewGuid(),
            Type = ConsentType.PersonalData,
            ConsentVersionId = version.Id,
            GivenAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(2)
        });
    
        var worker = scope.ServiceProvider.GetRequiredService<ConsentGrantRequestedKafkaConsumerWorker>();
    
        var action = async () => await worker.ConsumeOnce(CancellationToken.None);
    
        await action.Should().ThrowAsync<InvalidOperationException>();
    
        fake.Committed.Should().BeEmpty();
    
        (await Context.UserConsents.CountAsync()).Should().Be(0);
        (await Context.OutboxMessages.CountAsync()).Should().Be(0);
    }
    
    [Fact]
    public async Task ConsentGrantRequested_WhenConsentTypeDoesNotMatchVersion_DoesNotCommitMessage()
    {
        await DbTestUtils.ResetPrivacyDbAsync(Context);
    
        var version = PrivacyTestData.PublishedVersion();
    
        Context.ConsentVersions.Add(version);
        await Context.SaveChangesAsync();
    
        using var scope = Factory.Services.CreateScope();
    
        var fake = scope.ServiceProvider.GetRequiredService<FakeKafkaConsumer<ConsentGrantRequestedMessage>>();
    
        fake.Reset();
    
        fake.Enqueue(new ConsentGrantRequestedMessage
        {
            RequestId = Guid.NewGuid(),
            RegistrationId = Guid.NewGuid(),
            Type = (ConsentType)999,
            ConsentVersionId = version.Id,
            GivenAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(2)
        });
    
        var worker = scope.ServiceProvider.GetRequiredService<ConsentGrantRequestedKafkaConsumerWorker>();
    
        var action = async () => await worker.ConsumeOnce(CancellationToken.None);
    
        await action.Should().ThrowAsync<InvalidOperationException>();
    
        fake.Committed.Should().BeEmpty();
    
        (await Context.UserConsents.CountAsync()).Should().Be(0);
        (await Context.OutboxMessages.CountAsync()).Should().Be(0);
    }
    
    [Fact]
    public async Task ConsentGrantRequested_WhenVersionWasNotEffectiveAtGivenAt_DoesNotCommitMessage()
    {
        await DbTestUtils.ResetPrivacyDbAsync(Context);
    
        var givenAt = DateTime.UtcNow;
    
        var version = PrivacyTestData.PublishedVersion(effectiveAt: givenAt.AddHours(1));
    
        Context.ConsentVersions.Add(version);
        await Context.SaveChangesAsync();
    
        using var scope = Factory.Services.CreateScope();
    
        var fake = scope.ServiceProvider.GetRequiredService<FakeKafkaConsumer<ConsentGrantRequestedMessage>>();
        fake.Reset();
    
        fake.Enqueue(new ConsentGrantRequestedMessage
        {
            RequestId = Guid.NewGuid(),
            RegistrationId = Guid.NewGuid(),
            UserId = null,
            Type = ConsentType.PersonalData,
            ConsentVersionId = version.Id,
            GivenAt = givenAt,
            ExpiresAt = givenAt.AddHours(2)
        });
    
        var worker = scope.ServiceProvider.GetRequiredService<ConsentGrantRequestedKafkaConsumerWorker>();
    
        var action = async () => await worker.ConsumeOnce(CancellationToken.None);
    
        await action.Should().ThrowAsync<InvalidOperationException>();
    
        fake.Committed.Should().BeEmpty();
    
        (await Context.UserConsents.CountAsync()).Should().Be(0);
        (await Context.OutboxMessages.CountAsync()).Should().Be(0);
    }
}