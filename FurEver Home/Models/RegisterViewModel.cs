using System.ComponentModel.DataAnnotations;
using System.Web;

namespace FurEver_Home.Models
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Full name is required")]
        [Display(Name = "Full Name")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [Display(Name = "Email Address")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, ErrorMessage = "Password must be at least {2} characters long", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Please confirm your password")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; }

        // ID VERIFICATION PROPERTIES - ADD THESE IF MISSING
        [Display(Name = "ID Type")]
        public string IDType { get; set; }

        [Display(Name = "ID Image")]
        public HttpPostedFileBase IDImage { get; set; }

        [Required(ErrorMessage = "Mobile number is required")]
        [StringLength(20)]
        [Display(Name = "Mobile Number")]
        [RegularExpression(@"^(\+63|0)?[0-9]{10}$", ErrorMessage = "Please enter a valid Philippine mobile number")]
        public string MobileNumber { get; set; }
    }
}