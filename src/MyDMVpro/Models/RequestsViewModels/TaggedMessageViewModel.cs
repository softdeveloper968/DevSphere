using System.Collections.Generic;
using System;

namespace MyDMVpro.Models.RequestsViewModels
{
    public class TaggedMessageViewModel
    {
        public string ClientRemarks { get; set; }
        public List<TaggedUser> Users { get; set; }
        public Guid RequestId { get; set; }

    }

    public class TaggedUser
    {
        public Guid TaggedUserId { get; set; }
    }
}
