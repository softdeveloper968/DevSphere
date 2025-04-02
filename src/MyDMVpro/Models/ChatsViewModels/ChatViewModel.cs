using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyDMVpro.Models.ChatsViewModels
{
    public class ChatViewModel
    {
        public Guid? RequestId;
        public string VehicleVin;
        public string VehicleYear;
        public string VehicleMake;
        public IEnumerable<MyDMVpro.Models.Chats> Chats;
        public string ErrorMessage;
        public int PageLength;
        public string LengthMenu;
        public string KeyNavigation;
        public bool HasUnreadChats;
        public bool EnableKeyNavigation;

        public ChatViewModel()
        {
            InitPageLength();
        }
        public void InitPageLength()
        {
            LengthMenu = "[ 10, 25, 50, 100, 200, 500, 1000 ]";
            /*            
            33 // page up (previous page)
			34 // page down (next page)
			35 // end (end of current page)
			36 // home (start of current page)
            38 // Up
            40 // Down
            32 // space
            65 // a
            67 // c
            69 // e
            */
            KeyNavigation = "keys: { keys: [33,34,35,36,38, 40, 32, 65,67,69 ] },";
            PageLength = 25;
        }
    }
}
