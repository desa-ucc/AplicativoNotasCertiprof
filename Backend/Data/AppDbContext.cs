using Microsoft.EntityFrameworkCore;
using Backend.Models;

namespace Backend.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<CertiprofRecord> CertiprofRecords { get; set; } = null!;
        public DbSet<UploadHistory> UploadHistories { get; set; } = null!;
        public DbSet<User> Users { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Mapping entities to tables with the `cert_` prefix
            modelBuilder.Entity<CertiprofRecord>().ToTable("cert_registros");
            modelBuilder.Entity<UploadHistory>().ToTable("cert_UploadHistories");
            modelBuilder.Entity<User>().ToTable("cert_usuarios");
        }
    }
}
