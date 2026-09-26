using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonEndoVue.Server.Migrations
{
    /// <inheritdoc />
    public partial class GeneraliseRappels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // PreferencesRappel est conservée (non mappée) pour permettre un retour à l'image précédente ;
            // suppression prévue dans une migration ultérieure (voir docs/modernization-plan.md).

            migrationBuilder.CreateTable(
                name: "Rappels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CarnetSanteId = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Actif = table.Column<bool>(type: "bit", nullable: false),
                    Heure = table.Column<TimeOnly>(type: "time", nullable: false),
                    JourSemaine = table.Column<int>(type: "int", nullable: true),
                    FuseauHoraire = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    DernierEnvoiLe = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rappels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Rappels_CarnetSantes_CarnetSanteId",
                        column: x => x.CarnetSanteId,
                        principalTable: "CarnetSantes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Rappels_CarnetSanteId_Type",
                table: "Rappels",
                columns: new[] { "CarnetSanteId", "Type" },
                unique: true);

            // Reprise du réglage existant du rappel du bilan quotidien.
            migrationBuilder.Sql(@"
                INSERT INTO Rappels (CarnetSanteId, Type, Actif, Heure, JourSemaine, FuseauHoraire, DernierEnvoiLe)
                SELECT CarnetSanteId, 'BilanQuotidien', RappelActif, HeureRappel, NULL, FuseauHoraire, DernierRappelLe
                FROM PreferencesRappel");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Rappels");
        }
    }
}
