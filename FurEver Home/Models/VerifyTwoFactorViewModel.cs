using System.ComponentModel.DataAnnotations;

namespace FurEver_Home.Models
{
    public class VerifyTwoFactorViewModel
    {
        [Required]
        public string Email { get; set; }

        [Required(ErrorMessage = "2FA code is required")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Code must be 6 digits")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "Code must be exactly 6 digits")]
        [Display(Name = "Authenticator Code")]
        public string TwoFactorCode { get; set; }
    }
}