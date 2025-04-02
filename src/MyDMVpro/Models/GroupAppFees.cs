using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MyDMVpro.Models;

public class GroupAppFees
{
    [Key]
    public int id { get; set; }
    
    public Guid? VendorId { get; set; }
    public Guid? GroupId { get; set; }
    public string Lienholder { get; set; }
    public string AppType { get; set; }
    public string State { get; set; }
    public Decimal? ServiceFee { get; set; }
    public Decimal? MailingFee { get; set; }

}
