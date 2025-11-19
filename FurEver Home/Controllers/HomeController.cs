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
    }
}

