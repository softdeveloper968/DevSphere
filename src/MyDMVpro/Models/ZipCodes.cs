using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace MyDMVpro.Models
{
    public partial class ZipCodes
    {
        [JsonIgnore]
        public int Id { get; set; }
        public string ZipCode { get; set; }
        public string City { get; set; }
        public string StateName { get; set; }
        public string StateAbbr { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public string County { get; set; }
    }
}
