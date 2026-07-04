namespace TarimDonusum.Models
{
    public class DegerZinciriAsama
    {
        public DegerZinciri dz { get; set; } = new DegerZinciri();
        public int id { get; set; }
        public int degerZinciriId { get; set; }
        public int siraNo { get; set; }
        public string ad { get; set; } = "";
        public string aciklama { get; set; } = "";
        public bool aktif { get; set; } = true;
        public bool secili { get; set; } = false;
    }
}
