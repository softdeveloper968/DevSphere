using System;
using System.Collections.Generic;

namespace MyDMVpro.Models
{
    public partial class ModelDifferences
    {
        public ModelDifferences()
        {
            ModelDifferenceAspects = new HashSet<ModelDifferenceAspects>();
        }

        public int Id { get; set; }
        public string UserId { get; set; }
        public string ContextId { get; set; }
        public int Version { get; set; }

        public ICollection<ModelDifferenceAspects> ModelDifferenceAspects { get; set; }
    }
}
