using FurEver_Home.Models;
using System;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using FurEver_Home.Filters;

namespace FurEver_Home.Controllers
{
    public class PetsController : BaseController
    {
        private FurEverHomeContext db = new FurEverHomeContext();

        // GET: Pets (Browse all pets)
        public ActionResult Index()
        {
            var pets = db.Pets.Include(p => p.PetType)
                              .Where(p => !p.IsAdopted)
                              .OrderByDescending(p => p.DateAdded)
                              .ToList();

            ViewBag.PetType = null; // For showing "All Pets"
            return View(pets);
        }

        // GET: Pets/Dogs
        public ActionResult Dogs()
        {
            var dogs = db.Pets.Include(p => p.PetType)
                              .Where(p => p.PetTypeId == 1 && !p.IsAdopted)
                              .OrderByDescending(p => p.DateAdded)
                              .ToList();

            ViewBag.PetType = "Dogs";
            return View("Index", dogs);
        }

        // GET: Pets/Cats
        public ActionResult Cats()
        {
            var cats = db.Pets.Include(p => p.PetType)
                              .Where(p => p.PetTypeId == 2 && !p.IsAdopted)
                              .OrderByDescending(p => p.DateAdded)
                              .ToList();

            ViewBag.PetType = "Cats";
            return View("Index", cats);
        }

        // GET: Pets/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return RedirectToAction("Index");
            }

            var pet = db.Pets.Include(p => p.PetType).FirstOrDefault(p => p.PetId == id);
            if (pet == null)
            {
                return HttpNotFound();
            }

            // Count pending applications for this pet
            ViewBag.PendingApplicationsCount = db.AdoptionApplications
                .Count(a => a.PetId == id && a.Status == "Pending");

            // Check if current user has already applied
            if (Session["UserId"] != null)
            {
                int userId = (int)Session["UserId"];
                ViewBag.UserHasApplied = db.AdoptionApplications
                    .Any(a => a.PetId == id && a.UserId == userId && a.Status == "Pending");
            }

            return View(pet);
        }

        // GET: Pets/Apply/5
        public ActionResult Apply(int? id)
        {
            // Check if user is logged in
            if (Session["UserId"] == null)
            {
                TempData["Error"] = "Please login to apply for adoption.";
                return RedirectToAction("Login", "Account");
            }

            if (id == null)
            {
                return RedirectToAction("Index");
            }

            var pet = db.Pets.Include(p => p.PetType).FirstOrDefault(p => p.PetId == id);
            if (pet == null)
            {
                return HttpNotFound();
            }

            // Check if pet is already adopted
            if (pet.IsAdopted)
            {
                TempData["Error"] = "This pet has already been adopted.";
                return RedirectToAction("Details", new { id = id });
            }

            var model = new AdoptionApplication
            {
                PetId = id.Value,
                Pet = pet
            };

            return View(model);
        }
        // POST: Pets/Apply
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Apply(AdoptionApplication model)
        {
            if (Session["UserId"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (ModelState.IsValid)
            {
                int userId = (int)Session["UserId"];

                // Check if user already applied for this pet
                var existingApplication = db.AdoptionApplications
                    .FirstOrDefault(a => a.UserId == userId && a.PetId == model.PetId && a.Status == "Pending");

                if (existingApplication != null)
                {
                    TempData["Error"] = "You have already applied to adopt this pet. Please wait for admin review.";
                    return RedirectToAction("Details", new { id = model.PetId });
                }

                model.UserId = userId;
                model.ApplicationDate = DateTime.Now;
                model.Status = "Pending";
                model.CreatedAt = DateTime.Now;
                model.UpdatedAt = DateTime.Now;

                db.AdoptionApplications.Add(model);
                db.SaveChanges();

                TempData["Success"] = $"Your adoption application for {model.Pet.Name} has been submitted successfully! We'll review it and contact you within 24-48 hours.";
                return RedirectToAction("MyApplications");
            }

            // Reload pet data if validation fails
            model.Pet = db.Pets.Find(model.PetId);
            return View(model);
        }

        // GET: Pets/PostForAdoption
        public ActionResult PostForAdoption()
        {
            if (Session["UserId"] == null)
            {
                TempData["Error"] = "Please login to post a pet for adoption.";
                return RedirectToAction("Login", "Account");
            }

            int userId = (int)Session["UserId"];
            var user = db.Users.Find(userId);

            // Check if user's ID is verified
            if (user.IDStatus != "Verified")
            {
                TempData["Error"] = "Your ID must be verified before you can post pets for adoption. Please upload your ID in your profile.";
                return RedirectToAction("Profile", "Account");
            }

            ViewBag.PetTypes = db.PetTypes.ToList();
            return View();
        }

        // POST: Pets/PostForAdoption
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult PostForAdoption(Pet model, HttpPostedFileBase PetImage)
        {
            if (Session["UserId"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int userId = (int)Session["UserId"];

            if (ModelState.IsValid)
            {
                // Handle pet image upload
                if (PetImage != null && PetImage.ContentLength > 0)
                {
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                    var extension = Path.GetExtension(PetImage.FileName).ToLower();

                    if (allowedExtensions.Contains(extension))
                    {
                        var uploadsDir = Server.MapPath("~/Content/Uploads/Pets");
                        if (!Directory.Exists(uploadsDir))
                        {
                            Directory.CreateDirectory(uploadsDir);
                        }

                        var fileName = Guid.NewGuid().ToString() + extension;
                        var filePath = Path.Combine(uploadsDir, fileName);
                        PetImage.SaveAs(filePath);
                        model.ImageUrl = "/Content/Uploads/Pets/" + fileName;
                    }
                }

                model.DateAdded = DateTime.Now;
                model.CreatedAt = DateTime.Now;
                model.UpdatedAt = DateTime.Now;
                model.IsAdopted = false;
                model.CreatedBy = userId;

                db.Pets.Add(model);
                db.SaveChanges();

                TempData["Success"] = $"Pet '{model.Name}' has been posted for adoption successfully!";
                return RedirectToAction("Index", "Home");
            }

            ViewBag.PetTypes = db.PetTypes.ToList();
            return View(model);
        }

        // GET: Pets/ClaimPet/5
        public ActionResult ClaimPet(int id)
        {
            if (Session["UserId"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int userId = (int)Session["UserId"];

            var application = db.AdoptionApplications
                .Include(a => a.Pet)
                .Include(a => a.Pet.PetType)
                .FirstOrDefault(a => a.ApplicationId == id && a.UserId == userId);

            if (application == null || !application.IsReadyForPickup || application.ClaimedDate != null)
            {
                TempData["Error"] = "This application is not ready for claiming yet.";
                return RedirectToAction("MyApplications");
            }

            return View(application);
        }

        // POST: Pets/ConfirmClaim/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ConfirmClaim(int id)
        {
            if (Session["UserId"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int userId = (int)Session["UserId"];

            var application = db.AdoptionApplications.Find(id);

            if (application != null && application.UserId == userId && application.IsReadyForPickup && application.ClaimedDate == null)
            {
                application.ClaimedDate = DateTime.Now;
                application.Status = "Completed";
                application.UpdatedAt = DateTime.Now;

                db.SaveChanges();

                TempData["Success"] = $"Congratulations! {application.Pet.Name} is now officially yours! 🎉";
                return RedirectToAction("MyApplications");
            }

            TempData["Error"] = "Unable to process claim.";
            return RedirectToAction("MyApplications");
        }

        // GET: Pets/MyApplications
        public ActionResult MyApplications()
        {
            if (Session["UserId"] == null)
            {
                TempData["Error"] = "Please login to view your applications.";
                return RedirectToAction("Login", "Account");
            }

            int userId = (int)Session["UserId"];

            var applications = db.AdoptionApplications
                .Include(a => a.Pet)
                .Include(a => a.Pet.PetType)
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.ApplicationDate)
                .ToList();

            return View(applications);
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



