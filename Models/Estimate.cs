using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ITWebManagement.Models
{
    public class Estimate
    {
        [Key]
        public int EstimateID { get; set; }

        [Required]
        public required string AssetCode { get; set; } // Thêm required để đảm bảo khởi tạo

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
        public required string Department { get; set; } // Thêm required để đảm bảo khởi tạo
    }
}