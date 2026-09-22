namespace TarimDonusum.Models
{
    public enum enumDegerZinciriAsamaTuru
    {
        BirincilUretim = 1,
        DepolamaSogukZincir = 2,
        Lojistik = 3,
        Isleme = 4,
        IleriIsleme = 5,
        TarimsalBilesenUretimi = 6,
        AtikVeYanUrunDegerlendirme = 7
    }

    public static class DegerZinciriAsamaTuruTanimlari
    {
        public static string Ad(enumDegerZinciriAsamaTuru tur) => tur switch
        {
            enumDegerZinciriAsamaTuru.BirincilUretim => "Birincil Üretim",
            enumDegerZinciriAsamaTuru.DepolamaSogukZincir => "Depolama / Soğuk Zincir",
            enumDegerZinciriAsamaTuru.Lojistik => "Lojistik",
            enumDegerZinciriAsamaTuru.Isleme => "İşleme",
            enumDegerZinciriAsamaTuru.IleriIsleme => "İleri İşleme",
            enumDegerZinciriAsamaTuru.TarimsalBilesenUretimi => "Tarımsal Bileşen Üretimi",
            enumDegerZinciriAsamaTuru.AtikVeYanUrunDegerlendirme => "Atık ve Yan Ürün Değerlendirme",
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
}
