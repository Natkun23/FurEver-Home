using System;
using System.ComponentModel.DataAnnotations;

namespace FurEver_Home.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email or Full Name is required")]
        [Display(Name = "Email or Full Name")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [Display(Name = "Remember Me")]
        public bool RememberMe { get; set; }
    }
}