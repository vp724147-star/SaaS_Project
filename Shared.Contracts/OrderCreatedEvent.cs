namespace Shared.Contracts
{
    public class OrderCreatedEvent
    {
        public Guid OrderId { get; set; }
        public string ProductName { get; set; }
        public decimal Amount { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
