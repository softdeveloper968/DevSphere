using System;
using System.Collections.Generic;

namespace MyDMVpro.Models
{
    public partial class Dmvoffice
    {
        public int CommissionerId { get; set; }
        public string Commissioner { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Zip { get; set; }
        public string NameAndAddress { get; set; }
    }
}
