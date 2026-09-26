namespace MonEndoVue.Server.Models
{
    public class BilanQuotidien
    {
        public int Id { get; set; }
        public int CarnetSanteId { get; set; }
        public DateTime Date { get; set; }
        public string Mood { get; set; }
        public int StressPro { get; set; }
        public int StressPerso { get; set; }
        public int Fatigue { get; set; }
        public int Pas { get; set; }
        public int DouleurMoyenne { get; set; }
        public double Hydratation { get; set; }
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
    }
}