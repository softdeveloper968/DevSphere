using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyDMVpro.Models;
using MyDMVpro.Controllers;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

namespace MyDMVpro.Models.AppFormModels
{
    public class ProcessStatus
    {
        [JsonPropertyName("RequestId")]
        public Guid RequestId { get; set; }
        public List<ProcessFields> EnabledFields { get; set; }

        [Display(Name = "Application Type", ShortName = "App Type")]
        public string AppType { get; set; }

        [Display(Name = "State", ShortName = "State")]
        public string AppTypeState { get; set; }

        [Display(Name="Date Signed", ShortName ="Date Signed")]
        [DataType(DataType.Date)]
        public DateTime? DateSigned { get; set; }

        [Display(Name = "Date Received")]
        [DataType(DataType.Date)]
        public DateTime? DateReceived { get; set; }

        [Display(Name = "ETA")]
        [DataType(DataType.Date)]
        public DateTime? ETA { get; set; }

        [Display(Name = "Code")]
        public string Code { get; set; }

        [Display(Name = "Date To Vendor")]
        [DataType(DataType.Date)]
        public DateTime? DateToVendor { get; set; }

        [Display(Name = "To Vendor Courier")]
        public string ToVendorCourier { get; set; }

        [Display(Name = "To Vendor Tracking")]
        public string ToVendorTracking { get; set; }

        [Display(Name = "Date Printed")]
        [DataType(DataType.Date)]
        public DateTime? DatePrinted { get; set; }

        [Display(Name = "Date To DMV")]
        [DataType(DataType.Date)]
        public DateTime? DateToDmv { get; set; }

        [Display(Name = "Date Title Issued")]
        [DataType(DataType.Date)]
        public DateTime? DateTitleIssued { get; set; }

        [Display(Name = "Date From DMV")]
        [DataType(DataType.Date)]
        public DateTime? DateFromDmv { get; set; }

        [Display(Name = "Date Shipped to LH")]
        [DataType(DataType.Date)]
        public DateTime? DateShipped { get; set; }

        [Display(Name = "Courier")]
        public string Courier { get; set; }

        [Display(Name = "Tracking#")]
        public string TrackingNumber { get; set; }

        [Display(Name = "To DMV Courier")]
        public string DmvCourier { get; set; }

        [Display(Name = "Return from DMV Tracking")]
        public string? DmvTrackingNumber { get; set; }

        [Display(Name = "To DMV Tracking")]
        public string ToDmvTrackingNumber { get; set; }

        //[Display(Name = "Processing Stage")]
        //public int ProcessStageID;

        //[Display(Name = "Status")]
        //public int StatusID;

        [Display(Name = "Date Rejected")]
        [DataType(DataType.Date)]
        public DateTime? RejectionDate { get; set; }

        [Display(Name = "Check#")]
        public int? CheckNumber { get; set; }

        [Display(Name = "LI Requested")]
        [DataType(DataType.Date)]
        public DateTime? LI_DateToDmv { get; set; }

        [Display(Name = "LI Received")]
        [DataType(DataType.Date)]
        public DateTime? LI_DateFromDmv { get; set; }

        [Display(Name = "LI Check#")]
        public int? LI_CheckNumber { get; set; }

        [Display(Name = "LI Courier")]
        public string LI_ToDmvCourier { get; set; }

        [Display(Name = "LI Tracking")]
        public string LI_ToDmvTracking { get; set; }
    }
    public class AppFormViewModel
    {
        public Guid? RequestId { get; set; }
        public string AppTypeState { get; set; }
        public ApplicationTypes AppForm { get; set; }
        public string VendorCode { get; set; }
        public Dictionary<string, string> Values { get; set; }
        public ProcessStatus ProcessStatus { get; set; }
        public bool HideSaveDefaults { get; set; }
        public bool VendorMode { get; set; }
        public bool HideSubmit { get; set; }
        public bool EditMode { get; set; }
        public bool ShowPII { get; set; }
        public Guid? GroupId { get; set; }
        public string GroupName { get; set; }
        public Guid? UserId { get; set; }
        public string UserDisplayName { get; set; }

        public bool IsOnBehalfOf { get; set; }

        public string Value(AppFormFields field)
        {
            if (field == null) return "";

            foreach (var key in new string[] { field.ExcelName, field.AltExcelName })
            {
                if (!string.IsNullOrEmpty(key))
                {
                    if (Values.ContainsKey(key))
                        return Values[key];
                }
            }
            return "";
        }
        public IEnumerable<SelectListItem> GetUSStates(string selected, bool isReadOnly = false)
        {
            if (isReadOnly)
            {
                return new SelectList(new string[] { selected }, selected);
            }
            return new SelectList(USState.GetAllStates(), "Abbrev", "Display", selected);
        }
        public IEnumerable<SelectListItem> USStates
        {
            get { return new SelectList(USState.GetAllStates(), "Abbrev", "Display"); }
        }
        public List<SelectListItem> GetMileageBrands(string selected, bool isReadOnly = false)
        {
            List<SelectListItem> list = new List<SelectListItem>();

            if (!string.IsNullOrEmpty(selected))
            {
                selected = selected.ToUpper();

                switch (selected)
                {
                    case "A":
                    case "ACTUAL":  selected = "A"; break;

                    case "E":
                    case "EXCEEDS": selected = "E"; break;

                    case "N":
                    case "NOT ACTUAL":selected = "N"; break;

                    case "X":
                    case "EXCEMPT": selected = "X"; break;
                    
                    default:
                        if (selected.ToLower().Contains("not"))
                            selected = "N";
                        else
                            selected = selected.Substring(0, 1);
                        break;
                }
            }
            else
            {
                // Default to ACTUAL
                selected = "";
            }
            if (!isReadOnly || selected == "") list.Add(new SelectListItem("", ""));
            if (!isReadOnly || selected == "A") list.Add(new SelectListItem("Actual", "A"));
            if (!isReadOnly || selected == "N") list.Add(new SelectListItem("NOT Actual", "N"));
            if (!isReadOnly || selected == "E") list.Add(new SelectListItem("Exceeds", "E"));
            if (!isReadOnly || selected == "X") list.Add(new SelectListItem("Exempt", "X"));

            foreach (var item in list)
            {
                if (item.Value == selected)
                {
                    item.Selected = true;
                }
            }
            return list;
        }
        public List<SelectListItem> MileageBrands
        {
            get { return GetMileageBrands(null); }
        }
        public List<SelectListItem> GetOdometerDigitsDDL(string selected, bool isReadOnly = false)
        {
            List<SelectListItem> list = new List<SelectListItem>();
            if (string.IsNullOrEmpty(selected))
                selected = "6";
            foreach (string val in new string[] { "", "5", "6", "7" })
            {
                if (!isReadOnly || selected == val)
                {
                    SelectListItem sli = new SelectListItem(val, val);
                    if (selected != null && selected == val)
                    {
                        sli.Selected = true;
                    }
                    list.Add(sli);
                }
            }
            return list;
        }
        public List<SelectListItem> OdometerDigitsDDL
        {
            get { return GetOdometerDigitsDDL(null); }
        }
        public List<SelectListItem> GetOdomForOtherVehicles(string selected)
        {
            List<SelectListItem> list = new List<SelectListItem>();
            foreach (string val in new string[] { "N/A", "Motorcycle", "Pickup", "Tow", "Trailer", "Truck", "Van" })
            {
                SelectListItem sli = new SelectListItem(val, val);
                if (selected != null && selected == val)
                {
                    sli.Selected = true;
                }
                list.Add(sli);
            }
            return list;
        }
        public List<SelectListItem> OdomForOtherVehicles
        {
            get { return GetOdomForOtherVehicles(null); }
        }
        public List<SelectListItem> GetPowerTypes(string selected)
        {
            List<SelectListItem> list = new List<SelectListItem>();
            list.Add(new SelectListItem("", ""));
            list.Add(new SelectListItem("Bio Diesel", "B"));
            list.Add(new SelectListItem("Diesel", "D"));
            list.Add(new SelectListItem("Diesel Hybrid", "DH"));
            list.Add(new SelectListItem("Electric", "L"));
            list.Add(new SelectListItem("Flex Fuel", "F"));
            list.Add(new SelectListItem("Gasoline", "G"));
            list.Add(new SelectListItem("Hydrogen Fuel Cell", "H"));
            list.Add(new SelectListItem("Plug-in Hybrid", "I"));
            list.Add(new SelectListItem("Natural Gas", "N"));
            list.Add(new SelectListItem("Propane", "P"));
            list.Add(new SelectListItem("Gas/Electric Hybrid", "Y"));

            foreach (SelectListItem item in list)
            {
                if (item != null && item.Value == selected)
                {
                    item.Selected = true;
                }
            }
            return list;
        }
        public List<SelectListItem> PowerTypes
        {
            get { return GetPowerTypes(null); }
        }
        public List<Dmvoffice> Dmvoffices { get; set; }
        public List<PoliceAgency> PoliceAgencies { get; set; }
    }
}
