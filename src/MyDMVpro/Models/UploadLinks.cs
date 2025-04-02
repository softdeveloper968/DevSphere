using System.ComponentModel.DataAnnotations;
using System;

namespace MyDMVpro.Models
{
    public class UploadLinks
    {
        [Key]
        public Guid Id { get; set; } // Unique Identifier

        [Required]
        public Guid RequestId { get; set; } // Associated request identifier
        public Guid AttachmentId { get; set; } // Type of attachment
        public Guid AttachmentTypeId { get; set; }
        public string Token { get; set; } 
        public bool IsDocumentUploaded { get; set; } = false; // Indicates if document is uploaded
        public bool IsUsed { get; set; } = false; // Indicates if the token is used
        public string CreatedBy { get; set; } // User who created the link
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow; // Timestamp of creation
        public DateTime ExpirationDate { get; set; } // Expiration date for the link
    }
}
