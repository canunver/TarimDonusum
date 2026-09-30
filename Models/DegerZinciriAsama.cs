namespace TarimDonusum.Models
{
    public enum enumDegerZinciriAsamaTuru
    {
        Uretim = 1,
        IslemeIleriIsleme = 2,
        DepolamaSogukZincirVeDiger = 3
    }

    public static class DegerZinciriAsamaTuruTanimlari
    {
        public static string Ad(enumDegerZinciriAsamaTuru tur) => tur switch
        {
            enumDegerZinciriAsamaTuru.Uretim => "Üretim",
            enumDegerZinciriAsamaTuru.IslemeIleriIsleme => "İşleme/İleri İşleme",
            enumDegerZinciriAsamaTuru.DepolamaSogukZincirVeDiger => "Depolama, Soğuk Zincir ve Diğer",
            _ => ""
        };
    }

    public class DegerZinciriAsama
    {
        public DegerZinciri dz { get; set; } = new DegerZinciri();
        public int id { get; set; }
        public int degerZinciriId { get; set; }
        public int? uygulamaAdresiId { get; set; }
        public int siraNo { get; set; }
        public enumDegerZinciriAsamaTuru? asamaTuru { get; set; }
        public string asamaTuruAdi => asamaTuru.HasValue ? DegerZinciriAsamaTuruTanimlari.Ad(asamaTuru.Value) : "";
        public string ad { get; set; } = "";
        public string aciklama { get; set; } = "";
        public string? yapilacakFaaliyetler { get; set; }
        public bool aktif { get; set; } = true;
        public bool secili { get; set; } = false;
    }

    public static class DegerZinciriAsamalari
    {
        public static List<DegerZinciriAsama> SabitListe() => Enum.GetValues<enumDegerZinciriAsamaTuru>()
            .Select(tur => new DegerZinciriAsama { id = (int)tur, siraNo = (int)tur, asamaTuru = tur, ad = DegerZinciriAsamaTuruTanimlari.Ad(tur) })
            .ToList();
    }
}
