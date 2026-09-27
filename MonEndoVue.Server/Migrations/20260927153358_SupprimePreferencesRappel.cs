using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonEndoVue.Server.Migrations
{
    /// <summary>
    /// Supprime la table PreferencesRappel, hors modèle depuis GeneraliseRappels (réglages recopiés dans Rappels) et
    /// conservée jusqu'ici pour un retour à une image antérieure à la 1.0.0. Migration destructive, écrite à la main :
    /// la table est absente du modèle et du snapshot. Le Down recrée la table vide, avec son schéma d'origine.
    /// </summary>
    public partial class SupprimePreferencesRappel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PreferencesRappel");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
        }
    }
}
