using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyDMVpro.Models.VendorViewModels
{
    public class TitlesShippedViewModel
    {
        public TitlesShippedViewModel()
        {
            Shipments = new List<Shipment>();
        }
        public List<Shipment> Shipments { get; set; }

        public dynamic VendorSettings { get; set; }
        public bool RenderAsPdf { get; set; }
    }
}
