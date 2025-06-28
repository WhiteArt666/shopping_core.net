using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace shopping_tutorial.Models
{
    public class ContactModel
    {
        [Key]
        [Required(ErrorMessage = "Yêu Cầu Nhập Tiêu Đề")]
        public string Name { get; set; }
        [Required(ErrorMessage = "Yêu Cầu Nhập Thông Tin Liên Hệ")]

         public string Email { get; set; }
        [Required(ErrorMessage = "Yêu Cầu Nhập Thông Tin Liên Hệ")]

         public string Map { get; set; }
        [Required(ErrorMessage = "Yêu Cầu Nhập Thông Tin Liên Hệ")]

         public string Phone { get; set; }
        [Required(ErrorMessage = "Yêu Cầu Nhập Thông Tin Liên Hệ")]
        public string Description { get; set; }

        public string LogoImg { get; set; }

        [NotMapped]
        
        public IFormFile? ImageUpload { get; set; }


    }
}
