using System.Net.Mail;
using System.Net;

namespace shopping_tutorial.Areas.Admin.Repository
{
    public class EmailSender : IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string message)
        {
            var client = new SmtpClient("smtp.gmail.com", 587) // 587 cổng bảo mật hơn cổng 465
            {
                EnableSsl = true, //bật bảo mật
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential("bangy07102004@gmail.com", "xaziyffzzskbfreg") // mật khẩu và khóa bảo mật ứng dụng mình đã tạo 
            };

            return client.SendMailAsync(
                new MailMessage(from: "bangy07102004@gmail.com",
                                to: email,
                                subject,
                                message
                                ));
        }
    }
}
