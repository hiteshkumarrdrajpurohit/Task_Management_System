using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskManagement_02.Models
{
    public class CategoryModel
    {
        [Key]
        [Required]
        [MaxLength(100)]
        public string Name { get; set; }=string.Empty;

        [MaxLength(250)]
        public string? Description { get; set; }

        public virtual ICollection<TaskModel>? Tasks { get; set; }
    }
}
