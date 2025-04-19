using System;
using System.ComponentModel.DataAnnotations;

namespace ITWebManagement.Models
{
    public class Device
    {
        [Key]
        public required string AssetCode { get; set; }

        [Required]
        public required string Model { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [Required]
        public decimal Depreciation { get; set; }

        [Required]
        public required string Department { get; set; }

        [Required]
        public required string Status { get; set; }
    }
}