namespace TarimDonusum.Models
{
    public class DegerZinciriIl
    {
        public int Id { get; set; }
        public int DegerZinciriId { get; set; }
        public int IlId { get; set; }
        public bool Aktif { get; set; } = true;
    }
}
