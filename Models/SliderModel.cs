using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace shopping_tutorial.Models
{
    public class SliderModel
    {
        public int Id { get; set; }
        [Required(ErrorMessage = ("yeu cầu chọn slider"))]

        public string Name { get; set; }
        [Required(ErrorMessage = ("yeu cầu điền tên"))]

        public string Description { get; set; }

        public int? Status { get; set; }

        public string Image { get; set; }
        [NotMapped]
        public IFormFile? ImageUpload { get; set; }
    }
}