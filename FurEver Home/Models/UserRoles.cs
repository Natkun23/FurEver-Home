using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FurEver_Home.Models
{
    [Table("UserRoles")]
    public class UserRoles
    {
        // Primary Key
        [Key]
        [Column("UserRoleId")]
        public int UserRoleId { get; set; }

        // Foreign Key to Users table
        [Required]
        [Column("user_id")]
        public int UserId { get; set; }

        // Foreign Key to Roles table
        [Required]
        [Column("RoleId")]
        public int RoleId { get; set; }

        // Assignment Date
        [Column("AssignedAt")]
        public DateTime AssignedAt { get; set; }

        // User ID of the person who made the assignment (optional)
        [Column("AssignedBy")]
        public int? AssignedBy { get; set; }

        // Active Status
        [Column("IsActive")]
        public bool IsActive { get; set; }

        // --- Navigation Properties ---

        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        [ForeignKey("RoleId")]
        public virtual Role Role { get; set; }

        [ForeignKey("AssignedBy")]
        public virtual User AssignedByUser { get; set; }
    }
}