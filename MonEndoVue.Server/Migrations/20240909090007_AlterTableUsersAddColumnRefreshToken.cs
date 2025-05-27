using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonEndoVue.Server.Migrations
{
    /// <inheritdoc />
    public partial class AlterTableUsersAddColumnRefreshToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RefreshToken",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "RefreshTokenExpiryTime",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_JourRegles_CarnetSanteId",
                table: "JourRegles",
                column: "CarnetSanteId");

            migrationBuilder.AddForeignKey(
                name: "FK_JourRegles_CarnetSantes_CarnetSanteId",
                table: "JourRegles",
                column: "CarnetSanteId",
                principalTable: "CarnetSantes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_JourRegles_CarnetSantes_CarnetSanteId",
                table: "JourRegles");

            migrationBuilder.DropIndex(
                name: "IX_JourRegles_CarnetSanteId",
                table: "JourRegles");

            migrationBuilder.DropColumn(
                name: "RefreshToken",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "RefreshTokenExpiryTime",
                table: "AspNetUsers");
        }
    }
}
