using Microsoft.EntityFrameworkCore;
using OCR_AccessControl.Models;

namespace OCR_AccessControl.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Users> Users { get; set; } // Users table
        public DbSet<NonResidentLogs> NonResidentLogs { get; set; } // NonResidentLogs table

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Users>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Email).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Username).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Password).HasMaxLength(255).IsRequired();
                entity.HasIndex(e => e.Email).IsUnique(); // Ensure emails are unique
            });

            // Configure the NonResidentLogs     table
            modelBuilder.Entity<NonResidentLogs>(entity =>
            {
                entity.HasKey(e => e.id); // Primary key
                entity.Property(e => e.full_name).HasMaxLength(100); // Optional: Set max length for Name
                entity.Property(e => e.id_type).HasMaxLength(50); // Optional: Set max length for IdType
                entity.Property(e => e.id_number).HasMaxLength(50); // Optional: Set max length for IdNumber
                entity.Property(e => e.qr_code).HasMaxLength(255); // Optional: Set max length for QRCode
            });
        }
    }
}