using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FurEver_Home.Models
{
    // Maps the C# class to the "Roles" table in the database
    [Table("Roles")]
    public class Role
    {

        [Key]
        [Column("RoleID")]
        public int RoleId { get; set; }


        [Required]
        [Column("RoleName")]
        [StringLength(50)]
        public string RoleName { get; set; }

        [Column("RoleDescription")]
        [StringLength(500)]
        public string RoleDescription { get; set; }

        [Column("IsActive")]
        public bool IsActive { get; set; }

        [Column("CreatedAt")]
        public DateTime CreatedAt { get; set; }

        [Column("UpdatedAt")]
        public DateTime UpdatedAt { get; set; }


    }
}