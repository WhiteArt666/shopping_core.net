using System.ComponentModel.DataAnnotations;

namespace shopping_tutorial.Models
{
    public class ProductDetailsViewModel
    {
        public ProductModel ProductDetails { get; set; }


        [Required(ErrorMessage = "yêu cầu nhập bình luận sản phẩm")]
        public string Comment { get; set; }
        [Required(ErrorMessage = "yêu cầu nhập tên")]
        public string Name { get; set; }
        [Required(ErrorMessage = "yêu cầu nhập email")]

        public string Email { get; set; }


    }
}