using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UludagSoftwareTracking.Migrations
{
    public partial class AddCatalogRequestLink : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CatalogRequestId",
                table: "Softwares",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Softwares_CatalogRequestId",
                table: "Softwares",
                column: "CatalogRequestId");

            migrationBuilder.AddForeignKey(
                name: "FK_Softwares_SoftwareRequests_CatalogRequestId",
                table: "Softwares",
                column: "CatalogRequestId",
                principalTable: "SoftwareRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Softwares_SoftwareRequests_CatalogRequestId",
                table: "Softwares");

            migrationBuilder.DropIndex(
                name: "IX_Softwares_CatalogRequestId",
                table: "Softwares");

            migrationBuilder.DropColumn(
                name: "CatalogRequestId",
                table: "Softwares");
        }
    }
}
