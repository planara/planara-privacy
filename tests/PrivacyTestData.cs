using Planara.Common.Enums;
using Planara.Privacy.Data.Domain;
using Planara.Privacy.Data.Enums;

namespace Planara.Privacy.Tests;

public static class PrivacyTestData
{
    public static ConsentVersion PublishedVersion(ConsentType type = ConsentType.PersonalData, DateTime? effectiveAt = null)
    {
        var now = DateTime.UtcNow;

        return new ConsentVersion
        {
            Id = Guid.NewGuid(),
            Type = type,
            Version = Guid.NewGuid().ToString("N"),
            Title = "Test consent",
            Content = "Test content",
            HtmlContent = "<p>Test content</p>",
            Status = ConsentVersionStatus.Published,
            EffectiveAt = effectiveAt ?? now.AddMinutes(-1),
            PublishedAt = now.AddMinutes(-2),
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public static UserConsent UserConsent(Guid userId, ConsentVersion version, DateTime? givenAt = null, DateTime? revokedAt = null)
    {
        var now = DateTime.UtcNow;

        return new UserConsent
        {
            Id = Guid.NewGuid(),
            GrantRequestId = Guid.NewGuid(),
            ConsentVersionId = version.Id,
            ConsentVersion = version,
            UserId = userId,
            GivenAt = givenAt ?? now,
            RevokedAt = revokedAt,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}