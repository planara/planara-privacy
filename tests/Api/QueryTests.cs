using FluentAssertions;
using Planara.Privacy.Data.Enums;

namespace Planara.Privacy.Tests.Api;

public class QueryTests : BaseApiTest
{
    public QueryTests(ApiTestWebAppFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task CurrentConsentVersion_WhenPublishedVersionExists_ReturnsLatestEffectiveVersion()
    {
        await DbTestUtils.ResetPrivacyDbAsync(Context);

        var oldVersion = PrivacyTestData.PublishedVersion(
            effectiveAt: DateTime.UtcNow.AddDays(-10));

        oldVersion.Version = "1";

        var currentVersion = PrivacyTestData.PublishedVersion(
            effectiveAt: DateTime.UtcNow.AddDays(-1));

        currentVersion.Version = "2";

        Context.ConsentVersions.AddRange(
            oldVersion,
            currentVersion);

        await Context.SaveChangesAsync();

        const string query = """
                             query {
                               currentConsentVersion(type: PERSONAL_DATA) {
                                 id
                                 type
                                 version
                                 title
                               }
                             }
                             """;

        using var document = await Client.PostAsync(query);

        document.GetErrors().Should().BeNull();

        var version = document.GetData()
            .GetProperty("currentConsentVersion");

        version.GetProperty("id")
            .GetGuid()
            .Should()
            .Be(currentVersion.Id);

        version.GetProperty("version")
            .GetString()
            .Should()
            .Be("2");
    }

    [Fact]
    public async Task CurrentConsentVersion_WhenFutureVersionExists_IgnoresIt()
    {
        await DbTestUtils.ResetPrivacyDbAsync(Context);

        var current = PrivacyTestData.PublishedVersion(
            effectiveAt: DateTime.UtcNow.AddHours(-1));

        var future = PrivacyTestData.PublishedVersion(
            effectiveAt: DateTime.UtcNow.AddDays(1));

        Context.ConsentVersions.AddRange(
            current,
            future);

        await Context.SaveChangesAsync();

        const string query = """
                             query {
                               currentConsentVersion(type: PERSONAL_DATA) {
                                 id
                               }
                             }
                             """;

        using var document = await Client.PostAsync(query);

        document.GetErrors().Should().BeNull();

        document.GetData()
            .GetProperty("currentConsentVersion")
            .GetProperty("id")
            .GetGuid()
            .Should()
            .Be(current.Id);
    }

    [Fact]
    public async Task CurrentConsentVersion_WhenOnlyDraftExists_ReturnsNull()
    {
        await DbTestUtils.ResetPrivacyDbAsync(Context);

        var version = PrivacyTestData.PublishedVersion();
        version.Status = ConsentVersionStatus.Draft;
        version.PublishedAt = null;

        Context.ConsentVersions.Add(version);

        await Context.SaveChangesAsync();

        const string query = """
                             query {
                               currentConsentVersion(type: PERSONAL_DATA) {
                                 id
                               }
                             }
                             """;

        using var document = await Client.PostAsync(query);

        document.GetErrors().Should().BeNull();

        document.GetData()
            .GetProperty("currentConsentVersion")
            .ValueKind
            .Should()
            .Be(System.Text.Json.JsonValueKind.Null);
    }

    [Fact]
    public async Task ConsentVersions_FiltersAndSortsPublishedVersions()
    {
        await DbTestUtils.ResetPrivacyDbAsync(Context);

        var first = PrivacyTestData.PublishedVersion(
            effectiveAt: DateTime.UtcNow.AddDays(-10));

        first.Version = "1";

        var second = PrivacyTestData.PublishedVersion(
            effectiveAt: DateTime.UtcNow.AddDays(-1));

        second.Version = "2";

        var draft = PrivacyTestData.PublishedVersion();
        draft.Status = ConsentVersionStatus.Draft;

        Context.ConsentVersions.AddRange(
            first,
            second,
            draft);

        await Context.SaveChangesAsync();

        const string query = """
                             query {
                               consentVersions(
                                 where: {
                                   type: { eq: PERSONAL_DATA }
                                 }
                                 order: {
                                   effectiveAt: DESC
                                 }
                               ) {
                                 nodes {
                                   version
                                   type
                                 }
                               }
                             }
                             """;

        using var document = await Client.PostAsync(query);

        document.GetErrors().Should().BeNull();

        var nodes = document.GetData()
            .GetProperty("consentVersions")
            .GetProperty("nodes");

        nodes.GetArrayLength().Should().Be(2);

        nodes[0].GetProperty("version")
            .GetString()
            .Should()
            .Be("2");

        nodes[1].GetProperty("version")
            .GetString()
            .Should()
            .Be("1");
    }

    [Fact]
    public async Task MyConsents_WithoutAuthorization_ReturnsError()
    {
        await DbTestUtils.ResetPrivacyDbAsync(Context);

        Client.DefaultRequestHeaders.Remove("X-Test-UserId");

        const string query = """
                             query {
                               myConsents {
                                 nodes {
                                   id
                                 }
                               }
                             }
                             """;

        using var document = await Client.PostAsync(query);

        document.GetErrors().Should().NotBeNull();
    }

    [Fact]
    public async Task MyConsents_ReturnsOnlyCurrentUserHistory()
    {
        await DbTestUtils.ResetPrivacyDbAsync(Context);

        var version = PrivacyTestData.PublishedVersion();

        Context.ConsentVersions.Add(version);

        Context.UserConsents.AddRange(
            PrivacyTestData.UserConsent(
                UserId,
                version,
                DateTime.UtcNow.AddDays(-2)),

            PrivacyTestData.UserConsent(
                UserId,
                version,
                DateTime.UtcNow.AddDays(-1),
                DateTime.UtcNow.AddHours(-12)),

            PrivacyTestData.UserConsent(
                Guid.NewGuid(),
                version));

        await Context.SaveChangesAsync();

        const string query = """
                             query {
                               myConsents(
                                 order: { givenAt: DESC }
                               ) {
                                 nodes {
                                   id
                                   type
                                   consentVersionId
                                   version
                                   givenAt
                                   revokedAt
                                   isRevoked
                                 }
                               }
                             }
                             """;

        using var document = await Client.PostAsync(query);

        document.GetErrors().Should().BeNull();

        var nodes = document.GetData()
            .GetProperty("myConsents")
            .GetProperty("nodes");

        nodes.GetArrayLength().Should().Be(2);

        nodes[0]
            .GetProperty("isRevoked")
            .GetBoolean()
            .Should()
            .BeTrue();
    }
}