using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FurEver_Home.Models
{
    [Table("pet_screening_questions")]
    public class PetScreeningQuestion
    {
        [Key]
        [Column("question_id")]
        public int QuestionId { get; set; }

        [Required]
        [Column("pet_id")]
        public int PetId { get; set; }

        [Required]
        [Column("question_text")]
        [StringLength(500)]
        public string QuestionText { get; set; }

        [Column("question_type")]
        [StringLength(50)]
        public string QuestionType { get; set; } = "Text";

        [Column("is_required")]
        public bool IsRequired { get; set; } = true;

        [Column("order_number")]
        public int OrderNumber { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("PetId")]
        public virtual Pet Pet { get; set; }
    }
}