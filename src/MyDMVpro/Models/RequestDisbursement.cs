using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MyDMVpro.Models;

public partial class RequestDisbursement
{
    public RequestDisbursement()
    {
    }
    [Key]
    public int id { get; set; }

    public Guid RequestId { get; set; }

    public Decimal? Amount { get; set; }

    public DateTime Date { get; set; }

    [StringLength(100)]
    public string Description { get; set; }

    public int? CheckNumber { get; set; }

    [StringLength(100)]
    public string PayToName { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime? CreatedDate { get; set; }

    public Guid? ModifiedBy { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public bool? Voided { get; set; }

    public Guid? VoidedBy { get; set; }

    public DateTime? VoidedDate { get; set; }

    public bool? Cleared { get; set; }

    public Guid? ClearedBy { get; set; }

    [ForeignKey(nameof(ClearedBy))]
    public Users ClearedByUser { get; set; }

    public DateTime? ClearedDate { get; set; }

    [ForeignKey(nameof(RequestId))]
    public virtual Requests Request { get; set; }

    [ForeignKey(nameof(VoidedBy))]
    public Users VoidedByUser { get; set; }

    [NotMapped]
    public bool UseExisting { get; set; }
}
