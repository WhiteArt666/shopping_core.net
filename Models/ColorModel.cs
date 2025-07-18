using System.ComponentModel.DataAnnotations;

namespace shopping_tutorial.Models
{
    public class ColorModel
    {
        [Key]
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Yêu cầu nhập tên màu")]
        [MinLength(2, ErrorMessage = "Tên màu phải có ít nhất 2 ký tự")]
        public string Name { get; set; }
        
        [Required(ErrorMessage = "Yêu cầu nhập mã màu")]
        [RegularExpression(@"^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$", ErrorMessage = "Mã màu phải có định dạng hex (ví dụ: #FF0000)")]
        public string ColorCode { get; set; }
        
        public string Description { get; set; }
        
        public DateTime DateCreated { get; set; } = DateTime.Now;
    }
}
