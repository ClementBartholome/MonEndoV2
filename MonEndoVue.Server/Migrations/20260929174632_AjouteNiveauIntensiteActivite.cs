using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonEndoVue.Server.Migrations
{
    /// <inheritdoc />
    public partial class AjouteNiveauIntensiteActivite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "NiveauIntensite",
                table: "DonneesActivitePhysique",
                type: "int",
                nullable: true);

            // Reprise de l'ancienne échelle de 1 à 10 (décision du 2026-09-28) : 1-3 douce, 4-7 modérée, 8-10 soutenue.
            // L'ancienne colonne est conservée telle quelle.
            migrationBuilder.Sql("""
                UPDATE DonneesActivitePhysique
                SET NiveauIntensite = CASE WHEN Intensite <= 3 THEN 1 WHEN Intensite <= 7 THEN 2 ELSE 3 END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NiveauIntensite",
                table: "DonneesActivitePhysique");
        }
    }
}
