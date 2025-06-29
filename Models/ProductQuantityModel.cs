using System;
using System.ComponentModel.DataAnnotations;
namespace shopping_tutorial.Models
{
	public class ProductQuantityModel
	{
		[Key]

        public int Id { get; set; }
		public int Quantity { get; set; }
		[Required(ErrorMessage ="yêu cầu nhập số lượng")]
		public int ProductId { get; set; }
	
		public DateTime DateCreated { get; set; }
	
	}
}

