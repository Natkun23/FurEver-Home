
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
    public class AdminController : BaseController
    {
        private FurEverHomeContext db = new FurEverHomeContext();

        public ActionResult Dashboard()
        {
            ViewBag.TotalUsers = db.Users.Count(u => u.Role == "Client");
            ViewBag.TotalPets = db.Pets.Count(p => !p.IsAdopted);
            ViewBag.TotalDogs = db.Pets.Count(p => p.PetTypeId == 1 && !p.IsAdopted);
            ViewBag.TotalCats = db.Pets.Count(p => p.PetTypeId == 2 && !p.IsAdopted);
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
            ViewBag.AdoptedCount = db.Pets.Count(p => p.IsAdopted); // Add this
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

        // GET: Admin/AdoptedPets
        public ActionResult AdoptedPets()
        {
            var adoptedPetsData = db.Pets
                .Include(p => p.PetType)
                .Include(p => p.Creator)
                .Where(p => p.IsAdopted == true)
                .OrderByDescending(p => p.UpdatedAt)
                .ToList()
                .Select(pet => new AdoptedPetViewModel
                {
                    Pet = pet,
                    Application = db.AdoptionApplications
                        .Include(a => a.User)
                        .FirstOrDefault(a => a.PetId == pet.PetId && a.Status == "Completed")
                })
                .ToList();

            // For the badge count across all admin pages
            ViewBag.AdoptedCount = db.Pets.Count(p => p.IsAdopted);

            return View(adoptedPetsData);
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
                application.ReviewedBy = (int)Session["UserId"];

                var pet = db.Pets.Find(application.PetId);
                if (pet != null)
                {
                    pet.IsAdopted = true;
                    pet.UpdatedAt = DateTime.Now;
                }

                // CREATE NOTIFICATION FOR USER
                var notification = new UserNotification
                {
                    UserId = application.UserId,
                    NotificationType = "Application_Approved",
                    Title = "Adoption Application Approved! 🎉",
                    Message = $"Congratulations! Your application to adopt {pet.Name} has been approved! Please proceed to claim your new companion.",
                    IsRead = false,
                    CreatedAt = DateTime.Now
                };
                db.UserNotifications.Add(notification);

                db.SaveChanges();
                TempData["Success"] = $"Application approved! {pet.Name} is now ready for pickup.";
            }
            return RedirectToAction("Applications");
        }



        // POST: Admin/RejectApplication/5
        [HttpPost]
        public ActionResult RejectApplication(int id, string rejectionReason)
        {
            var application = db.AdoptionApplications.Find(id);
            if (application != null)
            {
                application.Status = "Rejected";
                application.RejectionReason = rejectionReason;
                application.ReviewedDate = DateTime.Now;
                application.ReviewedBy = (int)Session["UserId"];

                // CREATE NOTIFICATION FOR USER
                var notification = new UserNotification
                {
                    UserId = application.UserId,
                    NotificationType = "Application_Rejected",
                    Title = "Adoption Application Update",
                    Message = $"Unfortunately, your application to adopt {application.Pet.Name} was not approved. Reason: {rejectionReason}",
                    IsRead = false,
                    CreatedAt = DateTime.Now
                };
                db.UserNotifications.Add(notification);

                db.SaveChanges();
                TempData["Success"] = "Application has been rejected with reason provided.";
            }
            return RedirectToAction("Applications");
        }


        // GET: Admin/SetPickupDetails/5
        public ActionResult SetPickupDetails(int id)
        {
            var application = db.AdoptionApplications
                                .Include(a => a.User)
                                .Include(a => a.Pet)
                                .FirstOrDefault(a => a.ApplicationId == id);

            if (application == null || application.Status != "Approved")
            {
                return HttpNotFound();
            }

            return View(application);
        }

        // POST: Admin/SetPickupDetails/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SetPickupDetails(int id, string pickupLocation, DateTime pickupDate, string pickupNotes)
        {
            var application = db.AdoptionApplications.Find(id);
            if (application != null && application.Status == "Approved")
            {
                application.IsReadyForPickup = true;
                application.PickupLocation = pickupLocation;
                application.PickupDate = pickupDate;
                application.PickupNotes = pickupNotes;
                application.UpdatedAt = DateTime.Now;

                // NOTIFY USER
                var notification = new UserNotification
                {
                    UserId = application.UserId,
                    NotificationType = "Pickup_Ready",
                    Title = "Your Pet is Ready for Pickup! 🐾",
                    Message = $"{application.Pet.Name} is ready! Pickup at: {pickupLocation} on {pickupDate.ToString("MMM dd, yyyy")}. {pickupNotes}",
                    IsRead = false,
                    CreatedAt = DateTime.Now
                };
                db.UserNotifications.Add(notification);

                db.SaveChanges();
                TempData["Success"] = $"Pickup details set! {application.User.FullName} has been notified.";
                return RedirectToAction("Applications");
            }

            TempData["Error"] = "Unable to set pickup details.";
            return RedirectToAction("ApplicationDetails", new { id = id });
        }

        // POST: Admin/ConfirmTurnover/5
        [HttpPost]
        public ActionResult ConfirmTurnover(int id)
        {
            var application = db.AdoptionApplications.Find(id);
            if (application != null && application.IsReadyForPickup)
            {
                application.ClaimedDate = DateTime.Now;
                application.Status = "Completed";
                application.UpdatedAt = DateTime.Now;

                // FINAL NOTIFICATION
                var notification = new UserNotification
                {
                    UserId = application.UserId,
                    NotificationType = "Adoption_Complete",
                    Title = "Adoption Complete! 🎊",
                    Message = $"Congratulations! {application.Pet.Name} is now officially yours. Thank you for giving a loving home!",
                    IsRead = false,
                    CreatedAt = DateTime.Now
                };
                db.UserNotifications.Add(notification);

                db.SaveChanges();
                TempData["Success"] = $"Turnover completed! {application.Pet.Name} has been successfully adopted.";
            }
            return RedirectToAction("Applications");
        }

        // POST: Admin/DeletePet/5 - FIXED VERSION
        [HttpPost]
        public ActionResult DeletePet(int id)
        {
            var pet = db.Pets.Find(id);
            if (pet != null)
            {
                // CHECK IF PET HAS APPLICATIONS
                var hasApplications = db.AdoptionApplications.Any(a => a.PetId == id);

                if (hasApplications)
                {
                    TempData["Error"] = $"Cannot delete '{pet.Name}' because there are adoption applications associated with this pet. Please handle the applications first.";
                    return RedirectToAction("Pets");
                }

                db.Pets.Remove(pet);
                db.SaveChanges();
                TempData["Success"] = $"Pet '{pet.Name}' has been deleted.";
            }
            return RedirectToAction("Pets");
        }
    }
}
