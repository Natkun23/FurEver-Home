using System;
using System.ComponentModel.DataAnnotations;

namespace FurEver_Home.Models
{
    public class AdoptionApplication
    {
        public int ApplicationId { get; set; }

        public int UserId { get; set; }
        public User User { get; set; }

        public int PetId { get; set; }
        public Pet Pet { get; set; }

        [Required]
        public string PhoneNumber { get; set; }

        [Required]
        public string Address { get; set; }

        [Required]
        public string HousingType { get; set; } // "House", "Apartment", etc.

        [Required]
        public string HasPets { get; set; } // "Yes" or "No"

        public string Message { get; set; }

        public string Status { get; set; } // "Pending", "Approved", "Rejected"

        public DateTime ApplicationDate { get; set; }
    }
}