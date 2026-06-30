using System.ComponentModel.DataAnnotations;
using TarimDonusum.Araclar;

namespace TarimDonusum.Models
{
    public class Kullanici
    {
        public int Id { get; set; }

        [Required]
        [StringLength(11, MinimumLength = 11)]
        public string TCKN { get; set; } = "";

        [Required]
        [StringLength(100)]
        public string Ad { get; set; } = "";

        [Required]
        [StringLength(100)]
        public string Soyad { get; set; } = "";

        [Required]
        public DateTime DogumTarihi { get; set; }

        [Required]
        [StringLength(20)]
        public string Cinsiyet { get; set; } = "";

        [Required]
        [EmailAddress]
        [StringLength(256)]
        public string Eposta { get; set; } = "";

        [Required]
        [Phone]
        [StringLength(30)]
        public string Telefon { get; set; } = "";

        public string Parola { get; set; } = "";

        public DateTime KayitTarihi { get; set; } = DateTime.UtcNow;

        public bool Aktif { get; set; } = true;

        public List<KullaniciYetki> Yetkiler { get; set; } = new List<KullaniciYetki>();

        public Sonuc Dogrula(Sonuc sonuc)
        {
            if (!OrtakFonksiyonlar.TCKNGecerliMi(TCKN))
                sonuc.HataEkle("Geçerli bir TCKN girilmelidir.");

            if (string.IsNullOrWhiteSpace(Ad))
                sonuc.HataEkle("Ad girilmelidir.");

            if (string.IsNullOrWhiteSpace(Soyad))
                sonuc.HataEkle("Soyad girilmelidir.");

            if (DogumTarihi == default)
                sonuc.HataEkle("Doğum tarihi girilmelidir.");

            if (string.IsNullOrWhiteSpace(Cinsiyet))
                sonuc.HataEkle("Cinsiyet seçilmelidir.");

            if (!OrtakFonksiyonlar.EPostaGecerliMi(Eposta))
                sonuc.HataEkle("Geçerli bir e-posta adresi girilmelidir.");

            if (!OrtakFonksiyonlar.TelefonNoGecerliMi(Telefon))
                sonuc.HataEkle("Geçerli bir telefon numarası girilmelidir.");

            if (!OrtakFonksiyonlar.ParolaGecerliMi(Parola))
                sonuc.HataEkle("Parola en az 8 karakter olmalı ve yeterli güçte olmalıdır.");

            return sonuc;
        }
    }
}
