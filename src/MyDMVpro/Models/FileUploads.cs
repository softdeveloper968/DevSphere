using System;
using System.Collections.Generic;

namespace MyDMVpro.Models
{
    public partial class FileUploads
    {
        public int Id { get; set; }
        public Guid FileUploadId { get; set; }
        public string ReferenceNo { get; set; }
        public string Filename { get; set; }
        public DateTime DateUploaded { get; set; }
        public Guid UserId { get; set; }
        public byte[] FileImage { get; set; }
        public string FileUrl { get; set; }
        public Guid? AgentUploaded { get; set; }

        public Users User { get; set; }
    }
}
