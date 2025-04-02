using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace MyDMVpro.Models
{
    public partial class RequestAttachments
    {
        public RequestAttachments()
        {
            RequestAttachmentReviewHistories = new Collection<RequestAttachmentReviewHistory>();
        }
        public int Id { get; set; }

        [Key]
        public Guid AttachmentId { get; set; }

        public Guid? RequestId { get; set; }

        public string Filename { get; set; }

        public byte[] Image { get; set; }

        public DateTime? DateAdded { get; set; }

        public Guid? UploadedBy { get; set; }

        public string Description { get; set; }

        public bool? ContainsPII { get; set; }

        public bool? HardcopyReceived { get; set; }

        public Guid? AttachmentTypeId { get; set; }

        public byte[] MD5Hash { get; set; }

        public Guid? ReviewBy { get; set; }

        public DateTime? ReviewDate { get; set; }

        public bool? Approved { get; set; }

        [ForeignKey(nameof(AttachmentTypeId))]
        public MdpAttachmentTypes AttachmentType { get; set; }

        //public Guid? AppTypeAttachmentTypeId { get; set; }
        //[ForeignKey(nameof(AppTypeAttachmentTypeId))]
        //public MdpAppTypeAttachmentTypes AppTypeAttachmentType { get; set; }

        [ForeignKey(nameof(RequestId))]
        public Requests Request { get; set; }

        [ForeignKey(nameof(UploadedBy))]
        public Users UploadedByNavigation { get; set; }

        [NotMapped]
        [JsonProperty(PropertyName = "typename")]
        [JsonPropertyName("typename")]
        public string AttachmentTypeName { get; set; }

        public ICollection<RequestAttachmentReviewHistory> RequestAttachmentReviewHistories { get; set; }
    }
}
