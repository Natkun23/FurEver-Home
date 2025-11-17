using System;
using System.Linq;
using System.Web.Mvc;
using System.Web.Security;
using FurEver_Home.Models;

namespace FurEver_Home.Controllers
{
    public class AccountController : Controller
    {
        private FurEverHomeContext db = new FurEverHomeContext();

        // GET: Account/Login
        public ActionResult Login()
        {
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
                    // Check if password matches (in production, use hashed passwords!)
                    if (user.Password == model.Password)
                    {
                        // Check if user is active
                        if (!user.IsActive)
                        {
                            ModelState.AddModelError("", "Your account has been deactivated. Please contact support.");
                            return View(model);
                        }

                        // Set authentication cookie
                        FormsAuthentication.SetAuthCookie(user.Email, model.RememberMe);

                        // Store user info in session
                        Session["UserId"] = user.UserId;
                        Session["UserName"] = user.FullName;
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

                // If we got here, login failed
                ModelState.AddModelError("", "Invalid email or password.");
            }

            return View(model);
        }

        // GET: Account/Register
        public ActionResult Register()
        {
            return View();
        }

        // POST: Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Check if email already exists
                var existingUser = db.Users.FirstOrDefault(u => u.Email == model.Email);
                if (existingUser != null)
                {
                    ModelState.AddModelError("Email", "This email is already registered.");
                    return View(model);
                }

                // Create new user
                var user = new User
                {
                    FullName = model.FullName,
                    Email = model.Email,
                    Password = model.Password, // TODO: Hash password in production!
                    Role = "Client",
                    IDStatus = "Pending",
                    IsActive = true,
                    DateRegistered = DateTime.Now,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                db.Users.Add(user);
                db.SaveChanges();

                TempData["Success"] = "Account created successfully! Please login.";
                return RedirectToAction("Login");
            }

            return View(model);
        }

        // GET: Account/Logout
        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            Session.Clear();
            Session.Abandon();

            TempData["Success"] = "You have been logged out.";
            return RedirectToAction("Login");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}