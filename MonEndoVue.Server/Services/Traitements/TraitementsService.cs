using Microsoft.EntityFrameworkCore;
using MonEndoVue.Server.Data;
using MonEndoVue.Server.Dto;
using MonEndoVue.Server.Models;
using MonEndoVue.Server.ViewModels;

namespace MonEndoVue.Server.Services.Traitements;

/// <summary>
/// Traitements de l'utilisatrice connectée (carnet déduit de la session) : planning du jour, création, modification,
/// arrêt, prises (faites ou ignorées) et séances de soins. Toute entité est chargée en base et son carnet vérifié.
/// </summary>
public class TraitementsService(AppDbContext context, CarnetSanteService carnetSanteService, TimeProvider horloge)
{
    public async Task<ResultatOperation<TraitementsDuJourViewModel>> GetDuJourAsync(string userId, DateOnly jour, CancellationToken ct) =>
        await CarnetDeAsync(userId, ct) is { } carnetId
            ? ResultatOperation<TraitementsDuJourViewModel>.Succes(await DuJourAsync(carnetId, jour, ct))
            : ResultatOperation<TraitementsDuJourViewModel>.Echec(StatutOperation.NonAuthentifie);

    /// <summary>Planning d'un carnet déjà vérifié (partagé avec l'accueil).</summary>
    public async Task<TraitementsDuJourViewModel> DuJourAsync(int carnetId, DateOnly jour, CancellationToken ct)
    {
        var traitements = await context.Medicaments.AsNoTracking()
            .Where(m => m.CarnetSanteId == carnetId)
            .OrderBy(m => m.Nom)
            .ToListAsync(ct);

        var dernieresPrises = await context.DonneesMedicaments.AsNoTracking()
            .Where(p => p.CarnetSanteId == carnetId && p.Statut == StatutPrise.Pris)
            .GroupBy(p => p.MedicamentId)
            .Select(g => new { MedicamentId = g.Key, Date = g.Max(p => p.Date) })
            .ToDictionaryAsync(p => p.MedicamentId, p => p.Date, ct);
        var dernieresSeances = await context.DonneesTraitementNonMedicamenteux.AsNoTracking()
            .Where(s => s.CarnetSanteId == carnetId)
            .GroupBy(s => s.MedicamentId)
            .Select(g => new { MedicamentId = g.Key, Date = g.Max(s => s.Date) })
            .ToDictionaryAsync(s => s.MedicamentId, s => s.Date, ct);

        bool EnCours(Medicament m) => m.TraitementEnCours && (m.DateFinTraitement == null || DateOnly.FromDateTime(m.DateFinTraitement.Value) >= jour);
        var enCours = traitements.Where(EnCours).ToList();

        return new TraitementsDuJourViewModel
        {
            PrisesPrevues = await PrisesPrevuesAsync(carnetId, traitements, jour, ct),
            AuBesoin = enCours
                .Where(m => m.Type == TypeTraitement.Medicamenteux && m.Frequence == FrequencePrise.AuBesoin)
                .Select(m => new TraitementAuBesoinViewModel { Id = m.Id, Nom = m.Nom, Dose = m.Posologie, DernierePrise = dernieresPrises.TryGetValue(m.Id, out var d) ? d : null })
                .ToList(),
            Soins = enCours
                .Where(m => m.Type == TypeTraitement.NonMedicamenteux)
                .Select(m => new SoinViewModel { Id = m.Id, Nom = m.Nom, DerniereSeance = dernieresSeances.TryGetValue(m.Id, out var d) ? d : null })
                .ToList(),
            EnCours = enCours.Select(Vue).ToList(),
            Termines = traitements.Where(m => !EnCours(m)).Select(Vue).ToList(),
        };
    }

    /// <summary>Prises prévues du jour pour tous les traitements du carnet, avec la réponse déjà notée.</summary>
    private async Task<IReadOnlyList<PrisePrevueViewModel>> PrisesPrevuesAsync(int carnetId, IReadOnlyList<Medicament> traitements, DateOnly jour, CancellationToken ct)
    {
        var debut = jour.ToDateTime(TimeOnly.MinValue);
        var fin = jour.AddDays(1).ToDateTime(TimeOnly.MinValue);
        var reponses = await context.DonneesMedicaments.AsNoTracking()
            .Where(p => p.CarnetSanteId == carnetId && p.HeurePrevue != null && p.Date >= debut && p.Date < fin)
            .ToListAsync(ct);

        return traitements
            .SelectMany(t => PlanningTraitement.HorairesDuJour(t, jour).Select(heure => (t, heure)))
            .OrderBy(p => p.heure).ThenBy(p => p.t.Nom)
            .Select(p =>
            {
                var reponse = reponses.Where(r => r.MedicamentId == p.t.Id && r.HeurePrevue == p.heure).MaxBy(r => r.Id);
                return new PrisePrevueViewModel
                {
                    TraitementId = p.t.Id,
                    Nom = p.t.Nom,
                    Dose = p.t.Posologie,
                    HeurePrevue = p.heure.ToString("HH:mm"),
                    Reponse = reponse == null ? null : new ReponsePriseViewModel { PriseId = reponse.Id, Statut = reponse.Statut.ToString(), Date = reponse.Date },
                };
            })
            .ToList();
    }

    public async Task<ResultatOperation<int>> CreerAsync(string userId, TraitementDto dto, CancellationToken ct)
    {
        if (await CarnetDeAsync(userId, ct) is not { } carnetId) return ResultatOperation<int>.Echec(StatutOperation.NonAuthentifie);
        if (TraitementValidator.Valider(dto) is { } erreur) return ResultatOperation<int>.Echec(StatutOperation.Invalide, erreur);

        var traitement = new Medicament { CarnetSanteId = carnetId, TraitementEnCours = true };
        Appliquer(traitement, dto);
        context.Medicaments.Add(traitement);
        await context.SaveChangesAsync(ct);
        carnetSanteService.InvalidateCache(carnetId);
        return ResultatOperation<int>.Succes(traitement.Id);
    }

    public async Task<ResultatOperation> ModifierAsync(string userId, int id, TraitementDto dto, CancellationToken ct)
    {
        var (traitement, echec) = await TraitementDeAsync(userId, id, ct);
        if (echec != null) return echec;
        if (TraitementValidator.Valider(dto) is { } erreur) return ResultatOperation.Echec(StatutOperation.Invalide, erreur);

        Appliquer(traitement!, dto);
        // Une fin passée arrête le traitement, une fin future le garde en cours. Sans date de fin, l'état ne change pas :
        // enregistrer un ancien traitement arrêté (sans date) pour y ajouter une fréquence ne le relance pas.
        if (dto.DateFin is { } fin) traitement!.TraitementEnCours = fin >= DateOnly.FromDateTime(horloge.GetLocalNow().DateTime);
        await context.SaveChangesAsync(ct);
        carnetSanteService.InvalidateCache(traitement.CarnetSanteId);
        return ResultatOperation.Succes();
    }

    /// <summary>Arrête le traitement à la date donnée (il passe dans « terminés », son historique est conservé).</summary>
    public async Task<ResultatOperation> ArreterAsync(string userId, int id, DateOnly jour, CancellationToken ct)
    {
        var (traitement, echec) = await TraitementDeAsync(userId, id, ct);
        if (echec != null) return echec;

        traitement!.TraitementEnCours = false;
        traitement.DateFinTraitement = jour.ToDateTime(TimeOnly.MinValue);
        await context.SaveChangesAsync(ct);
        carnetSanteService.InvalidateCache(traitement.CarnetSanteId);
        return ResultatOperation.Succes();
    }

    /// <summary>Note une prise faite ou ignorée ; une nouvelle réponse au même horaire prévu remplace la précédente.</summary>
    public async Task<ResultatOperation<int>> NoterPriseAsync(string userId, int traitementId, PriseDto dto, CancellationToken ct)
    {
        var (traitement, echec) = await TraitementDeAsync(userId, traitementId, ct);
        if (echec != null) return ResultatOperation<int>.Echec(echec.Statut, echec.Message);
        if (traitement!.Type != TypeTraitement.Medicamenteux) return ResultatOperation<int>.Echec(StatutOperation.Invalide, "Un soin se note par séance.");
        if (!Enum.IsDefined(dto.Statut)) return ResultatOperation<int>.Echec(StatutOperation.Invalide, "Statut de prise inconnu.");

        if (dto.HeurePrevue is { } heure)
        {
            var debut = dto.Date.Date;
            var anciennes = await context.DonneesMedicaments
                .Where(p => p.MedicamentId == traitementId && p.HeurePrevue == heure && p.Date >= debut && p.Date < debut.AddDays(1))
                .ToListAsync(ct);
            context.DonneesMedicaments.RemoveRange(anciennes);
        }

        var prise = new DonneesMedicament
        {
            CarnetSanteId = traitement.CarnetSanteId,
            MedicamentId = traitementId,
            NombreComprimes = dto.Statut == StatutPrise.Pris ? 1 : 0,
            Date = dto.Date,
            Statut = dto.Statut,
            HeurePrevue = dto.HeurePrevue,
        };
        context.DonneesMedicaments.Add(prise);
        await context.SaveChangesAsync(ct);
        carnetSanteService.InvalidateCache(traitement.CarnetSanteId);
        return ResultatOperation<int>.Succes(prise.Id);
    }

    /// <summary>Annule une réponse notée par erreur (prise ou « ignoré »).</summary>
    public async Task<ResultatOperation> AnnulerPriseAsync(string userId, int priseId, CancellationToken ct)
    {
        if (await CarnetDeAsync(userId, ct) is not { } carnetId) return ResultatOperation.Echec(StatutOperation.NonAuthentifie);
        var prise = await context.DonneesMedicaments.FindAsync([priseId], ct);
        if (prise == null) return ResultatOperation.Echec(StatutOperation.Introuvable);
        if (prise.CarnetSanteId != carnetId) return ResultatOperation.Echec(StatutOperation.Interdit);

        context.DonneesMedicaments.Remove(prise);
        await context.SaveChangesAsync(ct);
        carnetSanteService.InvalidateCache(carnetId);
        return ResultatOperation.Succes();
    }

    /// <summary>Retire une séance notée par erreur (depuis l'historique du soin).</summary>
    public async Task<ResultatOperation> AnnulerSeanceAsync(string userId, int seanceId, CancellationToken ct)
    {
        if (await CarnetDeAsync(userId, ct) is not { } carnetId) return ResultatOperation.Echec(StatutOperation.NonAuthentifie);
        var seance = await context.DonneesTraitementNonMedicamenteux.FindAsync([seanceId], ct);
        if (seance == null) return ResultatOperation.Echec(StatutOperation.Introuvable);
        if (seance.CarnetSanteId != carnetId) return ResultatOperation.Echec(StatutOperation.Interdit);

        context.DonneesTraitementNonMedicamenteux.Remove(seance);
        await context.SaveChangesAsync(ct);
        carnetSanteService.InvalidateCache(carnetId);
        return ResultatOperation.Succes();
    }

    public async Task<ResultatOperation<int>> NoterSeanceAsync(string userId, int traitementId, SeanceDto dto, CancellationToken ct)
    {
        var (traitement, echec) = await TraitementDeAsync(userId, traitementId, ct);
        if (echec != null) return ResultatOperation<int>.Echec(echec.Statut, echec.Message);
        if (traitement!.Type != TypeTraitement.NonMedicamenteux) return ResultatOperation<int>.Echec(StatutOperation.Invalide, "Un médicament se note par prise.");

        var seance = new DonneesTraitementNonMedicamenteux
        {
            CarnetSanteId = traitement.CarnetSanteId,
            MedicamentId = traitementId,
            Date = dto.Date,
            Duree = dto.Duree,
            Commentaire = string.IsNullOrWhiteSpace(dto.Commentaire) ? null : dto.Commentaire.Trim(),
        };
        context.DonneesTraitementNonMedicamenteux.Add(seance);
        await context.SaveChangesAsync(ct);
        carnetSanteService.InvalidateCache(traitement.CarnetSanteId);
        return ResultatOperation<int>.Succes(seance.Id);
    }

    public async Task<int?> CarnetDeAsync(string userId, CancellationToken ct) => string.IsNullOrEmpty(userId)
        ? null
        : await context.CarnetSantes.Where(c => c.UserId == userId).Select(c => (int?)c.Id).SingleOrDefaultAsync(ct);

    /// <summary>Traitement chargé en base, avec ses horaires, s'il appartient au carnet de la session.</summary>
    private async Task<(Medicament? Traitement, ResultatOperation? Echec)> TraitementDeAsync(string userId, int id, CancellationToken ct)
    {
        if (await CarnetDeAsync(userId, ct) is not { } carnetId) return (null, ResultatOperation.Echec(StatutOperation.NonAuthentifie));
        var traitement = await context.Medicaments.Include(m => m.Horaires).SingleOrDefaultAsync(m => m.Id == id, ct);
        if (traitement == null) return (null, ResultatOperation.Echec(StatutOperation.Introuvable));
        return traitement.CarnetSanteId != carnetId ? (null, ResultatOperation.Echec(StatutOperation.Interdit)) : (traitement, null);
    }

    private static void Appliquer(Medicament traitement, TraitementDto dto)
    {
        var avecHoraires = dto.Frequence != FrequencePrise.AuBesoin;
        traitement.Nom = dto.Nom.Trim();
        traitement.Type = dto.Type;
        traitement.Posologie = string.IsNullOrWhiteSpace(dto.Dose) ? null : dto.Dose.Trim();
        traitement.Frequence = dto.Frequence;
        traitement.JoursSemaine = dto.Frequence == FrequencePrise.CertainsJours ? TraitementValidator.Jours(dto) ?? JoursSemaine.Aucun : JoursSemaine.Aucun;
        traitement.IntervalleJours = dto.Frequence == FrequencePrise.TousLesNJours ? dto.IntervalleJours : null;
        traitement.DateDebutTraitement = dto.DateDebut.ToDateTime(TimeOnly.MinValue);
        traitement.DateFinTraitement = dto.DateFin?.ToDateTime(TimeOnly.MinValue);
        traitement.Horaires.Clear();
        if (avecHoraires) traitement.Horaires.AddRange(dto.Horaires.Order().Select(h => new HorairePrise { Heure = h }));
    }

    internal static TraitementViewModel Vue(Medicament m) => new()
    {
        Id = m.Id,
        Nom = m.Nom,
        Type = m.Type.ToString(),
        Dose = m.Posologie,
        Frequence = m.Frequence.ToString(),
        JoursSemaine = Enum.GetValues<JoursSemaine>().Where(j => j != JoursSemaine.Aucun && m.JoursSemaine.HasFlag(j)).Select(j => j.ToString()).ToList(),
        IntervalleJours = m.IntervalleJours,
        Horaires = m.Horaires.Select(h => h.Heure).Order().Select(h => h.ToString("HH:mm")).ToList(),
        DateDebut = DateOnly.FromDateTime(m.DateDebutTraitement),
        DateFin = m.DateFinTraitement is { } fin ? DateOnly.FromDateTime(fin) : null,
    };
}
