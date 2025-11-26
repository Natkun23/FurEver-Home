using FurEver_Home.Models;
using System;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using FurEver_Home.Filters;
using System.Collections.Generic;

namespace FurEver_Home.Controllers
{
    public class AdminController : BaseController
    {
        private FurEverHomeContext db = new FurEverHomeContext();

        public ActionResult Dashboard()
        {
            // ============ EXISTING STATS ============
            ViewBag.TotalUsers = db.Users.Count(u => u.Role == "Client");
            ViewBag.TotalPets = db.Pets.Count(p => !p.IsAdopted);
            ViewBag.TotalDogs = db.Pets.Count(p => p.PetTypeId == 1 && !p.IsAdopted);
            ViewBag.TotalCats = db.Pets.Count(p => p.PetTypeId == 2 && !p.IsAdopted);
            ViewBag.PendingVerifications = db.Users.Count(u => u.IDStatus == "Pending");
            ViewBag.PendingApplications = db.AdoptionApplications.Count(a => a.Status == "Pending");
            ViewBag.PendingCancellations = db.AdoptionApplications.Count(a => a.CancellationRequested && !a.CancellationApproved);
            ViewBag.PendingCount = db.Pets.Count(p => p.PostStatus == "Pending" && p.PostedByType == "Customer");

            // ============ NEW: RECENT ACTIVITIES ============
            var recentActivities = new List<ActivityLog>();

            // 1. Recent Adoption Applications (last 2)
            var recentApplications = db.AdoptionApplications
                .Include(a => a.User)
                .Include(a => a.Pet)
                .OrderByDescending(a => a.ApplicationDate)
                .Take(2)
                .ToList();

            foreach (var a in recentApplications)
            {
                recentActivities.Add(new ActivityLog
                {
                    Title = "New adoption application submitted",
                    Description = $"{a.User.FullName} applied to adopt {a.Pet.Name}",
                    Status = a.Status,
                    Icon = "file-alt",
                    IconColor = "blue",
                    Timestamp = a.ApplicationDate,
                    ActivityType = "Application",
                    UserId = a.UserId,
                    PetId = a.PetId,
                    ApplicationId = a.ApplicationId
                });
            }

            // 2. Recently Verified IDs (last 2)
            var verifiedUsers = db.Users
                .Where(u => u.IDStatus == "Verified")
                .OrderByDescending(u => u.UpdatedAt)
                .Take(2)
                .ToList();

            foreach (var u in verifiedUsers)
            {
                recentActivities.Add(new ActivityLog
                {
                    Title = "ID verification completed",
                    Description = $"{u.FullName}'s ID was verified",
                    Status = "Verified",
                    Icon = "check-circle",
                    IconColor = "green",
                    Timestamp = u.UpdatedAt,
                    ActivityType = "Verification",
                    UserId = u.UserId
                });
            }

            // 3. Pending Customer Pet Posts (last 2)
            var pendingPosts = db.Pets
                .Include(p => p.Owner)
                .Where(p => p.PostStatus == "Pending" && p.PostedByType == "Customer")
                .OrderByDescending(p => p.DateAdded)
                .Take(2)
                .ToList();

            foreach (var p in pendingPosts)
            {
                recentActivities.Add(new ActivityLog
                {
                    Title = "New pet post awaiting approval",
                    Description = $"{p.Name} ({p.Type}, {p.Age} {p.AgeUnit}) posted by {p.Owner?.FullName ?? "Unknown"}",
                    Status = "Pending",
                    Icon = "paw",
                    IconColor = "yellow",
                    Timestamp = p.DateAdded,
                    ActivityType = "PetAdded",
                    PetId = p.PetId
                    // UserId removed - Pet model doesn't have this property
                });
            }

            // 4. Recent User Registrations (last 2)
            var newUsers = db.Users
                .Where(u => u.Role == "Client")
                .OrderByDescending(u => u.DateRegistered)
                .Take(2)
                .ToList();

            foreach (var u in newUsers)
            {
                recentActivities.Add(new ActivityLog
                {
                    Title = "User registration completed",
                    Description = $"{u.FullName} created an account",
                    Status = u.IDStatus,
                    Icon = "user-plus",
                    IconColor = "purple",
                    Timestamp = u.DateRegistered,
                    ActivityType = "UserRegistered",
                    UserId = u.UserId
                });
            }

            // 5. Recently Completed Adoptions (last 2)
            var completedAdoptions = db.AdoptionApplications
                .Include(a => a.User)
                .Include(a => a.Pet)
                .Where(a => a.Status == "Completed")
                .OrderByDescending(a => a.ClaimedDate)
                .Take(2)
                .ToList();

            foreach (var a in completedAdoptions)
            {
                recentActivities.Add(new ActivityLog
                {
                    Title = "Pet successfully adopted",
                    Description = $"{a.Pet.Name} found their forever home with {a.User.FullName}!",
                    Status = "Completed",
                    Icon = "heart",
                    IconColor = "green",
                    Timestamp = a.ClaimedDate ?? a.UpdatedAt,
                    ActivityType = "Adoption",
                    UserId = a.UserId,
                    PetId = a.PetId,
                    ApplicationId = a.ApplicationId
                });
            }

            // 6. Recent Cancellation Requests (last 2)
            var cancellationRequests = db.AdoptionApplications
                .Include(a => a.User)
                .Include(a => a.Pet)
                .Where(a => a.CancellationRequested && !a.CancellationApproved)
                .OrderByDescending(a => a.CancellationRequestedDate)
                .Take(2)
                .ToList();

            foreach (var a in cancellationRequests)
            {
                recentActivities.Add(new ActivityLog
                {
                    Title = "Cancellation request received",
                    Description = $"{a.User.FullName} requested to cancel adoption of {a.Pet.Name}",
                    Status = "Pending",
                    Icon = "exclamation-triangle",
                    IconColor = "yellow",
                    Timestamp = a.CancellationRequestedDate ?? DateTime.Now,
                    ActivityType = "Application",
                    UserId = a.UserId,
                    PetId = a.PetId,
                    ApplicationId = a.ApplicationId
                });
            }

            // Sort by timestamp and take top 8
            ViewBag.RecentActivities = recentActivities
                .OrderByDescending(a => a.Timestamp)
                .Take(5)
                .ToList();

            return View();
        }
        // ========== HELPER METHOD: Time Ago ==========
        private string GetTimeAgo(DateTime date)
        {
            var timeSpan = DateTime.Now - date;

            if (timeSpan.TotalMinutes < 1)
                return "Just now";
            if (timeSpan.TotalMinutes < 60)
                return $"{(int)timeSpan.TotalMinutes} minute{((int)timeSpan.TotalMinutes != 1 ? "s" : "")} ago";
            if (timeSpan.TotalHours < 24)
                return $"{(int)timeSpan.TotalHours} hour{((int)timeSpan.TotalHours != 1 ? "s" : "")} ago";
            if (timeSpan.TotalDays < 7)
                return $"{(int)timeSpan.TotalDays} day{((int)timeSpan.TotalDays != 1 ? "s" : "")} ago";
            if (timeSpan.TotalDays < 30)
                return $"{(int)(timeSpan.TotalDays / 7)} week{((int)(timeSpan.TotalDays / 7) != 1 ? "s" : "")} ago";
            if (timeSpan.TotalDays < 365)
                return $"{(int)(timeSpan.TotalDays / 30)} month{((int)(timeSpan.TotalDays / 30) != 1 ? "s" : "")} ago";

            return $"{(int)(timeSpan.TotalDays / 365)} year{((int)(timeSpan.TotalDays / 365) != 1 ? "s" : "")} ago";
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
            var pets = db.Pets.Include(p => p.PetType).Include(p => p.Creator).ToList();
            ViewBag.AdoptedCount = db.Pets.Count(p => p.IsAdopted);
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
        public ActionResult AddPet(Pet model, HttpPostedFileBase PetImage, string OrganizationName)
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
                model.CreatedBy = (int)Session["UserId"];

                // Set as organization post
                model.PostedByType = "Organization";
                model.OrganizationName = string.IsNullOrWhiteSpace(OrganizationName)
                    ? "FurEver Home Admin"
                    : OrganizationName.Trim();

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
        public ActionResult EditPet(Pet model, HttpPostedFileBase PetImage, string OrganizationName)
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
                    pet.AgeUnit = model.AgeUnit;
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
                    pet.UpdatedBy = (int)Session["UserId"];

                    // Update organization name if provided
                    if (!string.IsNullOrWhiteSpace(OrganizationName))
                    {
                        pet.PostedByType = "Organization";
                        pet.OrganizationName = OrganizationName.Trim();
                    }

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

        // ========== ⭐ NEW: APPROVE CUSTOMER PET POSTS ==========

        // GET: Admin/ApprovePets
        public ActionResult ApprovePets()
        {
            // Get all pending pet posts from customers
            var pendingPets = db.Pets
                .Include(p => p.PetType)
                .Include(p => p.Owner)  // Include the customer who posted
                .Where(p => p.PostStatus == "Pending" && p.PostedByType == "Customer")
                .OrderByDescending(p => p.DateAdded)
                .ToList();

            // Count custom questions for each pet
            var customQuestions = new Dictionary<int, int>();
            foreach (var pet in pendingPets)
            {
                var questionCount = db.PetScreeningQuestions.Count(q => q.PetId == pet.PetId);
                customQuestions[pet.PetId] = questionCount;
            }

            ViewBag.CustomQuestions = customQuestions;
            ViewBag.PendingCount = pendingPets.Count;

            return View(pendingPets);
        }

        // GET: Admin/ReviewPetPost/5
        public ActionResult ReviewPetPost(int id)
        {
            var pet = db.Pets
                .Include(p => p.PetType)
                .Include(p => p.Owner)
                .FirstOrDefault(p => p.PetId == id);

            if (pet == null)
            {
                return HttpNotFound();
            }

            // Load custom screening questions
            ViewBag.CustomQuestions = db.PetScreeningQuestions
                .Where(q => q.PetId == id)
                .OrderBy(q => q.OrderNumber)
                .ToList();

            return View(pet);
        }

        // POST: Admin/ApprovePetPost/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ApprovePetPost(int id)
        {
            var pet = db.Pets.Include(p => p.Owner).FirstOrDefault(p => p.PetId == id);

            if (pet != null && pet.PostStatus == "Pending")
            {
                pet.PostStatus = "Approved";
                pet.AdminVerified = true;
                pet.UpdatedAt = DateTime.Now;
                pet.UpdatedBy = (int)Session["UserId"];

                // Notify the pet owner
                var notification = new UserNotification
                {
                    UserId = pet.OwnerUserId.Value,
                    NotificationType = "Post_Approved",
                    Title = "Pet Post Approved! 🎉",
                    Message = $"Your pet post '{pet.Name}' has been approved and is now visible to potential adopters!",
                    IsRead = false,
                    CreatedAt = DateTime.Now
                };
                db.UserNotifications.Add(notification);

                db.SaveChanges();
                TempData["Success"] = $"Pet post '{pet.Name}' has been approved!";
            }

            return RedirectToAction("ApprovePets");
        }

        // POST: Admin/RejectPetPost/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RejectPetPost(int id, string rejectionReason)
        {
            var pet = db.Pets.Include(p => p.Owner).FirstOrDefault(p => p.PetId == id);

            if (pet != null && pet.PostStatus == "Pending")
            {
                pet.PostStatus = "Rejected";
                pet.AdminVerified = false;
                pet.UpdatedAt = DateTime.Now;
                pet.UpdatedBy = (int)Session["UserId"];

                // Notify the pet owner
                var notification = new UserNotification
                {
                    UserId = pet.OwnerUserId.Value,
                    NotificationType = "Post_Rejected",
                    Title = "Pet Post Rejected",
                    Message = $"Your pet post '{pet.Name}' was not approved. Reason: {rejectionReason}",
                    IsRead = false,
                    CreatedAt = DateTime.Now
                };
                db.UserNotifications.Add(notification);

                db.SaveChanges();
                TempData["Success"] = $"Pet post '{pet.Name}' has been rejected.";
            }

            return RedirectToAction("ApprovePets");
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

            ViewBag.AdoptedCount = db.Pets.Count(p => p.IsAdopted);

            return View(adoptedPetsData);
        }

        // POST: Admin/ApproveApplication/5
        [HttpPost]
        public ActionResult ApproveApplication(int id)
        {
            var application = db.AdoptionApplications
                .Include(a => a.Pet)
                .Include(a => a.Pet.PetType)
                .FirstOrDefault(a => a.ApplicationId == id);

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
        [ValidateAntiForgeryToken]
        public ActionResult RejectApplication(int id, string rejectionReason)
        {
            try
            {
                var application = db.AdoptionApplications
                    .Include(a => a.Pet)
                    .Include(a => a.Pet.PetType)
                    .Include(a => a.User)
                    .FirstOrDefault(a => a.ApplicationId == id);

                if (application == null)
                {
                    TempData["Error"] = "Application not found.";
                    return RedirectToAction("Applications");
                }

                application.Status = "Rejected";
                application.RejectionReason = rejectionReason;
                application.ReviewedDate = DateTime.Now;
                application.ReviewedBy = (int)Session["UserId"];
                application.UpdatedAt = DateTime.Now;

                // CREATE NOTIFICATION
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

                // SAVE FIRST
                db.SaveChanges();

                // THEN ARCHIVE
                try
                {
                    ArchiveApplicationToHistory(application, "Rejected");
                    db.SaveChanges();
                }
                catch (Exception archiveEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Archive error: {archiveEx.Message}");
                }

                TempData["Success"] = "Application has been rejected and user has been notified.";
                return RedirectToAction("Applications");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error: {ex.Message}";
                return RedirectToAction("Applications");
            }
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
            var application = db.AdoptionApplications
                .Include(a => a.Pet)
                .Include(a => a.User)
                .FirstOrDefault(a => a.ApplicationId == id);

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
            var application = db.AdoptionApplications
                .Include(a => a.Pet)
                .Include(a => a.User)
                .FirstOrDefault(a => a.ApplicationId == id);

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

        // ========== CANCELLATION REQUESTS MANAGEMENT ==========

        // GET: Admin/CancellationRequests
        public ActionResult CancellationRequests()
        {
            var cancellationRequests = db.AdoptionApplications
                .Include(a => a.User)
                .Include(a => a.Pet)
                .Include(a => a.Pet.PetType)
                .Where(a => a.CancellationRequested && !a.CancellationApproved && a.Status == "Approved")
                .OrderByDescending(a => a.CancellationRequestedDate)
                .ToList();

            return View(cancellationRequests);
        }

        // POST: Admin/ApproveCancellation/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ApproveCancellation(int id)
        {
            var application = db.AdoptionApplications
                .Include(a => a.Pet)
                .Include(a => a.Pet.PetType)
                .Include(a => a.User)
                .FirstOrDefault(a => a.ApplicationId == id);

            if (application != null && application.CancellationRequested)
            {
                application.CancellationApproved = true;
                application.CancellationReviewedBy = (int)Session["UserId"];
                application.CancellationReviewedDate = DateTime.Now;
                application.Status = "Cancelled";
                application.UpdatedAt = DateTime.Now;

                // Mark pet as available again
                var pet = db.Pets.Find(application.PetId);
                if (pet != null)
                {
                    pet.IsAdopted = false;
                    pet.UpdatedAt = DateTime.Now;
                }

                // Notify user
                var notification = new UserNotification
                {
                    UserId = application.UserId,
                    NotificationType = "Cancellation_Approved",
                    Title = "Cancellation Request Approved",
                    Message = $"Your cancellation request for {application.Pet.Name} has been approved. The pet is now available for other adopters.",
                    IsRead = false,
                    CreatedAt = DateTime.Now
                };
                db.UserNotifications.Add(notification);

                // Archive to history
                ArchiveApplicationToHistory(application, "Cancelled");

                db.SaveChanges();
                TempData["Success"] = $"Cancellation approved. {application.Pet.Name} is now available for adoption again.";
            }

            return RedirectToAction("CancellationRequests");
        }

        // POST: Admin/DenyCancellation/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DenyCancellation(int id, string denialReason)
        {
            var application = db.AdoptionApplications
                .Include(a => a.Pet)
                .Include(a => a.User)
                .FirstOrDefault(a => a.ApplicationId == id);

            if (application != null && application.CancellationRequested)
            {
                application.CancellationRequested = false;
                application.CancellationReason = null;
                application.CancellationRequestedDate = null;
                application.CancellationReviewedBy = (int)Session["UserId"];
                application.CancellationReviewedDate = DateTime.Now;
                application.AdminNotes = $"Cancellation denied: {denialReason}";
                application.UpdatedAt = DateTime.Now;

                // Notify user
                var notification = new UserNotification
                {
                    UserId = application.UserId,
                    NotificationType = "Cancellation_Denied",
                    Title = "Cancellation Request Denied",
                    Message = $"Your cancellation request for {application.Pet.Name} has been denied. Reason: {denialReason}. Please proceed with the adoption.",
                    IsRead = false,
                    CreatedAt = DateTime.Now
                };
                db.UserNotifications.Add(notification);

                db.SaveChanges();
                TempData["Success"] = "Cancellation request denied. User has been notified.";
            }

            return RedirectToAction("CancellationRequests");
        }

        // ========== ADOPTION HISTORY ==========

        // GET: Admin/AdoptionHistory
        public ActionResult AdoptionHistory()
        {
            var allHistory = db.AdoptionHistories
                .Include(h => h.User)
                .OrderByDescending(h => h.ArchivedAt)
                .ToList();

            // Statistics
            ViewBag.TotalCompleted = allHistory.Count(h => h.FinalStatus == "Completed");
            ViewBag.TotalWithdrawn = allHistory.Count(h => h.FinalStatus == "Withdrawn");
            ViewBag.TotalCancelled = allHistory.Count(h => h.FinalStatus == "Cancelled");
            ViewBag.TotalRejected = allHistory.Count(h => h.FinalStatus == "Rejected");

            return View(allHistory);
        }

        // GET: Admin/HistoryDetails/5
        public ActionResult HistoryDetails(int id)
        {
            var history = db.AdoptionHistories
                .Include(h => h.User)
                .Include(h => h.CancellationApprover)
                .FirstOrDefault(h => h.HistoryId == id);

            if (history == null)
            {
                return HttpNotFound();
            }

            return View(history);
        }

        // ========== HELPER METHOD: Archive to History ==========

        private void ArchiveApplicationToHistory(AdoptionApplication application, string finalStatus)
        {
            var history = new AdoptionHistory
            {
                ApplicationId = application.ApplicationId,
                UserId = application.UserId,
                PetId = application.PetId,

                // Pet snapshot
                PetName = application.Pet.Name,
                PetBreed = application.Pet.Breed,
                PetType = application.Pet.PetType?.TypeName,
                PetImageUrl = application.Pet.ImageUrl,

                // Application snapshot
                PhoneNumber = application.PhoneNumber,
                Address = application.Address,
                HousingType = application.HousingType,

                // Timeline
                ApplicationDate = application.ApplicationDate,
                ApprovalDate = application.ReviewedDate,
                ClaimedDate = application.ClaimedDate,
                CompletedDate = finalStatus == "Completed" ? DateTime.Now : (DateTime?)null,

                // Status
                FinalStatus = finalStatus,

                // Cancellation/Withdrawal
                CancellationReason = application.CancellationReason,
                WithdrawalReason = application.WithdrawalReason,
                CancellationRequestedDate = application.CancellationRequestedDate,
                CancellationApprovedBy = application.CancellationReviewedBy,
                CancellationApprovedDate = application.CancellationReviewedDate,

                // Admin data
                AdminNotes = application.AdminNotes,
                RejectionReason = application.RejectionReason,

                // Metadata
                CreatedAt = DateTime.Now,
                ArchivedAt = DateTime.Now,
                AutoDeleteAfter = finalStatus == "Completed" ? DateTime.Now.AddMonths(6) : (DateTime?)null // Only completed adoptions auto-delete
            };

            db.AdoptionHistories.Add(history);
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