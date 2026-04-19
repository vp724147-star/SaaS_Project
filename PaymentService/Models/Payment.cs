namespace PaymentService.Models
{
    public class Payment
    {
        public int Id { get; set; }
        public Guid OrderId { get; set; }   // UNIQUE 🔥
        public decimal Amount { get; set; }
        public string Status { get; set; }    // SUCCESS / FAILED
        public int RetryCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
