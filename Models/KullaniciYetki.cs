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
        BasvuruKullanicisi = 2,
        BirimKullanicisi = 3
    }

    public enum KullaniciIslemRolu
    {
        Yok = 0,
        Gorme = 1,
        Yazma = 2,
        Onaylama = 3
    }
}
