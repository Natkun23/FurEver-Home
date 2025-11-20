using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FurEver_Home.Models
{
    [Table("pets")]
    public class Pet
    {
        [Key]
        [Column("pet_id")]
        public int PetId { get; set; }

        [Required]
        [Column("pet_name")]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        [Column("pet_type_id")]
        public int PetTypeId { get; set; }

        [Required]
        [Column("breed")]
        [StringLength(100)]
        public string Breed { get; set; }

        [Required]
        [Column("age")]
        public int Age { get; set; }

        [Column("age_unit")]
        [StringLength(10)]
        public string AgeUnit { get; set; } = "Years"; // NEW: "Months" or "Years"

        [Required]
        [Column("gender")]
        [StringLength(10)]
        public string Gender { get; set; }

        [Required]
        [Column("size")]
        [StringLength(50)]
        public string Size { get; set; }

        [Required]
        [Column("description")]
        public string Description { get; set; }

        [Column("personality_traits")]
        [StringLength(500)]
        public string Traits { get; set; }

        [Column("vaccines_info")]
        public string Vaccines { get; set; }

        [Column("is_healthy")]
        public bool IsHealthy { get; set; } = true;

        [Column("is_neutered")]
        public bool IsNeutered { get; set; } = true;

        [Column("days_in_center")]
        public int DaysInCenter { get; set; } = 0;

        [Column("why_adopt_me")]
        public string WhyAdoptMe { get; set; }

        [Column("is_adopted")]
        public bool IsAdopted { get; set; } = false;

        [Column("image_url")]
        [StringLength(500)]
        public string ImageUrl { get; set; }

        [Column("date_added")]
        public DateTime DateAdded { get; set; } = DateTime.Now;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        [Column("created_by")]
        public int? CreatedBy { get; set; }

        [Column("updated_by")]
        public int? UpdatedBy { get; set; }

        // Navigation Properties
        [NotMapped]
        public string Type
        {
            get
            {
                return PetTypeId == 1 ? "Dog" : "Cat";
            }
        }

        // NEW: Display age with proper unit
        [NotMapped]
        public string AgeDisplay
        {
            get
            {
                if (AgeUnit == "Months")
                {
                    return Age == 1 ? "1 month" : $"{Age} months";
                }
                else
                {
                    return Age == 1 ? "1 year" : $"{Age} years";
                }
            }
        }

        [ForeignKey("PetTypeId")]
        public virtual PetType PetType { get; set; }

        [ForeignKey("CreatedBy")]
        public virtual User Creator { get; set; }

        [ForeignKey("UpdatedBy")]
        public virtual User Updater { get; set; }


        // ADD THESE PROPERTIES TO YOUR EXISTING Pet.cs

        [Column("posted_by_type")]
        [StringLength(50)]
        public string PostedByType { get; set; } // "User" or "Organization"

        [Column("organization_name")]
        [StringLength(255)]
        public string OrganizationName { get; set; } // If posted by admin as organization

        // Computed property for display
        [NotMapped]
        public string PostedByDisplay
        {
            get
            {
                if (PostedByType == "Organization" && !string.IsNullOrEmpty(OrganizationName))
                {
                    return OrganizationName;
                }
                else if (Creator != null)
                {
                    return Creator.FullName;
                }
                return "Unknown";
            }
        }

        [NotMapped]
        public string PostedByLabel
        {
            get
            {
                return PostedByType == "Organization" ? "Posted by Organization" : "Posted by";
            }
        }
    }
}