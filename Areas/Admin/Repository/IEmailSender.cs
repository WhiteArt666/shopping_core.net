namespace shopping_tutorial.Areas.Admin.Repository
{
    public interface IEmailSender
    {
        Task SendEmailAsync(string email, string subject, string message);// hàm gửi mail : mail cần gửi , tiêu đề mail, nội dung mail 
    }
}
