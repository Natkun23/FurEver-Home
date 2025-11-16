using System;
using System.ComponentModel.DataAnnotations;

namespace FurEver_Home.Models
{
    public class User
    {
        public int UserId { get; set; }

        [Required]
        public string FullName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }

        public string Role { get; set; } // "Admin" or "Client"

        public string IDType { get; set; } // "National ID", "Passport", etc.

        public string IDImageUrl { get; set; }

        public string IDStatus { get; set; } // "Pending", "Verified", "Rejected"

        public DateTime DateRegistered { get; set; }

        public bool IsActive { get; set; }
    }
}