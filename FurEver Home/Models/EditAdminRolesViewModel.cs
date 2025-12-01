using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace FurEver_Home.Models
{
    public class EditAdminRolesViewModel
    {
        public int UserId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }

        [Required]
        [Display(Name = "Assign Roles")]
        public List<int> SelectedRoleIds { get; set; }
    }
}