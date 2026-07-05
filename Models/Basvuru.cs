using Microsoft.Extensions.Localization;
using System.ComponentModel.DataAnnotations;

using System.Text.Json.Serialization;

namespace TarimDonusum.Models
{
    public class IsimBul
    {
        public static string EnumAdi<T>(T enumDegeri, IStringLocalizer<SharedResource> localizer) where T : Enum
        {
            return localizer[typeof(T).Name + "_" + Convert.ToInt32(enumDegeri)];
        }
    }
    public enum enumBasvuruSahibiTuru
    {
        Tanimsiz = 0, //"Tanımsız";
        Isletme = 1,
        UreticiOrgutu = 2,
        Kooperatif = 3,
        Birlik = 4,
        Diger = 5
    }

    public enum enumHarcamaTuru
    {
        Tanimsiz = 0, //"Tanımsız";
        YapimIsleri = 1,
        MakineEkipman = 2,
        HizmetAlimi = 3,
        Gorunurluk = 4,
        YazilimDonanım = 5
    }

    public enum enumYatirimYeriDurumu : int
    {
        Tanimsiz = 0, //"Tanımsız";
        Mulkiyet = 1,
        Kira = 2,
        Tahsis = 3,
        IrtifakHakki = 4,
        OrganizeSanayi_IhtisasAlaniTahsisi = 5,
        Diger = 6
    }

    public enum enumYapiRuhsatiDurumu : int
    {
        Tanimsiz = 0, //"Tanımsız";
        YapiRuhsatiMevcut = 1,
        YapiRuhsatiBasvurusuYapildi = 2,
        RuhsatGerekmedigineDairYaziMevcut = 3,
        HenuzTeminEdilmedi = 4,
        YapimIsiYok = 5
    }

    public enum enumBasvuruDurum : int
    {
        Tanimsiz = 0, //"Tanımsız";
        OnBasvuruDurumu = 1, //"Ön Başvuru";
        IptalDurumu = 9, //"İptal";
        BasvuruDurumu = 3, //"Başvuru";
        KabulEdildiDurumu = 5, //"Kabul Edildi";
    }

    public enum enumYatirimTuru : int
    {
        Tanimsiz = 0, //"Tanımsız";
        Yeni = 1,
        KapasiteArtirimi = 2,
        Modernizasyon = 3,
        TeknolojiYenileme = 4
    }

    public class BasvuruIletisim
    {
        public int BasvuruId { get; set; }

        [StringLength(150)]
        public string? kisi { get; set; } = "";

        [StringLength(100)]
        public string? unvan { get; set; } = "";

        [StringLength(30)]
        public string? telefon { get; set; } = "";

        [StringLength(256)]
        public string? ePosta { get; set; } = "";
        public string? adres { get; set; } = "";
        public string? yetkiliKisiler { get; set; } = "";

        internal void Dogrula(Sonuc<int> sonuc)
        {
            if (BasvuruId <= 0)
                sonuc.HataEkle("Başvuru kaydı daha önce yapılmalıdır.");
            if (string.IsNullOrWhiteSpace(kisi))
                sonuc.HataEkle("Başvuru kişisi girilmelidir.");
            if (string.IsNullOrWhiteSpace(telefon))
                sonuc.HataEkle("İrtibat telefonu girilmelidir.");
        }
    }


    public class Basvuru
    {
        public enumBasvuruDurum durum { get; set; } = enumBasvuruDurum.OnBasvuruDurumu;

        public BasvuruFirma basvuruFirma { get; set; } = new();

        public int Id
        {
            get => basvuruFirma.Id;
            set => basvuruFirma.Id = value;
        }

        public int? FirmaId
        {
            get => basvuruFirma.firma.Id;
            set => basvuruFirma.firma.Id = value;
        }

        public int? DonemId
        {
            get => basvuruFirma.donem.Id;
            set => basvuruFirma.donem.Id = value;
        }

        public int? IlId
        {
            get => basvuruFirma.il.Id;
            set => basvuruFirma.il.Id = value;
        }


        public BasvuruIletisim irtibat { get; set; } = new();
        public BasvuruYatirim yatirim { get; set; } = new();
        public List<BasvuruUygulamaAdresi> YatirimAdresleri { get; set; } = new();

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
        public string? YatiriminAmaci { get; set; } = "";

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
        public Donem donem { get; set; } = new();
        public Il Il { get; set; } = new();
    }

    public class BasvuruFirma
    {
        public int Id { get; set; }

        [JsonPropertyName("donem")]
        public Donem donem { get; set; } = new Donem();
        [JsonPropertyName("donemId")]
        public int? donemId { get { return donem.Id; } set { donem.Id = value; } }
        public Firma firma { get; set; } = new Firma();
        public int? firmaId { get { return firma.Id; } set { firma.Id = value; } }
        public Il il { get; set; } = new Il();
        public int? ilId { get { return il.Id; } set { il.Id = value; } }
        public string BasvuruKonusu { get; set; } = "";
        public bool SonIkiYildirFaalMi { get; set; }
        public enumBasvuruSahibiTuru? BasvuruSahibiTuru { get; set; }
        public decimal? OzelSektorPayi { get; set; }
        public bool BagliOrtakIsletmeVarMi { get; set; } = false;
        public string? BagliOrtakAciklama { get; set; } = "";
        public Sonuc Dogrula(Sonuc sonuc)
        {
            if (!donem.Id.HasValue || donem.Id.Value <= 0)
                sonuc.HataEkle("Başvuru dönemi seçilmelidir.");

            if (!il.Id.HasValue || il.Id.Value <= 0)
                sonuc.HataEkle("Başvuru ili seçilmelidir.");

            if (string.IsNullOrWhiteSpace(BasvuruKonusu))
                sonuc.HataEkle("Başvuru konusu girilmelidir.");

            if (!firma.Id.HasValue || firma.Id.Value <= 0)
                sonuc.HataEkle("Firma seçilmelidir.");

            if (!BasvuruSahibiTuru.HasValue)
                sonuc.HataEkle("Başvuru sahibi türü seçilmelidir.");

            return sonuc;
        }

    }

    public class BasvuruYatirim
    {
        public int basvuruId { get; set; }
        public string? yatirimAdi { get; set; } = "";
        public enumYatirimTuru yatirimTuru { get; set; } = enumYatirimTuru.Tanimsiz;
        public int? degerZinciriId { get; set; }
        public List<DegerZinciriAsama> degerZinciriAsamalari { get; set; } = new();
        public List<int> harcamaTurleri { get; set; } = new();

        public void Dogrula(Sonuc sonuc)
        {
            if (basvuruId <= 0)
                sonuc.HataEkle("Başvuru verilmelidir!");

            if (string.IsNullOrWhiteSpace(yatirimAdi))
                sonuc.HataEkle("Yatırım adı girilmelidir.");

            if (yatirimTuru == enumYatirimTuru.Tanimsiz)
                sonuc.HataEkle("Yatırım türü seçilmelidir.");

            if (degerZinciriAsamalari == null || degerZinciriAsamalari.Count == 0)
                sonuc.HataEkle("En az bir değer zinciri aşaması seçilmelidir.");

            if (harcamaTurleri == null || harcamaTurleri.Count == 0)
                sonuc.HataEkle("En az bir talep edilen harcama türü seçilmelidir.");
        }



        /*
                public string FaaliyetKonusu { get; set; } = "";
        public string? YatirimAdi { get; set; } = "";
        public enumYatirimTuru YatirimTuru { get; set; } = 0;
        public int YatirimAdresSayisi { get { return YatirimAdresleri.Count; } }
        public int? DegerZinciriId { get; set; }
        public string DegerZinciri { get; set; } = "";
        public List<DegerZinciriAsama> DegerZinciriAsamalari { get; set; } = new();
        public List<int> HarcamaTurleri { get; set; } = new();

        */
    }

    public class BasvuruYatirimAdresBilgisi
    {
        public int BasvuruId { get; set; }
        public List<BasvuruUygulamaAdresi> YatirimAdresleri { get; set; } = new();
    }

    public class BasvuruFinans
    {
        public int BasvuruId { get; set; }
        public decimal? ToplamYatirimTutari { get; set; }
        public decimal? UygunHarcamaTutari { get; set; }
        public decimal? TalepEdilenDestekTutari { get; set; }
        public decimal? BasvuruSahibiKatkisi { get; set; }
        public decimal? DestekOrani { get; set; }
        public string YatiriminAmaci { get; set; } = "";
    }

    public class BasvuruMali
    {
        public int BasvuruId { get; set; }
        public decimal? OncekiYilNetSatis { get; set; }
        public decimal? SonYilNetSatis { get; set; }
        public decimal? OncekiYilAktifToplami { get; set; }
        public decimal? SonYilAktifToplami { get; set; }
    }

    public class BasvuruBelge
    {
        public int BasvuruId { get; set; }
        public string BelgePaketiDosyaAdi { get; set; } = "";
        public string TaahhutDosyaAdi { get; set; } = "";
        public string BelgeBeyani { get; set; } = "";
        public List<string> BelgeGruplari { get; set; } = new();
    }

    public class BasvuruUygulamaAdresi
    {
        public int id { get; set; }
        public int basvuruId { get; set; }
        public int siraNo { get; set; }
        public int? ilceId { get; set; }
        public int? ilId { get; set; }
        public int? ilKod { get; set; }
        public string ilAdi { get; set; } = "";
        public string ilceAdi { get; set; } = "";
        public string tamAdres { get; set; } = "";
        public UygulamaAdresiYatirimYeriStatusu? yatirimYeriStatusu { get; set; }
        public int? kiraVeyaTahsisSuresi { get; set; }
        public DateTime? kiraTahsisBitisTarihi { get; set; }

        public string kiraTahsisBitis
        {
            get => string.Join(" / ", new[] { kiraVeyaTahsisSuresi.ToString(), kiraTahsisBitisTarihi?.ToString("yyyy-MM-dd") ?? "" }.Where(x => !string.IsNullOrWhiteSpace(x)));
        }
        public UygulamaAdresiYapiRuhsatiDurumu? yapiRuhsatiDurumu { get; set; }
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

