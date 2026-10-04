using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonEndoVue.Server.Migrations
{
    /// <inheritdoc />
    public partial class AjouteFluxEtCaillotsAuxJoursDeRegles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Caillots",
                table: "JourRegles",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Flux",
                table: "JourRegles",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Caillots",
                table: "JourRegles");

            migrationBuilder.DropColumn(
                name: "Flux",
                table: "JourRegles");
        }
    }
}
