﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace shopping_tutorial.Repository.Components
{
    public class BrandsViewComponent:ViewComponent
    {
        private readonly DataContext _dataContext;
        public BrandsViewComponent(DataContext context)
        {
            _dataContext = context;
        }
        public async Task<IViewComponentResult> InvokeAsync() => View( await _dataContext.Brands.ToListAsync());
    }
}
