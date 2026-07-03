using TarimDonusum.Models;

namespace TarimDonusum.ViewModels.Basvuru
{
    public class BasvuruFormViewModel
    {
        public Models.Basvuru Basvuru { get; set; } = new Models.Basvuru();
        public bool SaltOkunur { get; set; }
        public bool DenetciGorunumu { get; set; }
        public int AktifBolum { get; set; } = 1;
        public List<string> Hatalar { get; set; } = new();
        public List<Donem> Donemler { get; set; } = new();
        public List<Il> Iller { get; set; } = new();
        public List<Ilce> Ilceler { get; set; } = new();
        public List<DegerZinciri> DegerZincirleri { get; set; } = new();
        public int? SeciliDegerZinciriId { get; set; }
        public List<DegerZinciriAsama> SeciliDegerZinciriAsamalari { get; set; } = new();

        public bool IlkBolumKayitli => Basvuru.Id > 0;
    }
}
