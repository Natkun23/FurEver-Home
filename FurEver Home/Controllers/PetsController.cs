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
            // ⭐ LOAD NOTIFICATIONS FOR LOGGED IN USERS
            if (Session["UserId"] != null)
            {
                int userId = (int)Session["UserId"];
                var user = db.Users.Find(userId);

                if (user != null)
                {
                    var unreadNotifications = db.UserNotifications
                        .Where(n => n.UserId == userId && !n.IsRead)
                        .OrderByDescending(n => n.CreatedAt)
                        .ToList();

                    ViewBag.UnreadNotifications = unreadNotifications;
                    ViewBag.HasUnreadNotifications = unreadNotifications.Any();
                }
            }

            // ⭐ UPDATED: Only show approved posts OR admin posts
            var pets = db.Pets.Include(p => p.PetType)
                              .Include(p => p.Creator)
                              .Where(p => !p.IsAdopted &&
                                     (p.PostedByType == "Admin" ||
                                      p.PostedByType == "Organization" ||
                                      (p.PostedByType == "Customer" && p.PostStatus == "Approved")))
                              .OrderByDescending(p => p.DateAdded)
                              .ToList();

            ViewBag.PetType = null;
            return View(pets);
        }


        // GET: Pets/Dogs
        public ActionResult Dogs()
        {
            // ⭐ LOAD NOTIFICATIONS FOR LOGGED IN USERS
            if (Session["UserId"] != null)
            {
                int userId = (int)Session["UserId"];
                var user = db.Users.Find(userId);

                if (user != null)
                {
                    var unreadNotifications = db.UserNotifications
                        .Where(n => n.UserId == userId && !n.IsRead)
                        .OrderByDescending(n => n.CreatedAt)
                        .ToList();

                    ViewBag.UnreadNotifications = unreadNotifications;
                    ViewBag.HasUnreadNotifications = unreadNotifications.Any();
                }
            }

            // ⭐ UPDATED: Only show approved posts OR admin posts
            var dogs = db.Pets.Include(p => p.PetType)
                              .Include(p => p.Creator)
                              .Where(p => p.PetTypeId == 1 &&
                                     !p.IsAdopted &&
                                     (p.PostedByType == "Admin" ||
                                      p.PostedByType == "Organization" ||
                                      (p.PostedByType == "Customer" && p.PostStatus == "Approved")))
                              .OrderByDescending(p => p.DateAdded)
                              .ToList();

            ViewBag.PetType = "Dogs";
            return View("Index", dogs);
        }


        // GET: Pets/Cats
        public ActionResult Cats()
        {
            // ⭐ LOAD NOTIFICATIONS FOR LOGGED IN USERS
            if (Session["UserId"] != null)
            {
                int userId = (int)Session["UserId"];
                var user = db.Users.Find(userId);

                if (user != null)
                {
                    var unreadNotifications = db.UserNotifications
                        .Where(n => n.UserId == userId && !n.IsRead)
                        .OrderByDescending(n => n.CreatedAt)
                        .ToList();

                    ViewBag.UnreadNotifications = unreadNotifications;
                    ViewBag.HasUnreadNotifications = unreadNotifications.Any();
                }
            }

            // ⭐ UPDATED: Only show approved posts OR admin posts
            var cats = db.Pets.Include(p => p.PetType)
                              .Include(p => p.Creator)
                              .Where(p => p.PetTypeId == 2 &&
                                     !p.IsAdopted &&
                                     (p.PostedByType == "Admin" ||
                                      p.PostedByType == "Organization" ||
                                      (p.PostedByType == "Customer" && p.PostStatus == "Approved")))
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

            var pet = db.Pets.Include(p => p.PetType)
                             .Include(p => p.Creator)
                             .FirstOrDefault(p => p.PetId == id);
            if (pet == null)
            {
                return HttpNotFound();
            }

            ViewBag.PendingApplicationsCount = db.AdoptionApplications
                .Count(a => a.PetId == id && a.Status == "Pending");

            if (Session["UserId"] != null)
            {
                int userId = (int)Session["UserId"];
                ViewBag.UserHasApplied = db.AdoptionApplications
                    .Any(a => a.PetId == id && a.UserId == userId && a.Status == "Pending");
            }

            return View(pet);
        }

        // ⭐ UPDATED: GET: Pets/Apply/5
        public ActionResult Apply(int? id)
        {
            if (Session["UserId"] == null)
            {
                TempData["Error"] = "Please login to apply for adoption.";
                return RedirectToAction("Login", "Account");
            }

            if (id == null)
            {
                return RedirectToAction("Index");
            }

            int userId = (int)Session["UserId"];
            var user = db.Users.Find(userId);

            // Check if user's ID is verified
            if (user.IDStatus != "Verified")
            {
                TempData["Error"] = "Your ID must be verified before you can apply for adoption. Please upload your valid ID in your profile and wait for admin verification.";
                return RedirectToAction("Profile", "Account");
            }

            var pet = db.Pets
                .Include(p => p.PetType)
                .Include(p => p.Owner)  // ⭐ Include pet owner
                .FirstOrDefault(p => p.PetId == id);

            if (pet == null)
            {
                return HttpNotFound();
            }

            if (pet.IsAdopted)
            {
                TempData["Error"] = "This pet has already been adopted.";
                return RedirectToAction("Details", new { id = id });
            }

            // ⭐ NEW: Check if pet post is approved (for customer posts)
            if (pet.PostedByType == "Customer" && pet.PostStatus != "Approved")
            {
                TempData["Error"] = "This pet post is not yet approved for adoption applications.";
                return RedirectToAction("Details", new { id = id });
            }

            // Check if user already has a pending application for this pet
            var existingApplication = db.AdoptionApplications
                .FirstOrDefault(a => a.UserId == userId && a.PetId == id && a.Status == "Pending");

            if (existingApplication != null)
            {
                TempData["Error"] = "You have already applied to adopt this pet. Please wait for review.";
                return RedirectToAction("Details", new { id = id });
            }

            var model = new AdoptionApplication
            {
                PetId = id.Value,
                Pet = pet
            };

            // ⭐ NEW: Load custom screening questions if this is a customer post
            if (pet.PostedByType == "Customer")
            {
                var customQuestions = db.PetScreeningQuestions
                    .Where(q => q.PetId == id)
                    .OrderBy(q => q.OrderNumber)
                    .ToList();

                ViewBag.CustomQuestions = customQuestions;
            }
            else
            {
                ViewBag.CustomQuestions = null; // Admin post - use default questions
            }

            return View(model);
        }


        // ⭐ UPDATED: POST: Pets/Apply
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Apply(AdoptionApplication model, FormCollection form)
        {
            if (Session["UserId"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int userId = (int)Session["UserId"];
            var user = db.Users.Find(userId);

            // Double-check ID verification
            if (user.IDStatus != "Verified")
            {
                TempData["Error"] = "Your ID must be verified before you can apply for adoption.";
                return RedirectToAction("Profile", "Account");
            }

            // Load pet with owner info
            var pet = db.Pets
                .Include(p => p.PetType)
                .Include(p => p.Owner)
                .FirstOrDefault(p => p.PetId == model.PetId);

            if (pet == null)
            {
                TempData["Error"] = "Pet not found.";
                return RedirectToAction("Index");
            }

            if (ModelState.IsValid)
            {
                var existingApplication = db.AdoptionApplications
                    .FirstOrDefault(a => a.UserId == userId && a.PetId == model.PetId && a.Status == "Pending");

                if (existingApplication != null)
                {
                    TempData["Error"] = "You have already applied to adopt this pet.";
                    return RedirectToAction("Details", new { id = model.PetId });
                }

                // ⭐ NEW: Check if this is a customer post with custom questions
                var hasCustomQuestions = pet.PostedByType == "Customer" &&
                                         db.PetScreeningQuestions.Any(q => q.PetId == model.PetId);

                if (hasCustomQuestions)
                {
                    // ⭐ CUSTOMER POST: Validate custom question answers
                    var customQuestions = db.PetScreeningQuestions
                        .Where(q => q.PetId == model.PetId)
                        .ToList();

                    foreach (var question in customQuestions)
                    {
                        var answerKey = $"CustomAnswer_{question.QuestionId}";
                        var answer = form[answerKey];

                        if (question.IsRequired && string.IsNullOrWhiteSpace(answer))
                        {
                            ModelState.AddModelError("", $"Please answer question {question.OrderNumber}: {question.QuestionText}");
                        }
                    }

                    if (!ModelState.IsValid)
                    {
                        model.Pet = pet;
                        ViewBag.CustomQuestions = customQuestions;
                        return View(model);
                    }

                    // Clear default screening fields for custom questions
                    model.ScreeningQ1Experience = null;
                    model.ScreeningQ2Financial = null;
                    model.ScreeningQ2Explanation = null;
                    model.ScreeningQ3HouseholdAgreement = null;
                    model.ScreeningQ3Explanation = null;
                    model.ScreeningQ4RelocationPlan = null;
                    model.ScreeningQ5HoursAlone = null;
                }
                else
                {
                    // ⭐ ADMIN POST: Validate default screening questions
                    if (string.IsNullOrWhiteSpace(model.ScreeningQ1Experience))
                    {
                        ModelState.AddModelError("ScreeningQ1Experience", "Please describe your pet care experience.");
                    }
                    if (string.IsNullOrWhiteSpace(model.ScreeningQ2Financial))
                    {
                        ModelState.AddModelError("ScreeningQ2Financial", "Please answer the financial commitment question.");
                    }
                    if (string.IsNullOrWhiteSpace(model.ScreeningQ2Explanation))
                    {
                        ModelState.AddModelError("ScreeningQ2Explanation", "Please explain your financial readiness.");
                    }
                    if (string.IsNullOrWhiteSpace(model.ScreeningQ3HouseholdAgreement))
                    {
                        ModelState.AddModelError("ScreeningQ3HouseholdAgreement", "Please answer about household agreement.");
                    }
                    if (string.IsNullOrWhiteSpace(model.ScreeningQ3Explanation))
                    {
                        ModelState.AddModelError("ScreeningQ3Explanation", "Please explain household support.");
                    }
                    if (string.IsNullOrWhiteSpace(model.ScreeningQ4RelocationPlan))
                    {
                        ModelState.AddModelError("ScreeningQ4RelocationPlan", "Please describe your contingency plan.");
                    }
                    if (string.IsNullOrWhiteSpace(model.ScreeningQ5HoursAlone))
                    {
                        ModelState.AddModelError("ScreeningQ5HoursAlone", "Please select how many hours the pet will be alone.");
                    }

                    if (!ModelState.IsValid)
                    {
                        model.Pet = pet;
                        ViewBag.CustomQuestions = null;
                        return View(model);
                    }
                }

                // Save application
                model.UserId = userId;
                model.ApplicationDate = DateTime.Now;
                model.Status = "Pending";
                model.CreatedAt = DateTime.Now;
                model.UpdatedAt = DateTime.Now;

                db.AdoptionApplications.Add(model);
                db.SaveChanges();

                // ⭐ NEW: Save custom question answers if this is a customer post
                if (hasCustomQuestions)
                {
                    var customQuestions = db.PetScreeningQuestions
                        .Where(q => q.PetId == model.PetId)
                        .ToList();

                    foreach (var question in customQuestions)
                    {
                        var answerKey = $"CustomAnswer_{question.QuestionId}";
                        var answer = form[answerKey];

                        if (!string.IsNullOrWhiteSpace(answer))
                        {
                            var screeningAnswer = new PetScreeningAnswer
                            {
                                ApplicationId = model.ApplicationId,
                                QuestionId = question.QuestionId,
                                AnswerText = answer,
                                CreatedAt = DateTime.Now
                            };
                            db.PetScreeningAnswers.Add(screeningAnswer);
                        }
                    }

                    db.SaveChanges();

                    // ⭐ Notify pet owner (customer)
                    if (pet.OwnerUserId.HasValue)
                    {
                        var notification = new UserNotification
                        {
                            UserId = pet.OwnerUserId.Value,
                            NotificationType = "Application_Received",
                            Title = "New Adoption Application! 🎉",
                            Message = $"{user.FullName} has applied to adopt your pet '{pet.Name}'. Please review their application in your dashboard.",
                            IsRead = false,
                            CreatedAt = DateTime.Now
                        };
                        db.UserNotifications.Add(notification);
                        db.SaveChanges();
                    }

                    TempData["Success"] = $"Your adoption application for {pet.Name} has been submitted successfully! The pet owner will review your answers and contact you within 24-48 hours.";
                }
                else
                {
                    // Default admin notification (existing code)
                    TempData["Success"] = $"Your adoption application for {pet.Name} has been submitted successfully! We'll review your answers and contact you within 24-48 hours.";
                }

                return RedirectToAction("MyApplications");
            }

            model.Pet = pet;

            // Reload custom questions if needed
            if (pet.PostedByType == "Customer")
            {
                ViewBag.CustomQuestions = db.PetScreeningQuestions
                    .Where(q => q.PetId == model.PetId)
                    .OrderBy(q => q.OrderNumber)
                    .ToList();
            }
            else
            {
                ViewBag.CustomQuestions = null;
            }

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

            if (user.IDStatus != "Verified")
            {
                TempData["Error"] = "Your ID must be verified before you can post pets for adoption. Please upload your ID in your profile.";
                return RedirectToAction("Profile", "Account");
            }

            ViewBag.PetTypes = db.PetTypes.ToList();
            return View();
        }

        // POST: Pets/PostForAdoption - UPDATED TO HANDLE CUSTOM QUESTIONS
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult PostForAdoption(Pet model, HttpPostedFileBase PetImage, string[] Questions, string[] QuestionTypes)
        {
            if (Session["UserId"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int userId = (int)Session["UserId"];
            var user = db.Users.Find(userId);

            // Check ID verification
            if (user.IDStatus != "Verified")
            {
                TempData["Error"] = "Your ID must be verified before you can post pets for adoption.";
                return RedirectToAction("Profile", "Account");
            }

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

                // Set pet properties
                model.DateAdded = DateTime.Now;
                model.CreatedAt = DateTime.Now;
                model.UpdatedAt = DateTime.Now;
                model.IsAdopted = false;
                model.CreatedBy = userId;
                model.OwnerUserId = userId; // ⭐ NEW: Set owner
                model.PostedByType = "Customer"; // ⭐ NEW
                model.PostStatus = "Pending"; // ⭐ NEW: Requires admin approval
                model.RequiresAdminApproval = true; // ⭐ NEW
                model.AdminVerified = false; // ⭐ NEW
                model.OrganizationName = null;

                db.Pets.Add(model);
                db.SaveChanges();

                // ⭐ NEW: Save custom screening questions
                if (Questions != null && Questions.Length > 0)
                {
                    for (int i = 0; i < Questions.Length; i++)
                    {
                        if (!string.IsNullOrWhiteSpace(Questions[i]))
                        {
                            var question = new PetScreeningQuestion
                            {
                                PetId = model.PetId,
                                QuestionText = Questions[i],
                                QuestionType = QuestionTypes != null && i < QuestionTypes.Length ? QuestionTypes[i] : "Text",
                                IsRequired = true,
                                OrderNumber = i + 1,
                                CreatedAt = DateTime.Now
                            };
                            db.PetScreeningQuestions.Add(question);
                        }
                    }
                    db.SaveChanges();
                }

                TempData["Success"] = $"Pet '{model.Name}' has been submitted for admin review! You'll be notified once it's approved.";
                return RedirectToAction("CustomerDashboard", "MyPets"); // ⭐ CHANGED FROM "Dashboard"
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

            var application = db.AdoptionApplications
                .Include(a => a.Pet)
                .Include(a => a.Pet.PetType)
                .FirstOrDefault(a => a.ApplicationId == id);

            if (application != null &&
                application.UserId == userId &&
                application.IsReadyForPickup &&
                application.ClaimedDate == null)
            {
                application.ClaimedDate = DateTime.Now;
                application.Status = "Completed";
                application.UpdatedAt = DateTime.Now;

                ArchiveApplicationToHistory(application, "Completed");

                db.SaveChanges();

                TempData["Success"] = $"Congratulations! {application.Pet.Name} is now officially yours! 🎉";
                return RedirectToAction("MyApplications");
            }

            TempData["Error"] = "Unable to process claim.";
            return RedirectToAction("MyApplications");
        }

        // POST: Pets/WithdrawApplication - ✅ UPDATED WITH VALIDATION
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult WithdrawApplication(int id, string withdrawalReason)
        {
            if (Session["UserId"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int userId = (int)Session["UserId"];
            var application = db.AdoptionApplications
                .Include(a => a.Pet)
                .Include(a => a.Pet.PetType)
                .FirstOrDefault(a => a.ApplicationId == id);

            // ✅ VALIDATION: Can only withdraw if Status = "Pending"
            if (application == null || application.UserId != userId)
            {
                TempData["Error"] = "Application not found.";
                return RedirectToAction("MyApplications");
            }

            if (application.Status != "Pending")
            {
                TempData["Error"] = "Cannot withdraw application. It has already been reviewed by admin.";
                return RedirectToAction("MyApplications");
            }

            if (string.IsNullOrWhiteSpace(withdrawalReason))
            {
                TempData["Error"] = "Please provide a reason for withdrawal.";
                return RedirectToAction("MyApplications");
            }

            application.Status = "Withdrawn";
            application.WithdrawalReason = withdrawalReason;
            application.WithdrawalDate = DateTime.Now;
            application.UpdatedAt = DateTime.Now;

            ArchiveApplicationToHistory(application, "Withdrawn");

            db.SaveChanges();

            TempData["Success"] = "Your application has been withdrawn successfully.";
            return RedirectToAction("MyApplications");
        }

        // POST: Pets/RequestCancellation - ✅ BLOCK CANCELLATION IF APPROVED
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RequestCancellation(int id, string cancellationReason)
        {
            if (Session["UserId"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int userId = (int)Session["UserId"];
            var application = db.AdoptionApplications
                .Include(a => a.Pet)
                .FirstOrDefault(a => a.ApplicationId == id);

            // ✅ VALIDATION: Application must exist and belong to user
            if (application == null || application.UserId != userId)
            {
                TempData["Error"] = "Application not found.";
                return RedirectToAction("MyApplications");
            }

            // ✅ CRITICAL: NO CANCELLATIONS ALLOWED FOR APPROVED APPLICATIONS
            if (application.Status == "Approved")
            {
                TempData["Error"] = "❌ Cancellation Not Allowed: Your application has already been approved by our admin team. Once approved, the adoption is confirmed and cannot be cancelled. This policy ensures commitment to our pets. If you have serious concerns, please contact our support team directly.";
                return RedirectToAction("MyApplications");
            }

            // ✅ Can only withdraw if Status = "Pending"
            if (application.Status != "Pending")
            {
                TempData["Error"] = "Cannot withdraw application. It has already been reviewed by admin.";
                return RedirectToAction("MyApplications");
            }

            if (string.IsNullOrWhiteSpace(cancellationReason))
            {
                TempData["Error"] = "Please provide a reason for withdrawal.";
                return RedirectToAction("MyApplications");
            }

            // ✅ Only "Withdrawn" for pending applications
            application.Status = "Withdrawn";
            application.WithdrawalReason = cancellationReason;
            application.WithdrawalDate = DateTime.Now;
            application.UpdatedAt = DateTime.Now;

            ArchiveApplicationToHistory(application, "Withdrawn");

            db.SaveChanges();

            TempData["Success"] = "Your application has been withdrawn successfully.";
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

            ViewBag.ActiveApplications = db.AdoptionApplications
                .Include(a => a.Pet)
                .Include(a => a.Pet.PetType)
                .Where(a => a.UserId == userId &&
                            a.Status != "Withdrawn" &&
                            a.Status != "Completed" &&
                            a.Status != "Rejected" &&
                            a.Status != "Cancelled")
                .OrderByDescending(a => a.ApplicationDate)
                .ToList();

            var sixMonthsAgo = DateTime.Now.AddMonths(-6);
            ViewBag.History = db.AdoptionHistories
                .Where(h => h.UserId == userId && h.ArchivedAt >= sixMonthsAgo)
                .OrderByDescending(h => h.ArchivedAt)
                .ToList();

            var expiredHistory = db.AdoptionHistories
                .Where(h => h.AutoDeleteAfter != null && h.AutoDeleteAfter < DateTime.Now)
                .ToList();

            if (expiredHistory.Any())
            {
                db.AdoptionHistories.RemoveRange(expiredHistory);
                db.SaveChanges();
            }

            return View();
        }

        private void ArchiveApplicationToHistory(AdoptionApplication application, string finalStatus)
        {
            var history = new AdoptionHistory
            {
                ApplicationId = application.ApplicationId,
                UserId = application.UserId,
                PetId = application.PetId,

                PetName = application.Pet.Name,
                PetBreed = application.Pet.Breed,
                PetType = application.Pet.PetType?.TypeName,
                PetImageUrl = application.Pet.ImageUrl,

                PhoneNumber = application.PhoneNumber,
                Address = application.Address,
                HousingType = application.HousingType,

                ApplicationDate = application.ApplicationDate,
                ApprovalDate = application.ReviewedDate,
                ClaimedDate = application.ClaimedDate,
                CompletedDate = finalStatus == "Completed" ? DateTime.Now : (DateTime?)null,

                FinalStatus = finalStatus,

                CancellationReason = application.CancellationReason,
                WithdrawalReason = application.WithdrawalReason,
                CancellationRequestedDate = application.CancellationRequestedDate,
                CancellationApprovedBy = application.CancellationReviewedBy,
                CancellationApprovedDate = application.CancellationReviewedDate,

                AdminNotes = application.AdminNotes,
                RejectionReason = application.RejectionReason,

                CreatedAt = DateTime.Now,
                ArchivedAt = DateTime.Now,
                AutoDeleteAfter = finalStatus == "Completed" ? DateTime.Now.AddMonths(6) : (DateTime?)null
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