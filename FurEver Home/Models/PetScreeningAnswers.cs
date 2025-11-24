using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FurEver_Home.Models
{
    [Table("pet_screening_answers")]
    public class PetScreeningAnswer
    {
        [Key]
        [Column("answer_id")]
        public int AnswerId { get; set; }

        [Required]
        [Column("application_id")]
        public int ApplicationId { get; set; }

        [Required]
        [Column("question_id")]
        public int QuestionId { get; set; }

        [Required]
        [Column("answer_text")]
        public string AnswerText { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("ApplicationId")]
        public virtual AdoptionApplication Application { get; set; }

        [ForeignKey("QuestionId")]
        public virtual PetScreeningQuestion Question { get; set; }
    }
}