using System.ComponentModel.DataAnnotations;

namespace FurEver_Home.Models
{
    public class UpdateAdminProfileViewModel
    {
        [Required(ErrorMessage = "Full name is required")]
        [StringLength(255)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; }

        [Display(Name = "Email")]
        public string Email { get; set; } // Read-only, just for display

        [StringLength(20)]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }

        [Display(Name = "Address")]
        public string Address { get; set; }

        [Display(Name = "Age")]
        [Range(18, 120, ErrorMessage = "Age must be between 18 and 120")]
        public int? Age { get; set; }

        // For profile picture upload
        public string ProfilePictureUrl { get; set; }

        // For 2FA status display
        public bool TwoFactorEnabled { get; set; }
    }
}