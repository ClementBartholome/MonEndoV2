using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonEndoVue.Server.Migrations
{
    /// <inheritdoc />
    public partial class AjouteCodesInvitation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CodesInvitation",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CodeHache = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Libelle = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UsagesMax = table.Column<int>(type: "int", nullable: false),
                    Usages = table.Column<int>(type: "int", nullable: false),
                    CreeLe = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpireLe = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CodesInvitation", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CodesInvitation_CodeHache",
                table: "CodesInvitation",
                column: "CodeHache",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CodesInvitation");
        }
    }
}
