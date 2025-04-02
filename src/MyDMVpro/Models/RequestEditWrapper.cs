using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace MyDMVpro.Models
{
    public class DateRangeAttribute : ValidationAttribute
    {
        private int _daysBefore;
        private int _daysAfterToday;

        public DateRangeAttribute(int daysBefore, int daysAfterToday)
        {
            _daysAfterToday = daysAfterToday;
            _daysBefore = daysBefore;
        }
        public override bool IsValid(object value)// Return a boolean value: true == IsValid, false != IsValid
        {
            DateTime d = Convert.ToDateTime(value);

            if (d < DateTime.Today.AddDays(0 - _daysBefore))
                return false;
            if (d > DateTime.Today.AddDays(_daysAfterToday))
                return false;
            return true;
        }
    }
    public class NoticesView
    {
        public NoticesView()
        {
        }
        public RequestEditWrapper Request { get; set; }
        public IEnumerable<PoliceAgency> PoliceAgencies { get; set; }
        public IEnumerable<Dmvoffice> Dmvoffices { get; set; }
    }
    public class RequestEditWrapper
    {
        public RequestEditWrapper()
        {
        }
        public Guid RequestId { get; set; }

        //***************************************************
        //** NOTIFICATIONS
        //***************************************************

        [Display(Name = "DMV District Office Notified")]
        public string DmvDistrictOfficeNotified { get; set; }

        [Display(Name = "DMV Notification Date")]
        [DataType(DataType.Date)]
        public DateTime? DmvNotificationDate { get; set; }

        [Display(Name = "Other Notification Name")]
        public string OtherNotifiedName { get; set; }

        [Display(Name = "Other Notification Date")]
        [DataType(DataType.Date)]
        public DateTime? OtherNotifiedDate { get; set; }

        [Display(Name = "Police Agency Notified")]
        public string PoliceAgencyNotified { get; set; }

        [Display(Name = "Police Notification Date")]
        [DataType(DataType.Date)]
        public DateTime? PoliceNotificationDate { get; set; }

        [Display(Name = "State")]
        [StringLength(3, MinimumLength = 2)]
        public string State { get; set; }

        [Display(Name = "Auctioneer")]
        public string Auctioneer { get; set; }

        //***************************************************
        //** BORROWER
        //***************************************************
        [Display(Name = "Borrower Name", GroupName = "Borrower")]
        public string BorrowerName { get; set; }

        [Display(Name = "Borrower Address", GroupName ="Borrower")]
        public string BorrowerAddress { get; set; }

        [Display(Name = "Borrower City", GroupName = "Borrower")]
        public string BorrowerCity { get; set; }

        [Display(Name = "Borrower State", GroupName = "Borrower")]
        public string BorrowerState { get; set; }

        [Display(Name = "Borrower Zip", GroupName = "Borrower")]
        public string BorrowerZip { get; set; }

        [Display(Name = "Borrower Last Reg", GroupName = "Borrower")]
        public string BorrowerLastReg { get; set; }

        [Display(Name = "State Titled")]
        public string StateTitled { get; set; }

        [Display(Name = "Security Contract Date")]
        [DateRange(60, 0)]
        [DataType(DataType.Date)]
        public DateTime? SecurityContractDate { get; set; }


        [Display(Name = "Client Id Number")]
        public string ClientIdNumber { get; set; }

        [Display(Name = "Client Ref")]
        public string ClientRef { get; set; }

        //***************************************************
        //** LIENHOLDER
        //***************************************************
        [Display(Name = "Lien Holder Name", GroupName = "Lien Holder")]
        public string LienHolderName { get; set; }

        [Display(Name = "Lien Holder Address", GroupName = "Lien Holder")]
        public string LienHolderAddress { get; set; }

        [Display(Name = "Lien Holder City", GroupName = "Lien Holder")]
        public string LienHolderCity { get; set; }

        [Display(Name = "Lien Holder State", GroupName = "Lien Holder")]
        public string LienHolderState { get; set; }

        [Display(Name = "Lien Holder County", GroupName = "Lien Holder")]
        public string LienHolderCounty { get; set; }



        [Display(Name = "Lien Holder Legal Address", GroupName = "Lien Holder")]
        public string LienHolderLA { get; set; }

        [Display(Name = "Lien Holder Legal City", GroupName = "Lien Holder")]
        public string LienHolderLC { get; set; }

        [Display(Name = "Lien Holder Legal County", GroupName = "Lien Holder")]
        public string LienHolderLegalCounty { get; set; }

        [Display(Name = "Lien Holder Legal Zip", GroupName = "Lien Holder")]
        public string LienHolderLegalZip { get; set; }

        [Display(Name = "Lien Holder Legal State", GroupName = "Lien Holder")]
        public string LienHolderLS { get; set; }

        [Display(Name = "Lien Holder Phone", GroupName = "Lien Holder")]
        public string LienHolderPhone { get; set; }

        [Display(Name = "Lien Holder Zip", GroupName = "Lien Holder")]
        public string LienHolderZip { get; set; }

        [Display(Name = "Pa Title Number")]
        public string PaTitleNumber { get; set; }

        [Display(Name = "Person Submitting Form")]
        public string PersonSubmittingForm { get; set; }

        [Display(Name = "Repo Date")]
        [DateRange(60, 0)]
        [DataType(DataType.Date)]
        public DateTime? RepoDate { get; set; }

        [Display(Name = "Service Code")]
        public string ServiceCode { get; set; }

        [Display(Name = "Service Company Name")]
        public string ServiceCompanyName { get; set; }

        [Display(Name = "VA Title Number")]
        public string VATitleNumber { get; set; }

        [Display(Name = "Vehicle Owner Notification Date")]
        [DateRange(60, 0)]
        [DataType(DataType.Date)]
        public DateTime? VehicleOwnerNotificationDate { get; set; }


        //***************************************************
        //** VEHICLE 
        //***************************************************

        [Display(Name = "VIN", GroupName = "Vehicle")]
        [StringLength(17, MinimumLength = 17)]
        public string VehicleVin { get; set; }

        [RegularExpression(@"^(19|20)\d{2}$")]
        [Display(Name = "Year", GroupName = "Vehicle")]
        public string VehicleYear { get; set; }

        [Display(Name = "Make", GroupName = "Vehicle")]
        public string VehicleMake { get; set; }

        [Display(Name = "Color", GroupName = "Vehicle")]
        public string VehicleColor { get; set; }

        [Display(Name = "Body Type", GroupName ="Vehicle")]
        public string VehicleBodyType { get; set; }

        [Display(Name = "Doors", GroupName = "Vehicle")]
        public string VehicleDoors { get; set; }

        [Display(Name = "Power", GroupName = "Vehicle")]
        public string VehiclePower { get; set; }

        [RegularExpression(@"^\d{2}$")]
        [Display(Name = "Cylinders", GroupName = "Vehicle")]
        public string VehicleCylinders { get; set; }

        [Display(Name = "Unladen Weight", GroupName = "Vehicle")]
        public string VehicleUnladenWeight { get; set; }

        [Display(Name = "Max Gross Weight", GroupName = "Vehicle")]
        public string VehicleMaxGrossWeight { get; set; }

        [Display(Name = "Odometer", GroupName = "Vehicle")]
        public string VehicleOdometer { get; set; }

        [Display(Name = "Mileage Brand", GroupName = "Vehicle")]
        public string VehicleMileageBrand { get; set; }

        [RegularExpression(@"^(5|6|7)$")]
        [Display(Name = "Odometer Numbers", GroupName = "Vehicle")]
        public string VehicleOdometerNumbers { get; set; }


        //***************************************************
        //** OTHER
        //***************************************************
        [Display(Name = "Legal Sale Date")]
        [DateRange(60, 0)]
        [DataType(DataType.Date)]
        public DateTime? LegalSaleDate { get; set; }

    }

}
