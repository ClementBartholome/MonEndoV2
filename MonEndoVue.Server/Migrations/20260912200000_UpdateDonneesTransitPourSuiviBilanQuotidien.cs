using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonEndoVue.Server.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDonneesTransitPourSuiviBilanQuotidien : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Ajouter les nouvelles colonnes AVANT de dropper les anciennes,
            // pour pouvoir migrer les donnees existantes sans perte.
            migrationBuilder.AddColumn<bool>(
                name: "Selles",
                table: "DonneesTransit",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "TypeBristol",
                table: "DonneesTransit",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CrampesEstomac",
                table: "DonneesTransit",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "IntensiteCrampes",
                table: "DonneesTransit",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Ballonnements",
                table: "DonneesTransit",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "IntensiteBallonnements",
                table: "DonneesTransit",
                type: "nvarchar(max)",
                nullable: true);

            // 2. Migrer les donnees existantes vers le nouveau schema.
            // Ancien modele: TypeEvenement (ex: "Constipation"/"Diarrhee"), Intensite
            // ("Legere"/"Moderee"/"Severe"), Douleur (bool).
            // - Toute ligne avec un TypeEvenement renseigne (Constipation/Diarrhee/autre evenement de transit)
            //   => Selles = true (un episode de transit a bien ete note ce jour-la).
            // - Si Douleur = true => CrampesEstomac = true, et on reprend l'ancienne Intensite
            //   comme IntensiteCrampes (best-effort, l'ancien systeme ne distinguait pas
            //   crampes/ballonnements).
            // - TypeBristol et Ballonnements/IntensiteBallonnements n'existaient pas avant :
            //   laisses a NULL/false, a completer par l'utilisateur retroactivement si besoin.
            migrationBuilder.Sql(@"
                UPDATE DonneesTransit
                SET Selles = CASE WHEN TypeEvenement IS NOT NULL AND TypeEvenement <> '' THEN 1 ELSE 0 END,
                    CrampesEstomac = Douleur,
                    IntensiteCrampes = CASE WHEN Douleur = 1 THEN Intensite ELSE NULL END
            ");

            // 3. Ne dropper les anciennes colonnes qu'une fois les donnees migrees.
            migrationBuilder.DropColumn(
                name: "TypeEvenement",
                table: "DonneesTransit");

            migrationBuilder.DropColumn(
                name: "Intensite",
                table: "DonneesTransit");

            migrationBuilder.DropColumn(
                name: "Douleur",
                table: "DonneesTransit");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TypeEvenement",
                table: "DonneesTransit",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Intensite",
                table: "DonneesTransit",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "Douleur",
                table: "DonneesTransit",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // Best-effort reverse mapping (perte d'information possible : Bristol,
            // ballonnements et distinction crampes/ballonnements ne peuvent pas etre
            // reconstruits fidelement).
            migrationBuilder.Sql(@"
                UPDATE DonneesTransit
                SET TypeEvenement = CASE WHEN Selles = 1 THEN 'Transit' ELSE '' END,
                    Douleur = CrampesEstomac,
                    Intensite = CASE WHEN IntensiteCrampes IS NOT NULL THEN IntensiteCrampes ELSE '' END
            ");

            migrationBuilder.DropColumn(
                name: "Selles",
                table: "DonneesTransit");

            migrationBuilder.DropColumn(
                name: "TypeBristol",
                table: "DonneesTransit");

            migrationBuilder.DropColumn(
                name: "CrampesEstomac",
                table: "DonneesTransit");

            migrationBuilder.DropColumn(
                name: "IntensiteCrampes",
                table: "DonneesTransit");

            migrationBuilder.DropColumn(
                name: "Ballonnements",
                table: "DonneesTransit");

            migrationBuilder.DropColumn(
                name: "IntensiteBallonnements",
                table: "DonneesTransit");
        }
    }
}
