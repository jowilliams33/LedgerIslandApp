namespace LedgerIslandApp.Data;

using LedgerIslandApp.Models.Auth;
using LedgerIslandApp.Models;
using Microsoft.EntityFrameworkCore;

public sealed class AppDbContext : DbContext
{
    //fsd
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // Auth/session
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<LoginAudit> LoginAudits => Set<LoginAudit>();

    // Staging schema
    public DbSet<UploadBatch> UploadBatches => Set<UploadBatch>();
    public DbSet<RowRaw> RowsRaw => Set<RowRaw>();
    public DbSet<RowClean> RowsClean => Set<RowClean>();
    public DbSet<RowIssue> RowIssues => Set<RowIssue>();


    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        // ========== Auth / Session ==========
        b.Entity<LoginAudit>().ToTable("LoginAudit", "dbo");

        b.Entity<Session>(e =>
        {
            e.ToTable("Sessions", "dbo");
            e.HasKey(x => x.SessionId);
            e.Property(x => x.UserId).IsRequired().HasMaxLength(128);
            e.Property(x => x.Token).IsRequired();
            e.HasIndex(x => new { x.UserId, x.IsActive, x.LastSeenAt });
            e.HasIndex(x => x.Token).IsUnique();
        });

        // ========== LedgerIsland (li schema) ==========
        b.Entity<UploadBatch>(e =>
        {
            e.ToTable("UploadBatch", "li");
            e.HasKey(x => x.BatchId);
        });

        b.Entity<RowRaw>(e =>
        {
            e.ToTable("RowsRaw", "li");
            e.HasKey(x => x.RowId);

            e.HasOne<UploadBatch>()
             .WithMany()
             .HasForeignKey(x => x.BatchId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(x => new { x.BatchId, x.RowNum }).IsUnique();
            e.HasIndex(x => x.IngestedAtUtc);
        });

        b.Entity<RowClean>(e =>
        {
            e.ToTable("RowsClean", "li");
            e.HasKey(x => x.CleanId);

            // Batch link stays as-is
            e.HasOne<UploadBatch>()
             .WithMany()
             .HasForeignKey(x => x.BatchId)
             .OnDelete(DeleteBehavior.NoAction);

            // EXPLICIT 1:1 between RowClean.Row and RowRaw.Clean
            // RowClean is the dependent; RowId is the FK to RowRaw.RowId
            e.HasOne(rc => rc.Row)
             .WithOne(rr => rr.Clean)
             .HasForeignKey<RowClean>(rc => rc.RowId)
             .OnDelete(DeleteBehavior.NoAction); // avoid cascade surprises in staging

            // RowId must be unique to enforce 1:1
            e.HasIndex(x => x.RowId).IsUnique();

            e.HasIndex(x => x.BatchId);
            e.HasIndex(x => x.HashKey); // keep your existing property name
        });


        b.Entity<RowIssue>(e =>
        {
            e.ToTable("RowIssues", "li");
            e.HasKey(x => x.IssueId);

            e.HasOne<UploadBatch>()
             .WithMany()
             .HasForeignKey(x => x.BatchId)
             .OnDelete(DeleteBehavior.NoAction);

            e.HasOne<RowRaw>()
             .WithMany()
             .HasForeignKey(x => x.RowId)
             .OnDelete(DeleteBehavior.NoAction);

            e.HasOne<RowClean>()
             .WithMany()
             .HasForeignKey(x => x.CleanId)
             .OnDelete(DeleteBehavior.NoAction);

            e.HasIndex(x => x.BatchId);
            e.HasIndex(x => x.Severity);
        });
    }
}
