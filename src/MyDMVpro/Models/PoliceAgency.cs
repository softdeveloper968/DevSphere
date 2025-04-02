using System;
using System.Collections.Generic;

namespace MyDMVpro.Models
{
    public partial class PoliceAgency
    {
        public int Id { get; set; }
        public string Agency { get; set; }
        public string Street { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Zip { get; set; }
        public string NameAndAddress { get; set; }
    }
}
