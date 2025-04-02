using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyDMVpro.Models
{
    public class jsonRequestObject
    {
        public Guid? RequestId { get; set; }
        public Guid? FileUploadId { get; set; }
        public Guid? GroupId { get; set; }
        public Guid? UserId { get; set; }
        public Guid? VendorId { get; set; }
        public dynamic jRequest { get; set; }

        public bool? Signed { get; set; }
        public DateTime? DateSigned { get; set; }

        public bool CheckVIN { get; set; }

        public VINProperties VINProps { get; set; }

        public string Lienholder { get; set; }
        public string Username { get; set; }
        public string SubmittedBy { get; set; }

        public bool HasActiveChat { get; set; }
        public bool HasNewChat { get; set; }
        public bool NeedsChatReply { get; set; }
        public bool WaitingForUserReply { get; set; }
        public bool WaitingForVendorReply { get; set; }
        public bool HasAttachments { get; set; }
        public int? AttachmentStatus { get; set; }
    }

    public class VINProperties
    {
        public string VIN { get; set; }
        public string Make { get; set; }
        public string Model { get; set; }
        public string ModelYear { get; set; }
        public string Doors { get; set; }
        public string EngineCylinders { get; set; }
    }

    public static class AttachmentStatusEnum
    {
        public const int None = 0;
        public const int Pending = 1;
        public const int Approved = 2;
        public const int NoConditionsRequired = 3;
    };
}
