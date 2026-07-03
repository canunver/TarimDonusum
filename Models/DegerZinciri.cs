namespace TarimDonusum.Models
{
    public class DegerZinciri
    {
        public int Id { get; set; }
        public string Kod { get; set; } = "";
        public string Ad { get; set; } = "";
        public string Aciklama { get; set; } = "";
        public bool Aktif { get; set; } = true;
        public DateTime KayitTarihi { get; set; } = DateTime.Now;
        public DateTime GuncellemeTarihi { get; set; } = DateTime.Now;
        public List<Il> Iller { get; set; } = new();
        public List<DegerZinciriAsama> Asamalar { get; set; } = new();
    }
}
