using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyDMVpro.Models;

[Table("OrganizationContact")]
public class OrganizationContact
{
    public OrganizationContact()
    {

    }

    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid ContactId { get; set; }

    [StringLength(150)]
    public string ContactName { get; set; }

    [StringLength(100)]
    public string Email { get; set; }

    [DisplayName("Is Processing Contact")]
    public bool IsProcessingContact { get; set; }

    [StringLength(50)]
    public string PreferredShipper { get; set; }

    [StringLength(100)]
    public string AuthorityStates{ get; set; }

    [StringLength(50)]
    public string Phone { get; set; }

    [StringLength(100)]
    public string MailingAddress { get; set; }

    [StringLength(100)]
    public string MailingAttn { get; set; }

    [StringLength(100)]
    public string MailingCity { get; set; }

    [StringLength(2)]
    public string MailingState { get; set; }

    [StringLength(10)]
    public string MailingZip { get; set; }

    public string GeneralNotes { get; set; }

    public Guid? OrganizationId { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; }

    [ForeignKey(nameof(OrganizationId))]
    public Organization Organization { get; set; }

    [NotMapped]
    public string ContactTag
    {
        get
        {
            if (Organization == null)
            {
                return ContactName;
            }
            return $"{ContactName} ({Organization.OrganizationName})";
        }
    }
    [NotMapped]
    public string ContactTagDesc
    {
        get
        {
            if (Organization == null)
            {
                return ContactName;
            }
            return $"{ContactName} ({Organization.OrganizationName}) {Phone} / {Email}";
        }
    }
}
