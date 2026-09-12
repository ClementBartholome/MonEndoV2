using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonEndoVue.Server.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDonneesTransitPourSuiviBilanQuotidien : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TypeEvenement",
                table: "DonneesTransit");

            migrationBuilder.DropColumn(
                name: "Intensite",
                table: "DonneesTransit");

            migrationBuilder.DropColumn(
                name: "Douleur",
                table: "DonneesTransit");

            migrationBuilder.AddColumn<bool>(
                name: "Selles",
                table: "DonneesTransit",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "TypeBristol",
                table: "DonneesTransit",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CrampesEstomac",
                table: "DonneesTransit",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "IntensiteCrampes",
                table: "DonneesTransit",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Ballonnements",
                table: "DonneesTransit",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "IntensiteBallonnements",
                table: "DonneesTransit",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Selles",
                table: "DonneesTransit");

            migrationBuilder.DropColumn(
                name: "TypeBristol",
                table: "DonneesTransit");

            migrationBuilder.DropColumn(
                name: "CrampesEstomac",
                table: "DonneesTransit");

            migrationBuilder.DropColumn(
                name: "IntensiteCrampes",
                table: "DonneesTransit");

            migrationBuilder.DropColumn(
                name: "Ballonnements",
                table: "DonneesTransit");

            migrationBuilder.DropColumn(
                name: "IntensiteBallonnements",
                table: "DonneesTransit");

            migrationBuilder.AddColumn<string>(
                name: "TypeEvenement",
                table: "DonneesTransit",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Intensite",
                table: "DonneesTransit",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "Douleur",
                table: "DonneesTransit",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
