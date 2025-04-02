using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace MyDMVpro.Common
{
    public class GetRequestStatusV2Params
    {
        public Guid? VendorId { get; set; }
        public Guid? GroupId { get; set; }
        public Guid? UserId { get; set; }
        public List<string> AppTypes { get; set; } = new List<string>();
        public List<string> NotAppTypes { get; set; } = new List<string>();
        public int? ArchiveDays { get; set; }
        public bool? IsIncludeArchive { get; set; }
        public bool? IsActive { get; set; }
        public bool? IsDeletePending { get; set; }
        public bool? IsActiveOrWithinCutoffForPALC { get; set; }
        public List<int> ProcessStages { get; set; } = new List<int>();
        public List<int> Statuses { get; set; } = new List<int>();
        public bool? IsPALC { get; set; }
        public bool? IsActiveHoldOrWithinCutoff { get; set; }
        public bool? IsActiveOrWithinCutoff { get; set; }
        public bool? IsActiveOrComplete { get; set; }
        public bool? IsShipped { get; set; }
        public bool? IsInvoiced { get; set; }
        public bool? IsActiveChat { get; set; }
        public bool? IsSentToDmv { get; set; }
        public bool? IsStillAtDmv { get; set; }
        public bool? IsBillable { get; set; }
        public bool? IsReceivedFromDMV { get; set; }
        public bool? IsTitleScanned { get; set; }

        public Filter Filter { get; set; }
        public int? RowOffset { get; set; }
        public int? ReturnRowCount { get; set; }
        public List<string> DropDowns { get; set; }
        public bool? IsDebugMode { get; set; }
        /// <summary>
        /// Both|Grid|DropDown
        /// </summary>
        public string ReturnMode { get; set; }
        public List<SortItem> Sort { get; set; } = new List<SortItem>();

        [System.Text.Json.Serialization.JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public int ResultingTotalRowCount { get; set; }
    }

    public class FilterValue
    {
        public string FieldType { get; set; }
        public string FieldName { get; set; }
        public string FieldValue { get; set; }
    }

    public class Filter
    {
        public List<FilterValue> ValueFilter { get; set; } = new List<FilterValue>();
    }

    public class SortItem
    {
        public string ColumnName { get; set; }
        public string Direction { get; set; }
    }
}
