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
        OnBasvuruDurumu = 1, // Ön Başvuru
        OnBasvuruDuzeltmeDurumu = 2,
        OnBasvuruIncelemeDurumu = 3,
        BasvuruDurumu = 4, // Başvuru
        BasvuruIncelemeDurumu = 5,
        KabulEdildiDurumu = 6, // Başvuru Kabul
        BasvuruSecilmediDurumu = 7,
        ReddedildiDurumu = 8, // Başvuru Red
        IptalDurumu = 9, // Ön Başvuru Red / İptal
    }

    public enum enumBasvuruKayitTuru : int
    {
        OnBasvuru = 1,
        Basvuru = 2
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

    public enum enumOnBasvuruDenetimSonucu : int
    {
        Tanimsiz = 0,
        Reddedildi = 1,
        KabulEdildi = 2,
        DuzeltmeIcinIadeEdildi = 3
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
        public enumBasvuruKayitTuru kayitTuru { get; set; } = enumBasvuruKayitTuru.OnBasvuru;

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
        public List<BasvuruMakine> Makineler { get; set; } = new();
        public List<BasvuruUrunSurec> UrunSurecleri { get; set; } = new();
        public List<BasvuruYatirimOnBilgi> YatirimOnBilgileri { get; set; } = new();
        public BasvuruFinans finans = new();
        public BasvuruMali mali = new BasvuruMali();
        public BasvuruUygunHarcama uygunHarcama { get; set; } = new();
        public BasvuruYatirimOzeti yatirimOzeti { get; set; } = new();
        public BasvuruDbCtpTeknikProje dbCtpTeknikProje { get; set; } = new();
        public BasvuruCevreselSosyal cevreselSosyal { get; set; } = new();

        public string BelgePaketiDosyaAdi { get; set; } = "";
        public int? BelgePaketiDosyaId { get; set; }
        public string BelgePaketiAciklama { get; set; } = "";
        public string BelgeBeyani { get; set; } = "";
        public string TaahhutDosyaAdi { get; set; } = "";
        public int? TaahhutDosyaId { get; set; }
        public string TaahhutAciklama { get; set; } = "";
        public string TaahhutBeyanlarJson { get; set; } = "";
        public List<string> BelgeGruplari { get; set; } = new();
        public List<BasvuruOrtaklikDosya> ZorunluBelgeler { get; set; } = new();
        public List<BasvuruAdliSicilKisi> AdliSicilKisileri { get; set; } = new();

        public string DenetimAnketi { get; set; } = "";
        public string SistemDenetimAnketi { get; set; } = "";
        public string DenetimGerekcesi { get; set; } = "";
        public enumOnBasvuruDenetimSonucu? DenetimSonucu { get; set; }
    }

    public class DenetimListesiKayit
    {
        public int basvuruId { get; set; }
        public string listeTuru { get; set; } = "";
        public string json { get; set; } = "";
    }

    public class BasvuruFirma
    {
        public int basvuruAnaId { get; set; } = 0;
        public int id { get; set; } = 0;
        public int revizyonNo { get; set; } = 0;
        public int siraNo { get; set; } = 0;

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
        public bool? onBasvuruSonrasiDegisiklikVarMi { get; set; }
        [StringLength(2000)]
        public string? onBasvuruSonrasiDegisiklikSebebi { get; set; } = "";
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
        public List<int> yatirimTurleri { get; set; } = new();
        public string? yatiriminAmaci { get; set; }
        public string? yatirimFaaliyetleri { get; set; }
        public string? yatirimGirdileri { get; set; }
        public string? yatirimCiktilari { get; set; }
        public int? degerZinciriId { get; set; }
        public List<DegerZinciriAsama> degerZinciriAsamalari { get; set; } = new();
        public List<int> harcamaTurleri { get; set; } = new();
        public string? basvuruKonusuTesis { get; set; }
        public string? organizeAlanTuru { get; set; }
        public DateTime? planlananBaslangicTarihi { get; set; }
        public DateTime? planlananTamamlanmaTarihi { get; set; }
        public string? ilDegerZinciriEslesmesi { get; set; }
        public string? tarimGidaBaglantiTuru { get; set; }
        public string? tarimGidaBaglantiAciklamasi { get; set; }
        public string? yatirimAlaniTipolojisi { get; set; }
        public string? degerZinciriUygunlukAciklamasi { get; set; }
        public string? oncelikliYatirimUyumu { get; set; }
        public string? oncelikliYatirimKonuKodu { get; set; }
        public string? ithalatBagimliligiUyumu { get; set; }
        public string? ithalatBagimliligiUrunKodu { get; set; }
        public string? hedefUrunlerPazarCiktisi { get; set; }
        public string? rekabetcilikAciklamasi { get; set; }

        public void Dogrula(Sonuc sonuc)
        {
            if (basvuruId <= 0)
                sonuc.HataEkle("Başvuru verilmelidir!");

            if (string.IsNullOrWhiteSpace(yatirimAdi))
                sonuc.HataEkle("Yatırım adı girilmelidir.");

            if (yatirimTurleri == null || yatirimTurleri.Count == 0)
                sonuc.HataEkle("Yatırım türü seçilmelidir.");

            if (degerZinciriAsamalari == null || degerZinciriAsamalari.Count == 0)
                sonuc.HataEkle("En az bir değer zinciri aşaması seçilmelidir.");

            if (harcamaTurleri == null || harcamaTurleri.Count == 0)
                sonuc.HataEkle("En az bir talep edilen harcama türü seçilmelidir.");

            if (string.IsNullOrWhiteSpace(yatiriminAmaci))
                sonuc.HataEkle("Yatırımın amacı girilmelidir.");
            if (string.IsNullOrWhiteSpace(yatirimFaaliyetleri))
                sonuc.HataEkle("Yatırım faaliyetleri girilmelidir.");
            if (string.IsNullOrWhiteSpace(yatirimGirdileri))
                sonuc.HataEkle("Yatırım girdileri girilmelidir.");
            if (string.IsNullOrWhiteSpace(yatirimCiktilari))
                sonuc.HataEkle("Yatırım çıktıları girilmelidir.");
        }

        public void YatirimBilgileriDogrula(Sonuc sonuc)
        {
            if (basvuruId <= 0)
                sonuc.HataEkle("Başvuru verilmelidir!");

            if (string.IsNullOrWhiteSpace(yatirimAdi))
                sonuc.HataEkle("Yatırım adı girilmelidir.");

            if (yatirimTurleri == null || yatirimTurleri.Count == 0)
                sonuc.HataEkle("Yatırım türü seçilmelidir.");

            if (harcamaTurleri == null || harcamaTurleri.Count == 0)
                sonuc.HataEkle("En az bir talep edilen harcama türü seçilmelidir.");

            if (string.IsNullOrWhiteSpace(yatiriminAmaci))
                sonuc.HataEkle("Yatırımın amacı girilmelidir.");
            if (string.IsNullOrWhiteSpace(yatirimFaaliyetleri))
                sonuc.HataEkle("Yatırım faaliyetleri girilmelidir.");
            if (string.IsNullOrWhiteSpace(yatirimGirdileri))
                sonuc.HataEkle("Yatırım girdileri girilmelidir.");
            if (string.IsNullOrWhiteSpace(yatirimCiktilari))
                sonuc.HataEkle("Yatırım çıktıları girilmelidir.");
        }

        public void DegerZinciriDogrula(Sonuc sonuc)
        {
            if (basvuruId <= 0)
                sonuc.HataEkle("Başvuru verilmelidir!");

            if (degerZinciriAsamalari == null || degerZinciriAsamalari.Count == 0)
                sonuc.HataEkle("En az bir değer zinciri aşaması seçilmelidir.");

            if (degerZinciriAsamalari != null && degerZinciriAsamalari.Any(x => (x.yapilacakFaaliyetler?.Length ?? 0) > 500))
                sonuc.HataEkle("Yapılacak faaliyetler en fazla 500 karakter olmalıdır.");

            string[] teyitSecenekleri = ["Evet", "Hayır", "Kurum teyidi bekleniyor"];
            string[] baglantiSecenekleri = ["Yukarı yönlü tedarik bağlantısı", "Tarımsal girdi kullanan ürün", "Gıda odaklı çıktı", "Birden fazla bağlantı"];
            if (!string.IsNullOrWhiteSpace(ilDegerZinciriEslesmesi) && !teyitSecenekleri.Contains(ilDegerZinciriEslesmesi))
                sonuc.HataEkle("İl-değer zinciri eşleşmesi seçimi geçersizdir.");
            if (!string.IsNullOrWhiteSpace(tarimGidaBaglantiTuru) && !baglantiSecenekleri.Contains(tarimGidaBaglantiTuru))
                sonuc.HataEkle("Doğrudan tarım-gıda bağlantısı türü seçimi geçersizdir.");
            if (!string.IsNullOrWhiteSpace(oncelikliYatirimUyumu) && !teyitSecenekleri.Contains(oncelikliYatirimUyumu))
                sonuc.HataEkle("Öncelikli yatırım listesi uyumu seçimi geçersizdir.");
            if (!string.IsNullOrWhiteSpace(ithalatBagimliligiUyumu) && !teyitSecenekleri.Contains(ithalatBagimliligiUyumu))
                sonuc.HataEkle("İthalat bağımlılığı listesi uyumu seçimi geçersizdir.");
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
        public int? degisenOrtakSiraNo { get; set; }
        public string? bagliOrtakUnvani { get; set; } = "";
        public string? bagliOrtakKimlikNo { get; set; } = "";
        public decimal? bagliOrtakOncekiYilNetSatis { get; set; }
        public decimal? bagliOrtakSonYilNetSatis { get; set; }
        public decimal? bagliOrtakOncekiYilAktifToplami { get; set; }
        public decimal? bagliOrtakSonYilAktifToplami { get; set; }
        public List<BasvuruOrtaklikDosya> bagliOrtakDosyalari { get; set; } = new();

        internal void Dogrula(Sonuc sonuc, int? dogrulanacakSiraNo = null)
        {
            if (basvuruId <= 0)
                sonuc.HataEkle("Başvuru kaydı seçilmelidir.");

            OrtaklariDogrula(sonuc, dogrulanacakSiraNo);
            bagliOrtakIsletmeVarMi = ortaklar.Any(x => string.Equals(x.kisiTuru, "Tüzel Kişi", StringComparison.OrdinalIgnoreCase));

            decimal AgirlikliToplam(Func<BasvuruOrtak, decimal?> alan) => ortaklar
                .Where(x => string.Equals(x.kisiTuru, "Tüzel Kişi", StringComparison.OrdinalIgnoreCase))
                .Sum(x => alan(x).GetValueOrDefault() * BasvuruOrtak.HesabaDahilOranHesapla(x.payOrani) / 100m);

            bagliOrtakOncekiYilNetSatis = AgirlikliToplam(x => x.oncekiYilNetSatis);
            bagliOrtakSonYilNetSatis = AgirlikliToplam(x => x.sonYilNetSatis);
            bagliOrtakOncekiYilAktifToplami = AgirlikliToplam(x => x.oncekiYilAktifToplami);
            bagliOrtakSonYilAktifToplami = AgirlikliToplam(x => x.sonYilAktifToplami);
        }

        internal void OrtaklariDogrula(Sonuc sonuc, int? dogrulanacakSiraNo = null)
        {
            decimal toplamPay = ortaklar.Sum(x => x.payOrani.GetValueOrDefault());
            if (ortaklar.Count > 0 && toplamPay > 100)
                sonuc.HataEkle("Ortak/pay sahibi toplam pay oranı 100'ü geçemez.");

            foreach (BasvuruOrtak ortak in ortaklar.Where(x => (!dogrulanacakSiraNo.HasValue || x.siraNo == dogrulanacakSiraNo.Value) && (!x.payOrani.HasValue || x.payOrani <= 0 || x.payOrani > 100)))
                sonuc.HataEkle($"{ortak.adUnvan} için pay oranı 0'dan büyük ve 100'e eşit veya küçük olmalıdır.");

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

            foreach (BasvuruOrtak ortak in ortaklar.Where(x => (!dogrulanacakSiraNo.HasValue || x.siraNo == dogrulanacakSiraNo.Value) && string.Equals(x.kisiTuru, "Tüzel Kişi", StringComparison.OrdinalIgnoreCase)))
            {
                ortak.hesabaDahilOran = BasvuruOrtak.HesabaDahilOranHesapla(ortak.payOrani);
                string vkn = ortak.tcknVkn?.Trim() ?? "";
                if (vkn.Length != 10 || !vkn.All(char.IsDigit))
                    sonuc.HataEkle($"{ortak.adUnvan} için 10 haneli VKN girilmelidir.");
            }

            foreach (BasvuruOrtak ortak in ortaklar.Where(x => (!dogrulanacakSiraNo.HasValue || x.siraNo == dogrulanacakSiraNo.Value) && string.Equals(x.kisiTuru, "Gerçek Kişi", StringComparison.OrdinalIgnoreCase)))
            {
                string tckn = ortak.tcknVkn?.Trim() ?? "";
                if (tckn.Length != 11 || !tckn.All(char.IsDigit))
                    sonuc.HataEkle($"{ortak.adUnvan} için 11 haneli TCKN girilmelidir.");
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

    public class BasvuruOrtakSilModel
    {
        public int basvuruId { get; set; }
        public int ortakId { get; set; }
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
        public DateTime? dogumTarihi { get; set; }
        public string? cinsiyet { get; set; } = "";
        public string? sahiplikNiteligi { get; set; } = "Uygulanamaz";
        public string? nihaiFaydalaniciBilgisi { get; set; } = "";
        public string? uboKycBelgeAdi { get; set; } = "";
        public int? uboKycDosyaId { get; set; }
        public decimal? oncekiYilNetSatis { get; set; }
        public decimal? sonYilNetSatis { get; set; }
        public decimal? oncekiYilAktifToplami { get; set; }
        public decimal? sonYilAktifToplami { get; set; }
        public string? iliskiTuru { get; set; } = "";
        public string? belgeReferansi { get; set; } = "";

        public string SahiplikNiteligiHesapla(DateTime bugun)
        {
            if (!string.Equals(kisiTuru, "Gerçek Kişi", StringComparison.OrdinalIgnoreCase))
            {
                dogumTarihi = null;
                cinsiyet = "";
                return "Uygulanamaz";
            }
            bool kadin = string.Equals(cinsiyet, "Kadın", StringComparison.OrdinalIgnoreCase);
            bool genc = dogumTarihi.HasValue && dogumTarihi.Value.Date > bugun.Date.AddYears(-40);
            if (kadin && genc) return "Her ikisi";
            if (kadin) return "Kadın";
            if (genc) return "40 yaş altı";
            return "Uygulanamaz";
        }
        public static decimal HesabaDahilOranHesapla(decimal? payOrani)
        {
            decimal oran = payOrani.GetValueOrDefault();
            if (oran < 25) return 0;
            if (oran > 50) return 100;
            return oran;
        }
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
        public string? yetkiKapsami { get; set; } = "";
        public string? aciklama { get; set; } = "";
        public string? imzaYetkiDosyaAdi { get; set; } = "";
        public int? imzaYetkiDosyaId { get; set; }
        public string? dosyaAdi { get; set; } = "";
        public int? dosyaId { get; set; }

        internal void Dogrula(Sonuc sonuc)
        {
            if (basvuruId <= 0)
                sonuc.HataEkle("Başvuru kaydı seçilmelidir.");
            if (string.IsNullOrWhiteSpace(tckn))
                sonuc.HataEkle("TCKN girilmelidir.");
            else if (!Araclar.OrtakFonksiyonlar.TCKNGecerliMi(tckn))
                sonuc.HataEkle("Geçerli bir TCKN giriniz.");
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

    public enum enumYatirimOnBilgiTuru
    {
        MevcutUrun = 1,
        UretilecekUrun = 2,
        Girdi = 3,
        EnerjiKullanimi = 4,
        KuruluGuc = 5
    }

    public class BasvuruYatirimOnBilgi
    {
        public int id { get; set; }
        public int basvuruId { get; set; }
        public enumYatirimOnBilgiTuru tur { get; set; }
        public int siraNo { get; set; }
        public string ad { get; set; } = "";
        public decimal? miktar { get; set; }
        public string? birim { get; set; }
        public decimal? tekPanelGucu { get; set; }
        public string? tekPanelGucuBirim { get; set; }
        public decimal? toplamGuc { get; set; }
        public string? toplamGucBirim { get; set; }
    }

    public class BasvuruYatirimOnBilgiKayitModel
    {
        public int basvuruId { get; set; }
        public List<BasvuruYatirimOnBilgi> kayitlar { get; set; } = new();
    }

    public class BasvuruMakine
    {
        public int id { get; set; }
        public int basvuruId { get; set; }
        public int siraNo { get; set; }
        public string ad { get; set; } = "";
        public string birim { get; set; } = "";
        public decimal miktar { get; set; }
        public string aciklama { get; set; } = "";
        public string marka { get; set; } = "";
        public string model { get; set; } = "";
        public string kapasiteOzellikleri { get; set; } = "";
        public int? yerlesimPlaniSiraNo { get; set; }
        public string kullanimAmaci { get; set; } = "";
        public string durum { get; set; } = "";
        public string kapasiteSecimGerekcesi { get; set; } = "";
        public bool teklifKontroluYapilsin { get; set; }
        public List<BasvuruMakineOzellik> teknikOzellikler { get; set; } = new();
        public List<BasvuruMakineTeklif> teklifler { get; set; } = new();
        public List<BasvuruMakineUzmanDokuman> uzmanDokumanlari { get; set; } = new();
        public string? uzmanParaBirimi { get; set; }
        public decimal? uzmanKur { get; set; }
        public decimal? uzmanMinimumFiyat { get; set; }
        public decimal? uzmanMaksimumFiyat { get; set; }
        public int? uzmanSecilenTeklifId { get; set; }
        public decimal? uzmanOnerilenFiyatTl { get; set; }
        public string? uzmanKontrolSonucu { get; set; }
        public string? uzmanAciklama { get; set; }
    }

    public class BasvuruMakineOzellik
    {
        public int id { get; set; }
        public int makineId { get; set; }
        public int siraNo { get; set; }
        public string baslik { get; set; } = "";
        public string aciklamaAsgariGereklilik { get; set; } = "";
        public bool zorunluMu { get; set; }
    }

    public class BasvuruMakineTeklif
    {
        public int id { get; set; }
        public int makineId { get; set; }
        public int siraNo { get; set; }
        public bool basvuruyaEsas { get; set; }
        public string tedarikci { get; set; } = "";
        public string marka { get; set; } = "";
        public string model { get; set; } = "";
        public string paraBirimi { get; set; } = "";
        public decimal? kur { get; set; }
        public decimal? birimFiyat { get; set; }
        public DateTime? teklifTarihi { get; set; }
        public DateTime? gecerlilikTarihi { get; set; }
        public int? teklifBelgesiDosyaId { get; set; }
        public string? teklifBelgesiDosyaAdi { get; set; }
        public string aciklama { get; set; } = "";
    }

    public class BasvuruMakineKayitModel
    {
        public int basvuruId { get; set; }
        public List<BasvuruMakine> makineler { get; set; } = new();
    }

    public class BasvuruMakineUzmanKayitModel
    {
        public int basvuruId { get; set; }
        public int makineId { get; set; }
        public string? uzmanParaBirimi { get; set; }
        public decimal? uzmanKur { get; set; }
        public decimal? uzmanMinimumFiyat { get; set; }
        public decimal? uzmanMaksimumFiyat { get; set; }
        public int? uzmanSecilenTeklifId { get; set; }
        public decimal? uzmanOnerilenFiyatTl { get; set; }
        public string? uzmanKontrolSonucu { get; set; }
        public string? uzmanAciklama { get; set; }
    }

    public class BasvuruMakineUzmanDokuman
    {
        public int id { get; set; }
        public int basvuruId { get; set; }
        public int makineId { get; set; }
        public int siraNo { get; set; }
        public string dokumanAdi { get; set; } = "";
        public string dokumanTuru { get; set; } = "";
        public string kaynakTedarikci { get; set; } = "";
        public DateTime? belgeTarihi { get; set; }
        public string aciklama { get; set; } = "";
        public int? dosyaId { get; set; }
        public string? dosyaAdi { get; set; }
    }

    public class BasvuruUrunSurec
    {
        public int id { get; set; }
        public int basvuruId { get; set; }
        public int urunId { get; set; }
        public int siraNo { get; set; }
        public string surecAdi { get; set; } = "";
        public List<BasvuruUrunSurecMakine> makineler { get; set; } = new();
    }

    public class BasvuruUrunSurecMakine
    {
        public int id { get; set; }
        public int basvuruId { get; set; }
        public int surecId { get; set; }
        public int makineId { get; set; }
        public int siraNo { get; set; }
        public decimal adet { get; set; }
        public string yerlesimPlaniNo { get; set; } = "";
        public string girdilerMiktarlar { get; set; } = "";
        public string ciktilarMiktarlar { get; set; } = "";
        public string islemeKapasitesi { get; set; } = "";
        public decimal? gunlukCalismaSuresi { get; set; }
        public string gunlukCalismaSuresiBirimi { get; set; } = "Saat";
        public string aciklama { get; set; } = "";
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
        public int? talepEdilenVadeSuresiAy { get; set; }
        public int? yatirimSuresiAy { get; set; }
        public int? odemeSuresiAy { get; set; }
        public decimal? destekOrani { get; set; }
        public string? digerFinansmanKaynaklariAciklama { get; set; } = "";
        public string? finansmanParaBirimi { get; set; }
        public string? digerFinansmanKaynaklari { get; set; }
        public decimal? oncekiRffOnayliTutar { get; set; }
        public string? oncekiRffSozlesmesiKapaliMi { get; set; }
        public string? bankaTeminatMektubuSaglanabilirMi { get; set; }
        public bool detayliFinansmanKaydi { get; set; }
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

            if (talepEdilenVadeSuresiAy == null || talepEdilenVadeSuresiAy.Value <= 0)
                sonuc.HataEkle("Talep edilen vade süresi verilmelidir.");

            if (yatirimSuresiAy == null || yatirimSuresiAy <= 0)
                sonuc.HataEkle("Yatırım süresi ay olarak girilmelidir.");
            if (!string.IsNullOrWhiteSpace(finansmanParaBirimi) && !ParaBirimleri.GecerliMi(finansmanParaBirimi))
                sonuc.HataEkle("Geçerli bir finansman para birimi seçiniz.");
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
                sonuc.HataEkle("Uygun harcama ön listesi verisi çok uzun.");
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

    public class BasvuruDbCtpTeknikProje
    {
        public int basvuruId { get; set; }
        public string? dbCtpTeknikProjeJson { get; set; } = "";

        internal void Dogrula(Sonuc<int> sonuc)
        {
            if (basvuruId <= 0)
                sonuc.HataEkle("Başvuru bilgisi verilmelidir.");
            if (string.IsNullOrWhiteSpace(dbCtpTeknikProjeJson))
                sonuc.HataEkle("DB C-TP Teknik Proje bilgileri girilmelidir.");
            if (!string.IsNullOrWhiteSpace(dbCtpTeknikProjeJson) && dbCtpTeknikProjeJson.Length > 100000)
                sonuc.HataEkle("DB C-TP Teknik Proje verisi çok uzun.");
        }
    }

    public class BasvuruMali
    {
        public int basvuruId { get; set; }
        public decimal? oncekiYilNetSatis { get; set; }
        public decimal? sonYilNetSatis { get; set; }
        public decimal? oncekiYilAktifToplami { get; set; }
        public decimal? sonYilAktifToplami { get; set; }
        public decimal? oncekiYilIhracatSatis { get; set; }
        public decimal? sonYilIhracatSatis { get; set; }
        public int? oncekiYilCalisanSayisi { get; set; }
        public int? sonYilCalisanSayisi { get; set; }
        public string? aciklama { get; set; } = "";
        public string? belgeReferanslariJson { get; set; } = "";
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

    public class BasvuruTaahhutBeyanlar
    {
        public int basvuruId { get; set; }
        public string? taahhutBeyanlarJson { get; set; } = "";

        internal void Dogrula(Sonuc<int> sonuc)
        {
            if (basvuruId <= 0) sonuc.HataEkle("Başvuru bilgisi verilmelidir.");
            if (string.IsNullOrWhiteSpace(taahhutBeyanlarJson))
                sonuc.HataEkle("Taahhüt ve beyanlar onaylanmalıdır.");
            if (!string.IsNullOrWhiteSpace(taahhutBeyanlarJson) && taahhutBeyanlarJson.Length > 20000)
                sonuc.HataEkle("Taahhüt ve beyan verisi çok uzun.");
        }
    }

    public class BasvuruMaliBelgeReferansi
    {
        public int dosyaId { get; set; }
        public string dosyaAdi { get; set; } = "";
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
        public string? koordinat { get; set; }
        public string? adaParsel { get; set; }
        public string? segeKademesi { get; set; }
        public DateTime? kullanimHakkiBaslangicTarihi { get; set; }
        public bool? donemleriKapsiyorMu { get; set; }
        public string? izinTakvimAciklama { get; set; }
        public int? adresBelgeDosyaId { get; set; }
        public string? adresBelgeDosyaAdi { get; set; }
        public int? kullanimHakkiDosyaId { get; set; }
        public string? kullanimHakkiDosyaAdi { get; set; }
        public int? kanitDosyaId { get; set; }
        public string? kanitDosyaAdi { get; set; }

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

            bool kiraTahsisBilgisiGerekli =
                yatirimYeriStatusu == enumUygulamaAdresiYatirimYeriStatusu.Kira ||
                yatirimYeriStatusu == enumUygulamaAdresiYatirimYeriStatusu.Tahsis ||
                yatirimYeriStatusu == enumUygulamaAdresiYatirimYeriStatusu.IrtifakHakki ||
                yatirimYeriStatusu == enumUygulamaAdresiYatirimYeriStatusu.OrganizeSanayi_IhtisasAlaniTahsisi;

            if (kiraTahsisBilgisiGerekli && !kiraTahsisBitisTarihi.HasValue)
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

