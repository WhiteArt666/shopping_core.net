using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using shopping_tutorial.Models;
using shopping_tutorial.Repository;

namespace shopping_tutorial.Controllers
{
    [Authorize]
    public class ChatController : Controller
    {
        private readonly DataContext _dataContext;
        private readonly UserManager<AppUserModel> _userManager;

        public ChatController(DataContext dataContext, UserManager<AppUserModel> userManager)
        {
            _dataContext = dataContext;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var model = new ChatViewModel
            {
                CurrentUserId = currentUser.Id,
                IsAdmin = false
            };

            try
            {
                // Lấy danh sách admin để chat
                var adminRole = await _dataContext.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");
                var adminIds = new List<string>();
                
                if (adminRole != null)
                {
                    adminIds = await _dataContext.UserRoles
                        .Where(ur => ur.RoleId == adminRole.Id)
                        .Select(ur => ur.UserId)
                        .ToListAsync();
                }

                var admins = await _userManager.Users
                    .Where(u => adminIds.Contains(u.Id))
                    .ToListAsync();

                foreach (var admin in admins)
                {
                    var lastMessage = await _dataContext.ChatMessages
                        .Where(m => (m.SenderId == currentUser.Id && m.ReceiverId == admin.Id) ||
                                   (m.SenderId == admin.Id && m.ReceiverId == currentUser.Id))
                        .OrderByDescending(m => m.SentTime)
                        .FirstOrDefaultAsync();

                    var unreadCount = await _dataContext.ChatMessages
                        .CountAsync(m => m.SenderId == admin.Id && 
                                        m.ReceiverId == currentUser.Id && 
                                        !m.IsRead);

                    model.AvailableUsers.Add(new ChatUser
                    {
                        UserId = admin.Id,
                        UserName = admin.UserName,
                        Email = admin.Email,
                        IsOnline = false,
                        LastMessageDate = lastMessage?.SentTime ?? DateTime.MinValue,
                        UnreadCount = unreadCount
                    });
                }

                // Tự động chọn admin đầu tiên nếu có
                if (model.AvailableUsers.Any())
                {
                    var firstAdmin = model.AvailableUsers.OrderByDescending(u => u.LastMessageDate).First();
                    model.OtherUserId = firstAdmin.UserId;
                    model.OtherUserName = firstAdmin.UserName;

                    model.Messages = await _dataContext.ChatMessages
                        .Where(m => (m.SenderId == currentUser.Id && m.ReceiverId == firstAdmin.UserId) ||
                                   (m.SenderId == firstAdmin.UserId && m.ReceiverId == currentUser.Id))
                        .OrderBy(m => m.SentTime)
                        .ToListAsync();

                    // Đánh dấu đã đọc tin nhắn từ admin
                    var unreadMessages = await _dataContext.ChatMessages
                        .Where(m => m.SenderId == firstAdmin.UserId && m.ReceiverId == currentUser.Id && !m.IsRead)
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
                    IsFromAdmin = false
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
        public async Task<IActionResult> GetMessages(string adminId)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                
                var messages = await _dataContext.ChatMessages
                    .Where(m => (m.SenderId == currentUser.Id && m.ReceiverId == adminId) ||
                               (m.SenderId == adminId && m.ReceiverId == currentUser.Id))
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
