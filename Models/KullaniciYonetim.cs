namespace TarimDonusum.Models
{
    public class KullaniciArama
    {
        public string AdSoyad { get; set; } = "";
        public int? BirimId { get; set; }
        public KullaniciRol? KullaniciTipi { get; set; }
    }

    public class KullaniciKayit
    {
        public Kullanici Kullanici { get; set; } = new();
        public List<KullaniciYetki> Yetkiler { get; set; } = new();
    }

    public class ParolaBaglantisiSonucu
    {
        public string Token { get; set; } = "";
        public string Eposta { get; set; } = "";
        public string AdSoyad { get; set; } = "";
    }
}
