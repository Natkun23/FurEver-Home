using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FurEver_Home.Models
{
    [Table("pet_types")]
    public class PetType
    {
        [Key]
        [Column("pet_type_id")]
        public int PetTypeId { get; set; }

        [Required]
        [Column("type_name")]
        [StringLength(50)]
        public string TypeName { get; set; }

        [Column("description")]
        [StringLength(255)]
        public string Description { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}