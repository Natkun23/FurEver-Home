using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using FurEver_Home.Models;

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
                              .ToList();
            return View(pets);
        }

        // GET: Pets/Dogs
        public ActionResult Dogs()
        {
            var dogs = db.Pets.Include(p => p.PetType)
                              .Where(p => p.PetTypeId == 1 && !p.IsAdopted)
                              .ToList();
            return View("Index", dogs);
        }

        // GET: Pets/Cats
        public ActionResult Cats()
        {
            var cats = db.Pets.Include(p => p.PetType)
                              .Where(p => p.PetTypeId == 2 && !p.IsAdopted)
                              .ToList();
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
            if (id == null)
            {
                return RedirectToAction("Index");
            }

            var pet = db.Pets.Include(p => p.PetType).FirstOrDefault(p => p.PetId == id);
            if (pet == null)
            {
                return HttpNotFound();
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
            if (ModelState.IsValid)
            {
                // TODO: Get actual logged-in user ID (for now using 1)
                model.UserId = 1;
                model.ApplicationDate = DateTime.Now;
                model.Status = "Pending";

                db.AdoptionApplications.Add(model);
                db.SaveChanges();

                TempData["Success"] = "Your adoption application has been submitted! We'll contact you soon.";
                return RedirectToAction("Index", "Home");
            }

            model.Pet = db.Pets.Find(model.PetId);
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