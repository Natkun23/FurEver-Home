using System.ComponentModel.DataAnnotations;

namespace FurEver_Home.Models
{
    public class VerifyOtpViewModel
    {
        [Required]
        public string Email { get; set; }

        [Required(ErrorMessage = "OTP code is required")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "OTP must be 6 digits")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "OTP must be exactly 6 digits")]
        [Display(Name = "OTP Code")]
        public string OtpCode { get; set; }
    }
}