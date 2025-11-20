using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FurEver_Home.Models
{
    [Table("adoption_history")]
    public class AdoptionHistory
    {
        [Key]
        [Column("history_id")]
        public int HistoryId { get; set; }

        [Required]
        [Column("application_id")]
        public int ApplicationId { get; set; }

        [Required]
        [Column("user_id")]
        public int UserId { get; set; }

        [Required]
        [Column("pet_id")]
        public int PetId { get; set; }

        // Pet Snapshot (preserved even if pet is deleted)
        [Required]
        [Column("pet_name")]
        [StringLength(100)]
        public string PetName { get; set; }

        [Column("pet_breed")]
        [StringLength(100)]
        public string PetBreed { get; set; }

        [Column("pet_type")]
        [StringLength(50)]
        public string PetType { get; set; }

        [Column("pet_image_url")]
        [StringLength(500)]
        public string PetImageUrl { get; set; }

        // Application Snapshot
        [Column("phone_number")]
        [StringLength(20)]
        public string PhoneNumber { get; set; }

        [Column("address")]
        public string Address { get; set; }

        [Column("housing_type")]
        [StringLength(100)]
        public string HousingType { get; set; }

        // Timeline
        [Required]
        [Column("application_date")]
        public DateTime ApplicationDate { get; set; }

        [Column("approval_date")]
        public DateTime? ApprovalDate { get; set; }

        [Column("claimed_date")]
        public DateTime? ClaimedDate { get; set; }

        [Column("completed_date")]
        public DateTime? CompletedDate { get; set; }

        // Status
        [Required]
        [Column("final_status")]
        [StringLength(50)]
        public string FinalStatus { get; set; } // Completed, Withdrawn, Cancelled, Rejected

        // Cancellation/Withdrawal
        [Column("cancellation_reason")]
        public string CancellationReason { get; set; }

        [Column("withdrawal_reason")]
        [StringLength(500)]
        public string WithdrawalReason { get; set; }

        [Column("cancellation_requested_date")]
        public DateTime? CancellationRequestedDate { get; set; }

        [Column("cancellation_approved_by")]
        public int? CancellationApprovedBy { get; set; }

        [Column("cancellation_approved_date")]
        public DateTime? CancellationApprovedDate { get; set; }

        // Admin Data
        [Column("admin_notes")]
        public string AdminNotes { get; set; }

        [Column("rejection_reason")]
        public string RejectionReason { get; set; }

        // Metadata
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Column("archived_at")]
        public DateTime? ArchivedAt { get; set; }

        [Column("auto_delete_after")]
        public DateTime? AutoDeleteAfter { get; set; }

        // Navigation Properties
        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        [ForeignKey("PetId")]
        public virtual Pet Pet { get; set; }

        [ForeignKey("CancellationApprovedBy")]
        public virtual User CancellationApprover { get; set; }
    }
}