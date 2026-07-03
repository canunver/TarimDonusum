namespace TarimDonusum.Models
{
    public class FirmaBasvuran
    {
        public int KullaniciId { get; set; }
        public string AdSoyad { get; set; } = "";
        public string Eposta { get; set; } = "";
        public string Telefon { get; set; } = "";
        public bool Aktif { get; set; }
        public DateTime IliskiTarihi { get; set; }
    }
}
