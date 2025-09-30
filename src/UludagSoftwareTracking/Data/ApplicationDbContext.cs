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

    public DbSet<Notification> Notifications => Set<Notification>();

    public DbSet<ActionLog> ActionLogs => Set<ActionLog>();

    public DbSet<WorkflowDefinition> WorkflowDefinitions => Set<WorkflowDefinition>();

    public DbSet<WorkflowStep> WorkflowSteps => Set<WorkflowStep>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.UseCollation("Turkish_CI_AS");

        modelBuilder.Entity<Department>(entity =>
        {
            entity.ToTable("Department");
            entity.Property(d => d.Name).IsRequired().HasMaxLength(150);
            entity.HasIndex(d => d.Name).IsUnique();

            entity.HasMany(d => d.Users)
                .WithOne(u => u.Department)
                .HasForeignKey(u => u.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<UserProfile>(entity =>
        {
            entity.Property(u => u.UserName).IsRequired().HasMaxLength(100);
            entity.Property(u => u.FullName).IsRequired().HasMaxLength(150);
            entity.HasIndex(u => u.UserName).IsUnique();

            entity.HasOne(u => u.Department)
                .WithMany(d => d.Users)
                .HasForeignKey(u => u.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);
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
            entity.HasMany(p => p.WorkflowDefinitions)
                .WithOne(w => w.Project)
                .HasForeignKey(w => w.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WorkflowDefinition>(entity =>
        {
            entity.Property(w => w.Title).IsRequired().HasMaxLength(150);
            entity.HasOne(w => w.Project)
                .WithMany(p => p.WorkflowDefinitions)
                .HasForeignKey(w => w.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(w => new { w.ProjectId, w.DisplayOrder });
        });

        modelBuilder.Entity<WorkflowStep>(entity =>
        {
            entity.Property(s => s.SequenceCode).IsRequired().HasMaxLength(20);
            entity.Property(s => s.Description).IsRequired().HasMaxLength(500);
            entity.Property(s => s.StepType).HasConversion<int>();
            entity.Property(s => s.Role).HasMaxLength(150);
            entity.Property(s => s.NextStepCode).HasMaxLength(50);
            entity.Property(s => s.NextStepYesCode).HasMaxLength(50);
            entity.Property(s => s.NextStepNoCode).HasMaxLength(50);
            entity.HasOne(s => s.WorkflowDefinition)
                .WithMany(w => w.Steps)
                .HasForeignKey(s => s.WorkflowDefinitionId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(s => new { s.WorkflowDefinitionId, s.DisplayOrder });
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

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.Property(n => n.Title).IsRequired().HasMaxLength(150);
            entity.Property(n => n.Message).IsRequired().HasMaxLength(500);
            entity.Property(n => n.Link).HasMaxLength(200);
            entity.HasOne(n => n.RecipientUser)
                .WithMany()
                .HasForeignKey(n => n.RecipientUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ActionLog>(entity =>
        {
            entity.Property(l => l.ActionType).IsRequired().HasMaxLength(100);
            entity.Property(l => l.EntityType).IsRequired().HasMaxLength(100);
            entity.Property(l => l.Description).HasMaxLength(500);
            entity.HasOne(l => l.User)
                .WithMany()
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
