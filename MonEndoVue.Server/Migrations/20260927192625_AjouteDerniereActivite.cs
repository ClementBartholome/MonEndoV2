using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonEndoVue.Server.Migrations
{
    /// <inheritdoc />
    public partial class AjouteDerniereActivite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DerniereActiviteLe",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);

            // Comptes existants : l'inactivité se compte à partir du déploiement (aucun n'est supprimé avant 2 ans).
            migrationBuilder.Sql("UPDATE AspNetUsers SET DerniereActiviteLe = SYSUTCDATETIME() WHERE DerniereActiviteLe IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DerniereActiviteLe",
                table: "AspNetUsers");
        }
    }
}
