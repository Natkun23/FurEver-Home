using System;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using FurEver_Home.Models;
using FurEver_Home.Services;
using OtpNet;
using QRCoder;
using System.Drawing;
using System.Drawing.Imaging;
using BCrypt.Net;

namespace FurEver_Home.Controllers
{
    [AllowAnonymous]
    public class AccountController : Controller
    {
        private readonly FurEverHomeContext db = new FurEverHomeContext();
        private readonly EmailService emailService = new EmailService();

        // ==================== LOGIN WITH OTP ====================

        // GET: Account/Login
        public ActionResult Login()
        {
            // If already logged in, redirect appropriately
            if (Session["UserId"] != null)
            {
                string role = Session["UserRole"]?.ToString();

                // ✅ UPDATED: Check if NOT a client
                if (role != "Client" && !string.IsNullOrEmpty(role))
                {
                    return RedirectToAction("Dashboard", "Admin");
                }
                else if (role == "Client")
                {
                    return RedirectToAction("Index", "Pets");
                }
            }

            return View();
        }

        // ==================== UPDATED: Login Method (Step 1) ====================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = db.Users.FirstOrDefault(u =>
                    u.Email == model.Email ||
                    u.FullName.ToLower() == model.Email.ToLower()
                );

                if (user != null)
                {
                    if (BCrypt.Net.BCrypt.Verify(model.Password, user.Password))
                    {
                        if (!user.IsActive)
                        {
                            TempData["Error"] = "Your account has been deactivated. Please contact support.";
                            return View(model);
                        }

                        // ✅ Check if 2FA is enabled
                        if (user.TwoFactorEnabled)
                        {
                            // Skip email OTP, go directly to 2FA
                            TempData["TwoFactorEmail"] = user.Email;
                            TempData["Success"] = "Please enter the 6-digit code from your authenticator app.";
                            return RedirectToAction("VerifyTwoFactor");
                        }
                        else
                        {
                            // Original flow: Generate and send email OTP
                            Random random = new Random();
                            string otpCode = random.Next(100000, 999999).ToString();

                            user.OtpCode = otpCode;
                            user.OtpExpiry = DateTime.Now.AddMinutes(5);
                            user.OtpAttempts = 0;
                            user.UpdatedAt = DateTime.Now;
                            db.SaveChanges();

                            bool emailSent = emailService.SendOtpEmail(user.Email, user.FullName, otpCode);

                            if (emailSent)
                            {
                                TempData["OtpEmail"] = user.Email;
                                TempData["Success"] = "OTP code has been sent to your email. Please check your inbox.";
                                return RedirectToAction("VerifyOtp");
                            }
                            else
                            {
                                TempData["Error"] = "Failed to send OTP email. Please try again or contact support.";
                                return View(model);
                            }
                        }
                    }
                }

                TempData["Error"] = "Invalid email or password.";
            }

            return View(model);
        }

        // ==================== NEW: VERIFY TWO-FACTOR CODE ====================

        // GET: Account/VerifyTwoFactor
        public ActionResult VerifyTwoFactor()
        {
            if (TempData["TwoFactorEmail"] == null)
            {
                return RedirectToAction("Login");
            }

            var model = new VerifyTwoFactorViewModel
            {
                Email = TempData["TwoFactorEmail"].ToString()
            };

            TempData.Keep("TwoFactorEmail");
            return View(model);
        }

        // POST: Account/VerifyTwoFactor
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult VerifyTwoFactor(VerifyTwoFactorViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = db.Users.FirstOrDefault(u => u.Email == model.Email);
                if (user != null && user.TwoFactorEnabled)
                {
                    // Verify TOTP code
                    var totp = new Totp(Base32Encoding.ToBytes(user.TwoFactorSecretKey));
                    bool isValid = totp.VerifyTotp(model.TwoFactorCode, out long timeStepMatched, new VerificationWindow(2, 2));

                    if (isValid)
                    {
                        // Create session
                        Session["UserId"] = user.UserId;
                        Session["UserName"] = user.FullName;
                        Session["UserEmail"] = user.Email;
                        Session["UserRole"] = user.Role;

                        // ✅ BEST: Check if NOT a client
                        if (user.Role != "Client")
                        {
                            return RedirectToAction("Dashboard", "Admin");
                        }
                        else
                        {
                            return RedirectToAction("Index", "Pets");
                        }
                    }
                    else
                    {
                        TempData["Error"] = "Invalid authenticator code. Please try again.";
                        TempData["TwoFactorEmail"] = model.Email;
                        return View(model);
                    }
                }

                TempData["Error"] = "User not found or 2FA not enabled.";
                return RedirectToAction("Login");
            }

            TempData["TwoFactorEmail"] = model.Email;
            return View(model);
        }

        // ==================== NEW: 2FA MANAGEMENT IN PROFILE ====================

        // POST: Account/EnableTwoFactor
        [HttpPost]
        public ActionResult EnableTwoFactor()
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

            // Generate secret key
            var key = KeyGeneration.GenerateRandomKey(20);
            var base32Secret = Base32Encoding.ToString(key);

            user.TwoFactorSecretKey = base32Secret;
            user.TwoFactorEnabled = false; // Not enabled until user verifies the setup
            user.UpdatedAt = DateTime.Now;
            db.SaveChanges();

            return Json(new { success = true, secret = base32Secret });
        }

        // POST: Account/ConfirmTwoFactor
        [HttpPost]
        public ActionResult ConfirmTwoFactor(string code)
        {
            if (Session["UserId"] == null)
            {
                return Json(new { success = false, message = "Session expired" });
            }

            int userId = (int)Session["UserId"];
            var user = db.Users.Find(userId);

            if (user == null || string.IsNullOrEmpty(user.TwoFactorSecretKey))
            {
                return Json(new { success = false, message = "2FA setup not initiated" });
            }

            // Verify the code
            var totp = new Totp(Base32Encoding.ToBytes(user.TwoFactorSecretKey));
            bool isValid = totp.VerifyTotp(code, out long timeStepMatched, new VerificationWindow(2, 2));

            if (isValid)
            {
                user.TwoFactorEnabled = true;
                user.UpdatedAt = DateTime.Now;
                db.SaveChanges();

                return Json(new { success = true, message = "2FA enabled successfully!" });
            }
            else
            {
                return Json(new { success = false, message = "Invalid code. Please try again." });
            }
        }

        // POST: Account/DisableTwoFactor
        [HttpPost]
        public ActionResult DisableTwoFactor(string password)
        {
            if (Session["UserId"] == null)
            {
                return Json(new { success = false, message = "Session expired" });
            }

            int userId = (int)Session["UserId"];
            var user = db.Users.Find(userId);

            if (user == null)
            {
                return Json(new { success = false, message = "User not found" });
            }

            // Verify password
            if (!BCrypt.Net.BCrypt.Verify(password, user.Password))
            {
                return Json(new { success = false, message = "Incorrect password" });
            }

            user.TwoFactorEnabled = false;
            user.TwoFactorSecretKey = null;
            user.UpdatedAt = DateTime.Now;
            db.SaveChanges();

            return Json(new { success = true, message = "2FA disabled successfully" });
        }

        // GET: Account/GetQRCode
        public ActionResult GetQRCode()
        {
            if (Session["UserId"] == null)
            {
                return null;
            }

            int userId = (int)Session["UserId"];
            var user = db.Users.Find(userId);

            if (user == null || string.IsNullOrEmpty(user.TwoFactorSecretKey))
            {
                return null;
            }

            // Generate QR code
            string issuer = "FurEver Home";
            string account = user.Email;
            string otpAuthUrl = $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(account)}?secret={user.TwoFactorSecretKey}&issuer={Uri.EscapeDataString(issuer)}";

            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(otpAuthUrl, QRCodeGenerator.ECCLevel.Q);
            QRCode qrCode = new QRCode(qrCodeData);
            Bitmap qrCodeImage = qrCode.GetGraphic(20);

            using (MemoryStream ms = new MemoryStream())
            {
                qrCodeImage.Save(ms, ImageFormat.Png);
                return File(ms.ToArray(), "image/png");
            }
        }

        // GET: Account/VerifyOtp
        public ActionResult VerifyOtp()
        {
            if (TempData["OtpEmail"] == null)
            {
                return RedirectToAction("Login");
            }

            var model = new VerifyOtpViewModel
            {
                Email = TempData["OtpEmail"].ToString()
            };

            // Keep the email for the next request
            TempData.Keep("OtpEmail");

            return View(model);
        }

        // POST: Account/VerifyOtp (Step 2: Verify OTP and complete login)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult VerifyOtp(VerifyOtpViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = db.Users.FirstOrDefault(u => u.Email == model.Email);
                if (user != null)
                {
                    // Check if OTP has expired
                    if (user.OtpExpiry < DateTime.Now)
                    {
                        TempData["Error"] = "OTP has expired. Please login again to receive a new code.";
                        return RedirectToAction("Login");
                    }

                    // Check OTP attempts (max 3 attempts)
                    if (user.OtpAttempts >= 3)
                    {
                        // Clear OTP
                        user.OtpCode = null;
                        user.OtpExpiry = null;
                        user.OtpAttempts = 0;
                        db.SaveChanges();
                        TempData["Error"] = "Maximum OTP attempts exceeded. Please login again.";
                        return RedirectToAction("Login");
                    }

                    // Verify OTP
                    if (user.OtpCode == model.OtpCode)
                    {
                        // Clear OTP
                        user.OtpCode = null;
                        user.OtpExpiry = null;
                        user.OtpAttempts = 0;
                        db.SaveChanges();

                        // Create session
                        Session["UserId"] = user.UserId;
                        Session["UserName"] = user.FullName;
                        Session["UserEmail"] = user.Email;
                        Session["UserRole"] = user.Role;

                        // ✅ UPDATED: Check if NOT a client
                        if (user.Role != "Client")
                        {
                            return RedirectToAction("Dashboard", "Admin");
                        }
                        else
                        {
                            return RedirectToAction("Index", "Pets");
                        }
                    }
                    else
                    {
                        // Increment OTP attempts
                        user.OtpAttempts++;
                        db.SaveChanges();
                        int remainingAttempts = 3 - user.OtpAttempts;
                        TempData["Error"] = $"Invalid OTP code. {remainingAttempts} attempt(s) remaining.";
                        TempData["OtpEmail"] = model.Email;
                        TempData["FullName"] = user.FullName;
                        return View(model);
                    }
                }

                TempData["Error"] = "User not found.";
                return RedirectToAction("Login");
            }

            TempData["OtpEmail"] = model.Email;
            var userForName = db.Users.FirstOrDefault(u => u.Email == model.Email);
            if (userForName != null)
            {
                TempData["FullName"] = userForName.FullName;
            }
            return View(model);
        }

        // POST: Account/ResendOtp
        [HttpPost]
        public ActionResult ResendOtp(string email)
        {
            var user = db.Users.FirstOrDefault(u => u.Email == email);

            if (user != null)
            {
                // Generate new OTP
                Random random = new Random();
                string otpCode = random.Next(100000, 999999).ToString();

                // Update OTP
                user.OtpCode = otpCode;
                user.OtpExpiry = DateTime.Now.AddMinutes(5);
                user.OtpAttempts = 0;
                user.UpdatedAt = DateTime.Now;
                db.SaveChanges();

                // Send OTP via email
                bool emailSent = emailService.SendOtpEmail(user.Email, user.FullName, otpCode);

                if (emailSent)
                {
                    TempData["Success"] = "New OTP code has been sent to your email.";
                }
                else
                {
                    TempData["Error"] = "Failed to send OTP. Please try again.";
                }
            }

            TempData["OtpEmail"] = email;
            return RedirectToAction("VerifyOtp");
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
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".pdf" };
                    var extension = Path.GetExtension(IDImage.FileName).ToLower();

                    if (!allowedExtensions.Contains(extension))
                    {
                        ModelState.AddModelError("IDImage", "Only JPG, PNG, or PDF files are allowed.");
                        return View(model);
                    }

                    if (IDImage.ContentLength > 5 * 1024 * 1024)
                    {
                        ModelState.AddModelError("IDImage", "File size must be less than 5MB.");
                        return View(model);
                    }

                    var uploadsDir = Server.MapPath("~/Content/Uploads/IDs");
                    if (!Directory.Exists(uploadsDir))
                    {
                        Directory.CreateDirectory(uploadsDir);
                    }

                    var fileName = Guid.NewGuid().ToString() + extension;
                    var filePath = Path.Combine(uploadsDir, fileName);
                    IDImage.SaveAs(filePath);
                    idImagePath = "/Content/Uploads/IDs/" + fileName;
                }

                // Create new user
                var user = new User
                {
                    FullName = model.FullName,
                    Email = model.Email,
                    Password = BCrypt.Net.BCrypt.HashPassword(model.Password),
                    PhoneNumber = model.MobileNumber,
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

        // ==================== FORGOT PASSWORD WITH EMAIL ====================

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
                    user.ResetTokenExpiry = DateTime.Now.AddHours(1);
                    user.UpdatedAt = DateTime.Now;
                    db.SaveChanges();

                    // Create reset URL
                    var resetUrl = Url.Action("ResetPassword", "Account",
                        new { token = token, email = user.Email },
                        Request.Url.Scheme);

                    // Send password reset email
                    bool emailSent = emailService.SendPasswordResetEmail(user.Email, user.FullName, resetUrl);

                    if (emailSent)
                    {
                        TempData["Success"] = "Password reset instructions have been sent to your email. Please check your inbox.";
                    }
                    else
                    {
                        TempData["Error"] = "Failed to send password reset email. Please try again or contact support.";
                    }

                    return View();
                }

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
                    user.Password = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
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
        public ActionResult Logout()
        {
            Session.Clear();
            Session.Abandon();
            Session.RemoveAll();

            if (Request.Cookies[System.Web.Security.FormsAuthentication.FormsCookieName] != null)
            {
                var cookie = new HttpCookie(System.Web.Security.FormsAuthentication.FormsCookieName)
                {
                    Expires = DateTime.Now.AddDays(-1)
                };
                Response.Cookies.Add(cookie);
            }

            if (Request.Cookies["ASP.NET_SessionId"] != null)
            {
                Response.Cookies["ASP.NET_SessionId"].Expires = DateTime.Now.AddDays(-1);
            }

            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetExpires(DateTime.UtcNow.AddMinutes(-1));
            Response.Cache.SetNoStore();
            Response.AppendHeader("Cache-Control", "no-cache, no-store, must-revalidate, private, max-age=0");
            Response.AppendHeader("Pragma", "no-cache");
            Response.AppendHeader("Expires", "0");
            Response.AppendHeader("Clear-Site-Data", "\"cache\", \"cookies\", \"storage\"");

            TempData["Success"] = "You have been logged out successfully.";
            return RedirectToAction("Login");
        }

        // ==================== PROFILE ====================

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateProfile(User model, HttpPostedFileBase ProfilePicture, HttpPostedFileBase IDImage)
        {
            if (Session["UserId"] == null)
            {
                return RedirectToAction("Login");
            }

            try
            {
                int userId = (int)Session["UserId"];
                var user = db.Users.Find(userId);

                if (user == null)
                {
                    return HttpNotFound();
                }

                user.FullName = model.FullName;
                user.PhoneNumber = model.PhoneNumber;
                user.Address = model.Address;
                user.Age = model.Age;

                if (ProfilePicture != null && ProfilePicture.ContentLength > 0)
                {
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                    var extension = Path.GetExtension(ProfilePicture.FileName).ToLower();

                    if (!allowedExtensions.Contains(extension))
                    {
                        TempData["Error"] = "Only JPG and PNG files are allowed for profile pictures.";
                        return RedirectToAction("Profile");
                    }

                    if (ProfilePicture.ContentLength > 5 * 1024 * 1024)
                    {
                        TempData["Error"] = "Profile picture must be less than 5MB.";
                        return RedirectToAction("Profile");
                    }

                    if (!string.IsNullOrEmpty(user.ProfilePictureUrl))
                    {
                        var oldPath = Server.MapPath("~" + user.ProfilePictureUrl);
                        if (System.IO.File.Exists(oldPath))
                        {
                            try
                            {
                                System.IO.File.Delete(oldPath);
                            }
                            catch { }
                        }
                    }

                    var uploadsDir = Server.MapPath("~/Content/Uploads/Profiles");
                    if (!Directory.Exists(uploadsDir))
                    {
                        Directory.CreateDirectory(uploadsDir);
                    }

                    var fileName = $"profile_{userId}_{Guid.NewGuid()}{extension}";
                    var filePath = Path.Combine(uploadsDir, fileName);
                    ProfilePicture.SaveAs(filePath);

                    user.ProfilePictureUrl = "/Content/Uploads/Profiles/" + fileName;
                }

                if (IDImage != null && IDImage.ContentLength > 0)
                {
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".pdf" };
                    var extension = Path.GetExtension(IDImage.FileName).ToLower();

                    if (!allowedExtensions.Contains(extension))
                    {
                        TempData["Error"] = "Only JPG, PNG, and PDF files are allowed for ID documents.";
                        return RedirectToAction("Profile");
                    }

                    if (IDImage.ContentLength > 5 * 1024 * 1024)
                    {
                        TempData["Error"] = "ID document must be less than 5MB.";
                        return RedirectToAction("Profile");
                    }

                    if (!string.IsNullOrEmpty(user.IDImageUrl))
                    {
                        var oldPath = Server.MapPath("~" + user.IDImageUrl);
                        if (System.IO.File.Exists(oldPath))
                        {
                            try
                            {
                                System.IO.File.Delete(oldPath);
                            }
                            catch { }
                        }
                    }

                    var uploadsDir = Server.MapPath("~/Content/Uploads/IDs");
                    if (!Directory.Exists(uploadsDir))
                    {
                        Directory.CreateDirectory(uploadsDir);
                    }

                    var fileName = $"id_{userId}_{Guid.NewGuid()}{extension}";
                    var filePath = Path.Combine(uploadsDir, fileName);
                    IDImage.SaveAs(filePath);

                    user.IDImageUrl = "/Content/Uploads/IDs/" + fileName;
                    user.IDStatus = "Pending";
                    TempData["IDStatusChanged"] = "true";
                }

                user.UpdatedAt = DateTime.Now;
                db.SaveChanges();

                Session["UserName"] = user.FullName;

                TempData["Success"] = "Profile updated successfully!";
                return RedirectToAction("Profile");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while updating your profile. Please try again.";
                return RedirectToAction("Profile");
            }
        }

        public ActionResult CheckSession()
        {
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetNoStore();
            Response.AppendHeader("Cache-Control", "no-cache, no-store, must-revalidate");

            if (Session["UserId"] != null)
            {
                return new HttpStatusCodeResult(200, "OK");
            }
            else
            {
                return new HttpStatusCodeResult(401, "Unauthorized");
            }
        }

        }
    }
