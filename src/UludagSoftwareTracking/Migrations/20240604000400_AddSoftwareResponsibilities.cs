using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UludagSoftwareTracking.Migrations;

public partial class AddSoftwareResponsibilities : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "ReleasedSoftwareId",
            table: "Projects",
            type: "int",
            nullable: true);

        migrationBuilder.CreateTable(
            name: "SoftwareResponsibilities",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                SoftwareId = table.Column<int>(type: "int", nullable: false),
                UserId = table.Column<int>(type: "int", nullable: false),
                ResponsibilityType = table.Column<int>(type: "int", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SoftwareResponsibilities", x => x.Id);
                table.ForeignKey(
                    name: "FK_SoftwareResponsibilities_Softwares_SoftwareId",
                    column: x => x.SoftwareId,
                    principalTable: "Softwares",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_SoftwareResponsibilities_UserProfiles_UserId",
                    column: x => x.UserId,
                    principalTable: "UserProfiles",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Projects_ReleasedSoftwareId",
            table: "Projects",
            column: "ReleasedSoftwareId");

        migrationBuilder.CreateIndex(
            name: "IX_SoftwareResponsibilities_SoftwareId_UserId_ResponsibilityType",
            table: "SoftwareResponsibilities",
            columns: new[] { "SoftwareId", "UserId", "ResponsibilityType" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_SoftwareResponsibilities_UserId",
            table: "SoftwareResponsibilities",
            column: "UserId");

        migrationBuilder.AddForeignKey(
            name: "FK_Projects_Softwares_ReleasedSoftwareId",
            table: "Projects",
            column: "ReleasedSoftwareId",
            principalTable: "Softwares",
            principalColumn: "Id",
            onDelete: ReferentialAction.SetNull);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_Projects_Softwares_ReleasedSoftwareId",
            table: "Projects");

        migrationBuilder.DropTable(
            name: "SoftwareResponsibilities");

        migrationBuilder.DropIndex(
            name: "IX_Projects_ReleasedSoftwareId",
            table: "Projects");

        migrationBuilder.DropColumn(
            name: "ReleasedSoftwareId",
            table: "Projects");
    }
}
