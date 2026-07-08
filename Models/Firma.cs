using System.ComponentModel.DataAnnotations;

namespace TarimDonusum.Models
{
    public class Firma
    {
        public int id { get; set; } = 0;

        [Required]
        [StringLength(20)]
        public string vergiKimlikNo { get; set; } = "";

        [Required]
        [StringLength(250)]
        public string ticaretUnvani { get; set; } = "";

        [StringLength(100)]
        public string ticaretSicilNo { get; set; } = "";

        public DateTime? kurulusTarihi { get; set; }

        [StringLength(50)]
        public string mersisNo { get; set; } = "";

        [StringLength(50)]
        public string naceKodu { get; set; } = "";

        [StringLength(250)]
        public string webSitesi { get; set; } = "";

        [StringLength(30)]
        public string telefon { get; set; } = "";

        [StringLength(250)]
        public string kepAdresi { get; set; } = "";

        [EmailAddress]
        [StringLength(256)]
        public string eposta { get; set; } = "";

        public string faaliyetKonusu { get; set; } = "";
        public string adres { get; set; } = "";
        public List<FirmaBasvuran> basvuranlar { get; set; } = new();
    }
}
