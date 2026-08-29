using Planara.Common.Kafka.Messages.Privacy;
using Planara.Common.Workers;
using Planara.Kafka.Interfaces;

namespace Planara.Privacy.Workers;

public class ConsentGrantedOutboxPublisher(
    IServiceScopeFactory scopeFactory,
    IKafkaProducer<ConsentGrantedMessage> producer,
    ILogger<ConsentGrantedOutboxPublisher> logger
) : OutboxPublisherBase<ConsentGrantedMessage>(scopeFactory, producer, logger);