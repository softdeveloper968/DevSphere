using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyDMVpro.Models
{
    public partial class AppFormFields
    {
        public AppFormFields()
        {
            AppFormSectionFields = new HashSet<AppFormSectionFields>();
        }

        public int FieldId { get; set; }
        public string FormId { get; set; }
        public int? SectionId { get; set; }
        public int? FieldSortOrder { get; set; }
        public string FieldLabel { get; set; }
        public string FieldDesc { get; set; }
        public int? FieldType { get; set; }
        public string FieldHelpLink { get; set; }
        public string FieldPopupText { get; set; }
        public string ExcelName { get; set; }
        public string AltExcelName { get; set; }
        public bool? IsRequired { get; set; }
        public bool? AllowDefault { get; set; }
        public bool? IsVisible { get; set; }
        public string RegExValidator { get; set; }
        public string RegExValMsg { get; set; }
        public bool? VendorOnlyEdit { get; set; }
        public bool? VendorOnlyVisible { get; set; }
        public bool? VisibleOnEdit { get; set; }
        public bool? VendorIsRequired { get; set; }

        public bool? IsPII { get; set; }

        [NotMapped]
        public bool IsReadOnly { get; set; }
        public ICollection<AppFormSectionFields> AppFormSectionFields { get; set; }
    }
}

// Using public class rather than enum to allow use
// without casting to int
// WHENEVER TYPES ARE ADDED
public static class FieldTypes
{
	public const int StatesDDL = 1;
	public const int MileageBrandDDL = 2;
	public const int OdometerDigitsDDL = 3;
    public const int PastDate = 4;
    public const int FutureDate = 5;
    public const int PoliceAgencyDDL = 6;
    public const int DmvDDL = 7;
    public const int OdometerCertification = 8;
    public const int DamageDiscloure = 9;
    public const int ZipCode = 10;
    public const int Phone = 11;
    public const int Date = 12;
    public const int VIN = 13;
    public const int Email = 14;
    public const int PowerTypeDDL = 15;
    public const int Oath = 16;
    public const int MileageCertification = 17;
    public const int VINs = 18;
    public const int Auction = 19;
    public const int NameAddressParse = 20;
    public const int ChoiceList = 21;
    public const int Calculated = 22;
    public const int SinglefileUpload = 98;
    public const int MultifileUpload = 99;
    public const int VehicleColorDDL = 100;
    public const int DollarValue = 101;
    public const int MultilineText = 102;
}