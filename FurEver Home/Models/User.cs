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

        // PASSWORD RESET PROPERTIES
        [Column("reset_token")]
        [StringLength(255)]
        public string ResetToken { get; set; }

        [Column("reset_token_expiry")]
        public DateTime? ResetTokenExpiry { get; set; }

        // OTP PROPERTIES
        [Column("otp_code")]
        [StringLength(6)]
        public string OtpCode { get; set; }

        [Column("otp_expiry")]
        public DateTime? OtpExpiry { get; set; }

        [Column("otp_attempts")]
        public int OtpAttempts { get; set; } = 0;

        // ✅ NEW: TWO-FACTOR AUTHENTICATION PROPERTIES
        [Column("two_factor_enabled")]
        public bool TwoFactorEnabled { get; set; } = false;

        [Column("two_factor_secret_key")]
        [StringLength(100)]
        public string TwoFactorSecretKey { get; set; }

        [Column("date_registered")]
        public DateTime DateRegistered { get; set; } = DateTime.Now;

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        [Column("profile_picture_url")]
        [StringLength(500)]
        public string ProfilePictureUrl { get; set; }
    }
}