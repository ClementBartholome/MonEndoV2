using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonEndoVue.Server.Migrations
{
    /// <inheritdoc />
    public partial class AjouteLiaisonsAgenda : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LiaisonsAgenda",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CarnetSanteId = table.Column<int>(type: "int", nullable: false),
                    JetonActualisationProtege = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LieeLe = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LiaisonsAgenda", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LiaisonsAgenda_CarnetSantes_CarnetSanteId",
                        column: x => x.CarnetSanteId,
                        principalTable: "CarnetSantes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LiaisonsAgenda_CarnetSanteId",
                table: "LiaisonsAgenda",
                column: "CarnetSanteId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LiaisonsAgenda");
        }
    }
}
