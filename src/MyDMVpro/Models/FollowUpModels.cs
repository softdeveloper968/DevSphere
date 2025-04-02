using System;
using System.Collections.Generic;

namespace MyDMVpro.Models
{
    public class FollowUpIdentifier
    {
        public Guid RequestId { get; set; }
        public int FollowUpId { get; set; }
    }
    public class FollowUp_Complete_Model
    {
        public DateTime CompletedDate { get; set; }
        public List<FollowUpIdentifier> FollowUpIDs { get; set; }
    }
    public class FollowUpTag_Delete_Model
    {
        public int TagId { get; set; }
        public string TagName { get; set; }
    }
    public class FollowUpTag_Edit_Model
    {
        public int TagId { get; set; }
        public string TagName { get; set; }
        public string TagDesc { get; set; }
        public string TagClass { get; set; }
    }
    public class FollowUpTag_New_Model
    {
        public string TagName { get; set; }
        public string TagDesc { get; set; }
        public string TagClass { get; set; }
    }

    public class FollowUp_Create_Model
    {
        public Guid? RequestId { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        public string Title { get; set; }
        public string StatusOfRecord { get; set; }
        public string NewNote { get; set; }
        public string NewRemark { get; set; }
        public List<string> Tags { get; set; }
        public List<int> TagIds { get; set; }
        public string ContactAction { get; set; }
        public List<string> Contacts { get; set; }
        public List<Guid> ContactIds { get; set; }
    }
    public class FollowUpTag
    {
        public int? TagId { get; set; }
        public string TagName { get; set; }
    }
    public class FollowUp_NotesHistoryModel : FollowUpIdentifier
    {
        public List<RequestNotes> CurrentNotes { get; set; } = new List<RequestNotes>();
    }
    public class FollowUp_Edit_Model : FollowUpIdentifier
    {
        public DateTime? DueDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        public bool? IsCompleted { get; set; }
        public string Title { get; set; }
        public string StatusOfRecord { get; set; }
        public string NewNote { get; set; }
        public string NewRemark { get; set; }
        public List<string> Tags { get; set; }
        public List<int> TagIds { get; set; }
        public string ContactAction { get; set; }
        public List<string> Contacts { get; set; }
        public List<Guid> ContactIds { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string LastModifiedBy { get; set; }
        public DateTime? LastModified { get; set; }
    }
    public class FollowUp_Edit_Model_Post : FollowUpIdentifier
    {
        public DateTime? DueDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        public string Title { get; set; }
        public string Notes { get; set; }
        public string NewNote { get; set; }
        public string NewRemark { get; set; }
        public string Tags { get; set; }
        public string TagIds { get; set; }
        public string ContactAction { get; set; }
        public string Contacts { get; set; }
        public string ContactIds { get; set; }
    }
    public class FollowUp_Delete_Model
    {
        public List<FollowUpIdentifier> FollowUpIDs { get; set; }
    }

}
