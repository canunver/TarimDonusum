namespace TarimDonusum.Models
{
    public class KullaniciLog
    {
        public int Id { get; set; }

        public int KullaniciId { get; set; }

        public int? IslemYapanKullaniciId { get; set; }

        public DateTime IslemTarihi { get; set; } = DateTime.Now;

        public string Islem { get; set; } = "";

        public string JsonText { get; set; } = "";
    }
}
