using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonEndoVue.Server.Migrations
{
    /// <inheritdoc />
    public partial class AjouteFrequenceTraitements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Frequence",
                table: "Medicaments",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "AuBesoin");

            migrationBuilder.AddColumn<int>(
                name: "IntervalleJours",
                table: "Medicaments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "JoursSemaine",
                table: "Medicaments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "HeurePrevue",
                table: "DonneesMedicaments",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Statut",
                table: "DonneesMedicaments",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "Pris");

            migrationBuilder.CreateTable(
                name: "HorairesPrise",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Heure = table.Column<TimeOnly>(type: "time", nullable: false),
                    MedicamentId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HorairesPrise", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HorairesPrise_Medicaments_MedicamentId",
                        column: x => x.MedicamentId,
                        principalTable: "Medicaments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HorairesPrise_MedicamentId_Heure",
                table: "HorairesPrise",
                columns: new[] { "MedicamentId", "Heure" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HorairesPrise");

            migrationBuilder.DropColumn(
                name: "Frequence",
                table: "Medicaments");

            migrationBuilder.DropColumn(
                name: "IntervalleJours",
                table: "Medicaments");

            migrationBuilder.DropColumn(
                name: "JoursSemaine",
                table: "Medicaments");

            migrationBuilder.DropColumn(
                name: "HeurePrevue",
                table: "DonneesMedicaments");

            migrationBuilder.DropColumn(
                name: "Statut",
                table: "DonneesMedicaments");
        }
    }
}
