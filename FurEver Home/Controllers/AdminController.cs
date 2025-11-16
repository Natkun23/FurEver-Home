using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using FurEver_Home.Models;

namespace FurEver_Home.Controllers
{
    public class AdminController : Controller
    {
        // Temporary in-memory data
        private static List<User> users = new List<User>
        {
            new User { UserId = 1, FullName = "John Doe", Email = "john@example.com", Role = "Client", IDType = "National ID", IDStatus = "Pending", DateRegistered = DateTime.Now.AddDays(-5), IsActive = true },
            new User { UserId = 2, FullName = "Jane Smith", Email = "jane@example.com", Role = "Client", IDType = "Passport", IDStatus = "Verified", DateRegistered = DateTime.Now.AddDays(-10), IsActive = true },
            new User { UserId = 3, FullName = "Admin User", Email = "admin@furever.com", Role = "Admin", IDStatus = "Verified", DateRegistered = DateTime.Now.AddDays(-100), IsActive = true }
        };

        private static List<Pet> pets = new List<Pet>
        {
            new Pet { PetId = 1, Name = "Charlie", Type = "Dog", Breed = "Poodle Mix", Age = 1, Gender = "Male", Size = "Small", Description = "Gentle pup", Traits = "Gentle,Quiet", IsAdopted = false, DateAdded = DateTime.Now.AddDays(-3) },
            new Pet { PetId = 2, Name = "Max", Type = "Dog", Breed = "Golden Retriever", Age = 3, Gender = "Male", Size = "Large", Description = "Friendly dog", Traits = "Friendly,Energetic", IsAdopted = false, DateAdded = DateTime.Now.AddDays(-7) }
        };

        private static List<AdoptionApplication> applications = new List<AdoptionApplication>
        {
            new AdoptionApplication { ApplicationId = 1, UserId = 1, PetId = 1, PhoneNumber = "123-456-7890", Address = "123 Main St", HousingType = "House", HasPets = "No", Status = "Pending", ApplicationDate = DateTime.Now.AddDays(-2) }
        };

        // GET: Admin/Dashboard
        public ActionResult Dashboard()
        {
            ViewBag.TotalUsers = users.Count(u => u.Role == "Client");
            ViewBag.TotalPets = pets.Count(p => !p.IsAdopted);
            ViewBag.PendingVerifications = users.Count(u => u.IDStatus == "Pending");
            ViewBag.PendingApplications = applications.Count(a => a.Status == "Pending");

            return View();
        }

        // ========== USER MANAGEMENT ==========

        // GET: Admin/Users
        public ActionResult Users()
        {
            var clientUsers = users.Where(u => u.Role == "Client").ToList();
            return View(clientUsers);
        }

        // GET: Admin/UserDetails/5
        public ActionResult UserDetails(int id)
        {
            var user = users.FirstOrDefault(u => u.UserId == id);
            if (user == null)
            {
                return HttpNotFound();
            }
            return View(user);
        }

        // POST: Admin/ToggleUserStatus/5
        [HttpPost]
        public ActionResult ToggleUserStatus(int id)
        {
            var user = users.FirstOrDefault(u => u.UserId == id);
            if (user != null)
            {
                user.IsActive = !user.IsActive;
                TempData["Success"] = $"User {user.FullName} has been {(user.IsActive ? "activated" : "deactivated")}.";
            }
            return RedirectToAction("Users");
        }

        // ========== ID VERIFICATION ==========

        // GET: Admin/VerifyIDs
        public ActionResult VerifyIDs()
        {
            var pendingVerifications = users.Where(u => u.IDStatus == "Pending").ToList();
            return View(pendingVerifications);
        }

        // GET: Admin/VerifyIDDetails/5
        public ActionResult VerifyIDDetails(int id)
        {
            var user = users.FirstOrDefault(u => u.UserId == id);
            if (user == null)
            {
                return HttpNotFound();
            }
            return View(user);
        }

        // POST: Admin/ApproveID/5
        [HttpPost]
        public ActionResult ApproveID(int id)
        {
            var user = users.FirstOrDefault(u => u.UserId == id);
            if (user != null)
            {
                user.IDStatus = "Verified";
                TempData["Success"] = $"ID for {user.FullName} has been verified.";
            }
            return RedirectToAction("VerifyIDs");
        }

        // POST: Admin/RejectID/5
        [HttpPost]
        public ActionResult RejectID(int id, string reason)
        {
            var user = users.FirstOrDefault(u => u.UserId == id);
            if (user != null)
            {
                user.IDStatus = "Rejected";
                TempData["Success"] = $"ID for {user.FullName} has been rejected.";
            }
            return RedirectToAction("VerifyIDs");
        }

        // ========== PET MANAGEMENT ==========

        // GET: Admin/Pets
        public ActionResult Pets()
        {
            return View(pets);
        }

        // GET: Admin/AddPet
        public ActionResult AddPet()
        {
            return View();
        }

        // POST: Admin/AddPet
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddPet(Pet model)
        {
            if (ModelState.IsValid)
            {
                model.PetId = pets.Count > 0 ? pets.Max(p => p.PetId) + 1 : 1;
                model.DateAdded = DateTime.Now;
                model.IsAdopted = false;
                pets.Add(model);
                TempData["Success"] = $"Pet '{model.Name}' has been added successfully!";
                return RedirectToAction("Pets");
            }
            return View(model);
        }

        // GET: Admin/EditPet/5
        public ActionResult EditPet(int id)
        {
            var pet = pets.FirstOrDefault(p => p.PetId == id);
            if (pet == null)
            {
                return HttpNotFound();
            }
            return View(pet);
        }

        // POST: Admin/EditPet/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditPet(Pet model)
        {
            if (ModelState.IsValid)
            {
                var pet = pets.FirstOrDefault(p => p.PetId == model.PetId);
                if (pet != null)
                {
                    pet.Name = model.Name;
                    pet.Type = model.Type;
                    pet.Breed = model.Breed;
                    pet.Age = model.Age;
                    pet.Gender = model.Gender;
                    pet.Size = model.Size;
                    pet.Description = model.Description;
                    pet.Traits = model.Traits;
                    pet.ImageUrl = model.ImageUrl;

                    TempData["Success"] = $"Pet '{pet.Name}' has been updated successfully!";
                    return RedirectToAction("Pets");
                }
            }
            return View(model);
        }

        // POST: Admin/DeletePet/5
        [HttpPost]
        public ActionResult DeletePet(int id)
        {
            var pet = pets.FirstOrDefault(p => p.PetId == id);
            if (pet != null)
            {
                pets.Remove(pet);
                TempData["Success"] = $"Pet '{pet.Name}' has been deleted.";
            }
            return RedirectToAction("Pets");
        }

        // ========== ADOPTION APPLICATIONS ==========

        // GET: Admin/Applications
        public ActionResult Applications()
        {
            // Populate user and pet data
            foreach (var app in applications)
            {
                app.User = users.FirstOrDefault(u => u.UserId == app.UserId);
                app.Pet = pets.FirstOrDefault(p => p.PetId == app.PetId);
            }
            return View(applications);
        }

        // GET: Admin/ApplicationDetails/5
        public ActionResult ApplicationDetails(int id)
        {
            var application = applications.FirstOrDefault(a => a.ApplicationId == id);
            if (application == null)
            {
                return HttpNotFound();
            }

            application.User = users.FirstOrDefault(u => u.UserId == application.UserId);
            application.Pet = pets.FirstOrDefault(p => p.PetId == application.PetId);

            return View(application);
        }

        // POST: Admin/ApproveApplication/5
        [HttpPost]
        public ActionResult ApproveApplication(int id)
        {
            var application = applications.FirstOrDefault(a => a.ApplicationId == id);
            if (application != null)
            {
                application.Status = "Approved";
                var pet = pets.FirstOrDefault(p => p.PetId == application.PetId);
                if (pet != null)
                {
                    pet.IsAdopted = true;
                }
                TempData["Success"] = "Application has been approved!";
            }
            return RedirectToAction("Applications");
        }

        // POST: Admin/RejectApplication/5
        [HttpPost]
        public ActionResult RejectApplication(int id)
        {
            var application = applications.FirstOrDefault(a => a.ApplicationId == id);
            if (application != null)
            {
                application.Status = "Rejected";
                TempData["Success"] = "Application has been rejected.";
            }
            return RedirectToAction("Applications");
        }
    }
}