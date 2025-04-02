using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyDMVpro.Models.RequestsViewModels
{
    public class EditNotesModel
    {
        public Guid? RequestId { get; set; }
        public string Note { get; set; }
        public string Remark { get; set; }
        public string ClientRemarks { get; set; }
        public List<RequestNotes> CurrentNotes { get; set; } = new List<RequestNotes>();
        public string Code { get; set; }
        public bool SpecialBilling { get; set; }
        public bool StandaloneForm { get; set; }
    }
}
