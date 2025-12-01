using FurEver_Home.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.IO;  // ⭐ ADD THIS - needed for Path.Combine
using System.Web; // ⭐ ADD THIS - needed for HttpPostedFileBase
namespace FurEver_Home.Controllers
{
    public class MyPetsController : BaseController
    {
        private FurEverHomeContext db = new FurEverHomeContext();

        // ==================== CUSTOMER DASHBOARD ====================

        // GET: MyPets/CustomerDashboard
        public ActionResult CustomerDashboard()
        {
            if (Session["UserId"] == null)
            {
                TempData["Error"] = "Please login to access your dashboard.";
                return RedirectToAction("Login", "Account");
            }

            int userId = (int)Session["UserId"];

            // Get pets posted by this customer
            var myPostedPets = db.Pets
                .Include(p => p.PetType)
                .Where(p => p.OwnerUserId == userId && !p.IsDeleted) // ⭐ ADD THIS

                .OrderByDescending(p => p.DateAdded)
                .ToList();

            // Count applications for each pet
            foreach (var pet in myPostedPets)
            {
                pet.HasActiveApplications = db.AdoptionApplications
                    .Any(a => a.PetId == pet.PetId &&
                             (a.Status == "Pending" || a.Status == "Approved" || a.Status == "AwaitingPickup"));
            }

            // Load custom screening questions for each pet
            var petQuestions = new Dictionary<int, List<PetScreeningQuestion>>();
            foreach (var pet in myPostedPets)
            {
                var questions = db.PetScreeningQuestions
                    .Where(q => q.PetId == pet.PetId)
                    .OrderBy(q => q.OrderNumber)
                    .ToList();

                if (questions.Any())
                {
                    petQuestions[pet.PetId] = questions;
                }
            }
            ViewBag.PetQuestions = petQuestions;

            // Get applications received for my pets
            var myPetIds = myPostedPets.Select(p => p.PetId).ToList();

            ViewBag.ApplicationsReceived = db.AdoptionApplications
                .Include(a => a.User)
                .Include(a => a.Pet)
                .Include(a => a.Pet.PetType)
                .Where(a => myPetIds.Contains(a.PetId))
                .OrderByDescending(a => a.ApplicationDate)
                .ToList();

            // Statistics
            ViewBag.TotalPosts = myPostedPets.Count;
            ViewBag.PendingApproval = myPostedPets.Count(p => p.PostStatus == "Pending");
            ViewBag.ActivePosts = myPostedPets.Count(p => p.PostStatus == "Approved");
            ViewBag.TotalApplications = ViewBag.ApplicationsReceived.Count;

            // ⭐ FIXED: Include both "Pending" and "AwaitingPickup" in pending count
            ViewBag.PendingApplications = db.AdoptionApplications
    .Count(a => myPetIds.Contains(a.PetId) &&
           (a.Status == "Pending" || a.Status == "AwaitingPickup"));

            return View(myPostedPets);
        }



        // GET: MyPets/CustomerDeletedPosts
        public ActionResult CustomerDeletedPosts()
        {
            if (Session["UserId"] == null)
            {
                TempData["Error"] = "Please login to access this page.";
                return RedirectToAction("Login", "Account");
            }

            int userId = (int)Session["UserId"];

            // Get only deleted posts by this customer
            var deletedPets = db.Pets
                .Include(p => p.PetType)
                .Where(p => p.OwnerUserId == userId && p.IsDeleted)
                .OrderByDescending(p => p.DeletedAt)
                .ToList();

            return View(deletedPets);
        }



        // ==================== VIEW APPLICATION DETAILS ====================

        // GET: MyPets/CustomerApplicationDetails/5
        public ActionResult CustomerApplicationDetails(int id)
        {
            if (Session["UserId"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int userId = (int)Session["UserId"];

            var application = db.AdoptionApplications
                .Include(a => a.User)
                .Include(a => a.Pet)
                .Include(a => a.Pet.PetType)
                .FirstOrDefault(a => a.ApplicationId == id);

            if (application == null)
            {
                return HttpNotFound();
            }

            // Verify ownership
            if (application.Pet.OwnerUserId != userId)
            {
                TempData["Error"] = "You don't have permission to view this application.";
                return RedirectToAction("CustomerDashboard");
            }

            // Load custom screening answers
            ViewBag.CustomAnswers = db.PetScreeningAnswers
                .Include(a => a.Question)
                .Where(a => a.ApplicationId == id)
                .OrderBy(a => a.Question.OrderNumber)
                .ToList();

            return View(application);
        }

        // ==================== APPROVE/REJECT APPLICATIONS ====================

        // POST: MyPets/ApproveApplication/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ApproveApplication(int id)
        {
            if (Session["UserId"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int userId = (int)Session["UserId"];

            var application = db.AdoptionApplications
                .Include(a => a.Pet)
                .Include(a => a.User)
                .FirstOrDefault(a => a.ApplicationId == id);

            if (application == null || application.Pet.OwnerUserId != userId)
            {
                TempData["Error"] = "Invalid application.";
                return RedirectToAction("CustomerDashboard");
            }

            // ⭐ FIXED: Change status to "AwaitingPickup" instead of "Approved"
            // Only fully approve after pickup details are set
            application.Status = "AwaitingPickup";
            application.ReviewedDate = DateTime.Now;
            application.ReviewedByCustomer = userId;
            application.IsCustomerToCustomer = true;
            application.UpdatedAt = DateTime.Now;

            // ⭐ FIXED: Do NOT mark pet as adopted yet - wait until pickup details are confirmed
            // The pet will be marked as adopted when pickup details are saved

            // ⭐ FIXED: Send a different notification - pickup details coming soon
            var notification = new UserNotification
            {
                UserId = application.UserId,
                NotificationType = "Application_Pending_Pickup",
                Title = "Adoption Application Approved! 🎉",
                Message = $"Great news! Your application to adopt {application.Pet.Name} has been approved by the pet owner. They are now preparing pickup details and will contact you soon.",
                IsRead = false,
                CreatedAt = DateTime.Now
            };
            db.UserNotifications.Add(notification);

            db.SaveChanges();

            TempData["Success"] = $"Application approved! Please set pickup details to complete the adoption.";
            return RedirectToAction("CustomerSetPickupDetails", new { id = id });
        }

        // POST: MyPets/RejectApplication/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RejectApplication(int id, string rejectionReason)
        {
            if (Session["UserId"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int userId = (int)Session["UserId"];

            var application = db.AdoptionApplications
                .Include(a => a.Pet)
                .Include(a => a.User)
                .FirstOrDefault(a => a.ApplicationId == id);

            if (application == null || application.Pet.OwnerUserId != userId)
            {
                TempData["Error"] = "Invalid application.";
                return RedirectToAction("CustomerDashboard");
            }

            application.Status = "Rejected";
            application.RejectionReason = rejectionReason;
            application.ReviewedDate = DateTime.Now;
            application.ReviewedByCustomer = userId;
            application.UpdatedAt = DateTime.Now;

            // Notify adopter
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

            TempData["Success"] = "Application has been rejected.";
            return RedirectToAction("CustomerDashboard");
        }

        // ==================== SET PICKUP DETAILS ====================

        // GET: MyPets/CustomerSetPickupDetails/5
        public ActionResult CustomerSetPickupDetails(int id)
        {
            if (Session["UserId"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int userId = (int)Session["UserId"];

            var application = db.AdoptionApplications
                .Include(a => a.User)
                .Include(a => a.Pet)
                .FirstOrDefault(a => a.ApplicationId == id);

            if (application == null || application.Pet.OwnerUserId != userId)
            {
                return HttpNotFound();
            }

            return View(application);
        }

        // POST: MyPets/CustomerSetPickupDetails/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CustomerSetPickupDetails(int id, string pickupLocation, DateTime pickupDate, string pickupNotes)
        {
            if (Session["UserId"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int userId = (int)Session["UserId"];
            var user = db.Users.Find(userId);

            var application = db.AdoptionApplications
                .Include(a => a.Pet)
                .Include(a => a.User)
                .FirstOrDefault(a => a.ApplicationId == id);

            if (application != null && application.Pet.OwnerUserId == userId)
            {
                // ⭐ FIXED: Now finalize the approval
                application.Status = "Approved";
                application.IsReadyForPickup = true;
                application.PickupLocation = pickupLocation;
                application.PickupDate = pickupDate;
                application.PickupNotes = pickupNotes;
                application.UpdatedAt = DateTime.Now;

                // ⭐ FIXED: Mark pet as adopted NOW (when pickup details are confirmed)
                var pet = db.Pets.Find(application.PetId);
                if (pet != null)
                {
                    pet.IsAdopted = true;
                    pet.UpdatedAt = DateTime.Now;
                }

                // Notify adopter with contact info
                var notification = new UserNotification
                {
                    UserId = application.UserId,
                    NotificationType = "Pickup_Ready",
                    Title = "Your Pet is Ready for Pickup! 🐾",
                    Message = $"{application.Pet.Name} is ready! Pickup at: {pickupLocation} on {pickupDate:MMM dd, yyyy h:mm tt}. Contact pet owner at: {user.Email} or {user.PhoneNumber}. {pickupNotes}",
                    IsRead = false,
                    CreatedAt = DateTime.Now
                };
                db.UserNotifications.Add(notification);

                db.SaveChanges();

                TempData["Success"] = $"Pickup details set! {application.User.FullName} has been notified with your contact info.";
                return RedirectToAction("CustomerDashboard");
            }

            TempData["Error"] = "Unable to set pickup details.";
            return RedirectToAction("CustomerDashboard");
        }

        // ==================== DELETE PET ====================
        // POST: MyPets/DeletePet/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeletePet(int id, string deletionReason, string otherReason)
        {
            if (Session["UserId"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int userId = (int)Session["UserId"];
            var pet = db.Pets.Find(id);

            if (pet == null || pet.OwnerUserId != userId)
            {
                TempData["Error"] = "Pet not found or you don't have permission.";
                return RedirectToAction("CustomerDashboard");
            }

            // Check if pet is already adopted
            if (pet.IsAdopted)
            {
                TempData["Error"] = $"Cannot delete '{pet.Name}' because it has already been adopted.";
                return RedirectToAction("CustomerDashboard");
            }

            // Check if pet has active applications
            var hasActiveApplications = db.AdoptionApplications
                .Any(a => a.PetId == id &&
                         (a.Status == "Pending" || a.Status == "Approved" || a.Status == "AwaitingPickup"));

            if (hasActiveApplications)
            {
                TempData["Error"] = $"Cannot delete '{pet.Name}' because there are active applications.";
                return RedirectToAction("CustomerDashboard");
            }

            try
            {
                // ⭐ SOFT DELETE
                pet.IsDeleted = true;
                pet.DeletedBy = userId;
                pet.DeletedAt = DateTime.Now;
                pet.DeletionReason = deletionReason == "Others" ? otherReason : deletionReason;
                pet.UpdatedAt = DateTime.Now;

                db.SaveChanges();

                TempData["Success"] = $"Pet '{pet.Name}' has been deleted successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Unable to delete '{pet.Name}'. Error: {ex.Message}";
            }

            return RedirectToAction("CustomerDashboard");
        }

        // ==================== EDIT PET (CUSTOMER) ====================

        // GET: MyPets/CustomerEditPet/5
        [HttpGet]
        public ActionResult CustomerEditPet(int id)
        {
            if (Session["UserId"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int currentUserId = (int)Session["UserId"];

            var pet = db.Pets.Find(id);

            if (pet == null)
            {
                TempData["Error"] = "Pet not found.";
                return RedirectToAction("CustomerDashboard");
            }

            // Verify ownership
            if (pet.OwnerUserId != currentUserId)
            {
                TempData["Error"] = "You don't have permission to edit this pet.";
                return RedirectToAction("CustomerDashboard");
            }

            // Check if pet has active applications (Approved pets with pending applications)
            if (pet.PostStatus == "Approved")
            {
                var hasActiveApplications = db.AdoptionApplications
                    .Any(a => a.PetId == id && a.Status == "Pending");

                if (hasActiveApplications)
                {
                    TempData["Error"] = "Cannot edit pet with active adoption applications.";
                    return RedirectToAction("CustomerDashboard");
                }
            }

            // Check if pet is already adopted
            if (pet.IsAdopted)
            {
                TempData["Error"] = "Cannot edit an adopted pet.";
                return RedirectToAction("CustomerDashboard");
            }

            // Load pet types for dropdown
            ViewBag.PetTypes = db.PetTypes.ToList();

            return View(pet);
        }

        // POST: MyPets/CustomerEditPet
        // POST: MyPets/CustomerEditPet
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CustomerEditPet(Pet pet, HttpPostedFileBase PetImage, HttpPostedFileBase PetImage2, HttpPostedFileBase PetImage3)
        {
            if (Session["UserId"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int currentUserId = (int)Session["UserId"];

            // Get existing pet from database
            var existingPet = db.Pets.Find(pet.PetId);

            if (existingPet == null)
            {
                TempData["Error"] = "Pet not found.";
                return RedirectToAction("CustomerDashboard");
            }

            // Verify ownership
            if (existingPet.OwnerUserId != currentUserId)
            {
                TempData["Error"] = "You don't have permission to edit this pet.";
                return RedirectToAction("CustomerDashboard");
            }

            // Check if pet has active applications
            if (existingPet.PostStatus == "Approved")
            {
                var hasActiveApplications = db.AdoptionApplications
                    .Any(a => a.PetId == pet.PetId && a.Status == "Pending");

                if (hasActiveApplications)
                {
                    TempData["Error"] = "Cannot edit pet with active adoption applications.";
                    return RedirectToAction("CustomerDashboard");
                }
            }

            try
            {
                // Update basic pet information
                existingPet.Name = pet.Name;
                existingPet.PetTypeId = pet.PetTypeId;
                existingPet.Breed = pet.Breed;
                existingPet.Gender = pet.Gender;
                existingPet.Size = pet.Size;
                existingPet.Age = pet.Age;
                existingPet.AgeUnit = pet.AgeUnit;
                existingPet.Location = pet.Location;
                existingPet.DaysInCenter = pet.DaysInCenter;
                existingPet.Description = pet.Description;
                existingPet.Traits = pet.Traits;
                existingPet.Vaccines = pet.Vaccines;
                existingPet.WhyAdoptMe = pet.WhyAdoptMe;
                existingPet.IsHealthy = pet.IsHealthy;
                existingPet.IsNeutered = pet.IsNeutered;

                // ⭐ Handle Photo 1 (Main Photo)
                if (PetImage != null && PetImage.ContentLength > 0)
                {
                    string fileName = Path.GetFileNameWithoutExtension(PetImage.FileName);
                    string extension = Path.GetExtension(PetImage.FileName);
                    fileName = fileName + "_" + DateTime.Now.ToString("yyyyMMddHHmmss") + extension;
                    string path = Path.Combine(Server.MapPath("~/Content/Images/Pets/"), fileName);
                    PetImage.SaveAs(path);
                    existingPet.ImageUrl = "/Content/Images/Pets/" + fileName;
                }

                // ⭐ Handle Photo 2
                if (PetImage2 != null && PetImage2.ContentLength > 0)
                {
                    string fileName = Path.GetFileNameWithoutExtension(PetImage2.FileName);
                    string extension = Path.GetExtension(PetImage2.FileName);
                    fileName = fileName + "_" + DateTime.Now.ToString("yyyyMMddHHmmss") + extension;
                    string path = Path.Combine(Server.MapPath("~/Content/Images/Pets/"), fileName);
                    PetImage2.SaveAs(path);
                    existingPet.ImageUrl2 = "/Content/Images/Pets/" + fileName;
                }

                // ⭐ Handle Photo 3
                if (PetImage3 != null && PetImage3.ContentLength > 0)
                {
                    string fileName = Path.GetFileNameWithoutExtension(PetImage3.FileName);
                    string extension = Path.GetExtension(PetImage3.FileName);
                    fileName = fileName + "_" + DateTime.Now.ToString("yyyyMMddHHmmss") + extension;
                    string path = Path.Combine(Server.MapPath("~/Content/Images/Pets/"), fileName);
                    PetImage3.SaveAs(path);
                    existingPet.ImageUrl3 = "/Content/Images/Pets/" + fileName;
                }

                db.Entry(existingPet).State = EntityState.Modified;
                db.SaveChanges();

                TempData["Success"] = $"{pet.Name}'s information has been updated successfully!";
                return RedirectToAction("CustomerDashboard");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while updating the pet: " + ex.Message;
                ViewBag.PetTypes = db.PetTypes.ToList();
                return View(pet);
            }
        }
    }
}