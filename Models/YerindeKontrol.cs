namespace TarimDonusum.Models
{
    /// <summary>
    /// Sözleşme öncesi yerinde kontrol (DB 1201 "Yerinde Kontroller İçin Kontrol Listesi").
    /// Excel sayfaları: GB (Genel Bölüm), 1 (adres/GPS), 1.1 (yapılar), 1.2 (makine-ekipman), 2 (yatırımın gerçekleştirilmesi),
    /// 3 (diğer bulgular), 4 (ek dokümanlar), 5 (fotoğraf/video), 6 (çalışma dokümanı), 7 (yerinde kontrol özeti).
    /// Bir başvuru için birden fazla yerinde kontrol (ziyaret) açılabilir.
    /// </summary>
    public enum enumYerindeKontrolDurum
    {
        Taslak = 1,
        Tamamlandi = 2
    }

    public class YerindeKontrol
    {
        public const string FormKodu = "1201";

        public int id { get; set; }
        public int basvuruId { get; set; }
        /// <summary>Aynı başvurudaki yerinde kontrol sırası (1, 2, ...).</summary>
        public int siraNo { get; set; }
        /// <summary>Formdaki "1201/..." seri numarası.</summary>
        public string seriNo { get; set; } = "";
        public enumYerindeKontrolDurum durum { get; set; } = enumYerindeKontrolDurum.Taslak;

        // GB - Bölüm I: Genel Bölüm
        public DateTime? baslangicTarihi { get; set; }
        public DateTime? bitisTarihi { get; set; }
        public List<YerindeKontrolHazirBulunan> hazirBulunanlar { get; set; } = new();
        public int? uzman1KullaniciId { get; set; }
        public string uzman1Adi { get; set; } = "";
        public string uzman2Adi { get; set; } = "";
        public string birimAmiriAdi { get; set; } = "";

        // Sayfa 1.1 ve 1.2 alt yorumları
        public string yapiYorumu { get; set; } = "";
        public string makineYorumu { get; set; } = "";
        // Sayfa 3 - Kısım 3: Diğer bulgular
        public string digerBulgular { get; set; } = "";
        // Sayfa 6 - Kısım 6: Çalışma dokümanı
        public int? calismaKagidiSayisi { get; set; }
        public string calismaDokumaniNotu { get; set; } = "";
        // Sayfa 7 - Kısım 7: Yerinde kontrol özeti
        public bool? bildirimYapildiMi { get; set; }
        public DateTime? bildirimTarihi { get; set; }
        public List<string> ozetBulgular { get; set; } = new();
        public bool? basvuruSahibiKatiliyorMu { get; set; }
        public string basvuruSahibiYorumu { get; set; } = "";

        public int olusturanKullaniciId { get; set; }
        public DateTime olusturmaTarihi { get; set; }
        public DateTime? guncellemeTarihi { get; set; }

        public List<YerindeKontrolAdres> adresler { get; set; } = new();
        public List<YerindeKontrolYapi> yapilar { get; set; } = new();
        public List<YerindeKontrolMakine> makineler { get; set; } = new();
        public List<YerindeKontrolSoru> sorular { get; set; } = new();
        public List<YerindeKontrolBelge> belgeler { get; set; } = new();
        public List<YerindeKontrolFotograf> fotograflar { get; set; } = new();
    }

    /// <summary>GB: Yerinde kontrol sırasında hazır bulunan kişi.</summary>
    public class YerindeKontrolHazirBulunan
    {
        public string adSoyad { get; set; } = "";
        public string telefon { get; set; } = "";
    }

    /// <summary>Sayfa 1 - Tablo 1: Başvurudaki uygulama adresi ile yerinde görülen adresin doğrulanması (adres başına bir satır); GPS doğrulaması konumlardadır.</summary>
    public class YerindeKontrolAdres
    {
        public int id { get; set; }
        public int yerindeKontrolId { get; set; }
        public int? uygulamaAdresiId { get; set; }
        public int siraNo { get; set; }
        /// <summary>Başvuru formundaki uygulama adresi ile yatırımın gerçekleştirileceği adres aynı mı?</summary>
        public bool? adresAyniMi { get; set; }
        /// <summary>Adresteki değer zinciri aşamaları (Üretim, İşleme/İleri İşleme, Depolama ve Soğuk Zincir, Lojistik/Pazarlama ve Diğer).</summary>
        public string degerZinciriAsamalari { get; set; } = "";
        /// <summary>Yerinde denetlenen adres başvurudakinden farklıysa gözlemlenen adres ve notlar.</summary>
        public string notlar { get; set; } = "";
        public List<YerindeKontrolKonum> konumlar { get; set; } = new();
    }

    /// <summary>
    /// Sayfa 1 - Tablo 1 GPS koordinatları: adresin her konumu (ada/parsel) için ayrı doğrulama.
    /// Beyan edilen en düşük / en yüksek koordinatlar başvurudaki konumdan gelir; burada uzmanın cevabı ve ölçülenler tutulur.
    /// </summary>
    public class YerindeKontrolKonum
    {
        public int id { get; set; }
        public int yerindeKontrolAdresId { get; set; }
        /// <summary>Başvurudaki konum (BasvuruUygulamaAdresiKonum); başvuruda konumu olmayan adreste boştur.</summary>
        public int? adresKonumId { get; set; }
        /// <summary>Uzmanın "konum (GPS) bilgileri doğru mu?" cevabı; Hayır ise ölçülen koordinatlar girilir.</summary>
        public bool? konumDogruMu { get; set; }
        public enumKoordinatTuru koordinatTuru { get; set; } = enumKoordinatTuru.ITRF96;
        public int? dilimOrtaMeridyen { get; set; }
        public decimal? olculenEnDusukY { get; set; }
        public decimal? olculenEnDusukX { get; set; }
        public decimal? olculenEnYuksekY { get; set; }
        public decimal? olculenEnYuksekX { get; set; }
        public string notlar { get; set; } = "";
    }

    /// <summary>Sayfa 1.1 - Tablo 2.1: Riskli mevcut varlıkların / teknik projedeki yapıların doğrulanması.</summary>
    public class YerindeKontrolYapi
    {
        public int id { get; set; }
        public int yerindeKontrolId { get; set; }
        /// <summary>Başvurudaki bina kaydı; yerinde tespit edilen ek yapılarda boştur.</summary>
        public int? binaId { get; set; }
        public int siraNo { get; set; }
        public string yapiAdi { get; set; } = "";
        /// <summary>Uzmanın "beyan edilen bilgiler doğru mu?" cevabı; Hayır ise bulgu alanları girilir.</summary>
        public bool? bilgilerDogruMu { get; set; }
        public string mevcutDurum { get; set; } = "";
        public string kullanimAmaci { get; set; } = "";
        /// <summary>Uygun harcama (UHL alt kodu); Tanımlar > Uygun Harcama Listesi.</summary>
        public int? uygunHarcamaId { get; set; }
        public decimal? uzunlukBulgu { get; set; }
        public decimal? genislikBulgu { get; set; }
        public decimal? alanBeyan { get; set; }
        public decimal? alanBulgu { get; set; }
        public string ekBulgular { get; set; } = "";
        public string fotografNo { get; set; } = "";
    }

    /// <summary>Sayfa 1.2 - Tablo 2.2: Mevcut / teknik projedeki makine ve ekipmanın doğrulanması (beyan ve yerinde kontrol bulgusu çiftleri).</summary>
    public class YerindeKontrolMakine
    {
        public int id { get; set; }
        public int yerindeKontrolId { get; set; }
        /// <summary>Başvurudaki makine kaydı; yerinde tespit edilen ek ekipmanlarda boştur.</summary>
        public int? makineId { get; set; }
        public int siraNo { get; set; }
        public string mevcutDurum { get; set; } = "";
        /// <summary>Uzmanın "beyan edilen bilgiler doğru mu?" cevabı; Hayır ise her alan için karar (doğru / değiştirilmeli / kaldırılmalı) verilir.</summary>
        public bool? bilgilerDogruMu { get; set; }
        public bool riskliMevcutVarlik { get; set; }
        public bool projedeKullanilacak { get; set; }
        public string makineAdi { get; set; } = "";
        public int? uygunHarcamaId { get; set; }
        // Kontrol başlatıldığındaki beyanlar (başvurudaki makine silinirse bunlar kullanılır).
        public decimal? miktarBeyan { get; set; }
        public string markaModelBeyan { get; set; } = "";
        public string seriNoBeyan { get; set; } = "";
        public string kullanimAmaciBeyan { get; set; } = "";
        public string teknikOzelliklerBeyan { get; set; } = "";
        public string fotografNo { get; set; } = "";
        /// <summary>"Bilgiler doğru mu?" Hayır ise alan bazındaki kararlar.</summary>
        public List<YerindeKontrolMakineAlan> alanlar { get; set; } = new();
    }

    public enum enumYerindeKontrolAlanKarar
    {
        Dogru = 1,
        Degistirilmeli = 2,
        Kaldirilmali = 3
    }

    /// <summary>Makinenin bir alanı (makinenin kendisi, marka, miktar, teknik özellik...) için uzman kararı.</summary>
    public class YerindeKontrolMakineAlan
    {
        public int id { get; set; }
        public int yerindeKontrolMakineId { get; set; }
        /// <summary>Makine, Miktar, Marka, Model, SeriNo, KullanimAmaci, Kapasite veya Teknik:{özellik id}.</summary>
        public string alanKodu { get; set; } = "";
        public string alanAdi { get; set; } = "";
        public string beyanDeger { get; set; } = "";
        public enumYerindeKontrolAlanKarar karar { get; set; } = enumYerindeKontrolAlanKarar.Dogru;
        /// <summary>Değiştirilmeli: yerinde tespit edilen doğru değer.</summary>
        public string yeniDeger { get; set; } = "";
        public string aciklama { get; set; } = "";
    }

    /// <summary>Bir makinenin doğrulanacak alanı: kod, ekranda görünen ad ve başvurudaki beyan.</summary>
    public sealed record YerindeKontrolMakineAlanTanimi(string Kod, string Ad, string Beyan, bool Sayisal = false);

    public static class YerindeKontrolMakineAlanlari
    {
        public const string Makine = "Makine";
        public const string Miktar = "Miktar";
        public const string TeknikOnEki = "Teknik:";

        public static string KararAdi(enumYerindeKontrolAlanKarar karar) => karar switch
        {
            enumYerindeKontrolAlanKarar.Degistirilmeli => "Değiştirilmeli",
            enumYerindeKontrolAlanKarar.Kaldirilmali => "Kaldırılmalı",
            _ => "Bilgiler doğru"
        };

        /// <summary>
        /// Makinenin doğrulanacak alanları: makinenin kendisi, miktar, marka, model, seri no, kullanım amacı, kapasite ve
        /// başvurudaki her teknik özellik. Başvurudaki makine bulunamazsa kontrol başlatılırken alınan beyanlar kullanılır.
        /// </summary>
        public static List<YerindeKontrolMakineAlanTanimi> Olustur(YerindeKontrolMakine km, BasvuruMakine? bm)
        {
            string Sayi(decimal? x) => x?.ToString("#,##0.###", System.Globalization.CultureInfo.GetCultureInfo("tr-TR")) ?? "";
            if (bm == null)
                return
                [
                    new(Makine, "Makine / ekipman", km.makineAdi),
                    new(Miktar, "Miktar", Sayi(km.miktarBeyan), true),
                    new("MarkaModel", "Marka / model", km.markaModelBeyan),
                    new("SeriNo", "Seri numarası", km.seriNoBeyan),
                    new("KullanimAmaci", "Kullanım amacı (işlevi)", km.kullanimAmaciBeyan),
                    new("TeknikOzellikler", "Teknik özellikler", km.teknikOzelliklerBeyan)
                ];

            List<YerindeKontrolMakineAlanTanimi> liste =
            [
                new(Makine, "Makine / ekipman", bm.ad),
                new(Miktar, "Miktar", $"{Sayi(bm.miktar)} {bm.birim}".Trim(), true),
                new("Marka", "Marka", bm.marka),
                new("Model", "Model", bm.model),
                new("SeriNo", "Seri numarası", km.seriNoBeyan),
                new("KullanimAmaci", "Kullanım amacı (işlevi)", bm.kullanimAmaci),
                new("Kapasite", "Kapasite özellikleri", bm.kapasiteOzellikleri)
            ];
            foreach (BasvuruMakineOzellik o in bm.teknikOzellikler.OrderBy(x => x.siraNo))
                liste.Add(new($"{TeknikOnEki}{o.id}", $"Teknik özellik: {o.baslik}", o.aciklamaAsgariGereklilik));
            return liste;
        }
    }

    /// <summary>Sayfa 2 - Kısım 2: Yatırımın gerçekleştirilmesi soruları (3.1 yapım, 3.2 ekipman) ve kontrol tablosu bulguları.</summary>
    public class YerindeKontrolSoru
    {
        public int id { get; set; }
        public int yerindeKontrolId { get; set; }
        public string soruKodu { get; set; } = "";
        /// <summary>Evet / Hayır.</summary>
        public string cevap { get; set; } = "";
        public string bulgu { get; set; } = "";
        public string fotografNo { get; set; } = "";
        /// <summary>Kontrol tablosundaki Doğru / Yanlış işareti.</summary>
        public bool? dogruMu { get; set; }
        public string referans { get; set; } = "";
        public string yorum { get; set; } = "";
    }

    /// <summary>Sayfa 4 - Kısım 4: Ek dokümanlar.</summary>
    public class YerindeKontrolBelge
    {
        public int id { get; set; }
        public int yerindeKontrolId { get; set; }
        public int siraNo { get; set; }
        public DateTime? verilisTarihi { get; set; }
        public DateTime? gecerlilikTarihi { get; set; }
        public string belgeNo { get; set; } = "";
        public string konu { get; set; } = "";
        public int? dosyaId { get; set; }
        public string dosyaAdi { get; set; } = "";
    }

    /// <summary>Sayfa 5 - Kısım 5: Video kayıtları ve fotoğraflar.</summary>
    public class YerindeKontrolFotograf
    {
        public int id { get; set; }
        public int yerindeKontrolId { get; set; }
        /// <summary>Fotoğraf / video kayıt numarası; diğer tablolardaki "Fotoğraf No" alanları buna atıf yapar.</summary>
        public string kayitNo { get; set; } = "";
        public string neyeAit { get; set; } = "";
        public string notlar { get; set; } = "";
        public int? dosyaId { get; set; }
        public string dosyaAdi { get; set; } = "";
    }

    public sealed record YerindeKontrolSoruTanimi(string Kod, string Soru, string KontrolMetodolojisi, string Aciklama, string Referans);

    /// <summary>Kısım 2 sorularının sabit metinleri (DB 1201 V1.0.00).</summary>
    public static class YerindeKontrolTanimlari
    {
        public static readonly IReadOnlyList<YerindeKontrolSoruTanimi> Sorular =
        [
            new("3.1", "Yapım/genişletme/modernizasyon henüz başlamamıştır. (Başvuru paketinde belirtilen uygun olmayan harcamalar hariç)",
                "Görsel Kontrol – Fotoğraf Çekimi",
                "1. Yapım/genişletme/modernizasyon işlerinin başlamadığını doğrulamak için (vaziyet planı ve mimari proje ile bulgular karşılaştırılır) yatırım sahası görsel olarak kontrol edilir ve bulguları desteklemek için fotoğraf çekilir.\n" +
                "2. Yapım/genişletme/modernizasyondan sonra değişecek olan bina/sahanın her bölümünün fotoğrafı yapım/genişletme/modernizasyon öncesinde mevcut durumun belirlenmesi için çekilir ve daha sonraki muhtemel karşılaştırmalarda kullanılır. Yatırım alanında bina veya yapı olmadığı durumlarda, fotoğraf çekilen noktanın GPS koordinatları ölçülür ve Fotoğrafa/Kayda Ait Tanıtıcı İlave Bilgi ve Notlar bölümüne Y ve X değerleri yazılır.\n" +
                "3. YK Bulguları sütununda sahanın mevcut durumu tanımlanır.",
                "Başvuru Formu / Ek A9"),
            new("3.2", "Ekipman alımı ve montajı henüz başlamamıştır. (Başvuru paketinde belirtilen uygun olmayan harcamalar hariç)",
                "Görsel Kontrol – Muhasebe Kayıtları – Fotoğraf Çekimi",
                "1. Bu tür ekipmanların varlığını tespit etmek için görsel kontrol yapılır ve aynı zamanda fotoğraf çekilir, eğer mümkünse makine yerleşim planına göre yatırım alanı kontrol edilir. Böylece ekipman alımının başlangıcından önce sahanın mevcut durumu kayıt altına alınır ve bunlar daha sonraki muhtemel karşılaştırmalarda kullanılır.\n" +
                "2. YK Bulguları ve Muhasebe Kayıtları sütununda sahanın mevcut durumu tanımlanır.",
                "Başvuru Formu Ek A10")
        ];

        public static readonly IReadOnlyList<string> DegerZinciriAsamalari = ["Üretim", "İşleme/İleri İşleme", "Depolama ve Soğuk Zincir", "Lojistik, Pazarlama ve Diğer"];

        public const string BildirimMetni = "Yerinde kontrolün tarihi, yerinde kontrol sırasında yatırım adresinde hazır bulunulması gerektiği, gerektiğinde muhasebe kayıtlarının bulunduğu muhasebe bürosuna gidilebileceği ve yerinde kontrol sırasında başvuru sahibi tarafından bulundurulması gerekenler (hizmet araçları, makineler, muhasebe kayıtları vs.) telefon görüşmesi sırasında bildirim amaçlı başvuru sahibine iletilmiştir.";
        public const string OzetUyarisi = "Bu bulgular yerinde kontrol esnasında yapılan ölçümler ve gözlemlere dayanmaktadır, nihai sonuç/karar hükmü değildir.";
        public const string ItirazNotu = "İtiraz için başvuru TKDK Merkez'e, yerinde kontrol ziyaretinden itibaren 10 gün içinde yapılmalıdır.";

        /// <summary>Yerinde kontrol yalnızca incelemedeki veya kabul edilmiş başvurular için yapılır (sözleşme öncesi).</summary>
        public static bool KontrolEdilebilir(Basvuru b) => b.kayitTuru == enumBasvuruKayitTuru.Basvuru
            && b.basvuruFirma.siraNo == 0
            && b.durum is enumBasvuruDurum.BasvuruIncelemeDurumu or enumBasvuruDurum.KabulEdildiDurumu;
    }

    public sealed record YerindeKontrolBolumu(string Kod, string Baslik, string Aciklama);

    /// <summary>Yerinde kontrol formunun bölümleri; Excel'deki sayfa adlarıyla aynı kodları kullanır.</summary>
    public static class YerindeKontrolBolumleri
    {
        public static readonly IReadOnlyList<YerindeKontrolBolumu> Tum =
        [
            new("GB", "Genel Bölüm", "Başvuru sahibi, yatırım ve yerinde kontrol bilgileri"),
            new("1", "Adres ve GPS Doğrulaması", "Tablo 1: Uygulama adresi ve GPS koordinatları"),
            new("1.1", "Yapıların Doğrulanması", "Tablo 2.1: Riskli mevcut varlıklar / teknik projedeki yapılar"),
            new("1.2", "Makine-Ekipmanın Doğrulanması", "Tablo 2.2: Mevcut / teknik projedeki makine ve ekipman"),
            new("2", "Yatırımın Gerçekleştirilmesi", "Kısım 2: Yapım ve ekipman alımının başlamadığının doğrulanması"),
            new("3", "Diğer Bulgular", "Kısım 3: Diğer bulgular ve imzalar"),
            new("4", "Ek Dokümanlar", "Kısım 4: Ek dokümanlar"),
            new("5", "Video Kayıtları ve Fotoğraflar", "Kısım 5: Fotoğraf ve video kayıtları"),
            new("6", "Çalışma Dokümanı", "Kısım 6: Çalışma kağıtları"),
            new("7", "Yerinde Kontrol Özeti", "Kısım 7: Bulgular ve başvuru sahibinin görüşü")
        ];
    }

    /// <summary>Yerinde Kontrol listesinde bir başvuru satırı ve o başvurunun yerinde kontrolleri.</summary>
    public class YerindeKontrolListeSatiri
    {
        public Basvuru basvuru { get; set; } = new();
        public List<YerindeKontrol> kontroller { get; set; } = new();
    }

    /// <summary>Doğrulama bölümlerinin (1, 1.1, 1.2, 2) tek seferde kaydı; yalnızca ilgili bölümün listesi dolu gelir.</summary>
    public class YerindeKontrolBolumKayit
    {
        public int id { get; set; }
        public string bolum { get; set; } = "";
        public List<YerindeKontrolAdres> adresler { get; set; } = new();
        public List<YerindeKontrolYapi> yapilar { get; set; } = new();
        public List<YerindeKontrolMakine> makineler { get; set; } = new();
        public List<YerindeKontrolSoru> sorular { get; set; } = new();
        public List<YerindeKontrolBelge> belgeler { get; set; } = new();
        public List<YerindeKontrolFotograf> fotograflar { get; set; } = new();
        /// <summary>1.1 / 1.2 yorumları; 3 (diğer bulgular) ve 6 (çalışma dokümanı) bölümlerinde metin kutusunun içeriği.</summary>
        public string yorum { get; set; } = "";
        public int? calismaKagidiSayisi { get; set; }
    }

    /// <summary>Kısım 4 (ek dokümanlar, PDF) ve Kısım 5 (fotoğraf / video) dosya türleri.</summary>
    public static class YerindeKontrolDosyaTurleri
    {
        public const string Belge = "Belge";
        public const string Fotograf = "Fotograf";

        public static readonly string[] BelgeUzantilari = [".pdf"];
        public static readonly string[] ResimUzantilari = [".jpg", ".jpeg", ".png"];
        public static readonly string[] VideoUzantilari = [".mp4", ".mov", ".webm", ".avi"];

        public static string Uzanti(string? dosyaAdi) => System.IO.Path.GetExtension(dosyaAdi ?? "").ToLowerInvariant();
        public static bool VideoMu(string? dosyaAdi) => VideoUzantilari.Contains(Uzanti(dosyaAdi));

        public static string IcerikTuru(string? dosyaAdi) => Uzanti(dosyaAdi) switch
        {
            ".pdf" => "application/pdf",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".mp4" => "video/mp4",
            ".mov" => "video/quicktime",
            ".webm" => "video/webm",
            ".avi" => "video/x-msvideo",
            _ => "application/octet-stream"
        };
    }

    public class YerindeKontrolDosyaSil
    {
        public int id { get; set; }
        public string tur { get; set; } = "";
        public int satirId { get; set; }
    }

    /// <summary>Yerinde kontrol formu: kontrol kaydı ve beyan bilgilerinin geldiği başvuru.</summary>
    public class YerindeKontrolFormModel
    {
        public YerindeKontrol kontrol { get; set; } = new();
        public Basvuru basvuru { get; set; } = new();
        public string bolum { get; set; } = "GB";
        public bool duzenlenebilir { get; set; }
    }
}
