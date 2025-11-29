using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonEndoVue.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddDonneesTraitementNonMedicamenteux : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DonneesTraitementNonMedicamenteux",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CarnetSanteId = table.Column<int>(type: "int", nullable: false),
                    MedicamentId = table.Column<int>(type: "int", nullable: false),
                    Duree = table.Column<int>(type: "int", nullable: true),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Commentaire = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DonneesTraitementNonMedicamenteux", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DonneesTraitementNonMedicamenteux_CarnetSantes_CarnetSanteId",
                        column: x => x.CarnetSanteId,
                        principalTable: "CarnetSantes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DonneesTraitementNonMedicamenteux_Medicaments_MedicamentId",
                        column: x => x.MedicamentId,
                        principalTable: "Medicaments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DonneesTraitementNonMedicamenteux_CarnetSanteId",
                table: "DonneesTraitementNonMedicamenteux",
                column: "CarnetSanteId");

            migrationBuilder.CreateIndex(
                name: "IX_DonneesTraitementNonMedicamenteux_MedicamentId",
                table: "DonneesTraitementNonMedicamenteux",
                column: "MedicamentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DonneesTraitementNonMedicamenteux");
        }
    }
}
