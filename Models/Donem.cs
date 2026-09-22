namespace TarimDonusum.Models
{
    public class Donem
    {
        public int id { get; set; } = 0;
        public int yil { get; set; } = 0;
        public string ad { get; set; } = "";
        public bool basvuruyaAcikMi => OnBasvuruyaAcikMi() || BasvuruyaAcikMi();
        public DateTime? basvuruBaslangicTarihi { get; set; }
        public DateTime? basvuruBitisTarihi { get; set; }
        public DateTime? onBasvuruBaslangicTarihi { get; set; }
        public DateTime? onBasvuruBitisTarihi { get; set; }
        public decimal? onBasvuruCevrimKuru { get; set; }
        public decimal? basvuruCevrimKuru { get; set; }
        public decimal? minimumYatirimTutari { get; set; }
        public decimal? maksimumYatirimTutari { get; set; }
        public decimal? maksimumDestekTutari { get; set; }
        public decimal? destekOrani { get; set; }
        public decimal? istisnaDestekOrani { get; set; }
        public List<int> istisnaIlceIds { get; set; } = new();
        public int uygulamaAdresiSinirliMi { get; set; }
        public string aciklama { get; set; } = "";

        public decimal AzamiDestekOrani(IEnumerable<int?> yatirimIlceIds)
        {
            bool istisna = istisnaDestekOrani.HasValue && yatirimIlceIds.Any(x => x.HasValue && istisnaIlceIds.Contains(x.Value));
            return istisna ? istisnaDestekOrani!.Value : destekOrani.GetValueOrDefault();
        }

        public bool OnBasvuruyaAcikMi()
        {
            return TarihAraligindaMi(onBasvuruBaslangicTarihi, onBasvuruBitisTarihi);
        }

        public bool BasvuruyaAcikMi()
        {
            return TarihAraligindaMi(basvuruBaslangicTarihi, basvuruBitisTarihi);
        }

        public bool SecilebilirMi(enumBasvuruKayitTuru kayitTuru)
        {
            return kayitTuru == enumBasvuruKayitTuru.OnBasvuru
                ? OnBasvuruyaAcikMi()
                : BasvuruyaAcikMi();
        }

        private static bool TarihAraligindaMi(DateTime? baslangic, DateTime? bitis)
        {
            if (!baslangic.HasValue || !bitis.HasValue)
                return false;

            DateTime bugun = DateTime.Today;
            return baslangic.Value.Date <= bugun && bugun <= bitis.Value.Date;
        }
    }
}
