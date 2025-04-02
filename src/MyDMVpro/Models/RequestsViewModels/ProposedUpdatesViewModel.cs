using System;

namespace MyDMVpro.Models.RequestsViewModels
{
    public class ProposedUpdatesViewModel
    {
        public ProposedUpdatesViewModel() { }

        public Guid RequestId { get; set; }
        public Guid FieldId { get; set; }
        public Guid RequestCodeId { get; set; }
        public string NewValue { get; set; }
        public string NewNote { get; set; } 


    }
}
