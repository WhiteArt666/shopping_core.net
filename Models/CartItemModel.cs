namespace shopping_tutorial.Models
{
    public class CartItemModel
    {
        public int ProductId { get; set; }
        public int? ProductVariantId { get; set; } // Thêm để hỗ trợ variants
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal Total
        {
            get { return Quantity * Price; }
        }
        public string Image { get; set; }
        
        // Thông tin variant
        public string Size { get; set; }
        public string Color { get; set; }
        public string ColorCode { get; set; }
        
        public CartItemModel() { }
        
        public CartItemModel(ProductModel product)
        {
            ProductId = product.Id;
            ProductName = product.Name;
            Price = product.Price;
            Quantity = 1;
            Image = product.Image;
        }
        
        // Constructor cho variant
        public CartItemModel(ProductModel product, ProductVariantModel variant)
        {
            ProductId = product.Id;
            ProductVariantId = variant.Id;
            ProductName = product.Name;
            Price = variant.EffectivePrice;
            Quantity = 1;
            Image = product.Image;
            Size = variant.Size?.Name;
            Color = variant.Color?.Name;
            ColorCode = variant.Color?.ColorCode;
        }
    }
}
