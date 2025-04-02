using System;
using System.Collections.Generic;

namespace MyDMVpro.Models
{
    public partial class RequestJvalues
    {
        public int Id { get; set; }
        public Guid RequestId { get; set; }
        public string JRequest { get; set; }
        public string Vin { get; set; }
        public string Last6Vin { get; set; }
        public string State { get; set; }
        public string VehicleMake { get; set; }
        public string VehicleYear { get; set; }
        public string Auction { get; set; }
        public string LienholderName { get; set; }
        public string Odometer { get; set; }
        public string ClientRef { get; set; }
        public string PaTitleNo { get; set; }
        public string PaLrbol { get; set; }
        public DateTime? LienExpDate { get; set; }
        public string Elt { get; set; }
        public string BorrowerName { get; set; }
        public string PersonSubmittingForm { get; set; }
        public DateTime? RepoDate { get; set; }
        public string DtLhName { get; set; }
        public string RtLh { get; set; }
    }
}
