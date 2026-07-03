using System.ComponentModel.DataAnnotations;

namespace TarimDonusum.Models
{
    public class Firma
    {
        public int Id { get; set; }
        public int KullaniciId { get; set; }

        [Required]
        [StringLength(20)]
        public string VergiKimlikNo { get; set; } = "";

        [Required]
        [StringLength(250)]
        public string TicaretUnvani { get; set; } = "";

        [StringLength(100)]
        public string TicaretSicilNo { get; set; } = "";

        public DateTime? KurulusTarihi { get; set; }

        [StringLength(50)]
        public string MersisNo { get; set; } = "";

        [StringLength(50)]
        public string NaceKodu { get; set; } = "";

        [StringLength(250)]
        public string WebSitesi { get; set; } = "";

        [StringLength(30)]
        public string Telefon { get; set; } = "";

        [StringLength(250)]
        public string KepAdresi { get; set; } = "";

        [EmailAddress]
        [StringLength(256)]
        public string Eposta { get; set; } = "";

        public string FaaliyetKonusu { get; set; } = "";
        public string Adres { get; set; } = "";
        public List<FirmaBasvuran> Basvuranlar { get; set; } = new();
    }
}
