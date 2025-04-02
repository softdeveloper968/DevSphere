using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace MyDMVpro.Models.FormsViewModels;

public class MultiGroupProfileViewModel
{
    public List<GroupProfileViewModel> GroupProfileViewModels { get; set; }
    public List<SelectListItem> Groups { get; set; }

    public MultiGroupProfileViewModel(List<Groups> availableGroups)
    {
        GroupProfileViewModels = new();
        //foreach (var group in availableGroups)
        //    GroupProfileDataViewModels.Add(new GroupProfileDataViewModel());
        Groups = new List<SelectListItem>();
        InitSelectListItemsForGroups(availableGroups);
    }
    private void InitSelectListItemsForGroups(List<Groups> availableGroups)
    {
        Groups.Add(new SelectListItem("Select a group", "", true));
        foreach (var group in availableGroups)
            Groups.Add(new SelectListItem(group.GroupName, group.GroupId.ToString()));
    }
}
