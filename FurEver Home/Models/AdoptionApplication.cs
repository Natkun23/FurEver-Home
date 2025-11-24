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

        // ⭐ SCREENING QUESTIONS
        [Column("screening_q1_experience")]
        public string ScreeningQ1Experience { get; set; }

        [Column("screening_q2_financial")]
        [StringLength(10)]
        public string ScreeningQ2Financial { get; set; }

        [Column("screening_q2_explanation")]
        public string ScreeningQ2Explanation { get; set; }

        [Column("screening_q3_household_agreement")]
        [StringLength(10)]
        public string ScreeningQ3HouseholdAgreement { get; set; }

        [Column("screening_q3_explanation")]
        public string ScreeningQ3Explanation { get; set; }

        [Column("screening_q4_relocation_plan")]
        public string ScreeningQ4RelocationPlan { get; set; }

        [Column("screening_q5_hours_alone")]
        [StringLength(50)]
        public string ScreeningQ5HoursAlone { get; set; }

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

        // ⭐ NEW: C2C (Customer-to-Customer) Properties
        [Column("reviewed_by_customer")]
        public int? ReviewedByCustomer { get; set; }

        [Column("is_customer_to_customer")]
        public bool IsCustomerToCustomer { get; set; } = false;

        // ========== NAVIGATION PROPERTIES ==========
        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        [ForeignKey("PetId")]
        public virtual Pet Pet { get; set; }

        [ForeignKey("ReviewedBy")]
        public virtual User Reviewer { get; set; }

        [ForeignKey("CancellationReviewedBy")]
        public virtual User CancellationReviewer { get; set; }

        [ForeignKey("ReviewedByCustomer")]
        public virtual User CustomerReviewer { get; set; }
    }
}