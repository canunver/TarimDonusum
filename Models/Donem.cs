namespace TarimDonusum.Models
{
    public class Donem
    {
        public int id { get; set; } = 0;
        public int yil { get; set; } = 0;
        public string ad { get; set; } = "";
        public bool basvuruyaAcikMi { get; set; }
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
        public bool uygulamaAdresiSinirliMi { get; set; }
        public string aciklama { get; set; } = "";

        public decimal AzamiDestekOrani(IEnumerable<int?> yatirimIlceIds)
        {
            bool istisna = istisnaDestekOrani.HasValue && yatirimIlceIds.Any(x => x.HasValue && istisnaIlceIds.Contains(x.Value));
            return istisna ? istisnaDestekOrani!.Value : destekOrani.GetValueOrDefault();
        }

        public bool SecilebilirMi()
        {
            DateTime bugun = DateTime.Today;

            if (!basvuruyaAcikMi)
                return false;

            if (basvuruBaslangicTarihi.HasValue && basvuruBaslangicTarihi.Value.Date > bugun)
                return false;

            if (basvuruBitisTarihi.HasValue && basvuruBitisTarihi.Value.Date < bugun)
                return false;

            return true;
        }
    }
}
