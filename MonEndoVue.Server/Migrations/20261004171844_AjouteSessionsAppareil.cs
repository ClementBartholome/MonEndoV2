using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonEndoVue.Server.Migrations
{
    /// <inheritdoc />
    public partial class AjouteSessionsAppareil : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SessionsAppareil",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    JetonHache = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    JetonPrecedentHache = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    RotationLe = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreeLe = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DerniereUtilisationLe = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpireLe = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionsAppareil", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionsAppareil_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SessionsAppareil_JetonHache",
                table: "SessionsAppareil",
                column: "JetonHache",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SessionsAppareil_JetonPrecedentHache",
                table: "SessionsAppareil",
                column: "JetonPrecedentHache");

            migrationBuilder.CreateIndex(
                name: "IX_SessionsAppareil_UserId",
                table: "SessionsAppareil",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SessionsAppareil");
        }
    }
}
