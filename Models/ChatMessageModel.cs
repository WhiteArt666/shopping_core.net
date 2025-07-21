using System.ComponentModel.DataAnnotations;

namespace shopping_tutorial.Models
{
    public class ChatMessageModel
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string SenderId { get; set; } // User ID của người gửi
        
        [Required]
        public string ReceiverId { get; set; } // User ID của người nhận
        
        [Required]
        public string Message { get; set; }
        
        public DateTime SentTime { get; set; } = DateTime.Now;
        
        public bool IsRead { get; set; } = false;
        
        public bool IsFromAdmin { get; set; } = false; // Phân biệt tin nhắn từ admin hay customer
    }
}
