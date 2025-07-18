using System.ComponentModel.DataAnnotations;

namespace shopping_tutorial.Models
{
    public class SizeModel
    {
        [Key]
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Yêu cầu nhập tên size")]
        [MinLength(1, ErrorMessage = "Tên size phải có ít nhất 1 ký tự")]
        public string Name { get; set; }
        
        public string Description { get; set; }
        
        public DateTime DateCreated { get; set; } = DateTime.Now;
    }
}
