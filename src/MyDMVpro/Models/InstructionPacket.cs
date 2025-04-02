using System.Collections.Generic;
using System;

namespace MyDMVpro.Models
{
    public class InstructionPacketPrintRequest
    {
        public Dictionary<Guid, Guid> Requests { get; set; } = new();

        public Guid? VendorId { get; set; }
        public Guid? GroupId { get; set; }
    }

    public class InstructionPacketResponse
    {
        public string filename { get; set; }
        public byte[] file { get; set; }
    }
}
