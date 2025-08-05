using System.ComponentModel.DataAnnotations;
using shopping_tutorial.Models;

public class UserCouponModel
{
    [Key]
    public int Id { get; set; }

    public string UserId { get; set; }
    public AppUserModel User { get; set; }

    public int CouponId { get; set; }
    public CouponModel Coupon { get; set; }

    public DateTime RedeemedAt { get; set; }
    public bool IsUsed { get; set; } = false;
}
