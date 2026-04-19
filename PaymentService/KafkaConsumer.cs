using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using PaymentService.Data;
using PaymentService.Models;
using Shared.Contracts;
using System.Text.Json;

namespace PaymentService
{
    public class KafkaConsumer : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public KafkaConsumer(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine("🚀 KafkaConsumer started");

            var consumerConfig = new ConsumerConfig
            {
                GroupId = "payment-service-group",
                BootstrapServers = "kafka:9092",
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = true
            };

            using var consumer = new ConsumerBuilder<Ignore, string>(consumerConfig).Build();

            var producer = new ProducerBuilder<Null, string>(
                new ProducerConfig { BootstrapServers = "kafka:9092" }
            ).Build();

            // ✅ SAFE SUBSCRIBE (NO CRASH)
            while (true)
            {
                try
                {
                    consumer.Subscribe(new[] { "order-created", "order-created-retry" });
                    Console.WriteLine("✅ Subscribed successfully");
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⏳ Kafka not ready: {ex.Message}");
                    await Task.Delay(2000, stoppingToken);
                }
            }

            Console.WriteLine("👂 Listening...");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var cr = consumer.Consume(stoppingToken);

                    Console.WriteLine($"📥 Raw: {cr.Message.Value}");

                    var message = JsonSerializer.Deserialize<KafkaMessage<OrderCreatedEvent>>(cr.Message.Value);

                    if (message == null || message.Data == null)
                        continue;

                    // 🔥 create scope for DbContext (IMPORTANT FIX)
                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    // 🔥 Idempotency
                    var existing = await db.Payments
                        .FirstOrDefaultAsync(x => x.OrderId == message.Data.OrderId);

                    if (existing != null)
                    {
                        Console.WriteLine("⚠️ Duplicate ignored");
                        continue;
                    }

                    var payment = new Payment
                    {
                        OrderId = message.Data.OrderId,
                        Amount = message.Data.Amount,
                        Status = "PROCESSING",
                        RetryCount = message.RetryCount,
                        CreatedAt = DateTime.UtcNow
                    };

                    db.Payments.Add(payment);
                    await db.SaveChangesAsync();

                    try
                    {
                        // 🔥 Simulated payment logic
                        if (message.Data.Amount > 10000)
                            throw new Exception("Payment failed");

                        payment.Status = "SUCCESS";
                        await db.SaveChangesAsync();

                        Console.WriteLine($"✅ SUCCESS: {message.Data.OrderId}");
                    }
                    catch (Exception ex)
                    {
                        message.RetryCount++;

                        payment.Status = "FAILED";
                        payment.RetryCount = message.RetryCount;
                        await db.SaveChangesAsync();

                        Console.WriteLine($"❌ FAILED ({message.RetryCount})");

                        var json = JsonSerializer.Serialize(message);

                        if (message.RetryCount >= 3)
                        {
                            payment.Status = "DEAD_LETTER";
                            await db.SaveChangesAsync();

                            await producer.ProduceAsync("order-created-dlq",
                                new Message<Null, string> { Value = json });

                            Console.WriteLine("💀 Sent to DLQ");
                        }
                        else
                        {
                            await producer.ProduceAsync("order-created-retry",
                                new Message<Null, string> { Value = json });

                            Console.WriteLine("🔁 Sent to Retry");
                        }
                    }
                }
                catch (ConsumeException ex)
                {
                    Console.WriteLine($"⚠️ Kafka error: {ex.Error.Reason}");
                    await Task.Delay(3000, stoppingToken);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"🔥 Unexpected error: {ex.Message}");
                    await Task.Delay(3000, stoppingToken);
                }
            }
        }
    }
}