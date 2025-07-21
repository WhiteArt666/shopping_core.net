using System.ComponentModel.DataAnnotations;

namespace shopping_tutorial.Models
{
    public class CustomerAnalyticsViewModel
    {
        public List<TopCustomerBySpending> TopCustomersBySpending { get; set; } = new List<TopCustomerBySpending>();
        public List<TopCustomerByOrders> TopCustomersByOrders { get; set; } = new List<TopCustomerByOrders>();
        public List<VIPCustomer> VIPCustomers { get; set; } = new List<VIPCustomer>();
        public List<CustomerInfo> AllCustomers { get; set; } = new List<CustomerInfo>();
        public List<CouponInfo> AvailableCoupons { get; set; } = new List<CouponInfo>();
    }
    
    public class TopCustomerBySpending
    {
        public string UserId { get; set; }
        public string Email { get; set; }
        public string UserName { get; set; }
        public decimal TotalSpent { get; set; }
        public int OrderCount { get; set; }
        public bool IsVIP { get; set; }
    }
    
    public class TopCustomerByOrders
    {
        public string UserId { get; set; }
        public string Email { get; set; }
        public string UserName { get; set; }
        public int OrderCount { get; set; }
        public decimal TotalSpent { get; set; }
        public bool IsVIP { get; set; }
    }
    
    public class VIPCustomer
    {
        public string UserId { get; set; }
        public string Email { get; set; }
        public string UserName { get; set; }
        public decimal TotalSpent { get; set; }
        public int OrderCount { get; set; }
        public DateTime LastOrderDate { get; set; }
        public bool HasExclusiveVoucher { get; set; }
    }
    
    public class CustomerInfo
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
    }
    
    public class CouponInfo
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }
    
    public class ChatViewModel
    {
        public string CurrentUserId { get; set; }
        public string OtherUserId { get; set; }
        public string OtherUserName { get; set; }
        public List<ChatMessageModel> Messages { get; set; } = new List<ChatMessageModel>();
        public List<ChatUser> AvailableUsers { get; set; } = new List<ChatUser>();
        public bool IsAdmin { get; set; }
    }
    
    public class ChatUser
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public bool IsOnline { get; set; }
        public DateTime LastMessageDate { get; set; }
        public int UnreadCount { get; set; }
    }
}
