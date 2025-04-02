using MyDMVpro.Models;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace MyDMVpro.ViewComponents
{
    public class MenuTaskViewComponent : ViewComponent
    {

        public MenuTaskViewComponent()
        {
        }

        public IViewComponentResult Invoke(string filter)
        {
            var messages = GetData();
            return View(messages);
        }

        private List<Message> GetData()
        {
            var messages = new List<Message>();
#if false
            messages.Add(new Message
            {
                Id = 1,
                ShortDesc = "Design some buttons",
                URLPath = "#",
                Percentage = 20,
            });
#endif
            return messages;
        }
    }
}
