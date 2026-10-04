namespace MonEndoVue.Server.Models
{
    public class BilanQuotidien
    {
        public int Id { get; set; }
        public int CarnetSanteId { get; set; }
        public DateTime Date { get; set; }
        // Ancienne humeur (Heureuse, Neutre, Triste) des bilans saisis avant les émotions ; null pour les nouveaux bilans.
        public string? Mood { get; set; }
        // Une à trois émotions (table EmotionsBilan), chargées automatiquement avec le bilan.
        public List<EmotionBilan> Emotions { get; set; } = [];
        // Douleur du jour (0 à 10) : seule mesure obligatoire du bilan.
        public int DouleurMoyenne { get; set; }
        // Mesures facultatives : null = non renseigné (jamais 0 par défaut, qui fausserait les moyennes).
        // Stress et fatigue de 0 à 5, pas et hydratation (en litres) positifs.
        public int? StressPro { get; set; }
        public int? StressPerso { get; set; }
        public int? Fatigue { get; set; }
        public int? Pas { get; set; }
        public double? Hydratation { get; set; }
        public bool Gluten { get; set; }
        public bool Lactose { get; set; }
        public bool Grignotage { get; set; }
        public string? Commentaire { get; set; }

        // Transit (catégorie facultative du bilan) : null = non renseigné.
        public bool? Selles { get; set; }
        // Échelle de Bristol (1 à 7), uniquement si Selles == true ; null = « ne pas renseigner ».
        public int? TypeBristol { get; set; }
        public bool? CrampesEstomac { get; set; }
        // Légère, Modérée ou Forte, uniquement si CrampesEstomac == true.
        public string? IntensiteCrampes { get; set; }
        public bool? Ballonnements { get; set; }
        // Légère, Modérée ou Forte, uniquement si Ballonnements == true.
        public string? IntensiteBallonnements { get; set; }

        // Catégories facultatives ajoutées en 1.5.0 : null = non renseigné (jamais « non » ni 0 par défaut).
        // Transit (suite)
        public bool? DouleurSelle { get; set; }
        // Légère, Modérée ou Forte, uniquement si DouleurSelle == true.
        public string? IntensiteDouleurSelle { get; set; }
        public bool? Nausees { get; set; }
        public bool? SangSelles { get; set; }

        // Urinaire
        public bool? DouleurUriner { get; set; }
        // Légère, Modérée ou Forte, uniquement si DouleurUriner == true.
        public string? IntensiteDouleurUriner { get; set; }
        // Envies d'uriner fréquentes ou pressantes.
        public bool? EnviesUrinaires { get; set; }
        public bool? DifficulteVider { get; set; }
        public bool? SangUrines { get; set; }

        // Saignements hors règles (les jours de règles se notent dans l'onglet Règles)
        public bool? SaignementsHorsRegles { get; set; }
        // Traces, Legers ou Abondants, uniquement si SaignementsHorsRegles == true ; facultatif.
        public string? AbondanceSaignementsHorsRegles { get; set; }

        // Nuit et journée
        // Bonne, Moyenne ou Difficile.
        public string? Nuit { get; set; }
        public bool? ReveilsDouleur { get; set; }
        // PasLimitee, PeuLimitee ou TresLimitee.
        public string? LimitationJournee { get; set; }
        public bool? AbsenceTravail { get; set; }
        public bool? ActiviteAnnulee { get; set; }

        // Rapports (catégorie discrète, désactivée par défaut côté client) : Oui, Non ou PasDeRapport.
        public string? DouleurRapport { get; set; }
    }
}