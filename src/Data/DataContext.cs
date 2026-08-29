using Microsoft.EntityFrameworkCore;
using Planara.Common.Database;
using Planara.Common.Database.Domain;
using Planara.Privacy.Data.Domain;

namespace Planara.Privacy.Data;

public class DataContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<ConsentVersion> ConsentVersions { get; set; } = null!;
    public DbSet<UserConsent> UserConsents { get; set; } = null!;
    public DbSet<OutboxMessage> OutboxMessages { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.AddOutbox();

        modelBuilder.Entity<ConsentVersion>()
            .HasKey(x => x.Id);

        modelBuilder.Entity<ConsentVersion>()
            .Property(x => x.Content)
            .IsRequired();

        modelBuilder.Entity<ConsentVersion>()
            .Property(x => x.HtmlContent)
            .IsRequired();

        modelBuilder.Entity<ConsentVersion>()
            .HasIndex(x => new { x.Type, x.Version })
            .IsUnique();

        modelBuilder.Entity<ConsentVersion>()
            .HasIndex(x => new { x.Type, x.Status, x.EffectiveAt });

        modelBuilder.Entity<UserConsent>()
            .HasKey(x => x.Id);

        modelBuilder.Entity<UserConsent>()
            .HasIndex(x => x.GrantRequestId)
            .IsUnique();

        modelBuilder.Entity<UserConsent>()
            .HasIndex(x => x.ConsentVersionId);

        modelBuilder.Entity<UserConsent>()
            .HasIndex(x => new { x.UserId, x.ConsentVersionId });

        modelBuilder.Entity<UserConsent>()
            .HasIndex(x => new { x.RegistrationId, x.ConsentVersionId });

        modelBuilder.Entity<UserConsent>()
            .HasIndex(x => x.ExpiresAt);

        modelBuilder.Entity<UserConsent>()
            .HasIndex(x => x.RevokedAt);

        modelBuilder.Entity<UserConsent>()
            .HasOne(x => x.ConsentVersion)
            .WithMany()
            .HasForeignKey(x => x.ConsentVersionId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<UserConsent>()
            .ToTable(table =>
            {
                table.HasCheckConstraint(
                    "CK_UserConsents_Subject",
                    """
                    "RegistrationId" IS NOT NULL OR "UserId" IS NOT NULL
                    """);
            });
    }
}