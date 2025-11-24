using FurEver_Home.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace FurEver_Home.Controllers
{
    public class MyPetsController : Controller
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
                .Where(p => p.OwnerUserId == userId)
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
        public ActionResult DeletePet(int id)
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

            // ⭐ FIX: Check if pet is already adopted
            if (pet.IsAdopted)
            {
                TempData["Error"] = $"Cannot delete '{pet.Name}' because it has already been adopted.";
                return RedirectToAction("CustomerDashboard");
            }

            // ⭐ FIX: Check if pet has active applications (including AwaitingPickup)
            var hasActiveApplications = db.AdoptionApplications
                .Any(a => a.PetId == id &&
                         (a.Status == "Pending" || a.Status == "Approved" || a.Status == "AwaitingPickup"));

            if (hasActiveApplications)
            {
                TempData["Error"] = $"Cannot delete '{pet.Name}' because there are active applications. Please wait for applicants to withdraw or complete the adoption process.";
                return RedirectToAction("CustomerDashboard");
            }

            try
            {
                // ⭐ FIX: Delete related records in the correct order to avoid foreign key conflicts

                // 1. Delete screening answers first (if they reference screening questions)
                var screeningAnswers = db.PetScreeningAnswers
                    .Where(a => a.Question.PetId == id)
                    .ToList();
                if (screeningAnswers.Any())
                {
                    db.PetScreeningAnswers.RemoveRange(screeningAnswers);
                }

                // 2. Delete screening questions
                var questions = db.PetScreeningQuestions
                    .Where(q => q.PetId == id)
                    .ToList();
                if (questions.Any())
                {
                    db.PetScreeningQuestions.RemoveRange(questions);
                }

                // 3. Delete any rejected/withdrawn applications (safe to delete)
                var inactiveApplications = db.AdoptionApplications
                    .Where(a => a.PetId == id &&
                           (a.Status == "Rejected" || a.Status == "Withdrawn"))
                    .ToList();
                if (inactiveApplications.Any())
                {
                    db.AdoptionApplications.RemoveRange(inactiveApplications);
                }

                // 4. Finally delete the pet
                db.Pets.Remove(pet);
                db.SaveChanges();

                TempData["Success"] = $"Pet '{pet.Name}' has been deleted successfully.";
            }
            catch (Exception ex)
            {
                // ⭐ FIX: Better error handling
                TempData["Error"] = $"Unable to delete '{pet.Name}'. Error: {ex.Message}";
            }

            return RedirectToAction("CustomerDashboard");
        }
    }
}