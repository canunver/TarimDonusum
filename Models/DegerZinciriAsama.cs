namespace TarimDonusum.Models
{
    public class DegerZinciriAsama
    {
        public int Id { get; set; }
        public int DegerZinciriId { get; set; }
        public int SiraNo { get; set; }
        public string Ad { get; set; } = "";
        public string Aciklama { get; set; } = "";
        public bool Aktif { get; set; } = true;
    }
}
