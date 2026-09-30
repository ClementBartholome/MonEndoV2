using System.Globalization;
using Microsoft.EntityFrameworkCore;
using MonEndoVue.Server.Data;
using MonEndoVue.Server.Dto;
using MonEndoVue.Server.Models;
using MonEndoVue.Server.Services.Photos;
using MonEndoVue.Server.ViewModels;

namespace MonEndoVue.Server.Services.Cycle;

/// <summary>
/// Suivi de l'acné de l'utilisatrice connectée (carnet déduit de la session) : épisodes (début, fin, en cours) et points
/// de suivi avec photo. Les épisodes d'un carnet ne se chevauchent pas et un seul peut être en cours.
/// </summary>
/// <summary>Flux d'une photo de suivi et son type.</summary>
public sealed record PhotoDeSuivi(Stream Contenu, string TypeContenu);

public class AcneService(AppDbContext context, CarnetSanteService carnetSanteService, TimeProvider horloge, IStockagePhotos stockagePhotos)
{
    /// <summary>Type des symptômes de suivi (photo, intensité), partagé avec l'onglet et le rappel hebdomadaire.</summary>
    public const string TypeAcne = "Acné";

    /// <summary>Écart toléré entre le jour local envoyé par le client et la date du serveur (fuseaux horaires).</summary>
    private const int EcartMaximalJours = 2;

    /// <summary>Photos renvoyées par défaut : de quoi comparer à 6 mois, sans charger des années de photos.</summary>
    public const int MoisDePhotos = 7;

    /// <summary>Plafond d'une demande (« Voir les photos plus anciennes »).</summary>
    public const int MoisDePhotosMaximum = 120;

    public async Task<ResultatOperation<AcneViewModel>> GetAsync(string userId, DateOnly jour, CancellationToken ct, int moisDePhotos = MoisDePhotos)
    {
        if (await CarnetDeAsync(userId, ct) is not { } carnetId) return ResultatOperation<AcneViewModel>.Echec(StatutOperation.NonAuthentifie);
        if (Math.Abs(jour.DayNumber - AujourdhuiServeur().DayNumber) > EcartMaximalJours)
            return ResultatOperation<AcneViewModel>.Echec(StatutOperation.Invalide, "Le jour demandé doit être aujourd'hui.");

        var episodes = await context.EpisodesAcne.AsNoTracking()
            .Where(e => e.CarnetSanteId == carnetId)
            .OrderByDescending(e => e.Debut)
            .ToListAsync(ct);
        var depuis = jour.AddMonths(-Math.Clamp(moisDePhotos, 1, MoisDePhotosMaximum)).ToDateTime(TimeOnly.MinValue);
        var avecPhoto = context.SymptomesCycles.AsNoTracking()
            .Where(s => s.CarnetSanteId == carnetId && s.TypeSymptome == TypeAcne && s.PhotoUrl != null && s.PhotoUrl != "");
        var suivis = await avecPhoto.Where(s => s.Date >= depuis).OrderByDescending(s => s.Date).ToListAsync(ct);
        var plusAnciens = await avecPhoto.CountAsync(s => s.Date < depuis, ct);

        return ResultatOperation<AcneViewModel>.Succes(new AcneViewModel
        {
            Episodes = episodes.Select(e => new EpisodeAcneViewModel
            {
                Id = e.Id,
                Debut = Texte(e.Debut),
                Fin = e.Fin is { } fin ? Texte(fin) : null,
                Jours = (e.Fin ?? jour).DayNumber - e.Debut.DayNumber + 1,
            }).ToList(),
            Suivis = suivis.Select(s => new SuiviAcneViewModel
            {
                Id = s.Id,
                Date = s.Date.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture),
                Intensite = s.Intensite,
                Commentaire = s.Commentaire,
                // Chemin de l'API (jamais l'adresse du stockage) : la photo n'est lisible qu'en étant connectée.
                PhotoUrl = CheminPhoto(s.Id),
            }).ToList(),
            SuivisPlusAnciens = plusAnciens,
        });
    }

    /// <summary>Chemin relatif de l'API qui sert la photo d'un suivi (<c>GET Acne/photos/{id}</c>).</summary>
    public static string CheminPhoto(int symptomeId) => $"Acne/photos/{symptomeId}";

    /// <summary>
    /// Photo d'un suivi de l'utilisatrice connectée, lue dans le stockage par le serveur : l'adresse du stockage n'est jamais
    /// donnée au navigateur et le conteneur peut rester privé. Le carnet est celui de la session, l'adresse celle de la base.
    /// </summary>
    public async Task<ResultatOperation<PhotoDeSuivi>> PhotoAsync(string userId, int symptomeId, CancellationToken ct)
    {
        if (await CarnetDeAsync(userId, ct) is not { } carnetId) return ResultatOperation<PhotoDeSuivi>.Echec(StatutOperation.NonAuthentifie);
        var symptome = await context.SymptomesCycles.AsNoTracking().SingleOrDefaultAsync(s => s.Id == symptomeId, ct);
        if (symptome == null || string.IsNullOrEmpty(symptome.PhotoUrl)) return ResultatOperation<PhotoDeSuivi>.Echec(StatutOperation.Introuvable);
        if (symptome.CarnetSanteId != carnetId) return ResultatOperation<PhotoDeSuivi>.Echec(StatutOperation.Interdit);

        var contenu = await stockagePhotos.OuvrirAsync(symptome.PhotoUrl, ct);
        return contenu == null
            ? ResultatOperation<PhotoDeSuivi>.Echec(StatutOperation.Introuvable)
            : ResultatOperation<PhotoDeSuivi>.Succes(new PhotoDeSuivi(contenu, TypeDeContenu(symptome.PhotoUrl)));
    }

    /// <summary>Type d'après l'extension enregistrée, jamais le type déclaré par le client à l'envoi.</summary>
    private static string TypeDeContenu(string url) => Path.GetExtension(url).ToLowerInvariant() switch
    {
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png" => "image/png",
        ".webp" => "image/webp",
        ".heic" or ".heif" => "image/heic",
        _ => "application/octet-stream",
    };

    /// <summary>Nouvel épisode (« L'acné revient »), en cours ou déjà terminé si une fin est donnée.</summary>
    public async Task<ResultatOperation<int>> CreerAsync(string userId, EpisodeAcneDto dto, CancellationToken ct)
    {
        if (await CarnetDeAsync(userId, ct) is not { } carnetId) return ResultatOperation<int>.Echec(StatutOperation.NonAuthentifie);
        if (await ErreurAsync(carnetId, null, dto.Debut, dto.Fin, ct) is { } erreur) return ResultatOperation<int>.Echec(StatutOperation.Invalide, erreur);

        var episode = new EpisodeAcne { CarnetSanteId = carnetId, Debut = dto.Debut, Fin = dto.Fin };
        context.EpisodesAcne.Add(episode);
        await context.SaveChangesAsync(ct);
        carnetSanteService.InvalidateCache(carnetId);
        return ResultatOperation<int>.Succes(episode.Id);
    }

    /// <summary>Correction des dates d'un épisode.</summary>
    public async Task<ResultatOperation> ModifierAsync(string userId, int id, EpisodeAcneDto dto, CancellationToken ct)
    {
        var (episode, echec) = await EpisodeDeAsync(userId, id, ct);
        if (echec != null) return echec;
        if (await ErreurAsync(episode!.CarnetSanteId, id, dto.Debut, dto.Fin, ct) is { } erreur) return ResultatOperation.Echec(StatutOperation.Invalide, erreur);

        episode.Debut = dto.Debut;
        episode.Fin = dto.Fin;
        await context.SaveChangesAsync(ct);
        carnetSanteService.InvalidateCache(episode.CarnetSanteId);
        return ResultatOperation.Succes();
    }

    /// <summary>« Ça s'est calmé » : dernier jour de l'épisode en cours.</summary>
    public async Task<ResultatOperation> TerminerAsync(string userId, int id, DateOnly fin, CancellationToken ct)
    {
        var (episode, echec) = await EpisodeDeAsync(userId, id, ct);
        if (echec != null) return echec;
        if (episode!.Fin != null) return ResultatOperation.Echec(StatutOperation.Invalide, "Cet épisode est déjà terminé.");
        return await ModifierAsync(userId, id, new EpisodeAcneDto { Debut = episode.Debut, Fin = fin }, ct);
    }

    public async Task<ResultatOperation> SupprimerAsync(string userId, int id, CancellationToken ct)
    {
        var (episode, echec) = await EpisodeDeAsync(userId, id, ct);
        if (echec != null) return echec;

        context.EpisodesAcne.Remove(episode!);
        await context.SaveChangesAsync(ct);
        carnetSanteService.InvalidateCache(episode!.CarnetSanteId);
        return ResultatOperation.Succes();
    }

    /// <summary>Dates cohérentes, jamais à venir, sans chevaucher un autre épisode du carnet ; un seul épisode en cours.</summary>
    private async Task<string?> ErreurAsync(int carnetId, int? id, DateOnly debut, DateOnly? fin, CancellationToken ct)
    {
        var demain = AujourdhuiServeur().AddDays(1);
        if (debut > demain || fin > demain) return "Un jour à venir ne peut pas être noté.";
        if (fin < debut) return "La fin ne peut pas précéder le début.";

        var autres = await context.EpisodesAcne.AsNoTracking()
            .Where(e => e.CarnetSanteId == carnetId && e.Id != id)
            .ToListAsync(ct);
        if (fin == null && autres.Any(e => e.Fin == null)) return "Un épisode est déjà en cours.";
        var finDemandee = fin ?? DateOnly.MaxValue;
        return autres.Any(e => e.Debut <= finDemandee && (e.Fin ?? DateOnly.MaxValue) >= debut)
            ? "Ces dates chevauchent un autre épisode."
            : null;
    }

    private async Task<(EpisodeAcne? Episode, ResultatOperation? Echec)> EpisodeDeAsync(string userId, int id, CancellationToken ct)
    {
        if (await CarnetDeAsync(userId, ct) is not { } carnetId) return (null, ResultatOperation.Echec(StatutOperation.NonAuthentifie));
        var episode = await context.EpisodesAcne.SingleOrDefaultAsync(e => e.Id == id, ct);
        if (episode == null) return (null, ResultatOperation.Echec(StatutOperation.Introuvable));
        return episode.CarnetSanteId != carnetId ? (null, ResultatOperation.Echec(StatutOperation.Interdit)) : (episode, null);
    }

    private async Task<int?> CarnetDeAsync(string userId, CancellationToken ct) => string.IsNullOrEmpty(userId)
        ? null
        : await context.CarnetSantes.Where(c => c.UserId == userId).Select(c => (int?)c.Id).SingleOrDefaultAsync(ct);

    private DateOnly AujourdhuiServeur() => DateOnly.FromDateTime(horloge.GetUtcNow().UtcDateTime);

    private static string Texte(DateOnly jour) => jour.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}
