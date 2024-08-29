namespace YanikoRestaurant.Models
{
    public class OrderViewModel
    {
        public decimal TotalAmount { get; set; }
        public List<OrderItemViewModel> OrderItems { get; set; } = new List<OrderItemViewModel>();
        public IEnumerable<Product>? Products { get; set; }
    }
}