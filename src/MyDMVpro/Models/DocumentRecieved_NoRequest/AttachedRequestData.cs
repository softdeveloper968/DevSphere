using System;

namespace MyDMVpro.Models.DocumentsReceived_NoRequest
{
    public class AttachedRequestData
    {
        public DocumentReceivedViewModel DataToAttached { get; set; }
        public SelectedRecordModel OriginalData { get; set; }
        public Guid RequestId { get; set; }
    }

    public class SelectedRecordModel
    {
        public Guid? RequestId { get; set; }
        public Guid? GroupId { get; set; }
        public string? Vin { get; set; }
        public string? RequestNumber { get; set; }
    }
}
