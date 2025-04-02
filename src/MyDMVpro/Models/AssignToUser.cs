using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyDMVpro.Models
{
    public class AssignToUser
    {
        public Guid GroupId { get; set;}
        public Guid? UserId { get; set; }
    }
}
