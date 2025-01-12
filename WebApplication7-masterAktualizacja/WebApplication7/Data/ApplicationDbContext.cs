using Microsoft.EntityFrameworkCore;
using WebApplication7.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace WebApplication7.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Property> Properties { get; set; }
        public DbSet<Reservation> Reservations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique(); 

            modelBuilder.Entity<Property>()
                .Property(p => p.Price)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Property>()
                .HasOne(p => p.OwnerUser)
                .WithMany(u => u.Properties)
                .HasForeignKey(p => p.OwnerUserId);


            modelBuilder.Entity<Property>()
                .HasMany(p => p.Reservations)
                .WithOne(r => r.Property)
                .HasForeignKey(r => r.PropertyId);

            modelBuilder.Entity<User>()
                .HasMany(u => u.Reservations)
                .WithOne(r => r.User)
                .HasForeignKey(r => r.UserId);

            modelBuilder.Entity<Reservation>()
                .HasOne(r => r.Property)
                .WithMany(p => p.Reservations)
                .HasForeignKey(r => r.PropertyId);

            modelBuilder.Entity<Reservation>()
                .HasOne(r => r.User)
                .WithMany(u => u.Reservations)
                .HasForeignKey(r => r.UserId);
        }

        public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            
            var reservationsToUpdate = ChangeTracker.Entries<Reservation>()
                .Where(e => e.Entity.EndDate < DateTime.Now && e.Entity.Status != "Ended")
                .Select(e => e.Entity);

            foreach (var reservation in reservationsToUpdate)
            {
                reservation.Status = "Ended";
            }

            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }


    }
}
