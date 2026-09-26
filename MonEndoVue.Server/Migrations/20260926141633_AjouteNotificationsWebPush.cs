using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonEndoVue.Server.Migrations
{
    /// <inheritdoc />
    public partial class AjouteNotificationsWebPush : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AbonnementsPush",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CarnetSanteId = table.Column<int>(type: "int", nullable: false),
                    Endpoint = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    P256dh = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Auth = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreeLe = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AbonnementsPush", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AbonnementsPush_CarnetSantes_CarnetSanteId",
                        column: x => x.CarnetSanteId,
                        principalTable: "CarnetSantes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PreferencesRappel",
                columns: table => new
                {
                    CarnetSanteId = table.Column<int>(type: "int", nullable: false),
                    RappelActif = table.Column<bool>(type: "bit", nullable: false),
                    HeureRappel = table.Column<TimeOnly>(type: "time", nullable: false),
                    FuseauHoraire = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    DernierRappelLe = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PreferencesRappel", x => x.CarnetSanteId);
                    table.ForeignKey(
                        name: "FK_PreferencesRappel_CarnetSantes_CarnetSanteId",
                        column: x => x.CarnetSanteId,
                        principalTable: "CarnetSantes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AbonnementsPush_CarnetSanteId",
                table: "AbonnementsPush",
                column: "CarnetSanteId");

            migrationBuilder.CreateIndex(
                name: "IX_AbonnementsPush_Endpoint",
                table: "AbonnementsPush",
                column: "Endpoint",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AbonnementsPush");

            migrationBuilder.DropTable(
                name: "PreferencesRappel");
        }
    }
}
