using System.ComponentModel.DataAnnotations;

namespace shopping_tutorial.Models
{
    public class CouponModel
    {
        [Key]
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Yêu cầu nhập tên khuyến mãi")]
        public string Name { get; set; }
        
        [Required(ErrorMessage = "Yêu cầu nhập khuyến mãi khuyến mãi")]
        public string Description { get; set; }
        public DateTime DateStart { get; set; }
        public DateTime DateExpired { get; set; }

        [Required(ErrorMessage = "Yêu cầu nhập số lượng khuyến mãi")]
        public int Quantity { get; set; }
        
        public int Status { get; set; }

    }
}