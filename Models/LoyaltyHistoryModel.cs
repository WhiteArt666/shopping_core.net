using System;

namespace shopping_tutorial.Models
{
    public class LoyaltyHistoryModel
    {
        public int Id { get; set; }

        public string UserId { get; set; }
        public AppUserModel User { get; set; }

        public int Points { get; set; } // có thể là âm (trừ điểm)
        public string Reason { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
