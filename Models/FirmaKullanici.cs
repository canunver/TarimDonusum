namespace TarimDonusum.Models
{
    public class FirmaKullanici
    {
        public int Id { get; set; }
        public int FirmaId { get; set; }
        public int KullaniciId { get; set; }
        public bool Aktif { get; set; } = true;
        public DateTime IliskiTarihi { get; set; } = DateTime.Now;
        public int? IliskiyiKuranKullaniciId { get; set; }
    }
}
