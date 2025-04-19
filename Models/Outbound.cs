using System;
using System.ComponentModel.DataAnnotations;

namespace ITWebManagement.Models
{
    public class Outbound
    {
        [Key]
        public int OutboundID { get; set; }

        [Required]
        public required string AssetCode { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Required]
        public DateTime Time { get; set; }

        [Required]
        public required string ExportedBy { get; set; }
    }
}