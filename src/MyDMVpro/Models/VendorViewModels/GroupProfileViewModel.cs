using Microsoft.AspNetCore.Mvc.Rendering;
using MyDMVpro.Common;
using System.Collections.Generic;

namespace MyDMVpro.Models.FormsViewModels;

public class GroupProfileViewModel
{
    public string ProfileCategoryName { get; set; }
    public string GroupProfileCategoryId { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }

    public string ListId { get; set; }
    public List<Dictionary<string, string>> ListData { get; set; }
    public List<Dictionary<string, object>> EditorFields { get; set; }
    public List<Dictionary<string, object>> ListColumns { get; set; }
    public List<ProfileColumnInfo> ProfileColumns { get; set; }
    public string TableVarName { get; set; }
    public bool ShowColVisButton { get; set; } = true;
    public bool EnableFilter { get; set; } = true;
    public bool AddSelectDeselectButtons { get; set; } = true;
    public List<SelectListItem> Profiles { get; set; }
    public List<SelectListItem> ProfileCategories { get; set; }
    public GroupProfileViewModel()
    {
        ListData = new();
        EditorFields = new();
        ProfileColumns = new();
        Profiles = new();
        ProfileCategories = new();
        InitSelectListItemsForProfiles();
        InitSelectListItemsForProfileCategories();
    }
    private void InitSelectListItemsForProfiles()
    {
        Profiles.Add(new SelectListItem("Select a profile", "", true));
    }
    public void AddNewProfile(string profileName, string profileValue)
    {
        Profiles.Add(new SelectListItem(profileName, profileValue));
    }
    private void InitSelectListItemsForProfileCategories()
    {
        ProfileCategories.Add(new SelectListItem("Select a profile type", "", true));
    }
}
