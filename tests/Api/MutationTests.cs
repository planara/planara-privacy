using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Planara.Common.Kafka;
using Planara.Common.Kafka.Messages.Privacy;
using Planara.Privacy.Data.Enums;

namespace Planara.Privacy.Tests.Api;

public class MutationTests : BaseApiTest
{
    public MutationTests(ApiTestWebAppFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task GrantConsent_Success_PersistsConsent_AndCreatesOutboxMessage()
    {
        await DbTestUtils.ResetPrivacyDbAsync(Context);

        var version = PrivacyTestData.PublishedVersion();

        Context.ConsentVersions.Add(version);

        await Context.SaveChangesAsync();

        const string mutation = """
                                mutation ($request: GrantConsentRequestInput!) {
                                  grantConsent(request: $request) {
                                    consentId
                                    type
                                    consentVersionId
                                    changedAt
                                  }
                                }
                                """;

        using var document = await Client.PostAsync(
            mutation,
            new
            {
                request = new
                {
                    consentVersionId = version.Id
                }
            });

        document.GetErrors().Should().BeNull();

        Context.ChangeTracker.Clear();

        var consent = await Context.UserConsents
            .AsNoTracking()
            .SingleAsync();

        consent.UserId.Should().Be(UserId);
        consent.RegistrationId.Should().BeNull();
        consent.ConsentVersionId.Should().Be(version.Id);
        consent.RevokedAt.Should().BeNull();
        consent.ExpiresAt.Should().BeNull();

        var outbox = await Context.OutboxMessages
            .AsNoTracking()
            .SingleAsync();

        outbox.TopicKey.Should().Be(
            KafkaTopicKeys.ConsentGranted);

        outbox.Type.Should().Be(
            nameof(ConsentGrantedMessage));

        outbox.Key.Should().Be(UserId.ToString());

        outbox.ProcessedAt.Should().BeNull();
    }

    [Fact]
    public async Task GrantConsent_WhenVersionDoesNotExist_ReturnsError()
    {
        await DbTestUtils.ResetPrivacyDbAsync(Context);

        const string mutation = """
                                mutation ($request: GrantConsentRequestInput!) {
                                  grantConsent(request: $request) {
                                    consentId
                                  }
                                }
                                """;

        using var document = await Client.PostAsync(
            mutation,
            new
            {
                request = new
                {
                    consentVersionId = Guid.NewGuid()
                }
            });

        document.GetErrors().Should().NotBeNull();

        (await Context.UserConsents.CountAsync())
            .Should()
            .Be(0);

        (await Context.OutboxMessages.CountAsync())
            .Should()
            .Be(0);
    }

    [Fact]
    public async Task GrantConsent_WhenVersionIsDraft_ReturnsError()
    {
        await DbTestUtils.ResetPrivacyDbAsync(Context);

        var version = PrivacyTestData.PublishedVersion();

        version.Status = ConsentVersionStatus.Draft;
        version.PublishedAt = null;

        Context.ConsentVersions.Add(version);

        await Context.SaveChangesAsync();

        const string mutation = """
                                mutation ($request: GrantConsentRequestInput!) {
                                  grantConsent(request: $request) {
                                    consentId
                                  }
                                }
                                """;

        using var document = await Client.PostAsync(
            mutation,
            new
            {
                request = new
                {
                    consentVersionId = version.Id
                }
            });

        document.GetErrors().Should().NotBeNull();

        (await Context.UserConsents.CountAsync())
            .Should()
            .Be(0);
    }

    [Fact]
    public async Task GrantConsent_WhenVersionIsNotEffectiveYet_ReturnsError()
    {
        await DbTestUtils.ResetPrivacyDbAsync(Context);

        var version = PrivacyTestData.PublishedVersion(
            effectiveAt: DateTime.UtcNow.AddDays(1));

        Context.ConsentVersions.Add(version);

        await Context.SaveChangesAsync();

        const string mutation = """
                                mutation ($request: GrantConsentRequestInput!) {
                                  grantConsent(request: $request) {
                                    consentId
                                  }
                                }
                                """;

        using var document = await Client.PostAsync(
            mutation,
            new
            {
                request = new
                {
                    consentVersionId = version.Id
                }
            });

        document.GetErrors().Should().NotBeNull();
    }

    [Fact]
    public async Task GrantConsent_WhenSameVersionAlreadyGranted_IsIdempotent()
    {
        await DbTestUtils.ResetPrivacyDbAsync(Context);

        var version = PrivacyTestData.PublishedVersion();

        Context.ConsentVersions.Add(version);

        var existing = PrivacyTestData.UserConsent(
            UserId,
            version);

        Context.UserConsents.Add(existing);

        await Context.SaveChangesAsync();

        const string mutation = """
                                mutation ($request: GrantConsentRequestInput!) {
                                  grantConsent(request: $request) {
                                    consentId
                                  }
                                }
                                """;

        using var document = await Client.PostAsync(
            mutation,
            new
            {
                request = new
                {
                    consentVersionId = version.Id
                }
            });

        document.GetErrors().Should().BeNull();

        document.GetData()
            .GetProperty("grantConsent")
            .GetProperty("consentId")
            .GetGuid()
            .Should()
            .Be(existing.Id);

        (await Context.UserConsents.CountAsync())
            .Should()
            .Be(1);

        (await Context.OutboxMessages.CountAsync())
            .Should()
            .Be(0);
    }

    [Fact]
    public async Task RevokeConsent_Success_SetsRevokedAt_AndCreatesOutboxMessage()
    {
        await DbTestUtils.ResetPrivacyDbAsync(Context);

        var version = PrivacyTestData.PublishedVersion();

        Context.ConsentVersions.Add(version);

        var consent = PrivacyTestData.UserConsent(
            UserId,
            version);

        Context.UserConsents.Add(consent);

        await Context.SaveChangesAsync();

        const string mutation = """
                                mutation {
                                  revokeConsent(type: PERSONAL_DATA) {
                                    consentId
                                    type
                                    consentVersionId
                                    changedAt
                                  }
                                }
                                """;

        using var document = await Client.PostAsync(mutation);

        document.GetErrors().Should().BeNull();

        Context.ChangeTracker.Clear();

        var stored = await Context.UserConsents
            .AsNoTracking()
            .SingleAsync();

        stored.RevokedAt.Should().NotBeNull();

        var outbox = await Context.OutboxMessages
            .AsNoTracking()
            .SingleAsync();

        outbox.TopicKey.Should().Be(
            KafkaTopicKeys.ConsentRevoked);

        outbox.Type.Should().Be(
            nameof(ConsentRevokedMessage));

        outbox.Key.Should().Be(UserId.ToString());
    }

    [Fact]
    public async Task RevokeConsent_WhenNoActiveConsentExists_ReturnsError()
    {
        await DbTestUtils.ResetPrivacyDbAsync(Context);

        const string mutation = """
                                mutation {
                                  revokeConsent(type: PERSONAL_DATA) {
                                    consentId
                                  }
                                }
                                """;

        using var document = await Client.PostAsync(mutation);

        document.GetErrors().Should().NotBeNull();

        (await Context.OutboxMessages.CountAsync())
            .Should()
            .Be(0);
    }

    [Fact]
    public async Task RevokeConsent_WhenLatestConsentIsAlreadyRevoked_DoesNotRevokeOlderConsent()
    {
        await DbTestUtils.ResetPrivacyDbAsync(Context);

        var version = PrivacyTestData.PublishedVersion();

        Context.ConsentVersions.Add(version);

        var oldConsent = PrivacyTestData.UserConsent(
            UserId,
            version,
            DateTime.UtcNow.AddDays(-2));

        var latestConsent = PrivacyTestData.UserConsent(
            UserId,
            version,
            DateTime.UtcNow.AddDays(-1),
            DateTime.UtcNow.AddHours(-12));

        Context.UserConsents.AddRange(
            oldConsent,
            latestConsent);

        await Context.SaveChangesAsync();

        const string mutation = """
                                mutation {
                                  revokeConsent(type: PERSONAL_DATA) {
                                    consentId
                                  }
                                }
                                """;

        using var document = await Client.PostAsync(mutation);

        document.GetErrors().Should().NotBeNull();

        Context.ChangeTracker.Clear();

        var oldStored = await Context.UserConsents
            .AsNoTracking()
            .SingleAsync(x => x.Id == oldConsent.Id);

        oldStored.RevokedAt.Should().BeNull();
    }

    [Fact]
    public async Task GrantConsent_WithoutAuthorization_ReturnsError()
    {
        await DbTestUtils.ResetPrivacyDbAsync(Context);

        Client.DefaultRequestHeaders.Remove("X-Test-UserId");

        var version = PrivacyTestData.PublishedVersion();

        Context.ConsentVersions.Add(version);

        await Context.SaveChangesAsync();

        const string mutation = """
                                mutation ($request: GrantConsentRequestInput!) {
                                  grantConsent(request: $request) {
                                    consentId
                                  }
                                }
                                """;

        using var document = await Client.PostAsync(
            mutation,
            new
            {
                request = new
                {
                    consentVersionId = version.Id
                }
            });

        document.GetErrors().Should().NotBeNull();
    }
}