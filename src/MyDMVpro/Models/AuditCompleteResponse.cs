using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyDMVpro.Models
{
    public class AuditCompleteResponse
    {
        [Key]
        public int Id { get; set; }
        public string AuditBatchId { get; set; }
        public string ProcessStageName { get; set; }
        public string Status { get; set; }
        public string Type { get; set; }
        public string State { get; set; }
        public string Vin { get; set; }
        public int RequestNo { get; set; }
        public string VehicleMake { get; set; }
        public string VehicleYear { get; set; }
        public string GroupName { get; set; }
        [Column("RT-LH")]
        public string RT_LH { get; set; }

        [Column("DT-LH-Name")]
        public string DT_LH_Name { get; set; }
        public DateTime? Eta { get; set; }
        public DateTime? DateToDmv { get; set; }
        [Column("LH-SToV")]
        public string LH_SToV { get; set; }
        public DateTime? RepoDate { get; set; }
        public string Odometer { get; set; }
        public DateTime? Created { get; set; }
        public DateTime? CompletedDate { get; set; }
        public DateTime? DueDate { get; set; }
        public string? Title { get; set; }
        public string? Outcome { get; set; }
        public string? Notes { get; set; }
        public string? Internal { get; set; }
        public string? AssignedUser { get; set; }
        public string? AuditType { get; set; }
        public DateTime? AuditstartTime { get; set; }
        public DateTime? AuditCompleteTime { get; set; }
        public string? AuditCompletedBy { get; set; }
        public string? AuditstartedBy { get; set; }
        public void ValidateCreatedDate()
        {
            DateTime minSqlDate = new DateTime(1753, 1, 1);

            if (Created < minSqlDate)
            {
                Created = minSqlDate;
            }
        }
    }

}
