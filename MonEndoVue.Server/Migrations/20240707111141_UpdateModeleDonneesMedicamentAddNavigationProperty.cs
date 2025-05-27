using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonEndoVue.Server.Migrations
{
    /// <inheritdoc />
    public partial class UpdateModeleDonneesMedicamentAddNavigationProperty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_DonneesMedicaments_MedicamentId",
                table: "DonneesMedicaments",
                column: "MedicamentId");

            migrationBuilder.AddForeignKey(
                name: "FK_DonneesMedicaments_Medicaments_MedicamentId",
                table: "DonneesMedicaments",
                column: "MedicamentId",
                principalTable: "Medicaments",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DonneesMedicaments_Medicaments_MedicamentId",
                table: "DonneesMedicaments");

            migrationBuilder.DropIndex(
                name: "IX_DonneesMedicaments_MedicamentId",
                table: "DonneesMedicaments");
        }
    }
}
