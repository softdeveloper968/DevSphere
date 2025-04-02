using System.Collections.Generic;
using System;

namespace MyDMVpro.Models.DataReviewModels
{
    public class DataReviewFieldsModel
    {
        public Guid RequestCodeId { get; set; }
        public Guid RequestId { get; set; }
        public ICollection<RequestCodeFields> RequestCodeFields { get; set; }
        public RequestCode RequestCode { get; set; }
        public ProposedUpdates ProposedUpdates { get; set; }
        public string JRequest { get; set; }
        public string BeforeJRequest { get; set; }
    }
}
