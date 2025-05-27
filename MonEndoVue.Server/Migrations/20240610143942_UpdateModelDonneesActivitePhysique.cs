using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonEndoVue.Server.Migrations
{
    /// <inheritdoc />
    public partial class UpdateModelDonneesActivitePhysique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EffetSurDouleur",
                table: "DonneesActivitePhysique");

            migrationBuilder.AddColumn<string>(
                name: "Commentaire",
                table: "DonneesActivitePhysique",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EffetDouleur",
                table: "DonneesActivitePhysique",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Commentaire",
                table: "DonneesActivitePhysique");

            migrationBuilder.DropColumn(
                name: "EffetDouleur",
                table: "DonneesActivitePhysique");

            migrationBuilder.AddColumn<string>(
                name: "EffetSurDouleur",
                table: "DonneesActivitePhysique",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
