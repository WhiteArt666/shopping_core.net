using shopping_tutorial.Repository.Validation;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace shopping_tutorial.Models
{
	public class ProductModel
	{
		[Key]
        public int Id { get; set; }
		[Required,MinLength(4,ErrorMessage="Yêu Cầu Nhập Tên Sản Phẩm")]
		public string Name { get; set; }
		public string Slug { get; set; }
		[Required, MinLength(4, ErrorMessage = "Yêu Cầu Nhập Mô Tả Sản Phẩm")]
		public string Description { get; set; }

    	[Required( ErrorMessage = "Yêu Cầu Nhập Giá ")]
		[Range(0.01,double .MaxValue)]
		 public decimal Price { get; set; }

		 public int Quantity { get; set; }

		 public int Sold { get; set; }

		 public int BrandId { get; set; }

		 public int CategoryId { get; set; }

		 public CategoryModel Category { get; set; }
       
        public BrandModel Brand { get; set; }

		public RatingModel Ratings { get; set; }
		public string Image { get; set; } 
		[NotMapped]
		[FileExtension]
		public IFormFile? ImageUpload { get; set; }
		
		// Navigation properties for variants
		public ICollection<ProductVariantModel> ProductVariants { get; set; }
		
		// Computed property for total quantity (product quantity + all variants quantity)
		[NotMapped]
		public int TotalQuantity => Quantity + (ProductVariants?.Sum(pv => pv.Quantity) ?? 0);
		
		// Get default variant (first available variant)
		[NotMapped]
		public ProductVariantModel DefaultVariant => ProductVariants?.FirstOrDefault(pv => pv.IsActive);
		
		// Get effective price (from default variant if available, otherwise product price)
		[NotMapped]
		public decimal EffectivePrice => DefaultVariant?.EffectivePrice ?? Price;
		
		// Get original price (from default variant if available, otherwise product price)
		[NotMapped]
		public decimal DisplayOriginalPrice => DefaultVariant?.OriginalPrice ?? Price;
		
		// Get discount price (from default variant if available)
		[NotMapped]
		public decimal? DisplayDiscountPrice => DefaultVariant?.DiscountPrice > 0 ? DefaultVariant.DiscountPrice : null;
		
		// Get discount percentage
		[NotMapped]
		public decimal DiscountPercentage => DefaultVariant?.DiscountPercentage ?? 0; 
	}

    
}

