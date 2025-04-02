using System;
using System.Collections.Generic;

namespace MyDMVpro.Models
{
    public partial class ModelDifferenceAspects
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Xml { get; set; }
        public int? OwnerId { get; set; }

        public ModelDifferences Owner { get; set; }
    }
}
