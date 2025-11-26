using System;
using System.ComponentModel.DataAnnotations;

public class ActivityLog
{
    [Key]
    public int ActivityLogId { get; set; }

    [Required]
    [StringLength(50)]
    public string ActivityType { get; set; } // "Application", "Verification", "PetAdded", "UserRegistered", "Adoption"

    [Required]
    [StringLength(200)]
    public string Title { get; set; }

    [Required]
    [StringLength(500)]
    public string Description { get; set; }

    [StringLength(50)]
    public string Status { get; set; } // "Pending", "Verified", "New", "Completed"

    public DateTime Timestamp { get; set; }

    [StringLength(50)]
    public string Icon { get; set; } // FontAwesome icon name

    [StringLength(20)]
    public string IconColor { get; set; } // blue, green, yellow, purple

    // Optional: Link to specific entities
    public int? UserId { get; set; }
    public int? PetId { get; set; }
    public int? ApplicationId { get; set; }
}