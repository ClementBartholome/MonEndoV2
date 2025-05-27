using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonEndoVue.Server.Migrations
{
    /// <inheritdoc />
    public partial class AlterColumnFinTraitementMedicamentNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "DateFinTraitement",
                table: "Medicaments",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.CreateIndex(
                name: "IX_Medicaments_CarnetSanteId",
                table: "Medicaments",
                column: "CarnetSanteId");

            migrationBuilder.CreateIndex(
                name: "IX_DonneesMedicaments_CarnetSanteId",
                table: "DonneesMedicaments",
                column: "CarnetSanteId");

            migrationBuilder.AddForeignKey(
                name: "FK_DonneesMedicaments_CarnetSantes_CarnetSanteId",
                table: "DonneesMedicaments",
                column: "CarnetSanteId",
                principalTable: "CarnetSantes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Medicaments_CarnetSantes_CarnetSanteId",
                table: "Medicaments",
                column: "CarnetSanteId",
                principalTable: "CarnetSantes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DonneesMedicaments_CarnetSantes_CarnetSanteId",
                table: "DonneesMedicaments");

            migrationBuilder.DropForeignKey(
                name: "FK_Medicaments_CarnetSantes_CarnetSanteId",
                table: "Medicaments");

            migrationBuilder.DropIndex(
                name: "IX_Medicaments_CarnetSanteId",
                table: "Medicaments");

            migrationBuilder.DropIndex(
                name: "IX_DonneesMedicaments_CarnetSanteId",
                table: "DonneesMedicaments");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DateFinTraitement",
                table: "Medicaments",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);
        }
    }
}
