using System.Web.Mvc;
using FurEver_Home.Models;

namespace FurEver_Home.Controllers
{
    public class AccountController : Controller
    {
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
                // TODO: Add your authentication logic here
                // Example: Check if user exists in database
                // if (IsValidUser(model.Email, model.Password))
                // {
                //     FormsAuthentication.SetAuthCookie(model.Email, model.RememberMe);
                //     return RedirectToAction("Index", "Home");
                // }

                // For now, just show a success message
                TempData["Success"] = "Login functionality will be implemented!";
                return RedirectToAction("Index", "Home");
            }

            // If we got this far, something failed, redisplay form
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
                // TODO: Add your registration logic here
                // Example: Create new user in database
                // var user = new User
                // {
                //     FullName = model.FullName,
                //     Email = model.Email,
                //     Password = HashPassword(model.Password)
                // };
                // db.Users.Add(user);
                // db.SaveChanges();

                // For now, redirect to login
                TempData["Success"] = "Account created successfully! Please login.";
                return RedirectToAction("Login");
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        // GET: Account/Logout
        public ActionResult Logout()
        {
            // TODO: Add logout logic
            // FormsAuthentication.SignOut();
            TempData["Success"] = "You have been logged out.";
            return RedirectToAction("Login");
        }
    }
}