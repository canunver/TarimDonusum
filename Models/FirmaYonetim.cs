namespace TarimDonusum.Models
{
    public class FirmaArama
    {
        public string AramaMetni { get; set; } = "";
    }

    public class FirmaLogGorunum
    {
        public int Id { get; set; }
        public DateTime IslemTarihi { get; set; }
        public string Islem { get; set; } = "";
        public string KullaniciAdSoyad { get; set; } = "";
        public string JsonText { get; set; } = "";
    }
}
