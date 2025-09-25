using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using UludagSoftwareTracking.Data;

#nullable disable

namespace UludagSoftwareTracking.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    partial class ApplicationDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("Relational:Collation", "Turkish_CI_AS");

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("UludagSoftwareTracking.Models.Entities.ActionLog", b =>
            {
                b.Property<int>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("int");

                SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                b.Property<string>("ActionType")
                    .IsRequired()
                    .HasMaxLength(100)
                    .HasColumnType("nvarchar(100)");

                b.Property<DateTime>("CreatedAt")
                    .HasColumnType("datetime2");

                b.Property<string>("Description")
                    .HasMaxLength(500)
                    .HasColumnType("nvarchar(500)");

                b.Property<int?>("EntityId")
                    .HasColumnType("int");

                b.Property<string>("EntityType")
                    .IsRequired()
                    .HasMaxLength(100)
                    .HasColumnType("nvarchar(100)");

                b.Property<int>("UserId")
                    .HasColumnType("int");

                b.HasKey("Id");

                b.HasIndex("UserId");

                b.ToTable("ActionLogs");
            });

            modelBuilder.Entity("UludagSoftwareTracking.Models.Entities.Department", b =>
            {
                b.Property<int>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("int");

                SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                b.Property<string>("ContactEmail")
                    .HasMaxLength(200)
                    .HasColumnType("nvarchar(200)");

                b.Property<string>("Description")
                    .HasMaxLength(400)
                    .HasColumnType("nvarchar(400)");

                b.Property<string>("Name")
                    .IsRequired()
                    .HasMaxLength(150)
                    .HasColumnType("nvarchar(150)");

                b.Property<string>("PhoneNumber")
                    .HasMaxLength(25)
                    .HasColumnType("nvarchar(25)");

                b.HasKey("Id");

                b.HasIndex("Name")
                    .IsUnique();

                b.ToTable("Departments");
            });

            modelBuilder.Entity("UludagSoftwareTracking.Models.Entities.Notification", b =>
            {
                b.Property<int>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("int");

                SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                b.Property<DateTime>("CreatedAt")
                    .HasColumnType("datetime2");

                b.Property<bool>("IsRead")
                    .HasColumnType("bit");

                b.Property<string>("Link")
                    .HasMaxLength(200)
                    .HasColumnType("nvarchar(200)");

                b.Property<string>("Message")
                    .IsRequired()
                    .HasMaxLength(500)
                    .HasColumnType("nvarchar(500)");

                b.Property<int>("RecipientUserId")
                    .HasColumnType("int");

                b.Property<string>("Title")
                    .IsRequired()
                    .HasMaxLength(150)
                    .HasColumnType("nvarchar(150)");

                b.HasKey("Id");

                b.HasIndex("RecipientUserId");

                b.ToTable("Notifications");
            });

            modelBuilder.Entity("UludagSoftwareTracking.Models.Entities.Project", b =>
            {
                b.Property<int>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("int");

                SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                b.Property<string>("AlgorithmNotes")
                    .HasMaxLength(4000)
                    .HasColumnType("nvarchar(4000)");

                b.Property<DateTime?>("CompletedOn")
                    .HasColumnType("datetime2");

                b.Property<string>("Description")
                    .HasMaxLength(2000)
                    .HasColumnType("nvarchar(2000)");

                b.Property<DateTime?>("EndDate")
                    .HasColumnType("datetime2");

                b.Property<int?>("LeadUserId")
                    .HasColumnType("int");

                b.Property<string>("Name")
                    .IsRequired()
                    .HasMaxLength(200)
                    .HasColumnType("nvarchar(200)");

                b.Property<int>("RequestId")
                    .HasColumnType("int");

                b.Property<DateTime?>("StartDate")
                    .HasColumnType("datetime2");

                b.Property<int>("Status")
                    .HasColumnType("int");

                b.Property<DateTime?>("TestConfirmedOn")
                    .HasColumnType("datetime2");

                b.Property<string>("TechnicalGuidePath")
                    .HasMaxLength(200)
                    .HasColumnType("nvarchar(200)");

                b.Property<string>("UserGuidePath")
                    .HasMaxLength(200)
                    .HasColumnType("nvarchar(200)");

                b.HasKey("Id");

                b.HasIndex("LeadUserId");

                b.HasIndex("RequestId")
                    .IsUnique();

                b.ToTable("Projects");
            });

            modelBuilder.Entity("UludagSoftwareTracking.Models.Entities.ProjectAssignment", b =>
            {
                b.Property<int>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("int");

                SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                b.Property<DateTime>("AssignedOn")
                    .HasColumnType("datetime2");

                b.Property<string>("AssignedRole")
                    .IsRequired()
                    .HasMaxLength(200)
                    .HasColumnType("nvarchar(200)");

                b.Property<decimal>("CompletionPercent")
                    .HasColumnType("decimal(18,4)");

                b.Property<int>("ProjectId")
                    .HasColumnType("int");

                b.Property<int>("UserId")
                    .HasColumnType("int");

                b.HasKey("Id");

                b.HasIndex("ProjectId");

                b.HasIndex("UserId");

                b.ToTable("ProjectAssignments");
            });

            modelBuilder.Entity("UludagSoftwareTracking.Models.Entities.RequestApproval", b =>
            {
                b.Property<int>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("int");

                SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                b.Property<int?>("ApprovedByUserId")
                    .HasColumnType("int");

                b.Property<DateTime?>("DecidedAt")
                    .HasColumnType("datetime2");

                b.Property<string>("Notes")
                    .HasMaxLength(200)
                    .HasColumnType("nvarchar(200)");

                b.Property<int>("RequestId")
                    .HasColumnType("int");

                b.Property<int>("Status")
                    .HasColumnType("int");

                b.HasKey("Id");

                b.HasIndex("ApprovedByUserId");

                b.HasIndex("RequestId");

                b.ToTable("RequestApprovals");
            });

            modelBuilder.Entity("UludagSoftwareTracking.Models.Entities.RequestAssessment", b =>
            {
                b.Property<int>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("int");

                SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                b.Property<int?>("AssessedByUserId")
                    .HasColumnType("int");

                b.Property<DateTime?>("AssessedOn")
                    .HasColumnType("datetime2");

                b.Property<int?>("ExistingSoftwareId")
                    .HasColumnType("int");

                b.Property<string>("Notes")
                    .HasMaxLength(500)
                    .HasColumnType("nvarchar(500)");

                b.Property<int>("RequestId")
                    .HasColumnType("int");

                b.Property<int>("Result")
                    .HasColumnType("int");

                b.Property<int>("Stage")
                    .HasColumnType("int");

                b.HasKey("Id");

                b.HasIndex("AssessedByUserId");

                b.HasIndex("ExistingSoftwareId");

                b.HasIndex("RequestId");

                b.ToTable("RequestAssessments");
            });

            modelBuilder.Entity("UludagSoftwareTracking.Models.Entities.RequestDiscussionMessage", b =>
            {
                b.Property<int>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("int");

                SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                b.Property<string>("Message")
                    .IsRequired()
                    .HasMaxLength(1000)
                    .HasColumnType("nvarchar(1000)");

                b.Property<DateTime>("PostedOn")
                    .HasColumnType("datetime2");

                b.Property<int>("RequestId")
                    .HasColumnType("int");

                b.Property<int>("SenderId")
                    .HasColumnType("int");

                b.HasKey("Id");

                b.HasIndex("RequestId");

                b.HasIndex("SenderId");

                b.ToTable("RequestDiscussionMessages");
            });

            modelBuilder.Entity("UludagSoftwareTracking.Models.Entities.Software", b =>
            {
                b.Property<int>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("int");

                SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                b.Property<string>("Category")
                    .HasMaxLength(150)
                    .HasColumnType("nvarchar(150)");

                b.Property<DateTime>("CreatedDate")
                    .HasColumnType("datetime2");

                b.Property<int?>("DepartmentId")
                    .HasColumnType("int");

                b.Property<string>("Description")
                    .HasMaxLength(2000)
                    .HasColumnType("nvarchar(2000)");

                b.Property<bool>("IsActive")
                    .HasColumnType("bit");

                b.Property<string>("Name")
                    .IsRequired()
                    .HasMaxLength(200)
                    .HasColumnType("nvarchar(200)");

                b.Property<string>("SupportContact")
                    .HasMaxLength(300)
                    .HasColumnType("nvarchar(300)");

                b.Property<string>("TechnologyStack")
                    .HasMaxLength(150)
                    .HasColumnType("nvarchar(150)");

                b.Property<DateTime?>("UpdatedDate")
                    .HasColumnType("datetime2");

                b.Property<string>("WebsiteUrl")
                    .HasMaxLength(300)
                    .HasColumnType("nvarchar(300)");

                b.HasKey("Id");

                b.HasIndex("DepartmentId");

                b.ToTable("Softwares");
            });

            modelBuilder.Entity("UludagSoftwareTracking.Models.Entities.SoftwareManual", b =>
            {
                b.Property<int>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("int");

                SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                b.Property<string>("ChangeLog")
                    .HasColumnType("nvarchar(max)");

                b.Property<int>("ManualType")
                    .HasColumnType("int");

                b.Property<string>("FilePath")
                    .HasMaxLength(400)
                    .HasColumnType("nvarchar(400)");

                b.Property<int>("SoftwareId")
                    .HasColumnType("int");

                b.Property<string>("Title")
                    .IsRequired()
                    .HasMaxLength(200)
                    .HasColumnType("nvarchar(200)");

                b.Property<DateTime>("UploadedOn")
                    .HasColumnType("datetime2");

                b.Property<int?>("UploadedByUserId")
                    .HasColumnType("int");

                b.Property<string>("Version")
                    .HasMaxLength(50)
                    .HasColumnType("nvarchar(50)");

                b.HasKey("Id");

                b.HasIndex("SoftwareId");

                b.HasIndex("UploadedByUserId");

                b.ToTable("SoftwareManuals");
            });

            modelBuilder.Entity("UludagSoftwareTracking.Models.Entities.SoftwareRequest", b =>
            {
                b.Property<int>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("int");

                SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                b.Property<DateTime?>("AssessmentDueDate")
                    .HasColumnType("datetime2");

                b.Property<DateTime>("CreatedAt")
                    .HasColumnType("datetime2");

                b.Property<int>("DepartmentId")
                    .HasColumnType("int");

                b.Property<DateTime?>("DesiredCompletionDate")
                    .HasColumnType("datetime2");

                b.Property<int?>("ExistingSoftwareId")
                    .HasColumnType("int");

                b.Property<int>("Priority")
                    .HasColumnType("int");

                b.Property<string>("RejectionReason")
                    .HasMaxLength(200)
                    .HasColumnType("nvarchar(200)");

                b.Property<int>("RequestedByUserId")
                    .HasColumnType("int");

                b.Property<int>("Status")
                    .HasColumnType("int");

                b.Property<string>("Description")
                    .IsRequired()
                    .HasMaxLength(2000)
                    .HasColumnType("nvarchar(2000)");

                b.Property<string>("Title")
                    .IsRequired()
                    .HasMaxLength(200)
                    .HasColumnType("nvarchar(200)");

                b.Property<DateTime?>("UpdatedAt")
                    .HasColumnType("datetime2");

                b.HasKey("Id");

                b.HasIndex("DepartmentId");

                b.HasIndex("ExistingSoftwareId");

                b.HasIndex("RequestedByUserId");

                b.ToTable("SoftwareRequests");
            });

            modelBuilder.Entity("UludagSoftwareTracking.Models.Entities.UserProfile", b =>
            {
                b.Property<int>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("int");

                SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                b.Property<DateTime>("CreatedAt")
                    .HasColumnType("datetime2");

                b.Property<int?>("DepartmentId")
                    .HasColumnType("int");

                b.Property<string>("Email")
                    .HasMaxLength(200)
                    .HasColumnType("nvarchar(200)");

                b.Property<string>("FullName")
                    .IsRequired()
                    .HasMaxLength(150)
                    .HasColumnType("nvarchar(150)");

                b.Property<bool>("IsActive")
                    .HasColumnType("bit");

                b.Property<string>("PasswordHash")
                    .HasMaxLength(512)
                    .HasColumnType("nvarchar(512)");

                b.Property<string>("PhoneNumber")
                    .HasMaxLength(25)
                    .HasColumnType("nvarchar(25)");

                b.Property<int>("Role")
                    .HasColumnType("int");

                b.Property<string>("UserName")
                    .IsRequired()
                    .HasMaxLength(100)
                    .HasColumnType("nvarchar(100)");

                b.HasKey("Id");

                b.HasIndex("DepartmentId");

                b.HasIndex("UserName")
                    .IsUnique();

                b.ToTable("UserProfiles");
            });

            modelBuilder.Entity("UludagSoftwareTracking.Models.Entities.ActionLog", b =>
            {
                b.HasOne("UludagSoftwareTracking.Models.Entities.UserProfile", "User")
                    .WithMany()
                    .HasForeignKey("UserId")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();

                b.Navigation("User");
            });

            modelBuilder.Entity("UludagSoftwareTracking.Models.Entities.Notification", b =>
            {
                b.HasOne("UludagSoftwareTracking.Models.Entities.UserProfile", "RecipientUser")
                    .WithMany()
                    .HasForeignKey("RecipientUserId")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();

                b.Navigation("RecipientUser");
            });

            modelBuilder.Entity("UludagSoftwareTracking.Models.Entities.Project", b =>
            {
                b.HasOne("UludagSoftwareTracking.Models.Entities.UserProfile", "LeadUser")
                    .WithMany()
                    .HasForeignKey("LeadUserId")
                    .OnDelete(DeleteBehavior.SetNull);

                b.HasOne("UludagSoftwareTracking.Models.Entities.SoftwareRequest", "Request")
                    .WithOne("Project")
                    .HasForeignKey("UludagSoftwareTracking.Models.Entities.Project", "RequestId")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();

                b.Navigation("LeadUser");

                b.Navigation("Request");
            });

            modelBuilder.Entity("UludagSoftwareTracking.Models.Entities.ProjectAssignment", b =>
            {
                b.HasOne("UludagSoftwareTracking.Models.Entities.Project", "Project")
                    .WithMany("Assignments")
                    .HasForeignKey("ProjectId")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();

                b.HasOne("UludagSoftwareTracking.Models.Entities.UserProfile", "User")
                    .WithMany()
                    .HasForeignKey("UserId")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();

                b.Navigation("Project");

                b.Navigation("User");
            });

            modelBuilder.Entity("UludagSoftwareTracking.Models.Entities.RequestApproval", b =>
            {
                b.HasOne("UludagSoftwareTracking.Models.Entities.UserProfile", "ApprovedByUser")
                    .WithMany()
                    .HasForeignKey("ApprovedByUserId")
                    .OnDelete(DeleteBehavior.SetNull);

                b.HasOne("UludagSoftwareTracking.Models.Entities.SoftwareRequest", "Request")
                    .WithMany("Approvals")
                    .HasForeignKey("RequestId")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();

                b.Navigation("ApprovedByUser");

                b.Navigation("Request");
            });

            modelBuilder.Entity("UludagSoftwareTracking.Models.Entities.RequestAssessment", b =>
            {
                b.HasOne("UludagSoftwareTracking.Models.Entities.UserProfile", "AssessedByUser")
                    .WithMany()
                    .HasForeignKey("AssessedByUserId")
                    .OnDelete(DeleteBehavior.SetNull);

                b.HasOne("UludagSoftwareTracking.Models.Entities.Software", "ExistingSoftware")
                    .WithMany()
                    .HasForeignKey("ExistingSoftwareId")
                    .OnDelete(DeleteBehavior.SetNull);

                b.HasOne("UludagSoftwareTracking.Models.Entities.SoftwareRequest", "Request")
                    .WithMany("Assessments")
                    .HasForeignKey("RequestId")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();

                b.Navigation("AssessedByUser");

                b.Navigation("ExistingSoftware");

                b.Navigation("Request");
            });

            modelBuilder.Entity("UludagSoftwareTracking.Models.Entities.RequestDiscussionMessage", b =>
            {
                b.HasOne("UludagSoftwareTracking.Models.Entities.SoftwareRequest", "Request")
                    .WithMany("DiscussionMessages")
                    .HasForeignKey("RequestId")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();

                b.HasOne("UludagSoftwareTracking.Models.Entities.UserProfile", "Sender")
                    .WithMany()
                    .HasForeignKey("SenderId")
                    .OnDelete(DeleteBehavior.Restrict)
                    .IsRequired();

                b.Navigation("Request");

                b.Navigation("Sender");
            });

            modelBuilder.Entity("UludagSoftwareTracking.Models.Entities.Software", b =>
            {
                b.HasOne("UludagSoftwareTracking.Models.Entities.Department", "Department")
                    .WithMany("Softwares")
                    .HasForeignKey("DepartmentId")
                    .OnDelete(DeleteBehavior.Restrict);

                b.Navigation("Department");
            });

            modelBuilder.Entity("UludagSoftwareTracking.Models.Entities.SoftwareManual", b =>
            {
                b.HasOne("UludagSoftwareTracking.Models.Entities.Software", "Software")
                    .WithMany("Manuals")
                    .HasForeignKey("SoftwareId")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();

                b.HasOne("UludagSoftwareTracking.Models.Entities.UserProfile", "UploadedByUser")
                    .WithMany()
                    .HasForeignKey("UploadedByUserId")
                    .OnDelete(DeleteBehavior.SetNull);

                b.Navigation("Software");

                b.Navigation("UploadedByUser");
            });

            modelBuilder.Entity("UludagSoftwareTracking.Models.Entities.SoftwareRequest", b =>
            {
                b.HasOne("UludagSoftwareTracking.Models.Entities.Department", "Department")
                    .WithMany()
                    .HasForeignKey("DepartmentId")
                    .OnDelete(DeleteBehavior.Restrict)
                    .IsRequired();

                b.HasOne("UludagSoftwareTracking.Models.Entities.Software", "ExistingSoftware")
                    .WithMany()
                    .HasForeignKey("ExistingSoftwareId")
                    .OnDelete(DeleteBehavior.SetNull);

                b.HasOne("UludagSoftwareTracking.Models.Entities.UserProfile", "RequestedByUser")
                    .WithMany()
                    .HasForeignKey("RequestedByUserId")
                    .OnDelete(DeleteBehavior.Restrict)
                    .IsRequired();

                b.Navigation("Department");

                b.Navigation("ExistingSoftware");

                b.Navigation("RequestedByUser");
            });

            modelBuilder.Entity("UludagSoftwareTracking.Models.Entities.Department", b =>
            {
                b.Navigation("Softwares");

                b.Navigation("Users");
            });

            modelBuilder.Entity("UludagSoftwareTracking.Models.Entities.Project", b =>
            {
                b.Navigation("Assignments");
            });

            modelBuilder.Entity("UludagSoftwareTracking.Models.Entities.Software", b =>
            {
                b.Navigation("Manuals");
            });

            modelBuilder.Entity("UludagSoftwareTracking.Models.Entities.SoftwareRequest", b =>
            {
                b.Navigation("Approvals");

                b.Navigation("Assessments");

                b.Navigation("DiscussionMessages");

                b.Navigation("Project");
            });

            modelBuilder.Entity("UludagSoftwareTracking.Models.Entities.UserProfile", b =>
            {
                b.Navigation("Department");
            });
#pragma warning restore 612, 618
        }
    }
}
