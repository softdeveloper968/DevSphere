using System.Collections.Generic;

namespace MyDMVpro.Models
{
    public class AuditInstructionsModel
    {
        public string CollapseId { get; set; }
        public string Title { get; set; }
        public List<string> Instructions { get; set; }
    }
}
