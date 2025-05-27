using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonEndoVue.Server.Migrations
{
    /// <inheritdoc />
    public partial class CreateTableDonneesTransit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DonneesTransit",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CarnetSanteId = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TypeEvenement = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Intensite = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Saignement = table.Column<bool>(type: "bit", nullable: false),
                    Douleur = table.Column<bool>(type: "bit", nullable: false),
                    Commentaires = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DonneesTransit", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DonneesTransit_CarnetSantes_CarnetSanteId",
                        column: x => x.CarnetSanteId,
                        principalTable: "CarnetSantes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DonneesTransit_CarnetSanteId",
                table: "DonneesTransit",
                column: "CarnetSanteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DonneesTransit");
        }
    }
}
