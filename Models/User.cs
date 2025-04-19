using System.ComponentModel.DataAnnotations;

namespace ITWebManagement.Models
{
    public class User
    {
        [Key]
        public required string Username { get; set; }

        [Required]
        public required string Password { get; set; }

        [Required]
        public required string Role { get; set; }
    }
}