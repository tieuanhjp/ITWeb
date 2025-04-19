using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ITWebManagement.Models
{
    public class Purchase
    {
        [Key]
        public int PurchaseID { get; set; }

        [Required]
        public required string AssetCode { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; }

        [Required]
        public DateTime Time { get; set; }

        [Required]
        public required string Department { get; set; }

        [Required]
        public required string SupplierID { get; set; }
    }
}