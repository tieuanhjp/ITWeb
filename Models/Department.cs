using System.ComponentModel.DataAnnotations;

namespace ITWebManagement.Models
{
    public class Department
    {
        [Key]
        public required string DepartmentCode { get; set; }

        [Required]
        public required string DepartmentName { get; set; }
    }
}