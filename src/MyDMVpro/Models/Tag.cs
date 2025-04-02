using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyDMVpro.Models
{
    public partial class Tag
    {
        public Tag()
        {
        }

        public Guid VendorId { get; set; }
        public bool? Disabled { get; set; }

        public int TagId { get; set; }

        [StringLength(10)]
        public string TagType { get; set; }

        [StringLength(50)]
        public string TagName { get; set; }

        [StringLength(250)]
        public string TagDesc { get; set; }

        [StringLength(50)]
        public string TagClass { get; set; }

        public int? TagCategoryId { get; set; }

        [ForeignKey(nameof(TagCategoryId))]
        public TagCategory TagCategory { get; set; }

    }

    [Table("TagCategory")]
    public partial class TagCategory
    {
        public TagCategory()
        {
            //Tags = new HashSet<Tag>();
        }
        [Key]
        public int TagCategoryId { get; set; }

        [StringLength(50)]
        public string TagCategoryName { get; set; }

        // Reference not needed for our purposes
        //public virtual ICollection<Tag> Tags { get; }
    }
}
