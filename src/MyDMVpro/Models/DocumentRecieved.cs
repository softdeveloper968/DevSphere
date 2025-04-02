using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace MyDMVpro.Models
{
    public class DocumentReceived
    {

        [Key]
        public Guid DocumentReceivedID { get; set; } = Guid.NewGuid();

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public DateTime DateReceived { get; set; } = DateTime.Now;

        [Required]
        [StringLength(50)]
        [JsonProperty("VIN")]
        public string Vin { get; set; }

        [Required]
        public Guid GroupId { get; set; }
        public Guid? VendorId { get; set; }

        public string Notes { get; set; }

        public byte[] Document { get; set; }

        public string DocumentName { get; set; }

        public bool IsMatched { get; set; } = false;
        public bool IsShipped { get; set; } = false;
        public DateTime? DateShipped { get; set; } 

        public string ShippedNote { get; set; }
        public string TrackingNumber { get; set; }
        public bool IsShow { get; set; }
        public Guid AttachmentTypeId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        public virtual Groups Group { get; set; }
    }

}
