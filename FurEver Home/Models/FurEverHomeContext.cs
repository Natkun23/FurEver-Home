using System.Data.Entity;

namespace FurEver_Home.Models
{
    public class FurEverHomeContext : DbContext
    {
        // Constructor - uses connection string name from Web.config
        public FurEverHomeContext() : base("FurEverHomeDB")
        {
        }

        // DbSets - each represents a table in the database
        public DbSet<User> Users { get; set; }
        public DbSet<Pet> Pets { get; set; }
        public DbSet<PetType> PetTypes { get; set; }
        public DbSet<AdoptionApplication> AdoptionApplications { get; set; }
        public DbSet<UserNotification> UserNotifications { get; set; }
        public DbSet<AdoptionHistory> AdoptionHistories { get; set; }
        public DbSet<PetScreeningQuestion> PetScreeningQuestions { get; set; }
        public DbSet<PetScreeningAnswer> PetScreeningAnswers { get; set; }

        // ⭐ NEW: Add Breeds DbSet
        public DbSet<Breed> Breeds { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            // Optional: Configure relationships and constraints
            base.OnModelCreating(modelBuilder);
        }
    }
}