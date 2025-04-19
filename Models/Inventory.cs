using System;
using System.ComponentModel.DataAnnotations;

namespace ITWebManagement.Models
{
    public class Inventory
    {
        [Key]
        public int InventoryID { get; set; }

        [Required]
        public required string AssetCode { get; set; }

        [Required]
        public required string Model { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Required]
        public DateTime Time { get; set; }

        [Required]
        public required string ImportedBy { get; set; }

        [Required]
        public required string TransactionType { get; set; } // "Import" hoặc "Export"
    }
}