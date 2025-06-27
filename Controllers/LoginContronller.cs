using System;
using Microsoft.AspNetCore.Mvc;

namespace shopping_tutorial.Controllers
{
	public class LoginContronller:Controller
	{
        public IActionResult Index()
        {
            return View();
        }
       
	}
}

