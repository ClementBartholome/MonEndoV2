using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using MonEndoVue.Server.Data;
using MonEndoVue.Server.Services.Photos;

namespace MonEndoVue.Server.Services.Export;

/// <summary>
/// Export de toutes les données de l'utilisatrice connectée (droits d'accès et de portabilité, RGPD art. 15 et 20) :
/// une archive ZIP avec <c>donnees.json</c> (lisible, sans identifiant technique d'un autre compte), les photos de suivi
/// et un <c>LISEZMOI.txt</c>. Le carnet est déduit de la session : aucun identifiant n'est accepté en entrée.
/// </summary>
public class ExportDonneesService(
    AppDbContext context,
    IStockagePhotos stockagePhotos,
    TimeProvider horloge,
    ILogger<ExportDonneesService> logger)
{
    public const string TypeContenu = "application/zip";

    private static readonly JsonSerializerOptions OptionsJson = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
        // Accents lisibles dans le fichier (é plutôt que é) : il est destiné à être ouvert par l'utilisatrice.
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>
    /// Archive prête à être envoyée (fichier temporaire supprimé à la fermeture du flux), et son nom de fichier.
    /// </summary>
    public async Task<ResultatOperation<(Stream Archive, string NomFichier)>> PreparerAsync(string userId, CancellationToken ct)
    {
        var utilisatrice = await context.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new { u.Email, u.ConsentementDonneesSanteLe, u.VersionPolitiqueAcceptee })
            .SingleOrDefaultAsync(ct);
        var carnetId = await context.CarnetSantes.Where(c => c.UserId == userId).Select(c => (int?)c.Id).SingleOrDefaultAsync(ct);
        if (utilisatrice == null || carnetId == null)
        {
            return ResultatOperation<(Stream, string)>.Echec(StatutOperation.Introuvable);
        }

        var maintenant = horloge.GetUtcNow();
        // Nom aléatoire créé exclusivement (CreateNew) : pas de fichier temporaire prévisible ni réutilisé (Sonar S5445).
        var fichier = new FileStream(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()), FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None,
            4096, FileOptions.DeleteOnClose | FileOptions.Asynchronous);
        try
        {
            var nombrePhotos = 0;
            using (var archive = new ZipArchive(fichier, ZipArchiveMode.Create, leaveOpen: true))
            {
                var photos = await EcrirePhotosAsync(archive, carnetId.Value, ct);
                nombrePhotos = photos.Count;
                var donnees = await LireDonneesAsync(carnetId.Value, photos, ct);
                var export = new
                {
                    exporteLe = maintenant.UtcDateTime,
                    compte = new
                    {
                        email = utilisatrice.Email,
                        consentementDonneesSanteLe = utilisatrice.ConsentementDonneesSanteLe,
                        versionPolitiqueAcceptee = utilisatrice.VersionPolitiqueAcceptee,
                    },
                    donnees,
                };
                await EcrireTexteAsync(archive, "donnees.json", JsonSerializer.Serialize(export, OptionsJson), ct);
                await EcrireTexteAsync(archive, "LISEZMOI.txt", LisezMoi(maintenant, photos.Count), ct);
            }

            fichier.Position = 0;
            logger.LogInformation("Export des données du carnet {CarnetSanteId} ({NombrePhotos} photos)", carnetId, nombrePhotos);
            return ResultatOperation<(Stream, string)>.Succes((fichier, $"monendo-mes-donnees-{maintenant:yyyy-MM-dd}.zip"));
        }
        catch
        {
            await fichier.DisposeAsync();
            throw;
        }
    }

    /// <summary>Copie les photos de suivi dans l'archive ; renvoie, par symptôme, le chemin de sa photo dans l'archive.</summary>
    private async Task<Dictionary<int, string>> EcrirePhotosAsync(ZipArchive archive, int carnetId, CancellationToken ct)
    {
        var avecPhoto = await context.SymptomesCycles.AsNoTracking()
            .Where(s => s.CarnetSanteId == carnetId && s.PhotoUrl != null && s.PhotoUrl != "")
            .Select(s => new { s.Id, s.Date, s.PhotoUrl })
            .ToListAsync(ct);

        var chemins = new Dictionary<int, string>();
        foreach (var symptome in avecPhoto)
        {
            await using var contenu = await stockagePhotos.OuvrirAsync(symptome.PhotoUrl!, ct);
            if (contenu == null) continue;

            var chemin = $"photos/{symptome.Date:yyyy-MM-dd}-{symptome.Id}{Extension(symptome.PhotoUrl!)}";
            await using (var entree = archive.CreateEntry(chemin, CompressionLevel.NoCompression).Open())
            {
                await contenu.CopyToAsync(entree, ct);
            }
            chemins[symptome.Id] = chemin;
        }

        return chemins;
    }

    private async Task<object> LireDonneesAsync(int carnetId, Dictionary<int, string> photos, CancellationToken ct)
    {
        var traitements = await context.Medicaments.AsNoTracking().Where(m => m.CarnetSanteId == carnetId)
            .OrderBy(m => m.DateDebutTraitement).ToListAsync(ct);
        var nomTraitement = traitements.ToDictionary(m => m.Id, m => m.Nom);
        var symptomes = await context.SymptomesCycles.AsNoTracking().Where(s => s.CarnetSanteId == carnetId)
            .OrderBy(s => s.Date).ToListAsync(ct);

        return new
        {
            douleurs = await context.DonneesDouleurs.AsNoTracking().Where(d => d.CarnetSanteId == carnetId)
                .OrderBy(d => d.Date)
                .Select(d => new { d.Date, type = d.TypeDouleur, d.Intensite, d.Commentaire })
                .ToListAsync(ct),
            joursDeRegles = await context.JourRegles.AsNoTracking().Where(j => j.CarnetSanteId == carnetId)
                .OrderBy(j => j.Date).Select(j => j.Date).ToListAsync(ct),
            symptomesDuCycle = symptomes.Select(s => new
            {
                s.Date,
                type = s.TypeSymptome,
                s.Intensite,
                s.Commentaire,
                photo = photos.GetValueOrDefault(s.Id),
            }),
            traitements = traitements.Select(m => new
            {
                m.Nom,
                m.Type,
                m.Posologie,
                enCours = m.TraitementEnCours,
                debut = m.DateDebutTraitement,
                fin = m.DateFinTraitement,
            }),
            prisesDeTraitement = (await context.DonneesMedicaments.AsNoTracking().Where(p => p.CarnetSanteId == carnetId)
                    .OrderBy(p => p.Date).ToListAsync(ct))
                .Select(p => new { p.Date, traitement = nomTraitement.GetValueOrDefault(p.MedicamentId), p.NombreComprimes, p.Commentaire }),
            seancesDeTraitement = (await context.DonneesTraitementNonMedicamenteux.AsNoTracking()
                    .Where(t => t.CarnetSanteId == carnetId).OrderBy(t => t.Date).ToListAsync(ct))
                .Select(t => new { t.Date, traitement = nomTraitement.GetValueOrDefault(t.MedicamentId), dureeMinutes = t.Duree, t.Commentaire }),
            activitePhysique = await context.DonneesActivitePhysique.AsNoTracking().Where(a => a.CarnetSanteId == carnetId)
                .OrderBy(a => a.Date)
                .Select(a => new { a.Date, type = a.TypeActivite, dureeMinutes = a.Duree, a.Intensite, a.EffetDouleur, a.Commentaire })
                .ToListAsync(ct),
            transit = await context.DonneesTransit.AsNoTracking().Where(t => t.CarnetSanteId == carnetId)
                .OrderBy(t => t.Date)
                .Select(t => new { t.Date, type = t.TypeEvenement, t.Intensite, t.Saignement, t.Douleur, commentaire = t.Commentaires })
                .ToListAsync(ct),
            bilansQuotidiens = (await context.BilansQuotidiens.AsNoTracking().Where(b => b.CarnetSanteId == carnetId)
                    .OrderBy(b => b.Date).ToListAsync(ct))
                .Select(b => new
                {
                    b.Date,
                    emotions = b.Emotions.Select(e => e.Emotion),
                    humeur = b.Mood,
                    b.DouleurMoyenne,
                    b.StressPro,
                    b.StressPerso,
                    b.Fatigue,
                    b.Pas,
                    hydratationLitres = b.Hydratation,
                    b.Gluten,
                    b.Lactose,
                    b.Grignotage,
                    b.Selles,
                    b.TypeBristol,
                    b.CrampesEstomac,
                    b.IntensiteCrampes,
                    b.Ballonnements,
                    b.IntensiteBallonnements,
                    notes = b.Commentaire,
                }),
            rappels = await context.Rappels.AsNoTracking().Where(r => r.CarnetSanteId == carnetId)
                .Select(r => new { r.Type, r.Actif, r.Heure, r.JourSemaine, r.FuseauHoraire })
                .ToListAsync(ct),
            appareilsAbonnesAuxRappels = await context.AbonnementsPush.AsNoTracking().Where(a => a.CarnetSanteId == carnetId)
                .Select(a => new { abonneLe = a.CreeLe, serviceDeNotification = a.Endpoint })
                .ToListAsync(ct),
        };
    }

    private static async Task EcrireTexteAsync(ZipArchive archive, string nom, string contenu, CancellationToken ct)
    {
        await using var flux = archive.CreateEntry(nom, CompressionLevel.Optimal).Open();
        await flux.WriteAsync(Encoding.UTF8.GetBytes(contenu), ct);
    }

    private static string Extension(string url)
    {
        var chemin = Uri.TryCreate(url, UriKind.Absolute, out var uri) ? uri.AbsolutePath : url;
        var extension = Path.GetExtension(chemin).ToLowerInvariant();
        return string.IsNullOrEmpty(extension) ? ".jpg" : extension;
    }

    private static string LisezMoi(DateTimeOffset maintenant, int nombrePhotos) => $"""
        MonEndo — export de toutes tes données
        Exporté le {maintenant:dd/MM/yyyy} à {maintenant:HH:mm} (UTC).

        - donnees.json : ton compte (adresse e-mail, date de ton accord) et tout ce que tu as noté dans MonEndo,
          classé par rubrique et par date. Ce format peut être ouvert avec un éditeur de texte ou importé dans un
          autre service.
        - photos/ : tes photos de suivi ({nombrePhotos}), nommées par date ; la rubrique « symptomesDuCycle » de
          donnees.json indique à quel symptôme chacune est rattachée.

        Tes repères personnels et tes préférences d'affichage restent sur ton appareil : ils ne font pas partie de cet export.
        """;
}
