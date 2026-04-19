using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shared.Contracts;

namespace OrderService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private readonly KafkaProducer _producer;

        public OrderController(KafkaProducer producer)
        {
            _producer = producer;
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder()
        {
            var order = new OrderCreatedEvent
            {
                OrderId = Guid.NewGuid(),
                ProductName = "Laptop",
                Amount = 50000,
                CreatedAt = DateTime.UtcNow
            };

            await _producer.SendOrder(order);

            return Ok(order);
        }
    }
}
