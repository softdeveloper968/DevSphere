using MyDMVpro.Models;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace MyDMVpro.ViewComponents
{
    public class MenuNotificationViewComponent : ViewComponent
    {

        public MenuNotificationViewComponent()
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
                FontAwesomeIcon = "fa fa-users text-aqua",
                ShortDesc = "You have 5 new requests.",
                URLPath = "#",
            });
#endif
            return messages;
        }
    }
}
