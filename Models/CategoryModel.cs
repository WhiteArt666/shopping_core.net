using System.ComponentModel.DataAnnotations;

namespace shopping_tutorial.Models
{
    public class CategoryModel
    {
        [Key]

        public int Id { get; set; }
        [Required(ErrorMessage = "Yêu Cầu Nhập Ten Danh Muc")]
        public string Name { get; set; }
        public string Slug { get; set; }
        [Required( ErrorMessage = "Yêu Cầu Nhập Mô Tả Danh Muc")]
        public string Description { get; set; }
        public int Status { get; set; }


    }
}
