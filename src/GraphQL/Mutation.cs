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
using Planara.Privacy.Data.Domain;
using Planara.Privacy.Data.Enums;
using Planara.Privacy.Requests;
using Planara.Privacy.Responses;

namespace Planara.Privacy.GraphQL;

[ExtendObjectType(OperationTypeNames.Mutation)]
public class Mutation
{
    /// <summary>
    /// Выдача согласия текущим пользователем
    /// </summary>
    [Authorize]
    [GraphQLDescription("Выдача согласия текущим пользователем")]
    public async Task<ConsentMutationResponse> GrantConsent(
        [GraphQLDescription("Данные для выдачи согласия")]
        GrantConsentRequest request,
        ClaimsPrincipal claimsPrincipal,
        [Service] DataContext dataContext,
        CancellationToken cancellationToken)
    {
        var userId = claimsPrincipal.GetUserId();
        var now = DateTime.UtcNow;

        var consentVersion = await dataContext.ConsentVersions
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.ConsentVersionId, cancellationToken);

        if (consentVersion is null)
        {
            throw new GraphQLException(ErrorBuilder.New()
                .SetCode("CONSENT_VERSION_NOT_FOUND")
                .SetMessage("Consent version was not found.")
                .Build());
        }

        if (consentVersion.Status != ConsentVersionStatus.Published || consentVersion.EffectiveAt > now)
        {
            throw new GraphQLException(ErrorBuilder.New()
                .SetCode("CONSENT_VERSION_NOT_AVAILABLE")
                .SetMessage("Consent version is not available.")
                .Build());
        }
        
        var currentConsent = await dataContext.UserConsents
            .Where(x => x.UserId == userId && x.ConsentVersion.Type == consentVersion.Type)
            .OrderByDescending(x => x.GivenAt)
            .ThenByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        
        if (currentConsent is { RevokedAt: null } && currentConsent.ConsentVersionId == consentVersion.Id)
        {
            return new ConsentMutationResponse
            {
                ConsentId = currentConsent.Id,
                Type = consentVersion.Type,
                ConsentVersionId = currentConsent.ConsentVersionId,
                ChangedAt = currentConsent.GivenAt
            };
        }

        var requestId = Guid.NewGuid();

        var consent = new UserConsent
        {
            GrantRequestId = requestId,
            ConsentVersionId = consentVersion.Id,
            UserId = userId,
            RegistrationId = null,
            GivenAt = now,
            RevokedAt = null,
            ExpiresAt = null,
            CreatedAt = now,
            UpdatedAt = now
        };

        dataContext.UserConsents.Add(consent);

        var message = new ConsentGrantedMessage
        {
            ConsentId = consent.Id,
            RequestId = requestId,
            RegistrationId = null,
            UserId = userId,
            Type = consentVersion.Type,
            ConsentVersionId = consentVersion.Id,
            GivenAt = now
        };

        dataContext.OutboxMessages.Add(new OutboxMessage
        {
            TopicKey = KafkaTopicKeys.ConsentGranted,
            Type = nameof(ConsentGrantedMessage),
            Key = userId.ToString(),
            PayloadJson = JsonSerializer.Serialize(message, KafkaJson.SerializerOptions)
        });

        await dataContext.SaveChangesAsync(cancellationToken);

        return new ConsentMutationResponse
        {
            ConsentId = consent.Id,
            Type = consentVersion.Type,
            ConsentVersionId = consentVersion.Id,
            ChangedAt = now
        };
    }

    /// <summary>
    /// Отзыв действующего согласия текущего пользователя
    /// </summary>
    [Authorize]
    [GraphQLDescription("Отзыв действующего согласия текущего пользователя")]
    public async Task<ConsentMutationResponse> RevokeConsent(
        [GraphQLDescription("Тип отзываемого согласия")]
        ConsentType type,
        ClaimsPrincipal claimsPrincipal,
        [Service] DataContext dataContext,
        CancellationToken cancellationToken)
    {
        var userId = claimsPrincipal.GetUserId();
        var now = DateTime.UtcNow;
        
        var consent = await dataContext.UserConsents
            .Include(x => x.ConsentVersion)
            .Where(x => x.UserId == userId && x.ConsentVersion.Type == type)
            .OrderByDescending(x => x.GivenAt)
            .ThenByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (consent is null || consent.RevokedAt.HasValue)
        {
            throw new GraphQLException(ErrorBuilder.New()
                .SetCode("ACTIVE_CONSENT_NOT_FOUND")
                .SetMessage($"Active consent of type '{type}' was not found.")
                .Build());
        }

        consent.RevokedAt = now;
        consent.UpdatedAt = now;

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

        return new ConsentMutationResponse
        {
            ConsentId = consent.Id,
            Type = type,
            ConsentVersionId = consent.ConsentVersionId,
            ChangedAt = now
        };
    }
}