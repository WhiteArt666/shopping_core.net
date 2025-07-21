using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace shopping_tutorial.Models
{
    public class CustomerVoucherModel
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string UserId { get; set; } // Customer ID
        
        [Required]
        public int CouponId { get; set; } // Voucher ID
        
        public DateTime SentDate { get; set; } = DateTime.Now;
        
        public bool IsUsed { get; set; } = false;
        
        public DateTime? UsedDate { get; set; }
        
        public string SentBy { get; set; } // Admin ID đã gửi
        
        public string VoucherType { get; set; } = "Regular"; // Regular, VIP, Exclusive
        
        [ForeignKey("CouponId")]
        public CouponModel Coupon { get; set; }
    }
}
