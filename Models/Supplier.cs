using System.ComponentModel.DataAnnotations;

namespace ITWebManagement.Models
{
    public class Supplier
    {
        [Key]
        public required string SupplierID { get; set; }

        [Required]
        public required string SupplierName { get; set; }
    }
}