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


        [Column("rejection_reason")]
        public string RejectionReason { get; set; }

        [Column("is_ready_for_pickup")]
        public bool IsReadyForPickup { get; set; } = false;

        [Column("pickup_location")]
        [StringLength(500)]
        public string PickupLocation { get; set; }

        [Column("pickup_date")]
        public DateTime? PickupDate { get; set; }

        [Column("pickup_notes")]
        public string PickupNotes { get; set; }

        [Column("claimed_date")]
        public DateTime? ClaimedDate { get; set; }

        // ADD THESE PROPERTIES TO YOUR EXISTING AdoptionApplication.cs

        [Column("cancellation_requested")]
        public bool CancellationRequested { get; set; } = false;

        [Column("cancellation_reason")]
        public string CancellationReason { get; set; }

        [Column("cancellation_requested_date")]
        public DateTime? CancellationRequestedDate { get; set; }

        [Column("cancellation_reviewed_by")]
        public int? CancellationReviewedBy { get; set; }

        [Column("cancellation_reviewed_date")]
        public DateTime? CancellationReviewedDate { get; set; }

        [Column("cancellation_approved")]
        public bool CancellationApproved { get; set; } = false;

        [Column("withdrawal_reason")]
        [StringLength(500)]
        public string WithdrawalReason { get; set; }

        [Column("withdrawal_date")]
        public DateTime? WithdrawalDate { get; set; }

        // Navigation for cancellation reviewer
        [ForeignKey("CancellationReviewedBy")]
        public virtual User CancellationReviewer { get; set; }
    }
}