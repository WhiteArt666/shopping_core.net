using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using shopping_tutorial.Repository;

namespace Shopping_Tutorial.Repository.Components
{
	public class FooterViewComponent : ViewComponent
	{
		private readonly DataContext _dataContext;
		public FooterViewComponent(DataContext context)
		{
			_dataContext = context;
		}
        public async Task<IViewComponentResult> InvokeAsync()
        {
            var contact = await _dataContext.Contact.FirstOrDefaultAsync();
            if (contact == null)
            {
                return Content(""); // hoặc return View("Empty"); nếu bạn có view riêng
            }
            return View(contact);
        }

    }
}