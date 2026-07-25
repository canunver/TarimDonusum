using System.ComponentModel.DataAnnotations;

namespace TarimDonusum.ViewModels.Home
{
    public class SifremiUnuttumViewModel
    {
        [Required, StringLength(11, MinimumLength = 11)]
        public string TCKN { get; set; } = "";

        [Required, StringLength(100)]
        public string Ad { get; set; } = "";

        [Required, StringLength(100)]
        public string Soyad { get; set; } = "";

        [Required]
        public DateTime? DogumTarihi { get; set; }

        [Required, EmailAddress, StringLength(256)]
        public string Eposta { get; set; } = "";

        [Required, Phone, StringLength(30)]
        public string Telefon { get; set; } = "";
    }
}
