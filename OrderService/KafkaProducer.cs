using Confluent.Kafka;
using Shared.Contracts;
using System.Text.Json;

namespace OrderService
{
    public class KafkaProducer
    {
        private readonly ProducerConfig _config;

        public KafkaProducer()
        {
            _config = new ProducerConfig
            {
                BootstrapServers = "kafka:9092"
            };
        }

        public async Task SendOrder(OrderCreatedEvent order)
        {
            using var producer = new ProducerBuilder<Null, string>(_config).Build();

            var message = new KafkaMessage<OrderCreatedEvent>
            {
                Data = order,
                RetryCount = 0
            };

            var json = JsonSerializer.Serialize(message);

            Console.WriteLine("📤 Sending message to Kafka..."); // 👈 DEBUG

            await producer.ProduceAsync("order-created", new Message<Null, string>
            {
                Value = json
            });

            producer.Flush(TimeSpan.FromSeconds(5)); // 🔥 MUST
        }
    }
}

