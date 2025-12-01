using FurEver_Home.Filters;
using FurEver_Home.Models;
using FurEver_Home.Services;
using OtpNet;
using QRCoder;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;

namespace FurEver_Home.Controllers
{
    [AllAdminRoles]
    public class AdminController : BaseController
    {
        private FurEverHomeContext db = new FurEverHomeContext();
        private RoleService roleService;
        public AdminController()
        {
            roleService = new RoleService(db);
        }
        public ActionResult AccessDenied(string feature = null, string requiredRoles = null)
        {
            var currentUserId = GetCurrentUserId();
            ViewBag.Feature = feature;
            ViewBag.RequiredRoles = requiredRoles;
            ViewBag.UserRoles = currentUserId.HasValue ? roleService.GetUserRoles(currentUserId.Value) : new List<string>();
            Response.StatusCode = 403; // useful for clients / crawlers
            return View();
        }
        [AllAdminRoles]
        public ActionResult Dashboard()
        {
            int currentUserId = Convert.ToInt32(Session["UserId"]);

            // Get user's roles
            var userRoles = roleService.GetUserRoles(currentUserId);
            ViewBag.UserRoles = userRoles;
            ViewBag.IsSuperAdmin = roleService.IsSuperAdmin(currentUserId);
            ViewBag.IsModerator = roleService.IsModerator(currentUserId);
            ViewBag.IsSupport = roleService.IsSupport(currentUserId);

            // ============ EXISTING STATS ============
            ViewBag.TotalUsers = db.Users.Count(u => u.Role == "Client");
            ViewBag.TotalPets = db.Pets.Count(p => !p.IsAdopted && !p.IsDeleted);
            ViewBag.TotalDogs = db.Pets.Count(p => p.PetTypeId == 1 && !p.IsAdopted && !p.IsDeleted);
            ViewBag.TotalCats = db.Pets.Count(p => p.PetTypeId == 2 && !p.IsAdopted && !p.IsDeleted);
            ViewBag.PendingCount = db.Pets.Count(p => p.PostStatus == "Pending" && p.PostedByType == "Customer" && !p.IsDeleted);
            ViewBag.PendingVerifications = db.Users.Count(u => u.IDStatus == "Pending");
            ViewBag.PendingApplications = db.AdoptionApplications.Count(a => a.Status == "Pending");
            ViewBag.PendingCancellations = db.AdoptionApplications.Count(a => a.CancellationRequested && !a.CancellationApproved);

            // ============ RECENT ACTIVITIES ============
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
                .Where(p => p.PostStatus == "Pending" && p.PostedByType == "Customer" && !p.IsDeleted)
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


        // Helper method to check if pet is client-owned CLIENT OR ORGANIZATION
        private bool IsClientOwnedPet(int petId)
        {
            var pet = db.Pets.Find(petId);
            return pet != null && pet.PostedByType == "Customer" && pet.OwnerUserId.HasValue;
        }

        // ========== USER MANAGEMENT ==========

        [AllAdminRoles] // All can view
        public ActionResult Users()
        {
            var clientUsers = db.Users.Where(u => u.Role == "Client").ToList();


            // ADD THESE LINES:
            int currentUserId = Convert.ToInt32(Session["UserId"]);
            ViewBag.IsSuperAdmin = roleService.IsSuperAdmin(currentUserId);
            ViewBag.IsModerator = roleService.IsModerator(currentUserId);
            ViewBag.IsSupport = roleService.IsSupport(currentUserId);

            return View(clientUsers);
        }

        // GET: Admin/UserDetails/5
        [AllAdminRoles] // All can view details
        public ActionResult UserDetails(int id)
        {
            var user = db.Users.Find(id);
            if (user == null)
            {
                return HttpNotFound();
            }

            ViewBag.CanModify = roleService.IsSuperAdmin(Convert.ToInt32(Session["UserId"]));

            return View(user);
        }

        // POST: Admin/ToggleUserStatus/5
        [SuperAdminOnly] // Only Super Admin can toggle status
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

        [AdminOrModerator]
        public ActionResult VerifyIDs()
        {
            var pendingVerifications = db.Users.Where(u => u.IDStatus == "Pending").ToList();
            return View(pendingVerifications);
        }

        [AdminOrModerator]
        public ActionResult VerifyIDDetails(int id)
        {
            var user = db.Users.Find(id);
            if (user == null)
            {
                return HttpNotFound();
            }
            return View(user);
        }

        [AdminOrModerator]
        [HttpPost]
        public ActionResult ApproveID(int id)
        {
            var user = db.Users.Find(id);
            if (user != null)
            {
                user.IDStatus = "Verified";
                user.UpdatedAt = DateTime.Now;

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

        [AdminOrModerator]
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
        [AllAdminRoles] // All can view
        public ActionResult Pets()
        {
            var pets = db.Pets.Include(p => p.PetType).Include(p => p.Creator)
               .Where(p => !p.IsDeleted)  // ⭐ ADD THIS
                .ToList();

            ViewBag.AdoptedCount = db.Pets.Count(p => p.IsAdopted && !p.IsDeleted);  // ⭐ ADD THIS
            ViewBag.CanModify = roleService.IsSuperAdmin(Convert.ToInt32(Session["UserId"]));

            return View(pets);
        }

        //CUSTOMER DELETED PETS METHOD
        // GET: Admin/CustomerDeletedPosts
        [AllAdminRoles]
        public ActionResult CustomerDeletedPosts()
        {
            // Get all deleted posts by customers (PostedByType = "Customer")
            var deletedPosts = db.Pets
                .Include(p => p.PetType)
                .Include(p => p.Owner)
                .Where(p => p.IsDeleted && p.PostedByType == "Customer")
                .OrderByDescending(p => p.DeletedAt)
                .ToList();

            return View("AdminCustomerDeletedPosts", deletedPosts);
        }




        [SuperAdminOnly]
        public ActionResult AddPet()
        {
            ViewBag.PetTypes = db.PetTypes.ToList();
            return View();
        }

        [HttpGet]
        [AllAdminRoles]
        public JsonResult GetBreedsByPetType(int petTypeId)
        {
            try
            {
                var breeds = db.Breeds
                    .Where(b => b.PetTypeId == petTypeId)
                    .OrderBy(b => b.BreedName)
                    .Select(b => new {
                        breedId = b.BreedId,
                        breedName = b.BreedName
                    })
                    .ToList();

                return Json(new { success = true, breeds = breeds }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        // POST: Admin/AddPet
        [SuperAdminOnly]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddPet(Pet model, HttpPostedFileBase PetImage)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    ViewBag.PetTypes = db.PetTypes.ToList();
                    return View(model);
                }

                var existingBreed = db.Breeds.FirstOrDefault(b =>
                    b.BreedName.ToLower() == model.Breed.ToLower() &&
                    b.PetTypeId == model.PetTypeId);

                if (existingBreed == null)
                {
                    var newBreed = new Breed
                    {
                        BreedName = model.Breed,
                        PetTypeId = model.PetTypeId,
                        CreatedAt = DateTime.Now,
                        CreatedBy = Session["UserId"] != null ? (int?)Convert.ToInt32(Session["UserId"]) : null
                    };
                    db.Breeds.Add(newBreed);
                    db.SaveChanges();
                }

                // Handle multiple images (up to 3)
                if (PetImage != null && PetImage.ContentLength > 0)
                {
                    string fileName = Path.GetFileName(PetImage.FileName);
                    string fileExtension = Path.GetExtension(fileName);
                    string uniqueFileName = $"pet_{Guid.NewGuid()}{fileExtension}";
                    string uploadPath = Server.MapPath("~/Content/Images/Pets/");

                    if (!Directory.Exists(uploadPath))
                    {
                        Directory.CreateDirectory(uploadPath);
                    }

                    string fullPath = Path.Combine(uploadPath, uniqueFileName);
                    PetImage.SaveAs(fullPath);
                    model.ImageUrl = $"/Content/Images/Pets/{uniqueFileName}";
                }

                // NEW: Handle additional images
                var additionalImages = Request.Files;
                int imageCount = 0;

                for (int i = 0; i < additionalImages.Count; i++)
                {
                    var file = additionalImages[i];

                    // Skip the main image (already processed above)
                    if (file.FileName == PetImage?.FileName) continue;

                    if (file != null && file.ContentLength > 0 && imageCount < 2)
                    {
                        string fileExtension = Path.GetExtension(file.FileName);
                        string uniqueFileName = $"pet_{Guid.NewGuid()}{fileExtension}";
                        string uploadPath = Server.MapPath("~/Content/Images/Pets/");
                        string fullPath = Path.Combine(uploadPath, uniqueFileName);

                        file.SaveAs(fullPath);
                        string imageUrl = $"/Content/Images/Pets/{uniqueFileName}";

                        if (imageCount == 0)
                            model.ImageUrl2 = imageUrl;
                        else if (imageCount == 1)
                            model.ImageUrl3 = imageUrl;

                        imageCount++;
                    }
                }

                model.PostedByType = string.IsNullOrEmpty(model.OrganizationName) ? "Admin" : "Organization";
                model.Location = model.Location;  // Save location
                model.CreatedBy = Session["UserId"] != null ? (int?)Convert.ToInt32(Session["UserId"]) : null;
                model.CreatedAt = DateTime.Now;
                model.UpdatedAt = DateTime.Now;
                model.DateAdded = DateTime.Now;
                model.AdminVerified = true;
                model.RequiresAdminApproval = false;
                model.PostStatus = "Active";

                db.Pets.Add(model);
                db.SaveChanges();

                TempData["SuccessMessage"] = "Pet added successfully!";
                return RedirectToAction("Pets", "Admin");
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"Error adding pet: {ex.Message}";
                ViewBag.PetTypes = db.PetTypes.ToList();
                return View(model);
            }
        }

        // GET: Admin/EditPet/5

        [SuperAdminOnly]
        public ActionResult EditPet(int id)
        {
            var pet = db.Pets.Find(id);
            if (pet == null)
            {
                return HttpNotFound();
            }
            // ⭐ ADD THIS BLOCK - Block editing client posts
            if (pet.PostedByType == "Customer" && pet.OwnerUserId.HasValue)
            {
                TempData["Error"] = "Client posts cannot be edited by admin. Only viewing is allowed.";
                return RedirectToAction("Pets");
            }


            ViewBag.PetTypes = db.PetTypes.ToList();
            ViewBag.Breeds = db.Breeds
                .Where(b => b.PetTypeId == pet.PetTypeId)
                .OrderBy(b => b.BreedName)
                .ToList();

            return View(pet);
        }



        // POST: Admin/EditPet/5
        [SuperAdminOnly]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditPet(Pet model, HttpPostedFileBase PetImage, string OrganizationName)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    ViewBag.PetTypes = db.PetTypes.ToList();
                    ViewBag.Breeds = db.Breeds
                        .Where(b => b.PetTypeId == model.PetTypeId)
                        .OrderBy(b => b.BreedName)
                        .ToList();

                    return View(model);
                }

                var pet = db.Pets.Find(model.PetId);
                if (pet == null)
                {
                    return HttpNotFound();
                }

                var existingBreed = db.Breeds.FirstOrDefault(b =>
                    b.BreedName.ToLower() == model.Breed.ToLower() &&
                    b.PetTypeId == model.PetTypeId);

                if (existingBreed == null)
                {
                    var newBreed = new Breed
                    {
                        BreedName = model.Breed,
                        PetTypeId = model.PetTypeId,
                        CreatedAt = DateTime.Now,
                        CreatedBy = Session["UserId"] != null ? (int?)Convert.ToInt32(Session["UserId"]) : null
                    };
                    db.Breeds.Add(newBreed);
                    db.SaveChanges();
                }

                // Handle main image update
                if (PetImage != null && PetImage.ContentLength > 0)
                {
                    string fileName = Path.GetFileName(PetImage.FileName);
                    string fileExtension = Path.GetExtension(fileName);
                    string uniqueFileName = $"pet_{Guid.NewGuid()}{fileExtension}";
                    string uploadPath = Server.MapPath("~/Content/Images/Pets/");

                    if (!Directory.Exists(uploadPath))
                    {
                        Directory.CreateDirectory(uploadPath);
                    }

                    string fullPath = Path.Combine(uploadPath, uniqueFileName);
                    PetImage.SaveAs(fullPath);
                    pet.ImageUrl = $"/Content/Images/Pets/{uniqueFileName}";
                }

                // NEW: Handle additional images update
                var additionalImages = Request.Files;
                int imageCount = 0;

                for (int i = 0; i < additionalImages.Count; i++)
                {
                    var file = additionalImages[i];

                    // Skip the main image
                    if (file.FileName == PetImage?.FileName) continue;

                    if (file != null && file.ContentLength > 0 && imageCount < 2)
                    {
                        string fileExtension = Path.GetExtension(file.FileName);
                        string uniqueFileName = $"pet_{Guid.NewGuid()}{fileExtension}";
                        string uploadPath = Server.MapPath("~/Content/Images/Pets/");
                        string fullPath = Path.Combine(uploadPath, uniqueFileName);

                        file.SaveAs(fullPath);
                        string imageUrl = $"/Content/Images/Pets/{uniqueFileName}";

                        if (imageCount == 0)
                            pet.ImageUrl2 = imageUrl;
                        else if (imageCount == 1)
                            pet.ImageUrl3 = imageUrl;

                        imageCount++;
                    }
                }

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
                pet.Location = model.Location;


                pet.UpdatedAt = DateTime.Now;
                pet.UpdatedBy = Session["UserId"] != null ? Convert.ToInt32(Session["UserId"]) : (int?)null;

                pet.PostedByType = string.IsNullOrEmpty(OrganizationName) ? "Admin" : "Organization";
                pet.OrganizationName = string.IsNullOrEmpty(OrganizationName) ? null : OrganizationName.Trim();

                pet.AdminVerified = true;
                pet.RequiresAdminApproval = false;
                pet.PostStatus = "Active";

                db.SaveChanges();

                TempData["Success"] = $"Pet '{pet.Name}' has been updated successfully!";
                return RedirectToAction("Pets");
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"Error updating pet: {ex.Message}";
                ViewBag.PetTypes = db.PetTypes.ToList();
                ViewBag.Breeds = db.Breeds
                    .Where(b => b.PetTypeId == model.PetTypeId)
                    .OrderBy(b => b.BreedName)
                    .ToList();

                return View(model);
            }
        }

        // POST: Admin/DeletePet/5
        [SuperAdminOnly]
        [HttpPost]
        public ActionResult DeletePet(int id)
        {
            var pet = db.Pets.Find(id);
            if (pet != null)
            {
                // ⭐ ADD THIS BLOCK - Block deleting client posts
                if (pet.PostedByType == "Customer" && pet.OwnerUserId.HasValue)
                {
                    TempData["Error"] = "Client posts cannot be deleted by admin. Please contact the pet owner.";
                    return RedirectToAction("Pets");
                }
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
        [AdminOrModerator]
        public ActionResult ApprovePets()
        {
            var pendingPets = db.Pets
                .Include(p => p.PetType)
                .Include(p => p.Owner)
                .Where(p => p.PostStatus == "Pending" && p.PostedByType == "Customer" && !p.IsDeleted)  // ⭐ ADD THIS
                .OrderByDescending(p => p.DateAdded)
                .ToList();

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
        [AdminOrModerator]
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

            ViewBag.CustomQuestions = db.PetScreeningQuestions
                .Where(q => q.PetId == id)
                .OrderBy(q => q.OrderNumber)
                .ToList();

            return View(pet);
        }

        // POST: Admin/ApprovePetPost/5
        [AdminOrModerator]
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
        [AdminOrModerator]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RejectPetPost(int id, string rejectionReason)
        {
            var pet = db.Pets.Include(p => p.Owner).FirstOrDefault(p => p.PetId == id);

            if (pet != null && pet.PostStatus == "Pending")
            {
                pet.PostStatus = "Rejected";
                pet.RejectionReason = rejectionReason;
                pet.AdminVerified = false;
                pet.UpdatedAt = DateTime.Now;
                pet.UpdatedBy = (int)Session["UserId"];

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
        [AllAdminRoles]
        public ActionResult Applications()
        {
            var applications = db.AdoptionApplications
                                 .Include(a => a.User)
                                 .Include(a => a.Pet)
                                 .OrderByDescending(a => a.ApplicationDate)
                                 .ToList();

            ViewBag.CanModify = roleService.HasAnyRole(Convert.ToInt32(Session["UserId"]), "Super Admin", "Moderator");

            return View(applications);
        }

        [AllAdminRoles]
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

            ViewBag.CanModify = roleService.HasAnyRole(Convert.ToInt32(Session["UserId"]), "Super Admin", "Moderator");

            return View(application);
        }


        // GET: Admin/AdoptedPets
        [AllAdminRoles]
        public ActionResult AdoptedPets(int page = 1, string search = "", string searchLocation = "", int? filterYear = null, int? filterMonth = null)
        {
            int pageSize = 5;

            // Get all adopted pets first
            var adoptedPets = db.Pets
                .Include(p => p.PetType)
                .Include(p => p.Creator)
                .Where(p => p.IsAdopted == true && !p.IsDeleted)  // ⭐ ADD THIS
                .ToList();

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.ToLower().Trim();
                adoptedPets = adoptedPets
                    .Where(p =>
                        p.Name.ToLower().Contains(search) ||
                        p.Breed.ToLower().Contains(search))
                    .ToList();
            }

            // Apply location filter
            if (!string.IsNullOrWhiteSpace(searchLocation))
            {
                searchLocation = searchLocation.ToLower().Trim();
                adoptedPets = adoptedPets
                    .Where(p => p.Location != null && p.Location.ToLower().Contains(searchLocation))
                    .ToList();
            }

            // Apply year filter
            if (filterYear.HasValue)
            {
                adoptedPets = adoptedPets
                    .Where(p => p.UpdatedAt.Year == filterYear.Value)
                    .ToList();
            }

            // Apply month filter (only if year is also selected)
            if (filterMonth.HasValue && filterYear.HasValue)
            {
                adoptedPets = adoptedPets
                    .Where(p => p.UpdatedAt.Month == filterMonth.Value)
                    .ToList();
            }

            // Order by most recent
            adoptedPets = adoptedPets.OrderByDescending(p => p.UpdatedAt).ToList();

            // Create view models
            var adoptedPetsData = new List<AdoptedPetViewModel>();

            foreach (var pet in adoptedPets)
            {
                var application = db.AdoptionApplications
                    .Include(a => a.User)
                    .FirstOrDefault(a => a.PetId == pet.PetId && a.Status == "Completed");

                adoptedPetsData.Add(new AdoptedPetViewModel
                {
                    Pet = pet,
                    Application = application
                });
            }

            // Get available years for dropdown (years that have adopted pets)
            var availableYears = db.Pets
                .Where(p => p.IsAdopted == true)
                .Select(p => p.UpdatedAt.Year)
                .Distinct()
                .OrderByDescending(y => y)
                .ToList();

            ViewBag.AvailableYears = availableYears;

            // Total count
            ViewBag.TotalCount = adoptedPetsData.Count;
            ViewBag.AdoptedCount = adoptedPetsData.Count;

            // Pagination
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)adoptedPetsData.Count / pageSize);
            ViewBag.PageSize = pageSize;

            // Pass filters to view
            ViewBag.CurrentSearch = search ?? "";
            ViewBag.SelectedYear = filterYear;
            ViewBag.SelectedMonth = filterMonth;
            ViewBag.CurrentLocationSearch = searchLocation ?? "";

            var pagedPets = adoptedPetsData
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return View(pagedPets);
        }   

        [AdminOrModerator]
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
        [AdminOrModerator]
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
        [AdminOrModerator]
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
        [AdminOrModerator]
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
        [AdminOrModerator]
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
        [AdminOrModerator]
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
        [AdminOrModerator]
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
        [AdminOrModerator]
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
        [AllAdminRoles]
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
        [AllAdminRoles]
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
        [SuperAdminOnly]
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
                AutoDeleteAfter = finalStatus == "Completed" ? DateTime.Now.AddMonths(6) : (DateTime?)null
            };

            db.AdoptionHistories.Add(history);
        }

        // Add these methods to your AdminController.cs

        // ========== ADMIN PROFILE MANAGEMENT ==========

        [AllAdminRoles]
        public ActionResult AdminProfile(int? userId)
        {
            int currentUserId = Convert.ToInt32(Session["UserId"]);
            bool isSuperAdmin = roleService.IsSuperAdmin(currentUserId);

            // Determine which user profile to show
            int targetUserId = userId ?? currentUserId;

            // Check permissions
            if (targetUserId != currentUserId && !isSuperAdmin)
            {
                TempData["Error"] = "You don't have permission to view other admin profiles.";
                return RedirectToAction("Dashboard");
            }

            var user = db.Users.Find(targetUserId);
            if (user == null)
            {
                return HttpNotFound();
            }

            // Get all admin users for Super Admin dropdown with their primary role
            if (isSuperAdmin)
            {
                var adminUsers = db.Users
                    .Where(u => u.Role == "Admin")
                    .OrderBy(u => u.FullName)
                    .ToList()
                    .Select(u => new
                    {
                        UserId = u.UserId,
                        FullName = u.FullName,
                        Email = u.Email,
                        Role = u.Role,
                        PrimaryRole = GetPrimaryRoleName(u.UserId)
                    })
                    .ToList();

                ViewBag.AdminUsers = adminUsers;
            }

            ViewBag.CurrentUserId = currentUserId;
            ViewBag.IsSuperAdmin = isSuperAdmin;
            ViewBag.IsViewingOwnProfile = (targetUserId == currentUserId);
            ViewBag.TargetUser = user;

            // Get target user's roles
            ViewBag.TargetUserRoles = roleService.GetUserRoles(targetUserId);

            return View(user);
        }

        // Add this helper method right after AdminProfile
        private string GetPrimaryRoleName(int userId)
        {
            var primaryRole = db.UserRoles
                .Where(ur => ur.UserId == userId && ur.IsActive)
                .Include(ur => ur.Role)
                .OrderBy(ur => ur.RoleId) // Super Admin (1) will be first
                .Select(ur => ur.Role.RoleName)
                .FirstOrDefault();

            return primaryRole ?? "Admin";
        }

        //Sync UserRoles

        [SuperAdminOnly]
        public ActionResult SyncUserRoles()
        {
            try
            {
                // Get all admin users where role doesn't match their primary assigned role
                var adminUsers = db.Users
                    .Where(u => u.Role != "Client")
                    .ToList();

                int syncedCount = 0;
                foreach (var user in adminUsers)
                {
                    var primaryRole = GetPrimaryRoleName(user.UserId);

                    // Only update if role is different
                    if (user.Role != primaryRole)
                    {
                        user.Role = primaryRole;
                        user.UpdatedAt = DateTime.Now;
                        syncedCount++;
                    }
                }

                db.SaveChanges();

                TempData["Success"] = $"Successfully synchronized {syncedCount} admin user role(s)!";
                return RedirectToAction("ManageAdmins");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error syncing roles: {ex.Message}";
                return RedirectToAction("ManageAdmins");
            }
        }

        [AllAdminRoles]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateProfile(int targetUserId, string FullName, string PhoneNumber, int? Age, string Address)
        {
            try
            {
                int currentUserId = Convert.ToInt32(Session["UserId"]);
                bool isSuperAdmin = roleService.IsSuperAdmin(currentUserId);

                // Check permissions
                if (targetUserId != currentUserId && !isSuperAdmin)
                {
                    TempData["Error"] = "You don't have permission to update other admin profiles.";
                    return RedirectToAction("AdminProfile");
                }

                var user = db.Users.Find(targetUserId);
                if (user == null)
                {
                    return HttpNotFound();
                }

                // Update user info
                user.FullName = FullName;
                user.PhoneNumber = PhoneNumber;
                user.Age = Age;
                user.Address = Address;
                user.UpdatedAt = DateTime.Now;

                db.SaveChanges();

                TempData["Success"] = targetUserId == currentUserId
                    ? "Your profile has been updated successfully!"
                    : $"{user.FullName}'s profile has been updated successfully!";

                return RedirectToAction("AdminProfile", new { userId = targetUserId });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error updating profile: {ex.Message}";
                return RedirectToAction("AdminProfile", new { userId = targetUserId });
            }
        }
        // ========== 2FA MANAGEMENT FOR ADMINS ==========

        [AllAdminRoles]
        [HttpPost]
        public JsonResult EnableTwoFactorForAdmin(int? targetUserId)
        {
            try
            {
                int currentUserId = Convert.ToInt32(Session["UserId"]);
                bool isSuperAdmin = roleService.IsSuperAdmin(currentUserId);

                int userIdToUpdate = targetUserId ?? currentUserId;

                // Only Super Admin can enable 2FA for other admins
                if (userIdToUpdate != currentUserId && !isSuperAdmin)
                {
                    return Json(new { success = false, message = "Permission denied." });
                }

                var user = db.Users.Find(userIdToUpdate);
                if (user == null)
                {
                    return Json(new { success = false, message = "User not found." });
                }

                // Generate secret key
                var secretKey = GenerateSecretKey();
                user.TwoFactorSecretKey = secretKey;
                user.TwoFactorEnabled = false; // Will be enabled after verification
                user.UpdatedAt = DateTime.Now;

                db.SaveChanges();

                // Store in session for QR generation
                Session[$"TwoFactorSecret_{userIdToUpdate}"] = secretKey;

                return Json(new { success = true, secret = secretKey });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [AllAdminRoles]
        public ActionResult GetQRCodeForAdmin(int? targetUserId)
        {
            int currentUserId = Convert.ToInt32(Session["UserId"]);
            bool isSuperAdmin = roleService.IsSuperAdmin(currentUserId);

            int userIdToGet = targetUserId ?? currentUserId;

            // Check permissions
            if (userIdToGet != currentUserId && !isSuperAdmin)
            {
                return HttpNotFound();
            }

            var secretKey = Session[$"TwoFactorSecret_{userIdToGet}"] as string;
            if (string.IsNullOrEmpty(secretKey))
            {
                return HttpNotFound();
            }

            var user = db.Users.Find(userIdToGet);
            if (user == null)
            {
                return HttpNotFound();
            }

            // Generate QR Code
            var qrGenerator = new QRCodeGenerator();
            var qrData = $"otpauth://totp/FurEverHome:{user.Email}?secret={secretKey}&issuer=FurEverHome";
            var qrCode = qrGenerator.CreateQrCode(qrData, QRCodeGenerator.ECCLevel.Q);

            using (var qrCodeImage = new QRCode(qrCode))
            {
                using (var bitmap = qrCodeImage.GetGraphic(20))
                {
                    using (var stream = new System.IO.MemoryStream())
                    {
                        bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                        return File(stream.ToArray(), "image/png");
                    }
                }
            }
        }

        [AllAdminRoles]
        [HttpPost]
        public JsonResult ConfirmTwoFactorForAdmin(string code, int? targetUserId)
        {
            try
            {
                int currentUserId = Convert.ToInt32(Session["UserId"]);
                bool isSuperAdmin = roleService.IsSuperAdmin(currentUserId);

                int userIdToUpdate = targetUserId ?? currentUserId;

                // Check permissions
                if (userIdToUpdate != currentUserId && !isSuperAdmin)
                {
                    return Json(new { success = false, message = "Permission denied." });
                }

                var user = db.Users.Find(userIdToUpdate);
                if (user == null || string.IsNullOrEmpty(user.TwoFactorSecretKey))
                {
                    return Json(new { success = false, message = "2FA setup not found." });
                }

                // Verify code
                var totp = new Totp(Base32Encoding.ToBytes(user.TwoFactorSecretKey));
                if (totp.VerifyTotp(code, out long timeStepMatched, new VerificationWindow(2, 2)))
                {
                    user.TwoFactorEnabled = true;
                    user.UpdatedAt = DateTime.Now;
                    db.SaveChanges();

                    Session.Remove($"TwoFactorSecret_{userIdToUpdate}");

                    return Json(new { success = true });
                }

                return Json(new { success = false, message = "Invalid code. Please try again." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [AllAdminRoles]
        [HttpPost]
        public JsonResult DisableTwoFactorForAdmin(string password, int? targetUserId)
        {
            try
            {
                int currentUserId = Convert.ToInt32(Session["UserId"]);
                bool isSuperAdmin = roleService.IsSuperAdmin(currentUserId);

                int userIdToUpdate = targetUserId ?? currentUserId;

                // Check permissions
                if (userIdToUpdate != currentUserId && !isSuperAdmin)
                {
                    return Json(new { success = false, message = "Permission denied." });
                }

                var user = db.Users.Find(userIdToUpdate);
                if (user == null)
                {
                    return Json(new { success = false, message = "User not found." });
                }

                // Verify password (Super Admin verifies their own password)
                var currentUser = db.Users.Find(currentUserId);
                if (!VerifyPassword(password, currentUser.Password))
                {
                    return Json(new { success = false, message = "Incorrect password." });
                }

                user.TwoFactorEnabled = false;
                user.TwoFactorSecretKey = null;
                user.UpdatedAt = DateTime.Now;
                db.SaveChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ========== HELPER METHODS ==========

        private string GenerateSecretKey()
        {
            var key = KeyGeneration.GenerateRandomKey(20);
            return Base32Encoding.ToString(key);
        }

        private bool VerifyPassword(string password, string passwordHash)
        {
            return BCrypt.Net.BCrypt.Verify(password, passwordHash);
        }


        [SuperAdminOnly]
        public ActionResult ManageAdmins()
        {
            var adminUsers = db.Users
                .Where(u => u.Role != "Client")
                .OrderBy(u => u.FullName)
                .ToList();

            // Get roles for each admin
            var adminWithRoles = adminUsers.Select(u => new AdminUserViewModel
            {
                User = u,
                AssignedRoles = db.UserRoles
                    .Where(ur => ur.UserId == u.UserId && ur.IsActive)
                    .Include(ur => ur.Role)
                    .Select(ur => ur.Role)
                    .ToList()
            }).ToList();

            ViewBag.AvailableRoles = db.Roles
                .Where(r => r.IsActive && r.RoleName != "Client")
                .OrderBy(r => r.RoleName)
                .ToList();

            return View(adminWithRoles);
        }

        // GET: Admin/CreateAdmin
        [SuperAdminOnly]
        public ActionResult CreateAdmin()
        {
            // ✅ EXCLUDE "Super Admin" from the dropdown
            ViewBag.AvailableRoles = db.Roles
                .Where(r => r.IsActive && r.RoleName != "Client" && r.RoleName != "Super Admin")
                .OrderBy(r => r.RoleName)
                .ToList();

            return View();
        }

        // POST: Admin/CreateAdmin
        [SuperAdminOnly]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateAdmin(CreateAdminViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    ViewBag.AvailableRoles = db.Roles
                        .Where(r => r.IsActive && r.RoleName != "Client")
                        .OrderBy(r => r.RoleName)
                        .ToList();
                    return View(model);
                }

                // Check if email already exists
                if (db.Users.Any(u => u.Email == model.Email))
                {
                    ModelState.AddModelError("Email", "This email is already registered.");
                    ViewBag.AvailableRoles = db.Roles
                        .Where(r => r.IsActive && r.RoleName != "Client")
                        .OrderBy(r => r.RoleName)
                        .ToList();
                    return View(model);
                }

                // Validate that at least one role is selected
                if (model.SelectedRoleIds == null || !model.SelectedRoleIds.Any())
                {
                    ModelState.AddModelError("SelectedRoleIds", "Please select at least one role.");
                    ViewBag.AvailableRoles = db.Roles
                        .Where(r => r.IsActive && r.RoleName != "Client")
                        .OrderBy(r => r.RoleName)
                        .ToList();
                    return View(model);
                }

                // Create new admin user
                var newAdmin = new User
                {
                    FullName = model.FullName,
                    Email = model.Email,
                    Password = BCrypt.Net.BCrypt.HashPassword(model.Password),
                    Role = "Admin", // Base role
                    PhoneNumber = model.PhoneNumber,
                    Address = model.Address,
                    Age = model.Age,
                    IDStatus = "Verified", // Admins are pre-verified
                    IsActive = true,
                    DateRegistered = DateTime.Now,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                db.Users.Add(newAdmin);
                db.SaveChanges();

                // Assign selected roles
                int currentUserId = Convert.ToInt32(Session["UserId"]);
                foreach (var roleId in model.SelectedRoleIds)
                {
                    var userRole = new UserRoles
                    {
                        UserId = newAdmin.UserId,
                        RoleId = roleId,
                        AssignedAt = DateTime.Now,
                        AssignedBy = currentUserId,
                        IsActive = true
                    };
                    db.UserRoles.Add(userRole);
                }

                db.SaveChanges();

                // Get role names for display
                var assignedRoleNames = db.Roles
                    .Where(r => model.SelectedRoleIds.Contains(r.RoleId))
                    .Select(r => r.RoleName)
                    .ToList();

                TempData["Success"] = $"Admin account created successfully for {newAdmin.FullName} with roles: {string.Join(", ", assignedRoleNames)}";
                return RedirectToAction("ManageAdmins");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error creating admin account: {ex.Message}");
                ViewBag.AvailableRoles = db.Roles
                    .Where(r => r.IsActive && r.RoleName != "Client")
                    .OrderBy(r => r.RoleName)
                    .ToList();
                return View(model);
            }
        }

        // GET: Admin/EditAdminRoles/5
        [SuperAdminOnly]
        public ActionResult EditAdminRoles(int id)
        {
            var user = db.Users.Find(id);
            if (user == null || user.Role == "Client")
            {
                return HttpNotFound();
            }

            var currentRoles = db.UserRoles
                .Where(ur => ur.UserId == id && ur.IsActive)
                .Select(ur => ur.RoleId)
                .ToList();

            var model = new EditAdminRolesViewModel
            {
                UserId = user.UserId,
                FullName = user.FullName,
                Email = user.Email,
                SelectedRoleIds = currentRoles
            };

            ViewBag.AvailableRoles = db.Roles
                .Where(r => r.IsActive && r.RoleName != "Client")
                .OrderBy(r => r.RoleName)
                .ToList();

            return View(model);
        }

        // POST: Admin/EditAdminRoles/5
        [SuperAdminOnly]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditAdminRoles(EditAdminRolesViewModel model)
        {
            try
            {
                var user = db.Users.Find(model.UserId);
                if (user == null || user.Role == "Client")
                {
                    return HttpNotFound();
                }

                // Validate that at least one role is selected
                if (model.SelectedRoleIds == null || !model.SelectedRoleIds.Any())
                {
                    ModelState.AddModelError("SelectedRoleIds", "Please select at least one role.");
                    ViewBag.AvailableRoles = db.Roles
                        .Where(r => r.IsActive && r.RoleName != "Client")
                        .OrderBy(r => r.RoleName)
                        .ToList();
                    return View(model);
                }

                int currentUserId = Convert.ToInt32(Session["UserId"]);

                // Deactivate all current role assignments
                var existingRoles = db.UserRoles.Where(ur => ur.UserId == model.UserId).ToList();
                foreach (var role in existingRoles)
                {
                    role.IsActive = false;
                }

                // Add or reactivate selected roles
                foreach (var roleId in model.SelectedRoleIds)
                {
                    var existingRole = existingRoles.FirstOrDefault(ur => ur.RoleId == roleId);
                    if (existingRole != null)
                    {
                        // Reactivate existing role
                        existingRole.IsActive = true;
                        existingRole.AssignedAt = DateTime.Now;
                        existingRole.AssignedBy = currentUserId;
                    }
                    else
                    {
                        // Add new role
                        var userRole = new UserRoles
                        {
                            UserId = model.UserId,
                            RoleId = roleId,
                            AssignedAt = DateTime.Now,
                            AssignedBy = currentUserId,
                            IsActive = true
                        };
                        db.UserRoles.Add(userRole);
                    }
                }

                user.UpdatedAt = DateTime.Now;
                db.SaveChanges();

                // Get role names for display
                var assignedRoleNames = db.Roles
                    .Where(r => model.SelectedRoleIds.Contains(r.RoleId))
                    .Select(r => r.RoleName)
                    .ToList();

                TempData["Success"] = $"Roles updated successfully for {user.FullName}: {string.Join(", ", assignedRoleNames)}";
                return RedirectToAction("ManageAdmins");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error updating roles: {ex.Message}");
                ViewBag.AvailableRoles = db.Roles
                    .Where(r => r.IsActive && r.RoleName != "Client")
                    .OrderBy(r => r.RoleName)
                    .ToList();
                return View(model);
            }
        }

        // POST: Admin/DeactivateAdmin/5
        [SuperAdminOnly]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeactivateAdmin(int id)
        {
            try
            {
                int currentUserId = Convert.ToInt32(Session["UserId"]);

                // Prevent self-deactivation
                if (id == currentUserId)
                {
                    TempData["Error"] = "You cannot deactivate your own account.";
                    return RedirectToAction("ManageAdmins");
                }

                var user = db.Users.Find(id);
                if (user == null || user.Role == "Client")
                {
                    return HttpNotFound();
                }

                user.IsActive = !user.IsActive;
                user.UpdatedAt = DateTime.Now;

                // Also deactivate all role assignments if deactivating user
                if (!user.IsActive)
                {
                    var userRoles = db.UserRoles.Where(ur => ur.UserId == id).ToList();
                    foreach (var role in userRoles)
                    {
                        role.IsActive = false;
                    }
                }

                db.SaveChanges();

                TempData["Success"] = $"Admin account {user.FullName} has been {(user.IsActive ? "activated" : "deactivated")}.";
                return RedirectToAction("ManageAdmins");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error: {ex.Message}";
                return RedirectToAction("ManageAdmins");
            }
        }

        // POST: Admin/ResetAdminPassword/5
        [SuperAdminOnly]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ResetAdminPassword(int id, string newPassword)
        {
            try
            {
                var user = db.Users.Find(id);
                if (user == null || user.Role == "Client")
                {
                    return HttpNotFound();
                }

                if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
                {
                    TempData["Error"] = "Password must be at least 6 characters long.";
                    return RedirectToAction("ManageAdmins");
                }

                user.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);
                user.UpdatedAt = DateTime.Now;
                db.SaveChanges();

                TempData["Success"] = $"Password reset successfully for {user.FullName}.";
                return RedirectToAction("ManageAdmins");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error resetting password: {ex.Message}";
                return RedirectToAction("ManageAdmins");
            }
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                roleService?.Dispose();
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }

}