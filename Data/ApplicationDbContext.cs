using Microsoft.EntityFrameworkCore;
using TaskManagement_02.Models;


namespace TaskManagement_02.Data
{
    public class ApplicationDbContext : DbContext
    {
        
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<UserModel> Users { get; set; }
        public DbSet<CategoryModel> Categories { get; set; }
        public DbSet<TaskModel> Tasks { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
          
                
            base.OnModelCreating(modelBuilder);

            //User-task relationship

            modelBuilder.Entity<TaskModel>()
               .HasOne(t => t.AssignedPerson)
               .WithMany(u => u.Tasks)
               .HasForeignKey(t => t.AssignedPersonId)
               .OnDelete(DeleteBehavior.Restrict);


            // Category-task relationship

            modelBuilder.Entity<TaskModel>()
                 .HasOne(t => t.Category)
                 .WithMany(c => c.Tasks)
                 .HasForeignKey(t => t.CategoryName)
                 .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserModel>()
              .HasIndex(u => u.Email)
              .IsUnique();
        }
       

    }
}
