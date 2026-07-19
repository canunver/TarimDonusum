using Microsoft.Extensions.Localization;
using System.ComponentModel.DataAnnotations;

using System.Text.Json.Serialization;

namespace TarimDonusum.Models
{
    public class IsimBul
    {
        public static string MetneCevir(decimal? deger, int ondalikBasamak)
        {
            if (!deger.HasValue)
                return "";

            return deger.Value.ToString(
                $"N{ondalikBasamak}",
                System.Globalization.CultureInfo.CurrentCulture
            );
        }

        public static string MetneCevirKurussuz(decimal? deger)
        {
            return MetneCevir(deger, 0);
        }

        public static string MetneCevirKuruslu(decimal? deger)
        {
            return MetneCevir(deger, 2);
        }

        public static string EnumAdi<T>(T enumDegeri, IStringLocalizer<SharedResource> localizer) where T : Enum
        {
            return localizer[typeof(T).Name + "_" + Convert.ToInt32(enumDegeri)];
        }
    }

    public enum enumUygulamaAdresiYatirimYeriStatusu : int
    {
        Tanimsiz = 0, //"Tanımsız";
        Mulkiyet = 1,
        Kira = 2,
        Tahsis = 3,
        IrtifakHakki = 4,
        OrganizeSanayi_IhtisasAlaniTahsisi = 5,
        Diger = 6
    }

    public enum enumUygulamaAdresiYapiRuhsatiDurumu : int
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

    public enum enumBasvuruSahibiTuru
    {
        Tanimsiz = 0, //"Tanımsız";
        Isletme = 1,
        UreticiOrgutu = 2,
        Kooperatif = 3,
        Birlik = 4,
        Diger = 5
    }

    public enum enumHukukiTurSirketTuru
    {
        Tanimsiz = 0,
        AnonimSirket = 1,
        LimitedSirket = 2,
        KollektifSirket = 3,
        KomanditSirket = 4,
        UreticiOrgutuKooperatifBirlik = 5,
        Diger = 6
    }

    public enum enumHarcamaTuru
    {
        Tanimsiz = 0, //"Tanımsız";
        YapimIsleri = 1,
        MakineEkipman = 2,
        Danismanlik = 3,
        TedarikciGelistirmeHarcamalari = 4,
        YazilimDonanım = 5
    }

    public enum enumYatirimTuru : int
    {
        Tanimsiz = 0, //"Tanımsız";
        Yeni = 1,
        KapasiteArtirimi = 2,
        Modernizasyon = 3,
        TeknolojiYenileme = 4
    }

    public class BasvuruIrtibat
    {
        public int basvuruId { get; set; }

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

        internal void Dogrula(Sonuc<int> sonuc, bool basvuruIdZorunlu = true)
        {
            if (basvuruIdZorunlu && basvuruId <= 0)
                sonuc.HataEkle("Başvuru kaydı daha önce yapılmalıdır.");
            if (string.IsNullOrWhiteSpace(kisi))
                sonuc.HataEkle("İletişim kişisi girilmelidir.");
            if (string.IsNullOrWhiteSpace(telefon))
                sonuc.HataEkle("İrtibat telefonu girilmelidir.");
        }
    }

    public class Basvuru
    {
        public int BasvuruAnaId
        {
            get => basvuruFirma.basvuruAnaId;
            set => basvuruFirma.basvuruAnaId = value;
        }

        public enumBasvuruDurum durum { get; set; } = enumBasvuruDurum.OnBasvuruDurumu;

        public BasvuruFirma basvuruFirma { get; set; } = new();

        public int Id
        {
            get => basvuruFirma.id;
            set => basvuruFirma.id = value;
        }

        public BasvuruIrtibat irtibat { get; set; } = new();
        public BasvuruYatirim yatirim { get; set; } = new();
        public BasvuruOrtaklik ortaklik { get; set; } = new();
        public List<BasvuruUygulamaAdresi> YatirimAdresleri { get; set; } = new();
        public BasvuruFinans finans = new();
        public BasvuruMali mali = new BasvuruMali();
        public BasvuruUygunHarcama uygunHarcama { get; set; } = new();
        public BasvuruYatirimOzeti yatirimOzeti { get; set; } = new();
        public BasvuruCevreselSosyal cevreselSosyal { get; set; } = new();

        public string BelgePaketiDosyaAdi { get; set; } = "";
        public int? BelgePaketiDosyaId { get; set; }
        public string BelgePaketiAciklama { get; set; } = "";
        public string BelgeBeyani { get; set; } = "";
        public string TaahhutDosyaAdi { get; set; } = "";
        public int? TaahhutDosyaId { get; set; }
        public string TaahhutAciklama { get; set; } = "";
        public List<string> BelgeGruplari { get; set; } = new();
        public List<BasvuruOrtaklikDosya> ZorunluBelgeler { get; set; } = new();
        public List<BasvuruAdliSicilKisi> AdliSicilKisileri { get; set; } = new();

        public string DenetciNotu { get; set; } = "";
        public string DenetimSonucu { get; set; } = "";
    }

    public class BasvuruFirma
    {
        public int basvuruAnaId { get; set; } = 0;
        public int id { get; set; } = 0;
        public int revizyonNo { get; set; } = 0;
        public int siraNo { get; set; } = 1;

        [JsonPropertyName("donem")]
        public Donem donem { get; set; } = new Donem();
        [JsonPropertyName("donemId")]
        public int donemId { get { return donem.id; } set { donem.id = value; } }
        public Firma firma { get; set; } = new Firma();
        public int firmaId { get { return firma.id; } set { firma.id = value; } }
        public Il il { get; set; } = new Il();
        public int ilId { get { return il.id; } set { il.id = value; } }
        public string? basvuruKonusu { get; set; } = "";
        public bool? sonIkiYildirFaalMi { get; set; }
        public enumBasvuruSahibiTuru? basvuruSahibiTuru { get; set; }
        public enumHukukiTurSirketTuru? hukukiTurSirketTuru { get; set; }
        public string? yonetimKuruluUyeleriAdliSicilKisiler { get; set; } = "";
        public decimal? ozelSektorPayi { get; set; }
        public bool? bagliOrtakIsletmeVarMi { get; set; }
        public string? bagliOrtakAciklama { get; set; } = "";
        public Sonuc Dogrula(Sonuc sonuc)
        {
            if (donem.id <= 0)
                sonuc.HataEkle("Başvuru dönemi seçilmelidir.");

            if (il.id <= 0)
                sonuc.HataEkle("Başvuru ili seçilmelidir.");

            if (firma.id <= 0)
                sonuc.HataEkle("Firma seçilmelidir.");

            if (!sonIkiYildirFaalMi.HasValue)
                sonuc.HataEkle("Son 2 yıldır faal mi seçilmelidir.");

            if (!basvuruSahibiTuru.HasValue || basvuruSahibiTuru.Value == enumBasvuruSahibiTuru.Tanimsiz)
                sonuc.HataEkle("Başvuru sahibi türü seçilmelidir.");

            if (!hukukiTurSirketTuru.HasValue || hukukiTurSirketTuru.Value == enumHukukiTurSirketTuru.Tanimsiz)
                sonuc.HataEkle("Hukuki tür / şirket türü seçilmelidir.");

            return sonuc;
        }

    }

    public class BasvuruYatirim
    {
        public int basvuruId { get; set; }
        public string? yatirimAdi { get; set; } = "";
        public enumYatirimTuru yatirimTuru { get; set; } = enumYatirimTuru.Tanimsiz;
        public string? yatiriminAmaci { get; set; }
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

            if (string.IsNullOrWhiteSpace(yatiriminAmaci))
                sonuc.HataEkle("Yatırımın amacı, faaliyetleri ve çıktıları girilmelidir.");
        }

        public void YatirimBilgileriDogrula(Sonuc sonuc)
        {
            if (basvuruId <= 0)
                sonuc.HataEkle("Başvuru verilmelidir!");

            if (string.IsNullOrWhiteSpace(yatirimAdi))
                sonuc.HataEkle("Yatırım adı girilmelidir.");

            if (yatirimTuru == enumYatirimTuru.Tanimsiz)
                sonuc.HataEkle("Yatırım türü seçilmelidir.");

            if (harcamaTurleri == null || harcamaTurleri.Count == 0)
                sonuc.HataEkle("En az bir talep edilen harcama türü seçilmelidir.");

            if (string.IsNullOrWhiteSpace(yatiriminAmaci))
                sonuc.HataEkle("Yatırımın amacı, faaliyetleri ve çıktıları girilmelidir.");
        }

        public void DegerZinciriDogrula(Sonuc sonuc)
        {
            if (basvuruId <= 0)
                sonuc.HataEkle("Başvuru verilmelidir!");

            if (degerZinciriAsamalari == null || degerZinciriAsamalari.Count == 0)
                sonuc.HataEkle("En az bir değer zinciri aşaması seçilmelidir.");

            if (degerZinciriAsamalari != null && degerZinciriAsamalari.Any(x => (x.yapilacakFaaliyetler?.Length ?? 0) > 500))
                sonuc.HataEkle("Yapılacak faaliyetler en fazla 500 karakter olmalıdır.");
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

    public class BasvuruOrtaklik
    {
        public int basvuruId { get; set; }
        public bool? bagliOrtakIsletmeVarMi { get; set; }
        public decimal? ozelSektorPayi { get; set; }
        public List<BasvuruOrtak> ortaklar { get; set; } = new();
        public string? bagliOrtakUnvani { get; set; } = "";
        public string? bagliOrtakKimlikNo { get; set; } = "";
        public decimal? bagliOrtakOncekiYilNetSatis { get; set; }
        public decimal? bagliOrtakSonYilNetSatis { get; set; }
        public decimal? bagliOrtakOncekiYilAktifToplami { get; set; }
        public decimal? bagliOrtakSonYilAktifToplami { get; set; }
        public List<BasvuruOrtaklikDosya> bagliOrtakDosyalari { get; set; } = new();

        internal void Dogrula(Sonuc sonuc)
        {
            if (basvuruId <= 0)
                sonuc.HataEkle("Başvuru kaydı seçilmelidir.");

            if (!bagliOrtakIsletmeVarMi.HasValue)
                sonuc.HataEkle("Bağlı/ortak işletme ilişkisi seçilmelidir.");

            if (bagliOrtakIsletmeVarMi == true)
            {
                if (string.IsNullOrWhiteSpace(bagliOrtakUnvani))
                    sonuc.HataEkle("Bağlı/ortak işletme unvanı girilmelidir.");
                if (string.IsNullOrWhiteSpace(bagliOrtakKimlikNo))
                    sonuc.HataEkle("Bağlı/ortak işletme VKN/MERSİS girilmelidir.");
                if (!bagliOrtakOncekiYilNetSatis.HasValue)
                    sonuc.HataEkle("Bağlı/ortak işletme önceki yıl net satış hasılatı girilmelidir.");
                if (!bagliOrtakSonYilNetSatis.HasValue)
                    sonuc.HataEkle("Bağlı/ortak işletme son yıl net satış hasılatı girilmelidir.");
                if (!bagliOrtakOncekiYilAktifToplami.HasValue)
                    sonuc.HataEkle("Bağlı/ortak işletme önceki yıl aktif toplamı girilmelidir.");
                if (!bagliOrtakSonYilAktifToplami.HasValue)
                    sonuc.HataEkle("Bağlı/ortak işletme son yıl aktif toplamı girilmelidir.");
            }

            OrtaklariDogrula(sonuc);
        }

        internal void OrtaklariDogrula(Sonuc sonuc)
        {
            decimal toplamPay = ortaklar.Sum(x => x.payOrani.GetValueOrDefault());
            if (ortaklar.Count > 0 && toplamPay > 100)
                sonuc.HataEkle("Ortak/pay sahibi toplam pay oranı 100'ü geçemez.");

            List<string> tekrarliKimlikler = ortaklar
                .Select(x => TcknVknNormalizeEt(x.tcknVkn))
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .GroupBy(x => x, StringComparer.OrdinalIgnoreCase)
                .Where(x => x.Count() > 1)
                .Select(x => x.Key)
                .ToList();
            foreach (string kimlik in tekrarliKimlikler)
            {
                sonuc.HataEkle($"{kimlik} TCKN/VKN ile birden fazla ortak kaydedilemez.");
            }

            foreach (BasvuruOrtak ortak in ortaklar.Where(x => string.Equals(x.kisiTuru, "Tüzel Kişi", StringComparison.OrdinalIgnoreCase)))
            {
                if (!ortak.hesabaDahilOran.HasValue || ortak.hesabaDahilOran.Value <= 0)
                    sonuc.HataEkle($"{ortak.adUnvan} için hesaba dahil oran girilmelidir.");
            }
        }

        private static string TcknVknNormalizeEt(string? tcknVkn)
        {
            return new string((tcknVkn ?? "")
                .Trim()
                .Where(char.IsLetterOrDigit)
                .Select(char.ToUpperInvariant)
                .ToArray());
        }
    }

    public class BasvuruOrtak
    {
        public int id { get; set; }
        public int basvuruId { get; set; }
        public int siraNo { get; set; }
        public string? adUnvan { get; set; } = "";
        public string? tcknVkn { get; set; } = "";
        public string? kisiTuru { get; set; } = "";
        public decimal? payOrani { get; set; }
        public decimal? hesabaDahilOran { get; set; }
        public string? ozelKamuNiteligi { get; set; } = "";
        public string? nihaiFaydalaniciBilgisi { get; set; } = "";
        public string? uboKycBelgeAdi { get; set; } = "";
        public int? uboKycDosyaId { get; set; }
        public decimal? oncekiYilNetSatis { get; set; }
        public decimal? sonYilNetSatis { get; set; }
        public decimal? oncekiYilAktifToplami { get; set; }
        public decimal? sonYilAktifToplami { get; set; }
    }

    public class BasvuruOrtaklikDosya
    {
        public int dosyaNo { get; set; }
        public string dosyaTuru { get; set; } = "";
        public int? dosyaId { get; set; }
        public string dosyaAdi { get; set; } = "";
    }

    public class BasvuruAdliSicilKisi
    {
        public int id { get; set; }
        public int basvuruId { get; set; }
        public int siraNo { get; set; }
        public string? tckn { get; set; } = "";
        public string? ad { get; set; } = "";
        public string? soyad { get; set; } = "";
        public string? gorev { get; set; } = "";
        public string? dosyaAdi { get; set; } = "";
        public int? dosyaId { get; set; }

        internal void Dogrula(Sonuc sonuc)
        {
            if (basvuruId <= 0)
                sonuc.HataEkle("Başvuru kaydı seçilmelidir.");
            if (string.IsNullOrWhiteSpace(tckn))
                sonuc.HataEkle("TCKN girilmelidir.");
            if (string.IsNullOrWhiteSpace(ad))
                sonuc.HataEkle("Ad girilmelidir.");
            if (string.IsNullOrWhiteSpace(soyad))
                sonuc.HataEkle("Soyad girilmelidir.");
            if (string.IsNullOrWhiteSpace(gorev))
                sonuc.HataEkle("Görev seçilmelidir.");
        }
    }

    public class BasvuruAdliSicilKayitModel
    {
        public int basvuruId { get; set; }
        public List<BasvuruAdliSicilKisi> kisiler { get; set; } = new();
    }

    public class BasvuruFinans
    {
        public int basvuruId { get; set; }
        public decimal? toplamYatirimTutari { get; set; }
        public decimal? uygunHarcamaTutari { get; set; }
        public decimal? talepEdilenDestekTutari { get; set; }
        public decimal? talepEdilenFinansmanOrani { get; set; }
        public decimal? onBasvuruSahibiKatkisi { get; set; }
        public decimal? basvuruSahibiKatkisi { get; set; }
        public int? talepEdilenVadeSuresiYil { get; set; }
        public decimal? destekOrani { get; set; }
        public string? digerFinansmanKaynaklariAciklama { get; set; } = "";
        public string? yatiriminAmaci { get; set; }

        internal void Dogrula(Sonuc<int> sonuc)
        {
            if (basvuruId < 0)
                sonuc.HataEkle("Başvuru bilgisi verilmelidir.");

            if (toplamYatirimTutari == null || toplamYatirimTutari.Value <= 0)
                sonuc.HataEkle("Toplam yatırım tutarı verilmelidir.");

            if (talepEdilenFinansmanOrani == null || talepEdilenFinansmanOrani.Value <= 0)
                sonuc.HataEkle("Talep edilen finansman oranı verilmelidir.");

            if (onBasvuruSahibiKatkisi == null || onBasvuruSahibiKatkisi.Value < 0)
                sonuc.HataEkle("Ön başvuru sahibi katkısı verilmelidir.");

            if (basvuruSahibiKatkisi == null || basvuruSahibiKatkisi.Value < 0)
                sonuc.HataEkle("Başvuru sahibi katkısı verilmelidir.");

            if (talepEdilenVadeSuresiYil == null || talepEdilenVadeSuresiYil.Value <= 0)
                sonuc.HataEkle("Talep edilen vade süresi verilmelidir.");
        }
    }

    public class BasvuruUygunHarcama
    {
        public int basvuruId { get; set; }
        public string? pikkListesiJson { get; set; } = "";

        internal void Dogrula(Sonuc<int> sonuc)
        {
            if (basvuruId <= 0)
                sonuc.HataEkle("Başvuru bilgisi verilmelidir.");

            if (!string.IsNullOrWhiteSpace(pikkListesiJson) && pikkListesiJson.Length > 20000)
                sonuc.HataEkle("PİKK listesi verisi çok uzun.");
        }
    }

    public class BasvuruYatirimOzeti
    {
        public int basvuruId { get; set; }
        public string? yatirimOzetiJson { get; set; } = "";

        internal void Dogrula(Sonuc<int> sonuc)
        {
            if (basvuruId <= 0)
                sonuc.HataEkle("Başvuru bilgisi verilmelidir.");

            if (!string.IsNullOrWhiteSpace(yatirimOzetiJson) && yatirimOzetiJson.Length > 50000)
                sonuc.HataEkle("Yatırım özeti verisi çok uzun.");
        }
    }

    public class BasvuruMali
    {
        public int basvuruId { get; set; }
        public decimal? oncekiYilNetSatis { get; set; }
        public decimal? sonYilNetSatis { get; set; }
        public decimal? oncekiYilAktifToplami { get; set; }
        public decimal? sonYilAktifToplami { get; set; }
        public bool? bagimsizDenetimeTabiMi { get; set; }
        public string denetimDosyaAdi { get; set; } = "";
        public int? denetimDosyaId { get; set; }

        internal void Dogrula(Sonuc<int> sonuc)
        {
            if (basvuruId < 0)
                sonuc.HataEkle("Başvuru bilgisi verilmelidir.");

            if (!bagimsizDenetimeTabiMi.HasValue)
                sonuc.HataEkle("Bağımsız denetime tabi mi seçilmelidir.");

            if (oncekiYilNetSatis == null || oncekiYilNetSatis.Value <= 0)
                sonuc.HataEkle("Önceki yıl net satış tutarı verilmelidir.");

            if (sonYilNetSatis == null || sonYilNetSatis.Value <= 0)
                sonuc.HataEkle("Son yıl net satış tutarı verilmelidir.");

            if (oncekiYilAktifToplami == null || oncekiYilAktifToplami.Value <= 0)
                sonuc.HataEkle("Önceki yıl aktif toplamı verilmelidir.");

            if (sonYilAktifToplami == null || sonYilAktifToplami.Value <= 0)
                sonuc.HataEkle("Son yıl aktif toplamı verilmelidir.");
        }
    }

    public class BasvuruBelge
    {
        public int BasvuruId { get; set; }
        public string BelgePaketiDosyaAdi { get; set; } = "";
        public int? BelgePaketiDosyaId { get; set; }
        public string BelgePaketiAciklama { get; set; } = "";
        public string TaahhutDosyaAdi { get; set; } = "";
        public int? TaahhutDosyaId { get; set; }
        public string TaahhutAciklama { get; set; } = "";
        public string BelgeBeyani { get; set; } = "";
        public List<string> BelgeGruplari { get; set; } = new();
    }

    public class BasvuruDosyaYuklemeSonucu
    {
        public int BasvuruId { get; set; }
        public int DosyaId { get; set; }
        public string DosyaAdi { get; set; } = "";
        public string Aciklama { get; set; } = "";
    }

    public class BasvuruCevreselSosyal
    {
        public int basvuruId { get; set; }
        public string? cevreselSosyalJson { get; set; } = "";

        internal void Dogrula(Sonuc<int> sonuc)
        {
            if (basvuruId <= 0)
                sonuc.HataEkle("Başvuru kaydı seçilmelidir.");
            if (string.IsNullOrWhiteSpace(cevreselSosyalJson))
                sonuc.HataEkle("Çevresel-sosyal anket cevapları girilmelidir.");
        }
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
        public enumUygulamaAdresiYatirimYeriStatusu yatirimYeriStatusu { get; set; } = enumUygulamaAdresiYatirimYeriStatusu.Tanimsiz;
        public int? kiraVeyaTahsisSuresi { get; set; }
        public DateTime? kiraTahsisBitisTarihi { get; set; }

        public string kiraTahsisBitis
        {
            get => string.Join(" / ", new[] { kiraVeyaTahsisSuresi.ToString(), kiraTahsisBitisTarihi?.ToString("yyyy-MM-dd") ?? "" }.Where(x => !string.IsNullOrWhiteSpace(x)));
        }
        public enumUygulamaAdresiYapiRuhsatiDurumu yapiRuhsatiDurumu { get; set; } = enumUygulamaAdresiYapiRuhsatiDurumu.Tanimsiz;
        public string? yapiRuhsatiDurumuAd { get; set; }
        public string? yatirimYeriStatusuAd { get; set; }

        public void UygulamaAdresiDogrula(Sonuc sonuc)
        {
            if (basvuruId <= 0)
                sonuc.HataEkle("Başvuru kaydı seçilmelidir.");

            if (!ilceId.HasValue)
                sonuc.HataEkle("İlçe seçilmelidir.");

            if (string.IsNullOrWhiteSpace(tamAdres))
                sonuc.HataEkle("Tam adres girilmelidir.");

            if (!kiraTahsisBitisTarihi.HasValue)
                sonuc.HataEkle("Kira/tahsis bitiş tarihi girilmelidir.");
        }
    }

    public class Ilce
    {
        public int Id { get; set; }
        public int IlId { get; set; }
        public string Ad { get; set; } = "";
        public bool Aktif { get; set; } = true;
    }
}

