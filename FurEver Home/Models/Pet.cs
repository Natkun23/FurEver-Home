using System;
using System.ComponentModel.DataAnnotations;

namespace FurEver_Home.Models
{
    public class Pet
    {
        public int PetId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        public string Type { get; set; } // "Dog" or "Cat"

        [Required]
        public string Breed { get; set; }

        [Required]
        public int Age { get; set; }

        [Required]
        public string Gender { get; set; } // "Male" or "Female"

        [Required]
        public string Size { get; set; } // "Small", "Medium", "Large"

        [Required]
        public string Description { get; set; }

        public string ImageUrl { get; set; }

        public string Traits { get; set; } // Comma-separated: "Friendly,Playful,Trained"

        // NEW FIELDS
        public string Vaccines { get; set; } // Full vaccine information

        public int DaysInCenter { get; set; } // Days in shelter/center

        public string WhyAdoptMe { get; set; } // Reasons to adopt (one per line or comma-separated)

        public bool IsHealthy { get; set; } = true;

        public bool IsNeutered { get; set; } = true;

        // END NEW FIELDS

        public bool IsAdopted { get; set; }

        public DateTime DateAdded { get; set; }
    }
}