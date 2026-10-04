using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonEndoVue.Server.Migrations
{
    /// <inheritdoc />
    public partial class AjouteCategoriesFacultativesAuBilan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AbondanceSaignementsHorsRegles",
                table: "BilansQuotidiens",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AbsenceTravail",
                table: "BilansQuotidiens",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ActiviteAnnulee",
                table: "BilansQuotidiens",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "DifficulteVider",
                table: "BilansQuotidiens",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DouleurRapport",
                table: "BilansQuotidiens",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "DouleurSelle",
                table: "BilansQuotidiens",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "DouleurUriner",
                table: "BilansQuotidiens",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EnviesUrinaires",
                table: "BilansQuotidiens",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IntensiteDouleurSelle",
                table: "BilansQuotidiens",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IntensiteDouleurUriner",
                table: "BilansQuotidiens",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LimitationJournee",
                table: "BilansQuotidiens",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Nausees",
                table: "BilansQuotidiens",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Nuit",
                table: "BilansQuotidiens",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ReveilsDouleur",
                table: "BilansQuotidiens",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SaignementsHorsRegles",
                table: "BilansQuotidiens",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SangSelles",
                table: "BilansQuotidiens",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SangUrines",
                table: "BilansQuotidiens",
                type: "bit",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AbondanceSaignementsHorsRegles",
                table: "BilansQuotidiens");

            migrationBuilder.DropColumn(
                name: "AbsenceTravail",
                table: "BilansQuotidiens");

            migrationBuilder.DropColumn(
                name: "ActiviteAnnulee",
                table: "BilansQuotidiens");

            migrationBuilder.DropColumn(
                name: "DifficulteVider",
                table: "BilansQuotidiens");

            migrationBuilder.DropColumn(
                name: "DouleurRapport",
                table: "BilansQuotidiens");

            migrationBuilder.DropColumn(
                name: "DouleurSelle",
                table: "BilansQuotidiens");

            migrationBuilder.DropColumn(
                name: "DouleurUriner",
                table: "BilansQuotidiens");

            migrationBuilder.DropColumn(
                name: "EnviesUrinaires",
                table: "BilansQuotidiens");

            migrationBuilder.DropColumn(
                name: "IntensiteDouleurSelle",
                table: "BilansQuotidiens");

            migrationBuilder.DropColumn(
                name: "IntensiteDouleurUriner",
                table: "BilansQuotidiens");

            migrationBuilder.DropColumn(
                name: "LimitationJournee",
                table: "BilansQuotidiens");

            migrationBuilder.DropColumn(
                name: "Nausees",
                table: "BilansQuotidiens");

            migrationBuilder.DropColumn(
                name: "Nuit",
                table: "BilansQuotidiens");

            migrationBuilder.DropColumn(
                name: "ReveilsDouleur",
                table: "BilansQuotidiens");

            migrationBuilder.DropColumn(
                name: "SaignementsHorsRegles",
                table: "BilansQuotidiens");

            migrationBuilder.DropColumn(
                name: "SangSelles",
                table: "BilansQuotidiens");

            migrationBuilder.DropColumn(
                name: "SangUrines",
                table: "BilansQuotidiens");
        }
    }
}
