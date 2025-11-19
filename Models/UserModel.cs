using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TaskManagement_02.Types;

namespace TaskManagement_02.Models
{
    public class UserModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
      
        [Display(Name = "Full Name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        
        public string Password { get; set; } = string.Empty;

        [Required]
        public Des Designation { get; set; }

        [Required]
        public Dept Department { get; set; }

        [Required]
        public RoleType Role { get; set; }= RoleType.User;
        public virtual ICollection<TaskModel>? Tasks { get; set; } 
    }
}
