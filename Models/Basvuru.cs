using System.ComponentModel.DataAnnotations;

using System.Text.Json.Serialization;

namespace TarimDonusum.Models
{
    public class Basvuru
    {
        public const string OnBasvuruDurumu = "Ön Başvuru";
        public const string IptalDurumu = "İptal";
        public const string BasvuruDurumu = "Başvuru";
        public const string KabulEdildiDurumu = "Kabul Edildi";

        public int Id { get; set; }
        public int KullaniciId { get; set; }
        public int? FirmaId { get; set; }
        public int? DonemId { get; set; }
        public int? IlId { get; set; }
        public string IlAdi { get; set; } = "";
        public string Durum { get; set; } = OnBasvuruDurumu;
        public int AktifBolum { get; set; } = 1;
        public DateTime KayitTarihi { get; set; } = DateTime.Now;
        public DateTime GuncellemeTarihi { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "Başvuru dönemi girilmelidir.")]
        [StringLength(150)]
        public string BasvuruDonemi { get; set; } = "";

        [Required(ErrorMessage = "Başvuru konusu girilmelidir.")]
        [StringLength(250)]
        public string BasvuruKonusu { get; set; } = "";

        [Required(ErrorMessage = "Ticaret unvanÄ± girilmelidir.")]
        [StringLength(250)]
        public string TicaretUnvani { get; set; } = "";

        [StringLength(100)]
        public string TicaretSicilNo { get; set; } = "";

        public DateTime? KurulusTarihi { get; set; }

        [Required(ErrorMessage = "Vergi kimlik no girilmelidir.")]
        [StringLength(20)]
        public string VergiKimlikNo { get; set; } = "";

        [StringLength(50)]
        public string MersisNo { get; set; } = "";

        [StringLength(50)]
        public string NaceKodu { get; set; } = "";

        [Required(ErrorMessage = "BaÅŸvuru sahibi tÃ¼rÃ¼ seÃ§ilmelidir.")]
        [StringLength(50)]
        public string BasvuruSahibiTuru { get; set; } = "";

        [StringLength(10)]
        public string SonIkiYildirFaalMi { get; set; } = "";

        [StringLength(250)]
        public string WebSitesi { get; set; } = "";

        [StringLength(30)]
        public string Telefon { get; set; } = "";

        [StringLength(250)]
        public string KepAdresi { get; set; } = "";

        [EmailAddress(ErrorMessage = "GeÃ§erli bir e-posta adresi girilmelidir.")]
        [StringLength(256)]
        public string Eposta { get; set; } = "";

        [StringLength(150)]
        public string IrtibatKisisi { get; set; } = "";

        [StringLength(100)]
        public string IrtibatUnvani { get; set; } = "";

        [StringLength(150)]
        public string IrtibatTelefonEposta { get; set; } = "";

        [StringLength(30)]
        public string IrtibatTelefon { get; set; } = "";

        [EmailAddress(ErrorMessage = "Geçerli bir irtibat e-posta adresi girilmelidir.")]
        [StringLength(256)]
        public string IrtibatEposta { get; set; } = "";

        public string FaaliyetKonusu { get; set; } = "";
        public string IletisimAdresi { get; set; } = "";
        public string YetkiliKisiler { get; set; } = "";

        public decimal? OzelSektorPayi { get; set; }
        public string BagliOrtakIsletmeVarMi { get; set; } = "";
        public string BagliOrtakAciklama { get; set; } = "";

        public string YatirimAdi { get; set; } = "";
        public string YatirimTuru { get; set; } = "";
        public int YatirimAdresSayisi { get; set; }
        public List<BasvuruUygulamaAdresi> YatirimAdresleri { get; set; } = new();
        public int? DegerZinciriId { get; set; }
        public string DegerZinciri { get; set; } = "";
        public List<string> DegerZinciriAsamalari { get; set; } = new();
        public List<string> HarcamaTurleri { get; set; } = new();
        [JsonIgnore]
        public decimal? ToplamYatirimTutari { get; set; }

        [JsonIgnore]
        public decimal? UygunHarcamaTutari { get; set; }

        [JsonIgnore]
        public decimal? TalepEdilenDestekTutari { get; set; }

        [JsonIgnore]
        public decimal? BasvuruSahibiKatkisi { get; set; }

        [JsonIgnore]
        public decimal? DestekOrani { get; set; } = 80;

        [JsonIgnore]
        public string YatiriminAmaci { get; set; } = "";

        public decimal? OncekiYilNetSatis { get; set; }
        public decimal? SonYilNetSatis { get; set; }
        public decimal? OncekiYilAktifToplami { get; set; }
        public decimal? SonYilAktifToplami { get; set; }

        public string BelgePaketiDosyaAdi { get; set; } = "";
        public string BelgeBeyani { get; set; } = "";
        public string TaahhutDosyaAdi { get; set; } = "";
        public List<string> BelgeGruplari { get; set; } = new();

        public string DenetciNotu { get; set; } = "";
        public string DenetimSonucu { get; set; } = "";
        public Firma? Firma { get; set; }
        public Donem? Donem { get; set; }
        public Il? Il { get; set; }

        public Sonuc IlkBolumDogrula(Sonuc sonuc)
        {
            if (!DonemId.HasValue || DonemId.Value <= 0)
                sonuc.HataEkle("Başvuru dönemi seçilmelidir.");

            if (!IlId.HasValue || IlId.Value <= 0)
                sonuc.HataEkle("Başvuru ili seçilmelidir.");

            if (string.IsNullOrWhiteSpace(BasvuruKonusu))
                sonuc.HataEkle("Başvuru konusu girilmelidir.");

            if (string.IsNullOrWhiteSpace(VergiKimlikNo))
                sonuc.HataEkle("Vergi kimlik no girilmelidir.");

            if (!FirmaId.HasValue || FirmaId.Value <= 0)
                sonuc.HataEkle("Firma seçilmelidir.");

            if (string.IsNullOrWhiteSpace(BasvuruSahibiTuru))
                sonuc.HataEkle("Başvuru sahibi türü seçilmelidir.");

            return sonuc;
        }

    }

    public class BasvuruUygulamaAdresi
    {
        public int Id { get; set; }
        public int BasvuruId { get; set; }
        public int SiraNo { get; set; }
        public int? IlceId { get; set; }
        public int? IlId { get; set; }
        public int? IlKod { get; set; }
        public string IlAdi { get; set; } = "";
        public string IlceAdi { get; set; } = "";
        public string Il
        {
            get => IlAdi;
            set => IlAdi = value ?? "";
        }
        public string Ilce
        {
            get => IlceAdi;
            set => IlceAdi = value ?? "";
        }
        public string TamAdres { get; set; } = "";
        public string AcikAdres
        {
            get => TamAdres;
            set => TamAdres = value ?? "";
        }
        public UygulamaAdresiYatirimYeriStatusu? YatirimYeriStatusu { get; set; }
        public int? KiraVeyaTahsisSuresi { get; set; }
        public DateTime? KiraTahsisBitisTarihi { get; set; }
        public string KiraTahsisDurumu
        {
            get => KiraVeyaTahsisSuresi?.ToString() ?? "";
            set => KiraVeyaTahsisSuresi = int.TryParse(value, out int sure) ? sure : null;
        }
        public string KiraTahsisBitis
        {
            get => string.Join(" / ", new[] { KiraTahsisDurumu, KiraTahsisBitisTarihi?.ToString("yyyy-MM-dd") ?? "" }.Where(x => !string.IsNullOrWhiteSpace(x)));
            set
            {
                if (!string.IsNullOrWhiteSpace(value) && !KiraVeyaTahsisSuresi.HasValue)
                    KiraTahsisDurumu = value;
            }
        }
        public UygulamaAdresiYapiRuhsatiDurumu? YapiRuhsatiDurumu { get; set; }
    }

    public enum UygulamaAdresiYatirimYeriStatusu
    {
        Mulkiyet = 1,
        Kira = 2,
        Tahsis = 3,
        IrtifakHakki = 4,
        OrganizeSanayiIhtisasAlaniTahsis = 5,
        Diger = 6
    }

    public enum UygulamaAdresiYapiRuhsatiDurumu
    {
        YapiRuhsatiMevcut = 1,
        YapiRuhsatiBasvurusuYapildi = 2,
        RuhsatGerekmedigineDairYaziMevcut = 3,
        HenuzTeminEdilmedi = 4,
        YapimIsiYok = 5
    }

    public class Ilce
    {
        public int Id { get; set; }
        public int IlId { get; set; }
        public string Ad { get; set; } = "";
        public bool Aktif { get; set; } = true;
    }
}

