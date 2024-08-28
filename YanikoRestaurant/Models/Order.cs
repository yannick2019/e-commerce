namespace YanikoRestaurant.Models
{
    public class Order
    {
        public int OrderId { get; set; }

        public string? UserId { get; set; }

        public ApplicationUser User { get; set; } = null!;

        public decimal TotalAmount { get; set; }

        public DateTime OrderDate { get; set; }

        public ICollection<OrderItem>? OrderItems { get; set; }
    }
}