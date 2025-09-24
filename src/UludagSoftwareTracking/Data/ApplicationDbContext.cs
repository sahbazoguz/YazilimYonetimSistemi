using Microsoft.EntityFrameworkCore;
using UludagSoftwareTracking.Models.Entities;

namespace UludagSoftwareTracking.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Department> Departments => Set<Department>();

    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();

    public DbSet<Software> Softwares => Set<Software>();

    public DbSet<SoftwareManual> SoftwareManuals => Set<SoftwareManual>();

    public DbSet<SoftwareRequest> SoftwareRequests => Set<SoftwareRequest>();

    public DbSet<RequestApproval> RequestApprovals => Set<RequestApproval>();

    public DbSet<RequestAssessment> RequestAssessments => Set<RequestAssessment>();

    public DbSet<Project> Projects => Set<Project>();

    public DbSet<ProjectAssignment> ProjectAssignments => Set<ProjectAssignment>();

    public DbSet<RequestDiscussionMessage> RequestDiscussionMessages => Set<RequestDiscussionMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.UseCollation("Turkish_CI_AS");

        modelBuilder.Entity<Department>(entity =>
        {
            entity.Property(d => d.Name).IsRequired().HasMaxLength(150);
            entity.HasIndex(d => d.Name).IsUnique();
        });

        modelBuilder.Entity<UserProfile>(entity =>
        {
            entity.Property(u => u.UserName).IsRequired().HasMaxLength(100);
            entity.Property(u => u.FullName).IsRequired().HasMaxLength(150);
            entity.HasIndex(u => u.UserName).IsUnique();
        });

        modelBuilder.Entity<Software>(entity =>
        {
            entity.Property(s => s.Name).IsRequired().HasMaxLength(200);
            entity.Property(s => s.Category).HasMaxLength(150);
            entity.Property(s => s.TechnologyStack).HasMaxLength(150);
            entity.HasOne(s => s.Department)
                .WithMany(d => d.Softwares)
                .HasForeignKey(s => s.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SoftwareManual>(entity =>
        {
            entity.Property(m => m.Title).IsRequired().HasMaxLength(200);
            entity.HasOne(m => m.Software)
                .WithMany(s => s.Manuals)
                .HasForeignKey(m => m.SoftwareId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SoftwareRequest>(entity =>
        {
            entity.Property(r => r.Title).IsRequired().HasMaxLength(200);
            entity.Property(r => r.Description).IsRequired().HasMaxLength(2000);
            entity.HasOne(r => r.Department)
                .WithMany()
                .HasForeignKey(r => r.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(r => r.RequestedByUser)
                .WithMany()
                .HasForeignKey(r => r.RequestedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(r => r.ExistingSoftware)
                .WithMany()
                .HasForeignKey(r => r.ExistingSoftwareId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<RequestApproval>(entity =>
        {
            entity.HasOne(a => a.Request)
                .WithMany(r => r.Approvals)
                .HasForeignKey(a => a.RequestId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(a => a.ApprovedByUser)
                .WithMany()
                .HasForeignKey(a => a.ApprovedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<RequestAssessment>(entity =>
        {
            entity.HasOne(a => a.Request)
                .WithMany(r => r.Assessments)
                .HasForeignKey(a => a.RequestId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(a => a.AssessedByUser)
                .WithMany()
                .HasForeignKey(a => a.AssessedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(a => a.ExistingSoftware)
                .WithMany()
                .HasForeignKey(a => a.ExistingSoftwareId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Project>(entity =>
        {
            entity.HasOne(p => p.Request)
                .WithOne(r => r.Project)
                .HasForeignKey<Project>(p => p.RequestId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(p => p.LeadUser)
                .WithMany()
                .HasForeignKey(p => p.LeadUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ProjectAssignment>(entity =>
        {
            entity.HasOne(a => a.Project)
                .WithMany(p => p.Assignments)
                .HasForeignKey(a => a.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RequestDiscussionMessage>(entity =>
        {
            entity.Property(m => m.Message).IsRequired().HasMaxLength(1000);
            entity.HasOne(m => m.Request)
                .WithMany(r => r.DiscussionMessages)
                .HasForeignKey(m => m.RequestId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(m => m.Sender)
                .WithMany()
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
