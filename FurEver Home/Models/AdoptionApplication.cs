using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FurEver_Home.Models
{
    [Table("adoption_applications")]
    public class AdoptionApplication
    {
        [Key]
        [Column("application_id")]
        public int ApplicationId { get; set; }

        [Required]
        [Column("user_id")]
        public int UserId { get; set; }

        [Required]
        [Column("pet_id")]
        public int PetId { get; set; }

        [Required]
        [Column("phone_number")]
        [StringLength(20)]
        public string PhoneNumber { get; set; }

        [Required]
        [Column("address")]
        public string Address { get; set; }

        [Required]
        [Column("housing_type")]
        [StringLength(100)]
        public string HousingType { get; set; }

        [Required]
        [Column("has_pets")]
        [StringLength(10)]
        public string HasPets { get; set; }

        [Column("message")]
        public string Message { get; set; }

        [Column("status")]
        [StringLength(50)]
        public string Status { get; set; } = "Pending";

        [Column("admin_notes")]
        public string AdminNotes { get; set; }

        [Column("application_date")]
        public DateTime ApplicationDate { get; set; } = DateTime.Now;

        [Column("reviewed_date")]
        public DateTime? ReviewedDate { get; set; }

        [Column("reviewed_by")]
        public int? ReviewedBy { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // Navigation Properties
        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        [ForeignKey("PetId")]
        public virtual Pet Pet { get; set; }

        [ForeignKey("ReviewedBy")]
        public virtual User Reviewer { get; set; }
    }
}