using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace shopping_tutorial.Models
{
    public class ProductVariantModel
    {
        [Key]
        public int Id { get; set; }
        
        public int ProductId { get; set; }
        public int ColorId { get; set; }
        public int SizeId { get; set; }
        
        [Required(ErrorMessage = "Yêu cầu nhập giá gốc")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Giá gốc phải lớn hơn 0")]
        public decimal OriginalPrice { get; set; }
        
        [Range(0, double.MaxValue, ErrorMessage = "Giá khuyến mãi phải lớn hơn hoặc bằng 0")]
        public decimal DiscountPrice { get; set; } = 0;
        
        [Range(0, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn hoặc bằng 0")]
        public int Quantity { get; set; } = 0;
        
        [Range(0, int.MaxValue, ErrorMessage = "Số lượng đã bán phải lớn hơn hoặc bằng 0")]
        public int Sold { get; set; } = 0;
        
        public string SKU { get; set; } // Stock Keeping Unit
        
        public bool IsActive { get; set; } = true;
        
        public DateTime DateCreated { get; set; } = DateTime.Now;
        public DateTime DateUpdated { get; set; } = DateTime.Now;
        
        // Navigation properties
        [ForeignKey("ProductId")]
        public ProductModel Product { get; set; }
        
        [ForeignKey("ColorId")]
        public ColorModel Color { get; set; }
        
        [ForeignKey("SizeId")]
        public SizeModel Size { get; set; }
        
        // Computed property for effective price
        [NotMapped]
        public decimal EffectivePrice => DiscountPrice > 0 && DiscountPrice < OriginalPrice ? DiscountPrice : OriginalPrice;
        
        // Computed property for discount percentage
        [NotMapped]
        public decimal DiscountPercentage => OriginalPrice > 0 && DiscountPrice > 0 && DiscountPrice < OriginalPrice 
            ? Math.Round(((OriginalPrice - DiscountPrice) / OriginalPrice) * 100, 2) 
            : 0;
    }
}
