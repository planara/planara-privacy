using System.Security.Claims;
using System.Text.Json;
using HotChocolate;
using HotChocolate.Types;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Planara.Common.Auth.Claims;
using Planara.Common.Database.Domain;
using Planara.Common.Enums;
using Planara.Common.Kafka;
using Planara.Common.Kafka.Messages.Privacy;
using Planara.Kafka.Configurations;
using Planara.Privacy.Data;

namespace Planara.Privacy.GraphQL;

[ExtendObjectType(OperationTypeNames.Mutation)]
public class Mutation
{
    [Authorize]
    public async Task<bool> RevokeConsent(
        ConsentType type,
        ClaimsPrincipal claimsPrincipal,
        [Service] DataContext dataContext,
        CancellationToken cancellationToken)
    {
        var userId = claimsPrincipal.GetUserId();
        var now = DateTime.UtcNow;

        var consent = await dataContext.UserConsents
            .Include(x => x.ConsentVersion)
            .Where(x =>
                x.UserId == userId &&
                x.ConsentVersion.Type == type &&
                x.RevokedAt == null)
            .OrderByDescending(x => x.GivenAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (consent is null)
            return false;

        consent.RevokedAt = now;

        var message = new ConsentRevokedMessage
        {
            ConsentId = consent.Id,
            UserId = userId,
            Type = type,
            ConsentVersionId = consent.ConsentVersionId,
            RevokedAt = now
        };

        dataContext.OutboxMessages.Add(new OutboxMessage
        {
            TopicKey = KafkaTopicKeys.ConsentRevoked,
            Type = nameof(ConsentRevokedMessage),
            Key = userId.ToString(),
            PayloadJson = JsonSerializer.Serialize(message, KafkaJson.SerializerOptions)
        });

        await dataContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}