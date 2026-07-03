namespace TarimDonusum.Models
{
    public class BasvuruLog
    {
        public int Id { get; set; }
        public int BasvuruId { get; set; }
        public int KullaniciId { get; set; }
        public DateTime IslemTarihi { get; set; } = DateTime.Now;
        public string Islem { get; set; } = "";
        public string JsonText { get; set; } = "";
    }
}
