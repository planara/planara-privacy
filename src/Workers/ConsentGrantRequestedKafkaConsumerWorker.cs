using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Planara.Common.Database.Domain;
using Planara.Common.Kafka;
using Planara.Common.Kafka.Messages.Privacy;
using Planara.Common.Workers;
using Planara.Kafka.Configurations;
using Planara.Kafka.Interfaces;
using Planara.Privacy.Data;
using Planara.Privacy.Data.Domain;
using Planara.Privacy.Data.Enums;

namespace Planara.Privacy.Workers;

public class ConsentGrantRequestedKafkaConsumerWorker(
    ILogger<ConsentGrantRequestedKafkaConsumerWorker> logger,
    IKafkaConsumer<ConsentGrantRequestedMessage> consumer,
    IServiceScopeFactory scopeFactory)
    : KafkaConsumerWorkerBase<ConsentGrantRequestedMessage>(logger, consumer, scopeFactory)
{
    protected override string TopicKey => KafkaTopicKeys.ConsentGrantRequested;

    protected override async Task HandleMessage(ConsentGrantRequestedMessage message, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var dataContext = serviceProvider.GetRequiredService<DataContext>();

        ValidateSubject(message);

        var exists = await dataContext.UserConsents.AnyAsync(x => 
            x.GrantRequestId == message.RequestId, cancellationToken);

        if (exists)
        {
            logger.LogInformation("Consent grant request {RequestId} already processed. Skipping.", message.RequestId);

            return;
        }

        var consentVersion = await dataContext.ConsentVersions
            .FirstOrDefaultAsync(x => x.Id == message.ConsentVersionId, cancellationToken);

        if (consentVersion is null)
            throw new InvalidOperationException($"Consent version '{message.ConsentVersionId}' was not found.");

        if (consentVersion.Type != message.Type)
            throw new InvalidOperationException(
                $"Consent version '{message.ConsentVersionId}' belongs to '{consentVersion.Type}', but '{message.Type}' was requested.");

        if (consentVersion.Status != ConsentVersionStatus.Published)
            throw new InvalidOperationException($"Consent version '{message.ConsentVersionId}' is not published.");

        if (consentVersion.EffectiveAt > message.GivenAt)
            throw new InvalidOperationException($"Consent version '{message.ConsentVersionId}' was not effective when consent was given.");

        var consent = new UserConsent
        {
            GrantRequestId = message.RequestId,
            ConsentVersionId = consentVersion.Id,
            RegistrationId = message.RegistrationId,
            UserId = message.UserId,
            GivenAt = message.GivenAt,
            ExpiresAt = message.ExpiresAt,
            IpAddress = message.IpAddress,
            UserAgent = message.UserAgent
        };

        dataContext.UserConsents.Add(consent);

        var grantedMessage = new ConsentGrantedMessage
        {
            ConsentId = consent.Id,
            RequestId = message.RequestId,
            RegistrationId = message.RegistrationId,
            UserId = message.UserId,
            Type = message.Type,
            ConsentVersionId = consentVersion.Id,
            GivenAt = message.GivenAt
        };

        dataContext.OutboxMessages.Add(new OutboxMessage
        {
            TopicKey = KafkaTopicKeys.ConsentGranted,
            Type = nameof(ConsentGrantedMessage),
            Key = GetMessageKey(message),
            PayloadJson = JsonSerializer.Serialize(grantedMessage, KafkaJson.SerializerOptions)
        });

        await dataContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Consent {ConsentId} granted. RequestId: {RequestId}, Type: {ConsentType}.", consent.Id, message.RequestId, message.Type);
    }

    /// <summary>
    /// Проверяет корректность субъекта, для которого запрашивается согласие.
    /// </summary>
    private static void ValidateSubject(ConsentGrantRequestedMessage message)
    {
        if (message.RegistrationId.HasValue == message.UserId.HasValue)
            throw new InvalidOperationException("Exactly one consent subject must be specified: RegistrationId or UserId.");

        if (message.RegistrationId.HasValue && !message.ExpiresAt.HasValue)
            throw new InvalidOperationException("ExpiresAt is required for registration consent.");

        if (message.UserId.HasValue && message.ExpiresAt.HasValue)
            throw new InvalidOperationException("ExpiresAt must not be specified for permanent user consent.");
    }

    /// <summary>
    /// Возвращает Kafka partition key для события согласия.
    /// </summary>
    private static string GetMessageKey(ConsentGrantRequestedMessage message) =>
        message.UserId?.ToString() ?? message.RegistrationId!.Value.ToString();
}