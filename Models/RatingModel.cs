using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace shopping_tutorial.Models
{
    public class RatingModel
    {
        [Key]
        public int Id { get; set; }

        public int ProductId { get; set; }
        [Required(ErrorMessage = "yêu cầu nhập tên sản phẩm")]

        public string Comment { get; set; }
        [Required(ErrorMessage = "yêu cầu nhập đánh giá sản phẩm")]

        public string Name { get; set; }
        [Required(ErrorMessage = "yêu cầu nhập email")]

        public string Email { get; set; }

        public string Star { get; set; }


        [ForeignKey("ProductId")]
        public ProductModel Product { get; set; }
 

    }
}