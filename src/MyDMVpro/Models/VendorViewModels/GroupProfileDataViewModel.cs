using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
namespace MyDMVpro.Models.FormsViewModels
{
    public class GroupProfileDataViewModel
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
        public List<SelectListItem> Profiles { get; set; }
        public List<SelectListItem> ProfileTypes { get; set; }
        public GroupProfileDataViewModel(List<Groups> availableGroups)
        {
            ListData = new List<Dictionary<string, string>>();
            EditorFields = new List<Dictionary<string, object>>();
            ListColumns = new List<Dictionary<string, object>>();
            Groups = new List<SelectListItem>();
            Profiles = new List<SelectListItem>();
            ProfileTypes = new List<SelectListItem>();
            InitSelectListItemsForGroups(availableGroups);
            InitSelectListItemsForProfiles();
            InitSelectListItemsForProfileTypes();
        }
        private void InitSelectListItemsForGroups(List<Groups> availableGroups)
        {
            Groups.Add(new SelectListItem("Select a group", "", true));
            foreach (var group in availableGroups)
                Groups.Add(new SelectListItem(group.GroupName, group.GroupId.ToString()));
        }
        private void InitSelectListItemsForProfiles()
        {
            Profiles.Add(new SelectListItem("Select a profile", "", true));
        }
        public void AddNewProfile(string profileName, string profileValue)
        {
            Profiles.Add(new SelectListItem(profileName, profileValue));
        }
        private void InitSelectListItemsForProfileTypes()
        {
            ProfileTypes.Add(new SelectListItem("Select a profile type", "", true));
        }
    }
}

