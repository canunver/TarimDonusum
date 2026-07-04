namespace TarimDonusum.Models
{
    public class Donem
    {
        public int? Id { get; set; }
        public int Yil { get; set; } = 0;
        public string Ad { get; set; } = "";
        public bool BasvuruyaAcikMi { get; set; }
        public DateTime? BasvuruBaslangicTarihi { get; set; }
        public DateTime? BasvuruBitisTarihi { get; set; }
        public DateTime? OnBasvuruBitisTarihi { get; set; }
        public decimal? MinimumYatirimTutari { get; set; }
        public decimal? MaksimumYatirimTutari { get; set; }
        public decimal? MaksimumDestekTutari { get; set; }
        public decimal? DestekOrani { get; set; }
        public string Aciklama { get; set; } = "";

        public bool SecilebilirMi()
        {
            DateTime bugun = DateTime.Today;

            if (!BasvuruyaAcikMi)
                return false;

            if (BasvuruBaslangicTarihi.HasValue && BasvuruBaslangicTarihi.Value.Date > bugun)
                return false;

            if (BasvuruBitisTarihi.HasValue && BasvuruBitisTarihi.Value.Date < bugun)
                return false;

            return true;
        }
    }
}
