using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FurEver_Home.Models
{
    public class AdminUserViewModel
    {
        public User User { get; set; }
        public List<Role> AssignedRoles { get; set; }
    }
}