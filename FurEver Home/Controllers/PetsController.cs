using FurEver_Home.Models;
using System;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace FurEver_Home.Controllers
{
    public class PetsController : Controller
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

                model.UserId = userId;
                model.ApplicationDate = DateTime.Now;
                model.Status = "Pending";
                model.CreatedAt = DateTime.Now;
                model.UpdatedAt = DateTime.Now;

                db.AdoptionApplications.Add(model);
                db.SaveChanges();

                TempData["Success"] = "Your adoption application has been submitted successfully! We'll contact you soon.";
                return RedirectToAction("Index", "Home");
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