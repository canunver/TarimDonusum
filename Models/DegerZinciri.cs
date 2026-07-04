namespace TarimDonusum.Models
{
    public class DegerZinciri
    {
        public int id { get; set; }
        public string ad { get; set; } = "";
        public string aciklama { get; set; } = "";
        public bool aktif { get; set; } = true;
        public List<Il> iller { get; set; } = new();
        public List<DegerZinciriAsama> asamalar { get; set; } = new();
        public bool secili { get; set; } = false;
    }
}
