using MyDMVpro.Models;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace MyDMVpro.ViewComponents
{
    public class MenuUserViewComponent : ViewComponent
    {

        public MenuUserViewComponent()
        {
        }

        public IViewComponentResult Invoke(string filter)
        {
            return View();
        }
    }
}
