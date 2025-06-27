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

		 public int BrandId { get; set; }
		 public int CategoryId { get; set; }

		 public CategoryModel Category { get; set; }
       
        public BrandModel Brand { get; set; }

		public RatingModel Ratings { get; set; }
		public string Image { get; set; } 
		[NotMapped]
		[FileExtension]
		public IFormFile? ImageUpload { get; set; } 
	}

    
}

