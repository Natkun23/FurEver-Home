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

        // ⭐ ADD THIS LINE - NEW DbSet for History
        public DbSet<AdoptionHistory> AdoptionHistories { get; set; }

        // ⭐ ADD THESE TWO NEW LINES
        public DbSet<PetScreeningQuestion> PetScreeningQuestions { get; set; }
        public DbSet<PetScreeningAnswer> PetScreeningAnswers { get; set; }


        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            // Optional: Configure relationships and constraints
            base.OnModelCreating(modelBuilder);
        }
    }
}