using System;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using FurEver_Home.Models;
using FurEver_Home.Filters;

namespace FurEver_Home.Controllers
{
    public class AccountController : Controller
    {
        private readonly FurEverHomeContext db = new FurEverHomeContext();

        // ==================== LOGIN ====================

        // GET: Account/Login
        public ActionResult Login()
        {
            // Prevent caching
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetExpires(DateTime.UtcNow.AddMinutes(-1));
            Response.Cache.SetNoStore();

            // If already logged in, redirect to home
            if (Session["UserId"] != null)
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        // POST: Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Find user by email
                var user = db.Users.FirstOrDefault(u => u.Email == model.Email);

                if (user != null)
                {
                    // TODO: In production, use proper password hashing (BCrypt, etc.)
                    // For now, direct comparison
                    if (user.Password == model.Password)
                    {
                        // Check if account is active
                        if (!user.IsActive)
                        {
                            TempData["Error"] = "Your account has been deactivated. Please contact support.";
                            return View(model);
                        }

                        // Create session
                        Session["UserId"] = user.UserId;
                        Session["UserName"] = user.FullName;
                        Session["UserEmail"] = user.Email;
                        Session["UserRole"] = user.Role;

                        // Redirect based on role
                        if (user.Role == "Admin")
                        {
                            return RedirectToAction("Dashboard", "Admin");
                        }
                        else
                        {
                            return RedirectToAction("Index", "Home");
                        }
                    }
                }

                TempData["Error"] = "Invalid email or password.";
            }

            return View(model);
        }

        // ==================== REGISTER ====================

        // GET: Account/Register
        public ActionResult Register()
        {
            return View();
        }

        // POST: Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(RegisterViewModel model, HttpPostedFileBase IDImage)
        {
            if (ModelState.IsValid)
            {
                // Check if email already exists
                if (db.Users.Any(u => u.Email == model.Email))
                {
                    ModelState.AddModelError("Email", "This email is already registered.");
                    return View(model);
                }

                // Handle ID image upload
                string idImagePath = null;
                if (IDImage != null && IDImage.ContentLength > 0)
                {
                    // Validate file type
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".pdf" };
                    var extension = Path.GetExtension(IDImage.FileName).ToLower();

                    if (!allowedExtensions.Contains(extension))
                    {
                        ModelState.AddModelError("IDImage", "Only JPG, PNG, or PDF files are allowed.");
                        return View(model);
                    }

                    // Validate file size (max 5MB)
                    if (IDImage.ContentLength > 5 * 1024 * 1024)
                    {
                        ModelState.AddModelError("IDImage", "File size must be less than 5MB.");
                        return View(model);
                    }

                    // Create uploads directory if it doesn't exist
                    var uploadsDir = Server.MapPath("~/Content/Uploads/IDs");
                    if (!Directory.Exists(uploadsDir))
                    {
                        Directory.CreateDirectory(uploadsDir);
                    }

                    // Generate unique filename
                    var fileName = Guid.NewGuid().ToString() + extension;
                    var filePath = Path.Combine(uploadsDir, fileName);

                    // Save file
                    IDImage.SaveAs(filePath);
                    idImagePath = "/Content/Uploads/IDs/" + fileName;
                }

                // Create new user
                var user = new User
                {
                    FullName = model.FullName,
                    Email = model.Email,
                    Password = model.Password, // TODO: Hash password in production!
                    Role = "Client",
                    IDType = model.IDType,
                    IDImageUrl = idImagePath,
                    IDStatus = IDImage != null ? "Pending" : "Not Submitted",
                    DateRegistered = DateTime.Now,
                    IsActive = true,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                db.Users.Add(user);
                db.SaveChanges();

                TempData["Success"] = "Registration successful! Please login.";
                return RedirectToAction("Login");
            }

            return View(model);
        }

        // ==================== FORGOT PASSWORD ====================

        // GET: Account/ForgotPassword
        public ActionResult ForgotPassword()
        {
            return View();
        }

        // POST: Account/ForgotPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = db.Users.FirstOrDefault(u => u.Email == model.Email);

                if (user != null)
                {
                    // Generate reset token
                    var token = Guid.NewGuid().ToString();
                    user.ResetToken = token;
                    user.ResetTokenExpiry = DateTime.Now.AddHours(1); // Token valid for 1 hour
                    user.UpdatedAt = DateTime.Now;
                    db.SaveChanges();

                    // Create reset URL
                    var resetUrl = Url.Action("ResetPassword", "Account",
                        new { token = token, email = user.Email },
                        Request.Url.Scheme);

                    // For testing: Display the link
                    TempData["Success"] = $"Password reset link has been generated! Click here to reset: <a href='{resetUrl}' style='color: #3FA9F5; text-decoration: underline; font-weight: 700;'>Reset Password</a>";

                    // In production, you would send an email here
                    // SendEmail(user.Email, "Password Reset", $"Click here to reset your password: {resetUrl}");

                    return View();
                }

                // Don't reveal if email exists or not (security best practice)
                TempData["Success"] = "If your email exists in our system, you will receive password reset instructions.";
            }

            return View(model);
        }

        // ==================== RESET PASSWORD ====================

        // GET: Account/ResetPassword
        public ActionResult ResetPassword(string token, string email)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
            {
                return RedirectToAction("Login");
            }

            var user = db.Users.FirstOrDefault(u => u.Email == email && u.ResetToken == token);

            if (user == null || user.ResetTokenExpiry < DateTime.Now)
            {
                TempData["Error"] = "Invalid or expired reset token.";
                return RedirectToAction("Login");
            }

            var model = new ResetPasswordViewModel
            {
                Token = token,
                Email = email
            };

            return View(model);
        }

        // POST: Account/ResetPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ResetPassword(ResetPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = db.Users.FirstOrDefault(u => u.Email == model.Email && u.ResetToken == model.Token);

                if (user != null && user.ResetTokenExpiry >= DateTime.Now)
                {
                    // Update password
                    user.Password = model.NewPassword; // TODO: Hash password in production!
                    user.ResetToken = null;
                    user.ResetTokenExpiry = null;
                    user.UpdatedAt = DateTime.Now;
                    db.SaveChanges();

                    TempData["Success"] = "Your password has been reset successfully! Please login with your new password.";
                    return RedirectToAction("Login");
                }

                TempData["Error"] = "Invalid or expired reset token.";
            }

            return View(model);
        }

        // ==================== LOGOUT ====================

        // GET: Account/Logout
        public ActionResult Logout()
        {
            // Clear session
            Session.Clear();
            Session.Abandon();

            // Prevent caching
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetExpires(DateTime.UtcNow.AddMinutes(-1));
            Response.Cache.SetNoStore();
            Response.AppendHeader("Pragma", "no-cache");

            TempData["Success"] = "You have been logged out successfully.";
            return RedirectToAction("Login");
        }

        // GET: Account/Profile
        public ActionResult Profile()
        {
            if (Session["UserId"] == null)
            {
                return RedirectToAction("Login");
            }

            int userId = (int)Session["UserId"];
            var user = db.Users.Find(userId);

            if (user == null)
            {
                return HttpNotFound();
            }

            return View(user);
        }

        // POST: Account/UpdateProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateProfile(User model, HttpPostedFileBase ProfilePicture, HttpPostedFileBase IDImage)
        {
            if (Session["UserId"] == null)
            {
                return RedirectToAction("Login");
            }

            int userId = (int)Session["UserId"];
            var user = db.Users.Find(userId);

            if (user == null)
            {
                return HttpNotFound();
            }

            // Update basic info
            user.FullName = model.FullName;
            user.PhoneNumber = model.PhoneNumber;
            user.Address = model.Address;
            user.Age = model.Age;

            // Handle Profile Picture Upload
            if (ProfilePicture != null && ProfilePicture.ContentLength > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                var extension = Path.GetExtension(ProfilePicture.FileName).ToLower();

                if (allowedExtensions.Contains(extension))
                {
                    var uploadsDir = Server.MapPath("~/Content/Uploads/Profiles");
                    if (!Directory.Exists(uploadsDir))
                    {
                        Directory.CreateDirectory(uploadsDir);
                    }

                    var fileName = Guid.NewGuid().ToString() + extension;
                    var filePath = Path.Combine(uploadsDir, fileName);
                    ProfilePicture.SaveAs(filePath);
                    user.ProfilePictureUrl = "/Content/Uploads/Profiles/" + fileName;
                }
            }

            // Handle ID Image Upload
            if (IDImage != null && IDImage.ContentLength > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".pdf" };
                var extension = Path.GetExtension(IDImage.FileName).ToLower();

                if (allowedExtensions.Contains(extension))
                {
                    var uploadsDir = Server.MapPath("~/Content/Uploads/IDs");
                    if (!Directory.Exists(uploadsDir))
                    {
                        Directory.CreateDirectory(uploadsDir);
                    }

                    var fileName = Guid.NewGuid().ToString() + extension;
                    var filePath = Path.Combine(uploadsDir, fileName);
                    IDImage.SaveAs(filePath);
                    user.IDImageUrl = "/Content/Uploads/IDs/" + fileName;
                    user.IDStatus = "Pending"; // Reset to pending for admin review
                }
            }

            user.UpdatedAt = DateTime.Now;
            db.SaveChanges();

            TempData["Success"] = "Profile updated successfully!";
            return RedirectToAction("Profile");
        }
    }
}
