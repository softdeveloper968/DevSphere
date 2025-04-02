using MyDMVpro.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyDMVpro.Models;

[Table("GroupProfile")]
public class GroupProfile
{
    public GroupProfile()
    {
    }

    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid GroupProfileID { get; set; }
    public Guid GroupId { get; set; }

    private string _jrequest = null;

    [JsonPropertyBucket(DictionaryFieldName = nameof(Fields))]
    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public string JRequest
    {
        get
        {
            if (Fields == null)
                return null;
            return JsonHelper.SerializeDictionaryToJson(Fields);
        }
        set
        {
            _jrequest = value;
            if (string.IsNullOrWhiteSpace(value))
                Fields = null;
            else
            {
                Fields = JsonHelper.DeserializeJsonToDictionary(value);
            }

        }
    }

    [Column("GroupProfileTypeID")]
    public Guid? GroupProfileTypeID { get; set; }

    [StringLength(100)]
    public string GroupProfileName { get; set; }

    [Column("GroupProfileCategoryID")]
    public Guid? GroupProfileCategoryId { get; set; }

    public string Description { get; set; }
    public bool Active { get; set; }

    [ForeignKey(nameof(GroupProfileTypeID))]
    public virtual GroupProfileType GroupProfileType { get; set; }

    public virtual GroupProfileCategory GroupProfileCategory { get; set; }

    [ForeignKey(nameof(GroupId))]
    public virtual Groups Group { get; set; }

    // This is used to add/update values that
    // will then serialize into the jRequest value
    [NotMapped]
    public Dictionary<string, object> Fields
    {
        get;
        set;
    }
}

