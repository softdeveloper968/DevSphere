using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace MyDMVpro.Models;

[Table("Organization")]
public partial class Organization
{
    public Organization()
    {
        //OrganizationContacts = new HashSet<OrganizationContact>();
    }

    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid OrganizationId { get; set; }

    [StringLength(150)]
    public string OrganizationName { get; set; }

    [StringLength(100)]
    public string MailingAttn { get; set; }

    [StringLength(100)]
    public string MailingAddress { get; set; }

    [StringLength(75)]
    public string MailingCity { get; set; }

    [StringLength(2)]
    public string MailingState { get; set; }

    [StringLength(10)]
    public string MailingZip { get; set; }

    [StringLength(200)]
    public string Website { get; set; }

    public string GeneralNotes { get; set; }

    public bool? IsProcessingVendor { get; set; }

    public string PreferredShipper { get; set; }

    public string AuthorityStates
    {
        get
        {
            return AuthorityStatesList != null ? String.Join(",", AuthorityStatesList) : null;
        }
        set
        {
            value = value?.Trim();
            AuthorityStatesList = !String.IsNullOrWhiteSpace(value)
                                    ? value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList()
                                    : new List<string>();
        }
    }

    [NotMapped]
    public List<string> AuthorityStatesList { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; }

    //public virtual ICollection<OrganizationContact> OrganizationContacts { get; set; }
}
