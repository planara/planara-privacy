using Confluent.Kafka;
using Planara.Kafka.Interfaces;

namespace Planara.Privacy.Tests;

public class FakeKafkaConsumer<TMessage> : IKafkaConsumer<TMessage> where TMessage : class
{
    private readonly Queue<ConsumeResult<string, TMessage>> _messages = [];

    private long _offset;

    public List<ConsumeResult<string, TMessage>> Committed { get; } = [];

    public bool IsClosed { get; private set; }

    public void Enqueue(TMessage message, string key = "test-key", string topic = "test-topic")
    {
        _messages.Enqueue(new ConsumeResult<string, TMessage>
        {
            Topic = topic,
            Partition = new Partition(0),
            Offset = new Offset(_offset++),
            Message = new Message<string, TMessage>
            {
                Key = key,
                Value = message
            }
        });
    }

    public Task<ConsumeResult<string, TMessage>?> ConsumeAsync(string topicKey, CancellationToken cancellationToken) =>
        Task.FromResult(_messages.Count == 0 ? null : _messages.Dequeue());

    public Task CommitAsync(ConsumeResult<string, TMessage> result, CancellationToken cancellationToken)
    {
        Committed.Add(result);

        return Task.CompletedTask;
    }

    public void Close()
    {
        IsClosed = true;
    }

    public void Reset()
    {
        _messages.Clear();
        Committed.Clear();
        IsClosed = false;
    }
}