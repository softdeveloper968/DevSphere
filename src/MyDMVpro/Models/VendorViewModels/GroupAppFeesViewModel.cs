using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.Net.Mail;

namespace MyDMVpro.Models.FormsViewModels
{
    public class GroupAppFeesViewModel
    {
        public string ListId { get; set; }
        public List<Dictionary<string, string>> ListData { get; set; }
        public List<Dictionary<string, object>> EditorFields { get; set; }
        public List<Dictionary<string, object>> ListColumns { get; set; }
        public string TableVarName { get; set; }
        public bool ShowColVisButton { get; set; } = true;
        public bool EnableFilter { get; set; } = true;
        public bool AddSelectDeselectButtons { get; set; } = true;
        public List<SelectListItem> Groups { get; set; }
        public bool ReadOnlyMode { get; set; } = true;
        public GroupAppFeesViewModel(List<Groups> availableGroups)
        {
            ListData = new();
            EditorFields = new();
            ListColumns = new();
            Groups = new List<SelectListItem>();
            InitSelectListItemsForGroups(availableGroups);
            
        }
        private void InitSelectListItemsForGroups(List<Groups> availableGroups)
        {
            Groups.Add(new SelectListItem("Select a group", "", true));
            foreach (var group in availableGroups)
                Groups.Add(new SelectListItem(group.GroupName, group.GroupId.ToString()));
        }
        private void InitColumns()
        {
            var dict = new Dictionary<string, object>
            {
                { "data", nameof(GroupAppFees.id) },
                { "visible", false },
                { "className", "" }
            };
            ListColumns.Add(dict);
            dict = new Dictionary<string, object>
            {
                { "data", nameof(GroupAppFees.VendorId) },
                { "visible", false },
                { "className", "" }
            };
            ListColumns.Add(dict);
            dict = new Dictionary<string, object>
            {
                { "data", nameof(GroupAppFees.GroupId) },
                { "visible", false },
                { "className", "" }
            };
            ListColumns.Add(dict);
            dict = new Dictionary<string, object>
            {
                { "data", nameof(GroupAppFees.Lienholder) },
                { "title", "Lienholder" },
                { "className", "editable" }
            };
            ListColumns.Add(dict);
            dict = new Dictionary<string, object>
            {
                { "data", nameof(GroupAppFees.AppType) },
                { "title", "App Type" },
                { "className", "editable" }
            };
            ListColumns.Add(dict);
            dict = new Dictionary<string, object>
            {
                { "data", nameof(GroupAppFees.State) },
                { "title", "State" },
                { "className", "editable" }
            };
            ListColumns.Add(dict);
            dict = new Dictionary<string, object>
            {
                { "data", nameof(GroupAppFees.ServiceFee) },
                { "title", "Service Fee" },
                { "className", "editable" }
            };
            ListColumns.Add(dict);
            dict = new Dictionary<string, object>
            {
                { "data", nameof(GroupAppFees.MailingFee) },
                { "title", "Mailing Fee" },
                { "className", "editable" }
            };
            ListColumns.Add(dict);
        }
    }
}

