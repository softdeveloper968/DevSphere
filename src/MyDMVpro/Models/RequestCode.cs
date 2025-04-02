using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyDMVpro.Models;

public partial class RequestCode
{
    public RequestCode()
    {
        Fields = new HashSet<RequestCodeFields>();
    }

    [Key]
    public Guid RequestCodeId { get; set; }

    public Guid RequestId { get; set; }

    public int TagId { get; set; }

    public string Note { get; set; }

    public string Resolution { get; set; }

    public bool Cleared { get; set; }
    public DateTime? ClearedDate { get; set; }
    public Guid? ClearedBy { get; set; }
    public string ClearedNote { get; set; }

    [ForeignKey(nameof(RequestId))]
    public Requests Request { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    [ForeignKey(nameof(TagId))]
    public Tag Tag { get; set; }

    [ForeignKey(nameof(ClearedBy))]
    public Users ClearedByUser { get; set; }

    [NotMapped]
    public string ClearedByUserName
    {
        get { return ClearedByUser?.DisplayName; }
    }

    [NotMapped]
    public string TagName { get { return Tag?.TagName; } }

    [NotMapped]
    public string TagNameForField { get; set; }

    public ICollection<RequestCodeFields> Fields { get; set; }
}

public partial class RequestCodeFields
{
    public RequestCodeFields()
    {

    }

    [Key]
    public int RequestCodeFieldId { get; set; }

    public Guid RequestCodeId { get; set; }

    public Guid FieldId { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    [ForeignKey(nameof(RequestCodeId))]
    public RequestCode RequestCode { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    [ForeignKey(nameof(FieldId))]
    public MasterFields Field { get; set; }

    /// <summary>
    ///  Used for json export only
    /// </summary>
    [NotMapped]
    public string ExcelName { get { return Field?.ExcelName; } }

    [NotMapped]
    public string Label { get { return Field?.DisplayName; } }
}
