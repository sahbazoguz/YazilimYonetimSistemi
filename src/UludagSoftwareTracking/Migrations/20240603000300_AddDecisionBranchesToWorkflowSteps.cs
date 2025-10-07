using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UludagSoftwareTracking.Migrations
{
    public partial class AddDecisionBranchesToWorkflowSteps : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NextStepNoCode",
                table: "WorkflowSteps",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NextStepYesCode",
                table: "WorkflowSteps",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StepType",
                table: "WorkflowSteps",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NextStepNoCode",
                table: "WorkflowSteps");

            migrationBuilder.DropColumn(
                name: "NextStepYesCode",
                table: "WorkflowSteps");

            migrationBuilder.DropColumn(
                name: "StepType",
                table: "WorkflowSteps");
        }
    }
}
