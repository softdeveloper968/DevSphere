using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MyDMVpro.Models
{
    public partial class FileData
    {
        [Key]
        public Guid Id { get; set; }
        public int Size { get; set; }
        public string FileName { get; set; }
        public byte[] Content { get; set; }
    }
}
