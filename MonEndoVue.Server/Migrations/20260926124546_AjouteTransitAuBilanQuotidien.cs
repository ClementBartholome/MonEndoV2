using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonEndoVue.Server.Migrations
{
    /// <inheritdoc />
    public partial class AjouteTransitAuBilanQuotidien : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Ballonnements",
                table: "BilansQuotidiens",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CrampesEstomac",
                table: "BilansQuotidiens",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IntensiteBallonnements",
                table: "BilansQuotidiens",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IntensiteCrampes",
                table: "BilansQuotidiens",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Selles",
                table: "BilansQuotidiens",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TypeBristol",
                table: "BilansQuotidiens",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Ballonnements",
                table: "BilansQuotidiens");

            migrationBuilder.DropColumn(
                name: "CrampesEstomac",
                table: "BilansQuotidiens");

            migrationBuilder.DropColumn(
                name: "IntensiteBallonnements",
                table: "BilansQuotidiens");

            migrationBuilder.DropColumn(
                name: "IntensiteCrampes",
                table: "BilansQuotidiens");

            migrationBuilder.DropColumn(
                name: "Selles",
                table: "BilansQuotidiens");

            migrationBuilder.DropColumn(
                name: "TypeBristol",
                table: "BilansQuotidiens");
        }
    }
}
