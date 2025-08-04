namespace shopping_tutorial.Models.ViewModels
{
    public class CartItemViewModel
    {
        public List<CartItemModel> CartItems { get; set; }
        public decimal GrandTotal { get; set; }

        public decimal ShippingCost { get; set; }

        public string CouponCode { get; set; }
        public decimal Discount { get; set; }
        public decimal FinalTotal { get; set; }
    }
    
}
