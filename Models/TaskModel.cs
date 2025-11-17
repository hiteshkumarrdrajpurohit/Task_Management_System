using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TaskManagement_02.Types;

namespace TaskManagement_02.Models
{
    public class TaskModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int AssignedPersonId { get; set; }
        [ForeignKey(nameof(AssignedPersonId))]

        public virtual UserModel ? AssignedPerson {  get; set; }
        [Required,MaxLength(100)]
        public string Name {  get; set; }=string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }
        [Required]
        [DataType(DataType.Date)]
        public DateTime AssignedDate { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime SubmissionDate { get; set; }

        [Required]
        public Status TaskStatus { get; set; }=Status.Pending;


        [Required]
        [ForeignKey(nameof(Category))]
        [MaxLength(100)]
        public string CategoryName { get; set; } = string.Empty;

        public virtual CategoryModel? Category { get; set; }
    }
}
