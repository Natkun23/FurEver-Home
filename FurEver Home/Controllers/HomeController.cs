using System.Linq;
using System.Web.Mvc;
using FurEver_Home.Models;
using System.Data.Entity;
using FurEver_Home.Filters;

namespace FurEver_Home.Controllers
{
    public class HomeController : BaseController
    {
        private FurEverHomeContext db = new FurEverHomeContext();

        public ActionResult Index()
        {
            // Check if user is logged in
            if (Session["UserId"] != null)
            {
                int userId = (int)Session["UserId"];
                var user = db.Users.Find(userId);

                if (user != null)
                {
                    ViewBag.IsLoggedIn = true;
                    ViewBag.UserName = user.FullName;
                    ViewBag.UserProfilePicture = user.ProfilePictureUrl;

                    // Check for unread notifications
                    var unreadNotifications = db.UserNotifications
                        .Where(n => n.UserId == userId && !n.IsRead)
                        .OrderByDescending(n => n.CreatedAt)
                        .ToList();

                    ViewBag.UnreadNotifications = unreadNotifications;
                    ViewBag.HasUnreadNotifications = unreadNotifications.Any();
                }
            }
            else
            {
                ViewBag.IsLoggedIn = false;
            }

            // GET FEATURED PETS FROM DATABASE (Latest 4 pets)
            var featuredPets = db.Pets
                .Include(p => p.PetType)
                .Where(p => !p.IsAdopted)
                .OrderByDescending(p => p.DateAdded)
                .Take(4)
                .ToList();

            ViewBag.FeaturedPets = featuredPets;

            return View();
        }

        // Mark notification as read
        [HttpPost]
        public ActionResult MarkNotificationAsRead(int id)
        {
            var notification = db.UserNotifications.Find(id);
            if (notification != null)
            {
                notification.IsRead = true;
                db.SaveChanges();
            }
            return Json(new { success = true });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

    // GET: Home/NotificationHistory
public ActionResult NotificationHistory()
        {
            if (Session["UserId"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int userId = (int)Session["UserId"];

            // Get ALL notifications (both read and unread), ordered by most recent
            var allNotifications = db.UserNotifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ToList();

            // Separate unread and read for display
            ViewBag.UnreadNotifications = allNotifications.Where(n => !n.IsRead).ToList();
            ViewBag.ReadNotifications = allNotifications.Where(n => n.IsRead).ToList();

            return View(allNotifications);
        }

        // GET: Home/SuccessfulStories
        public ActionResult SuccessfulStories()
        {
            // Check if user is logged in (optional - stories can be public)
            if (Session["UserId"] != null)
            {
                int userId = (int)Session["UserId"];
                var user = db.Users.Find(userId);
                if (user != null)
                {
                    ViewBag.IsLoggedIn = true;
                    ViewBag.UserName = user.FullName;
                    ViewBag.UserProfilePicture = user.ProfilePictureUrl;

                    // Check for unread notifications
                    var unreadNotifications = db.UserNotifications
                        .Where(n => n.UserId == userId && !n.IsRead)
                        .OrderByDescending(n => n.CreatedAt)
                        .ToList();

                    ViewBag.UnreadNotifications = unreadNotifications;
                    ViewBag.HasUnreadNotifications = unreadNotifications.Any();
                }
            }
            else
            {
                ViewBag.IsLoggedIn = false;
            }

            // 🎯 OPTION 1: Pull from database (if you have a SuccessStories table)
            // var stories = db.SuccessStories.OrderByDescending(s => s.AdoptionDate).ToList();
            // return View(stories);

            // 🎯 OPTION 2: For now, we'll pass hardcoded sample stories via ViewBag
            return View();
        }
    }


}
