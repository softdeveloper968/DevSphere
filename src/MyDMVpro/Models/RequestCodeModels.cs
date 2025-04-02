using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyDMVpro.Models;

public class RequestCodeIdentifier
{
    public Guid? RequestCodeId { get; set; }
}
public class RequestCode_Clear_Model
{
    public Guid? RequestId { get; set; }
    public Guid? RequestCodeId { get; set; }
    public string ClearedNote { get; set; }
    public bool OverrideChecks { get; set; }
}

public class RequestCode_Create_Model
{
    public Guid? RequestId { get; set; }
    public string Note { get; set; }
    public string Resolution { get; set; }
    public int TagId { get; set; }
    public List<Guid> FieldIds { get; set; }
}

public class RequestCodeTag
{
    public int? TagId { get; set; }
    public string TagName { get; set; }
}

public class RequestCode_Edit_Model_GET : RequestCodeIdentifier
{
    public string Note { get; set; }
    public string Resolution { get; set; }
    public DateTime? ClearedDate { get; set; }
    public string ClearedBy { get; set; }
    public int TagId { get; set; }
    public List<Guid> FieldIds { get; set; }
}

public class RequestCode_Edit_Model_POST : RequestCodeIdentifier
{
    public string Note { get; set; }
    public string Resolution { get; set; }
    public int TagId { get; set; }
    public List<Guid> FieldIds { get; set; }
}

public class RequestCode_Delete_Model
{
    public List<RequestCodeIdentifier> RequestCodeIDs { get; set; }
}
