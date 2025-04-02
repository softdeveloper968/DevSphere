using System;
using System.Collections.Generic;

namespace MyDMVpro.Models
{
    public partial class Repossession
    {
        public int Id { get; set; }
        public string Tracking { get; set; }
        public string Courier { get; set; }
        public string MissingItems { get; set; }
        public string DateSubmittedtoDmv { get; set; }
        public string DateReceived { get; set; }
        public string FinishDate { get; set; }
        public string ServiceCode { get; set; }
        public string Vin { get; set; }
        public string DateUpdated { get; set; }
        public string Status { get; set; }
    }
}
