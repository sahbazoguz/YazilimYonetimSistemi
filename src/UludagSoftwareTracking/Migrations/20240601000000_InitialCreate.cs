using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UludagSoftwareTracking.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Relational:Collation", "Turkish_CI_AS");

            migrationBuilder.CreateTable(
                name: "Departments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    ContactEmail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PasswordHash = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    Role = table.Column<int>(type: "int", nullable: false),
                    DepartmentId = table.Column<int>(type: "int", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserProfiles_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Softwares",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Category = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    TechnologyStack = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    DepartmentId = table.Column<int>(type: "int", nullable: true),
                    SupportContact = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    WebsiteUrl = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Softwares", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Softwares_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ActionLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ActionType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<int>(type: "int", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActionLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActionLogs_UserProfiles_UserId",
                        column: x => x.UserId,
                        principalTable: "UserProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Link = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    RecipientUserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_UserProfiles_RecipientUserId",
                        column: x => x.RecipientUserId,
                        principalTable: "UserProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SoftwareRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DesiredCompletionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AssessmentDueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DepartmentId = table.Column<int>(type: "int", nullable: false),
                    RequestedByUserId = table.Column<int>(type: "int", nullable: false),
                    ExistingSoftwareId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SoftwareRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SoftwareRequests_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SoftwareRequests_Softwares_ExistingSoftwareId",
                        column: x => x.ExistingSoftwareId,
                        principalTable: "Softwares",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SoftwareRequests_UserProfiles_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalTable: "UserProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SoftwareManuals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ManualType = table.Column<int>(type: "int", nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    Version = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ChangeLog = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UploadedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SoftwareId = table.Column<int>(type: "int", nullable: false),
                    UploadedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SoftwareManuals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SoftwareManuals_Softwares_SoftwareId",
                        column: x => x.SoftwareId,
                        principalTable: "Softwares",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SoftwareManuals_UserProfiles_UploadedByUserId",
                        column: x => x.UploadedByUserId,
                        principalTable: "UserProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RequestId = table.Column<int>(type: "int", nullable: false),
                    LeadUserId = table.Column<int>(type: "int", nullable: true),
                    AlgorithmNotes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    TechnicalGuidePath = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    UserGuidePath = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CompletedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TestConfirmedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Projects_SoftwareRequests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "SoftwareRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Projects_UserProfiles_LeadUserId",
                        column: x => x.LeadUserId,
                        principalTable: "UserProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "RequestApprovals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Status = table.Column<int>(type: "int", nullable: false),
                    DecidedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RequestId = table.Column<int>(type: "int", nullable: false),
                    ApprovedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestApprovals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RequestApprovals_SoftwareRequests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "SoftwareRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RequestApprovals_UserProfiles_ApprovedByUserId",
                        column: x => x.ApprovedByUserId,
                        principalTable: "UserProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "RequestAssessments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Stage = table.Column<int>(type: "int", nullable: false),
                    Result = table.Column<int>(type: "int", nullable: false),
                    AssessedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RequestId = table.Column<int>(type: "int", nullable: false),
                    AssessedByUserId = table.Column<int>(type: "int", nullable: true),
                    ExistingSoftwareId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestAssessments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RequestAssessments_Softwares_ExistingSoftwareId",
                        column: x => x.ExistingSoftwareId,
                        principalTable: "Softwares",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_RequestAssessments_SoftwareRequests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "SoftwareRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RequestAssessments_UserProfiles_AssessedByUserId",
                        column: x => x.AssessedByUserId,
                        principalTable: "UserProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "RequestDiscussionMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestId = table.Column<int>(type: "int", nullable: false),
                    SenderId = table.Column<int>(type: "int", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    PostedOn = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestDiscussionMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RequestDiscussionMessages_SoftwareRequests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "SoftwareRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RequestDiscussionMessages_UserProfiles_SenderId",
                        column: x => x.SenderId,
                        principalTable: "UserProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProjectAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    AssignedRole = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AssignedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletionPercent = table.Column<decimal>(type: "decimal(18,4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectAssignments_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectAssignments_UserProfiles_UserId",
                        column: x => x.UserId,
                        principalTable: "UserProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActionLogs_UserId",
                table: "ActionLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_Name",
                table: "Departments",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_RecipientUserId",
                table: "Notifications",
                column: "RecipientUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectAssignments_ProjectId",
                table: "ProjectAssignments",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectAssignments_UserId",
                table: "ProjectAssignments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_LeadUserId",
                table: "Projects",
                column: "LeadUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_RequestId",
                table: "Projects",
                column: "RequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RequestApprovals_ApprovedByUserId",
                table: "RequestApprovals",
                column: "ApprovedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestApprovals_RequestId",
                table: "RequestApprovals",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestAssessments_AssessedByUserId",
                table: "RequestAssessments",
                column: "AssessedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestAssessments_ExistingSoftwareId",
                table: "RequestAssessments",
                column: "ExistingSoftwareId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestAssessments_RequestId",
                table: "RequestAssessments",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestDiscussionMessages_RequestId",
                table: "RequestDiscussionMessages",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestDiscussionMessages_SenderId",
                table: "RequestDiscussionMessages",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_SoftwareManuals_SoftwareId",
                table: "SoftwareManuals",
                column: "SoftwareId");

            migrationBuilder.CreateIndex(
                name: "IX_SoftwareManuals_UploadedByUserId",
                table: "SoftwareManuals",
                column: "UploadedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SoftwareRequests_DepartmentId",
                table: "SoftwareRequests",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_SoftwareRequests_ExistingSoftwareId",
                table: "SoftwareRequests",
                column: "ExistingSoftwareId");

            migrationBuilder.CreateIndex(
                name: "IX_SoftwareRequests_RequestedByUserId",
                table: "SoftwareRequests",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Softwares_DepartmentId",
                table: "Softwares",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_DepartmentId",
                table: "UserProfiles",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_UserName",
                table: "UserProfiles",
                column: "UserName",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActionLogs");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "ProjectAssignments");

            migrationBuilder.DropTable(
                name: "RequestApprovals");

            migrationBuilder.DropTable(
                name: "RequestAssessments");

            migrationBuilder.DropTable(
                name: "RequestDiscussionMessages");

            migrationBuilder.DropTable(
                name: "SoftwareManuals");

            migrationBuilder.DropTable(
                name: "Projects");

            migrationBuilder.DropTable(
                name: "SoftwareRequests");

            migrationBuilder.DropTable(
                name: "Softwares");

            migrationBuilder.DropTable(
                name: "UserProfiles");

            migrationBuilder.DropTable(
                name: "Departments");
        }
    }
}
