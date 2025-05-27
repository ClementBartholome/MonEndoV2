using TypeGen.Core.TypeAnnotations;

namespace MonEndoVue.Server.Models
{
    [ExportTsInterface]
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
        [TsOptional]
        public string? Commentaire { get; set; }
    }
}