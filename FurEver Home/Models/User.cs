using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FurEver_Home.Models
{
    [Table("users")]
    public class User
    {
        [Key]
        [Column("user_id")]
        public int UserId { get; set; }

        [Required]
        [Column("full_name")]
        [StringLength(255)]
        public string FullName { get; set; }

        [Required]
        [Column("email")]
        [StringLength(100)]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [Column("password")]
        [StringLength(255)]
        public string Password { get; set; }

        [Column("role")]
        [StringLength(50)]
        public string Role { get; set; } = "Client";

        [Column("id_type")]
        [StringLength(100)]
        public string IDType { get; set; }

        [Column("id_image_url")]
        [StringLength(500)]
        public string IDImageUrl { get; set; }

        [Column("id_status")]
        [StringLength(50)]
        public string IDStatus { get; set; } = "Pending";

        [Column("phone_number")]
        [StringLength(20)]
        public string PhoneNumber { get; set; }

        [Column("address")]
        public string Address { get; set; }

        [Column("age")]
        public int? Age { get; set; }

        // PASSWORD RESET PROPERTIES - ADD THESE
        [Column("reset_token")]
        [StringLength(255)]
        public string ResetToken { get; set; }

        [Column("reset_token_expiry")]
        public DateTime? ResetTokenExpiry { get; set; }

        [Column("date_registered")]
        public DateTime DateRegistered { get; set; } = DateTime.Now;

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}