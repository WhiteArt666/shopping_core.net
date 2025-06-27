using Microsoft.AspNetCore.Mvc;
using shopping_tutorial.Areas.Admin.Repository;
using shopping_tutorial.Models;
using shopping_tutorial.Repository;
using System.Security.Claims;

namespace shopping_tutorial.Controllers
{
	public class CheckoutController : Controller
	{
		private readonly DataContext _dataContext;
        //private readonly IEmailSender _emailSender;
        //public CheckoutController(IEmailSender emailSender, DataContext context)
        //{
        //    _dataContext = context;
        //    _emailSender = emailSender;
        //}
        public CheckoutController(  DataContext context)
		{
			_dataContext = context;
		}
		public async Task<IActionResult> Checkout()
		{
			var userEmail = User.FindFirstValue(ClaimTypes.Email);
			if(userEmail == null)
			{
				return RedirectToAction("Login", "Account");
			}
			else
			{
				var ordercode = Guid.NewGuid().ToString();
				var orderItem = new OrderModel();
				orderItem.OrderCode = ordercode;
				orderItem.UserName = userEmail;
				orderItem.Status = 1; // 1 là đơn hàng mới 
				orderItem.CreatedDate = DateTime.Now;
				_dataContext.Add(orderItem);// thêm dữ liệu tạo đơn hàng mới 
				_dataContext.SaveChanges();
				List<CartItemModel> cartitems = HttpContext.Session.GetJson<List<CartItemModel>>("Cart") ?? new List<CartItemModel>();
				foreach(var cart in cartitems)
				{
					var orderdetails = new OrderDetails();
					orderdetails.UserName = userEmail;
					orderdetails.OrderCode = ordercode;
					orderdetails.ProductId = cart.ProductId;
					orderdetails.Price = cart.Price;
					orderdetails.Quantity = cart.Quantity;
					_dataContext.Add(orderdetails);// thêm dữ liệu tạo đơn hàng mới 
					_dataContext.SaveChanges();
				}
				HttpContext.Session.Remove("Cart");
				//send mail order when success 
				//var receive = userEmail;// email nhận sẽ là email của người đặt hàng 
				//var subject = "Đăng nhập trên thiết bị thành công";
				//var message = "Đặt hàng thành công, trải nhiệm dịch vụ nhé";

				//await _emailSender.SendEmailAsync(receive, subject, message);

				//Message checkout successfully 
				TempData["success"] = "Đơn hàng đã được tạo, vui lòng chờ duyệt đơn hàng nhé";
				return RedirectToAction("Index", "Cart");
			}
			return View();
		}
	}
}
