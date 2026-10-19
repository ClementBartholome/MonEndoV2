using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonEndoVue.Server.Migrations
{
    /// <summary>
    /// La 1.5.0 exige une adresse confirmée pour se connecter et supprime les comptes non confirmés au bout de 7 jours.
    /// Les comptes créés avant elle n'ont jamais eu de lien de confirmation (EmailConfirmed à 0) : sans cette migration, ils
    /// ne pourraient plus se connecter et seraient supprimés avec leurs données. Tout compte existant au déploiement est donc
    /// marqué confirmé ; les comptes créés ensuite passent par la confirmation.
    /// </summary>
    public partial class ConfirmeLesComptesExistants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE AspNetUsers SET EmailConfirmed = 1 WHERE EmailConfirmed = 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Rien à défaire : avant la 1.5.0, EmailConfirmed n'était pas lu.
        }
    }
}
