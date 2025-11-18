using FurEver_Home.Models;
using System;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace FurEver_Home.Controllers
{
    public class AdminController : Controller
    {
        private FurEverHomeContext db = new FurEverHomeContext();

        // GET: Admin/Dashboard
        public ActionResult Dashboard()
        {
            ViewBag.TotalUsers = db.Users.Count(u => u.Role == "Client");
            ViewBag.TotalPets = db.Pets.Count(p => !p.IsAdopted);
            ViewBag.PendingVerifications = db.Users.Count(u => u.IDStatus == "Pending");
            ViewBag.PendingApplications = db.AdoptionApplications.Count(a => a.Status == "Pending");

            return View();
        }

        // ========== USER MANAGEMENT ==========

        // GET: Admin/Users
        public ActionResult Users()
        {
            var clientUsers = db.Users.Where(u => u.Role == "Client").ToList();
            return View(clientUsers);
        }

        // GET: Admin/UserDetails/5
        public ActionResult UserDetails(int id)
        {
            var user = db.Users.Find(id);
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
            var user = db.Users.Find(id);
            if (user != null)
            {
                user.IsActive = !user.IsActive;
                user.UpdatedAt = DateTime.Now;
                db.SaveChanges();
                TempData["Success"] = $"User {user.FullName} has been {(user.IsActive ? "activated" : "deactivated")}.";
            }
            return RedirectToAction("Users");
        }

        // ========== ID VERIFICATION ==========

        // GET: Admin/VerifyIDs
        public ActionResult VerifyIDs()
        {
            var pendingVerifications = db.Users.Where(u => u.IDStatus == "Pending").ToList();
            return View(pendingVerifications);
        }

        // GET: Admin/VerifyIDDetails/5
        public ActionResult VerifyIDDetails(int id)
        {
            var user = db.Users.Find(id);
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
            var user = db.Users.Find(id);
            if (user != null)
            {
                user.IDStatus = "Verified";
                user.UpdatedAt = DateTime.Now;

                // CREATE NOTIFICATION
                var notification = new UserNotification
                {
                    UserId = user.UserId,
                    NotificationType = "ID_Verified",
                    Title = "ID Verified Successfully!",
                    Message = "Congratulations! Your ID has been verified. You can now post pets for adoption.",
                    IsRead = false,
                    CreatedAt = DateTime.Now
                };
                db.UserNotifications.Add(notification);

                db.SaveChanges();
                TempData["Success"] = $"ID for {user.FullName} has been verified.";
            }
            return RedirectToAction("VerifyIDs");
        }

        // POST: Admin/RejectID/5
        [HttpPost]
        public ActionResult RejectID(int id, string reason)
        {
            var user = db.Users.Find(id);
            if (user != null)
            {
                user.IDStatus = "Rejected";
                user.UpdatedAt = DateTime.Now;
                db.SaveChanges();
                TempData["Success"] = $"ID for {user.FullName} has been rejected.";
            }
            return RedirectToAction("VerifyIDs");
        }

        // ========== PET MANAGEMENT ==========

        // GET: Admin/Pets
        public ActionResult Pets()
        {
            var pets = db.Pets.Include(p => p.PetType).ToList();
            return View(pets);
        }

        // GET: Admin/AddPet
        public ActionResult AddPet()
        {
            ViewBag.PetTypes = db.PetTypes.ToList();
            return View();
        }

        // POST: Admin/AddPet
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddPet(Pet model, HttpPostedFileBase PetImage)
        {
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
                model.CreatedBy = 1; // TODO: Get from session

                db.Pets.Add(model);
                db.SaveChanges();
                TempData["Success"] = $"Pet '{model.Name}' has been added successfully!";
                return RedirectToAction("Pets");
            }

            ViewBag.PetTypes = db.PetTypes.ToList();
            return View(model);
        }

        // GET: Admin/EditPet/5
        public ActionResult EditPet(int id)
        {
            var pet = db.Pets.Find(id);
            if (pet == null)
            {
                return HttpNotFound();
            }
            ViewBag.PetTypes = db.PetTypes.ToList();
            return View(pet);
        }

        // POST: Admin/EditPet/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditPet(Pet model, HttpPostedFileBase PetImage)
        {
            if (ModelState.IsValid)
            {
                var pet = db.Pets.Find(model.PetId);
                if (pet != null)
                {
                    // Handle pet image upload if new image is provided
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
                            pet.ImageUrl = "/Content/Uploads/Pets/" + fileName;
                        }
                    }

                    // Update pet properties
                    pet.Name = model.Name;
                    pet.PetTypeId = model.PetTypeId;
                    pet.Breed = model.Breed;
                    pet.Age = model.Age;
                    pet.Gender = model.Gender;
                    pet.Size = model.Size;
                    pet.Description = model.Description;
                    pet.Traits = model.Traits;
                    pet.Vaccines = model.Vaccines;
                    pet.DaysInCenter = model.DaysInCenter;
                    pet.WhyAdoptMe = model.WhyAdoptMe;
                    pet.IsHealthy = model.IsHealthy;
                    pet.IsNeutered = model.IsNeutered;
                    pet.UpdatedAt = DateTime.Now;
                    pet.UpdatedBy = 1; // TODO: Get from session

                    db.SaveChanges();
                    TempData["Success"] = $"Pet '{pet.Name}' has been updated successfully!";
                    return RedirectToAction("Pets");
                }
            }

            ViewBag.PetTypes = db.PetTypes.ToList();
            return View(model);
        }

        // POST: Admin/DeletePet/5
        [HttpPost]
        public ActionResult DeletePet(int id)
        {
            var pet = db.Pets.Find(id);
            if (pet != null)
            {
                db.Pets.Remove(pet);
                db.SaveChanges();
                TempData["Success"] = $"Pet '{pet.Name}' has been deleted.";
            }
            return RedirectToAction("Pets");
        }

        // ========== ADOPTION APPLICATIONS ==========

        // GET: Admin/Applications
        public ActionResult Applications()
        {
            var applications = db.AdoptionApplications
                                 .Include(a => a.User)
                                 .Include(a => a.Pet)
                                 .OrderByDescending(a => a.ApplicationDate)
                                 .ToList();
            return View(applications);
        }

        // GET: Admin/ApplicationDetails/5
        public ActionResult ApplicationDetails(int id)
        {
            var application = db.AdoptionApplications
                                .Include(a => a.User)
                                .Include(a => a.Pet)
                                .FirstOrDefault(a => a.ApplicationId == id);

            if (application == null)
            {
                return HttpNotFound();
            }

            return View(application);
        }

        // POST: Admin/ApproveApplication/5
        [HttpPost]
        public ActionResult ApproveApplication(int id)
        {
            var application = db.AdoptionApplications.Find(id);
            if (application != null)
            {
                application.Status = "Approved";
                application.ReviewedDate = DateTime.Now;
                application.ReviewedBy = 1; // TODO: Get from session

                var pet = db.Pets.Find(application.PetId);
                if (pet != null)
                {
                    pet.IsAdopted = true;
                    pet.UpdatedAt = DateTime.Now;
                }

                db.SaveChanges();
                TempData["Success"] = "Application has been approved!";
            }
            return RedirectToAction("Applications");
        }

        // POST: Admin/RejectApplication/5
        [HttpPost]
        public ActionResult RejectApplication(int id)
        {
            var application = db.AdoptionApplications.Find(id);
            if (application != null)
            {
                application.Status = "Rejected";
                application.ReviewedDate = DateTime.Now;
                application.ReviewedBy = 1; // TODO: Get from session

                db.SaveChanges();
                TempData["Success"] = "Application has been rejected.";
            }
            return RedirectToAction("Applications");
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