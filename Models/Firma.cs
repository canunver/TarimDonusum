namespace TarimDonusum.Models
{
    public class Firma
    {
        public int id { get; set; } = 0;

        public string? vergiKimlikNo { get; set; } = "";

        public string? ticaretUnvani { get; set; } = "";

        public string? ticaretSicilNo { get; set; } = "";

        public DateTime? kurulusTarihi { get; set; }

        public string? mersisNo { get; set; } = "";

        public string? naceKodu { get; set; } = "";

        public string? webSitesi { get; set; } = "";

        public string? telefon { get; set; } = "";

        public string? kepAdresi { get; set; } = "";

        public string? eposta { get; set; } = "";

        public string? faaliyetKonusu { get; set; } = "";
        public string? adres { get; set; } = "";
        public List<FirmaBasvuran> basvuranlar { get; set; } = new();

        public Sonuc Dogrula(Sonuc sonuc)
        {
            if (string.IsNullOrWhiteSpace(vergiKimlikNo))
                sonuc.HataEkle("Vergi kimlik no girilmelidir.");

            if (vergiKimlikNo?.Length > 20)
                sonuc.HataEkle("Vergi kimlik no en fazla 20 karakter olmalıdır.");

            if (string.IsNullOrWhiteSpace(ticaretUnvani))
                sonuc.HataEkle("Firma adı girilmelidir.");

            if (ticaretUnvani?.Length > 250)
                sonuc.HataEkle("Firma adı en fazla 250 karakter olmalıdır.");

            if (ticaretSicilNo?.Length > 100)
                sonuc.HataEkle("Ticaret sicil no en fazla 100 karakter olmalıdır.");

            if (mersisNo?.Length > 50)
                sonuc.HataEkle("MERSİS no en fazla 50 karakter olmalıdır.");

            if (naceKodu?.Length > 50)
                sonuc.HataEkle("NACE kodu en fazla 50 karakter olmalıdır.");

            if (webSitesi?.Length > 250)
                sonuc.HataEkle("Web sitesi en fazla 250 karakter olmalıdır.");

            if (telefon?.Length > 30)
                sonuc.HataEkle("Telefon en fazla 30 karakter olmalıdır.");

            if (kepAdresi?.Length > 250)
                sonuc.HataEkle("KEP adresi en fazla 250 karakter olmalıdır.");

            if (eposta?.Length > 256)
                sonuc.HataEkle("E-posta en fazla 256 karakter olmalıdır.");

            if (!string.IsNullOrWhiteSpace(eposta) && !Araclar.OrtakFonksiyonlar.EPostaGecerliMi(eposta))
                sonuc.HataEkle("Geçerli bir e-posta adresi girilmelidir.");

            return sonuc;
        }
    }
}
