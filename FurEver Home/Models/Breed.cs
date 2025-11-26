using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FurEver_Home.Models
{
    [Table("breeds")]
    public class Breed
    {
        [Key]
        [Column("breed_id")]
        public int BreedId { get; set; }

        [Required]
        [Column("breed_name")]
        [StringLength(100)]
        public string BreedName { get; set; }

        [Required]
        [Column("pet_type_id")]
        public int PetTypeId { get; set; } // 1 = Dog, 2 = Cat

        [Column("description")]
        [StringLength(500)]
        public string Description { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Column("created_by")]
        public int? CreatedBy { get; set; }

        // Navigation Property
        [ForeignKey("PetTypeId")]
        public virtual PetType PetType { get; set; }

        [ForeignKey("CreatedBy")]
        public virtual User Creator { get; set; }
    }
}