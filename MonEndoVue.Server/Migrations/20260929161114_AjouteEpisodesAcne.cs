using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonEndoVue.Server.Migrations
{
    /// <inheritdoc />
    public partial class AjouteEpisodesAcne : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EpisodesAcne",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CarnetSanteId = table.Column<int>(type: "int", nullable: false),
                    Debut = table.Column<DateOnly>(type: "date", nullable: false),
                    Fin = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EpisodesAcne", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EpisodesAcne_CarnetSantes_CarnetSanteId",
                        column: x => x.CarnetSanteId,
                        principalTable: "CarnetSantes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EpisodesAcne_CarnetSanteId_Debut",
                table: "EpisodesAcne",
                columns: new[] { "CarnetSanteId", "Debut" });

            // Reprise des jours d'acné déjà notés (une entrée par jour) : jours consécutifs = un épisode, comme les
            // regroupait l'ancien onglet. Le dernier épisode reste en cours s'il va jusqu'à hier ou aujourd'hui (fuseau).
            // Les entrées d'origine sont conservées (historique, photos, export).
            migrationBuilder.Sql("""
                WITH Jours AS (
                    SELECT DISTINCT CarnetSanteId, CAST([Date] AS date) AS Jour
                    FROM SymptomesCycles
                    WHERE TypeSymptome = N'Acné'
                ),
                Ilots AS (
                    SELECT CarnetSanteId, Jour,
                           DATEADD(day, -ROW_NUMBER() OVER (PARTITION BY CarnetSanteId ORDER BY Jour), Jour) AS Groupe
                    FROM Jours
                )
                INSERT INTO EpisodesAcne (CarnetSanteId, Debut, Fin)
                SELECT CarnetSanteId, MIN(Jour),
                       CASE WHEN MAX(Jour) >= DATEADD(day, -1, CAST(GETDATE() AS date)) THEN NULL ELSE MAX(Jour) END
                FROM Ilots
                GROUP BY CarnetSanteId, Groupe;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EpisodesAcne");
        }
    }
}
