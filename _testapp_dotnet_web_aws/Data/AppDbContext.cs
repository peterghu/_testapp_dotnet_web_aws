using Microsoft.EntityFrameworkCore;

namespace _testapp_dotnet_web_aws.Data
{
    public partial class AppDbContext : DbContext
    {
        public AppDbContext()
        {
        }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public virtual DbSet<RequestLogs> RequestLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<RequestLogs>(entity =>
            {
                entity.ToTable("request_logs");

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .UseIdentityAlwaysColumn();

                entity.Property(e => e.Message).HasColumnName("message");

                entity.Property(e => e.Origin).HasColumnName("origin");

                entity.Property(e => e.CreatedOn).HasColumnName("created_on");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}