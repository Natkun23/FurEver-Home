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

        // Navigation Property
        [Required]
        [Column("pet_type_id")]
        [ForeignKey("PetType")] // <--- CORRECT PLACEMENT: ON THE FK PROPERTY
        public int PetTypeId { get; set; }

        [Column("description")]
        [StringLength(500)]
        public string Description { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Column("created_by")]
        public int? CreatedBy { get; set; }


        // Navigation Property
        public virtual PetType PetType { get; set; }

        [ForeignKey("CreatedBy")]
        public virtual User Creator { get; set; }
    }
}