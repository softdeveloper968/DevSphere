using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyDMVpro.Models;

public class RequestTagIdentifier
{
    public Guid RequestId { get; set; }
    public int RequestCodeId { get; set; }
}
public class RequestTag_Delete_Model
{
    public int TagId { get; set; }
    public string TagType { get; set; }
}
public class RequestTag_Edit_Model
{
    public int TagId { get; set; }
    public string TagName { get; set; }
    public string TagDesc { get; set; }
    public string TagClass { get; set; }
    public string TagType { get; set; }
    public int? TagCategoryId { get; set; }
    public bool Disabled { get; set; }
}
public class RequestTag_New_Model
{
    public string TagName { get; set; }
    public string TagDesc { get; set; }
    public string TagType { get; set; }
    public string TagClass { get; set; }
    public int? TagCategoryId { get; set; }
    public bool Disabled { get; set; }
}

public class RequestTag
{
    public int? TagId { get; set; }
    public string TagName { get; set; }
}
