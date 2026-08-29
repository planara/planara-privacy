using Planara.Common.Kafka.Messages.Privacy;
using Planara.Common.Workers;
using Planara.Kafka.Interfaces;

namespace Planara.Privacy.Workers;

public class ConsentRevokedOutboxPublisher(
    IServiceScopeFactory scopeFactory,
    IKafkaProducer<ConsentRevokedMessage> producer,
    ILogger<ConsentRevokedOutboxPublisher> logger
) : OutboxPublisherBase<ConsentRevokedMessage>(scopeFactory, producer, logger);