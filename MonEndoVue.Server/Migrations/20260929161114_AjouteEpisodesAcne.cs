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

            // Reprise des jours d'acné déjà notés : des entrées espacées d'au plus 7 jours (jours consécutifs, ou une photo
            // par semaine) forment un épisode ; au-delà, un nouvel épisode commence. Le dernier épisode reste en cours s'il va
            // jusqu'à hier ou aujourd'hui (fuseau). Les entrées d'origine sont conservées (historique, photos, export).
            migrationBuilder.Sql("""
                WITH Jours AS (
                    SELECT DISTINCT CarnetSanteId, CAST([Date] AS date) AS Jour
                    FROM SymptomesCycles
                    WHERE TypeSymptome = N'Acné'
                ),
                Marques AS (
                    SELECT CarnetSanteId, Jour,
                           CASE WHEN DATEDIFF(day, LAG(Jour) OVER (PARTITION BY CarnetSanteId ORDER BY Jour), Jour) <= 7 THEN 0 ELSE 1 END AS Nouveau
                    FROM Jours
                ),
                Ilots AS (
                    SELECT CarnetSanteId, Jour,
                           SUM(Nouveau) OVER (PARTITION BY CarnetSanteId ORDER BY Jour ROWS UNBOUNDED PRECEDING) AS Groupe
                    FROM Marques
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
