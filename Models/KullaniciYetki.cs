namespace TarimDonusum.Models
{
    public class KullaniciYetki
    {
        public int Id { get; set; }

        public int KullaniciId { get; set; }

        public KullaniciRol Rol { get; set; }

        public int YetkiKodu { get; set; }

        public int? Birim { get; set; }
    }

    public enum KullaniciRol
    {
        SistemYoneticisi = 1,
        KullaniciYoneticisi = 2,
        Basvuran = 3,
        Denetleyen = 4,
        MerkezKullanicisi = 5
    }
}
