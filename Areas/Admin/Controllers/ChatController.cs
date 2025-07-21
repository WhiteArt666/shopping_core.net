using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using shopping_tutorial.Models;
using shopping_tutorial.Repository;

namespace shopping_tutorial.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ChatController : Controller
    {
        private readonly DataContext _dataContext;
        private readonly UserManager<AppUserModel> _userManager;

        public ChatController(DataContext dataContext, UserManager<AppUserModel> userManager)
        {
            _dataContext = dataContext;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(string userId = null)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var model = new ChatViewModel
            {
                CurrentUserId = currentUser.Id,
                IsAdmin = true
            };

            try
            {
                // Lấy danh sách khách hàng đã chat với admin
                var chatUserIds = await _dataContext.ChatMessages
                    .Where(m => m.SenderId == currentUser.Id || m.ReceiverId == currentUser.Id)
                    .Select(m => m.SenderId == currentUser.Id ? m.ReceiverId : m.SenderId)
                    .Distinct()
                    .ToListAsync();

                // Thêm cả khách hàng đã gửi tin nhắn cho bất kỳ admin nào
                var customerMessageIds = await _dataContext.ChatMessages
                    .Where(m => m.IsFromAdmin == false)
                    .Select(m => m.SenderId)
                    .Distinct()
                    .ToListAsync();

                var allCustomerIds = chatUserIds.Union(customerMessageIds).Distinct().ToList();

                var customers = await _userManager.Users
                    .Where(u => allCustomerIds.Contains(u.Id))
                    .ToListAsync();

                foreach (var customer in customers)
                {
                    var lastMessage = await _dataContext.ChatMessages
                        .Where(m => (m.SenderId == customer.Id && m.ReceiverId == currentUser.Id) ||
                                   (m.SenderId == currentUser.Id && m.ReceiverId == customer.Id))
                        .OrderByDescending(m => m.SentTime)
                        .FirstOrDefaultAsync();

                    var unreadCount = await _dataContext.ChatMessages
                        .CountAsync(m => m.SenderId == customer.Id && 
                                        m.ReceiverId == currentUser.Id && 
                                        !m.IsRead);

                    model.AvailableUsers.Add(new ChatUser
                    {
                        UserId = customer.Id,
                        UserName = customer.UserName,
                        Email = customer.Email,
                        IsOnline = false, // Có thể implement SignalR sau
                        LastMessageDate = lastMessage?.SentTime ?? DateTime.MinValue,
                        UnreadCount = unreadCount
                    });
                }

                // Sắp xếp theo tin nhắn mới nhất
                model.AvailableUsers = model.AvailableUsers
                    .OrderByDescending(u => u.LastMessageDate)
                    .ToList();

                // Nếu có userId được chọn, load tin nhắn
                if (!string.IsNullOrEmpty(userId))
                {
                    model.OtherUserId = userId;
                    var otherUser = await _userManager.FindByIdAsync(userId);
                    model.OtherUserName = otherUser?.UserName ?? "Unknown";

                    model.Messages = await _dataContext.ChatMessages
                        .Where(m => (m.SenderId == currentUser.Id && m.ReceiverId == userId) ||
                                   (m.SenderId == userId && m.ReceiverId == currentUser.Id))
                        .OrderBy(m => m.SentTime)
                        .ToListAsync();

                    // Đánh dấu đã đọc tin nhắn từ customer
                    var unreadMessages = await _dataContext.ChatMessages
                        .Where(m => m.SenderId == userId && m.ReceiverId == currentUser.Id && !m.IsRead)
                        .ToListAsync();

                    foreach (var msg in unreadMessages)
                    {
                        msg.IsRead = true;
                    }

                    if (unreadMessages.Any())
                    {
                        await _dataContext.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Có lỗi xảy ra: {ex.Message}";
            }

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage(string receiverId, string message)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                
                if (string.IsNullOrWhiteSpace(message) || string.IsNullOrEmpty(receiverId))
                {
                    return Json(new { success = false, message = "Tin nhắn không hợp lệ" });
                }

                var chatMessage = new ChatMessageModel
                {
                    SenderId = currentUser.Id,
                    ReceiverId = receiverId,
                    Message = message.Trim(),
                    SentTime = DateTime.Now,
                    IsRead = false,
                    IsFromAdmin = true
                };

                _dataContext.ChatMessages.Add(chatMessage);
                await _dataContext.SaveChangesAsync();

                return Json(new { 
                    success = true, 
                    messageId = chatMessage.Id,
                    sentTime = chatMessage.SentTime.ToString("HH:mm")
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Có lỗi xảy ra: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetMessages(string userId)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                
                var messages = await _dataContext.ChatMessages
                    .Where(m => (m.SenderId == currentUser.Id && m.ReceiverId == userId) ||
                               (m.SenderId == userId && m.ReceiverId == currentUser.Id))
                    .OrderBy(m => m.SentTime)
                    .Select(m => new {
                        id = m.Id,
                        senderId = m.SenderId,
                        message = m.Message,
                        sentTime = m.SentTime.ToString("HH:mm"),
                        isFromAdmin = m.IsFromAdmin,
                        isRead = m.IsRead
                    })
                    .ToListAsync();

                return Json(new { success = true, messages });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
