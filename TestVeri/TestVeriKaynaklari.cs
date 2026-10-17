using System.Globalization;
using System.Text;
using TarimDonusum.Araclar;

namespace TarimDonusum.TestVeri;

/// <summary>
/// TEST: Rastgele test verisi üretiminde kullanılan isimler, metinler ve yardımcılar.
/// Test süreci bitince TestVeri klasörü ile birlikte silinebilir; başka kod buna bağlı değildir.
/// </summary>
public sealed class TestVeriKaynaklari
{
    private readonly Random _r = new();
    public static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");

    public int Sayi(int min, int maxDahil) => _r.Next(min, maxDahil + 1);
    public decimal Tutar(decimal min, decimal max, int yuvarla = 1) => Math.Round((min + (decimal)_r.NextDouble() * (max - min)) / yuvarla) * yuvarla;
    public bool Olasilik(double oran) => _r.NextDouble() < oran;
    public T Sec<T>(IReadOnlyList<T> liste) => liste[_r.Next(liste.Count)];
    public List<T> Karistir<T>(IEnumerable<T> liste) => liste.OrderBy(_ => _r.Next()).ToList();
    public List<T> AltKume<T>(IReadOnlyList<T> liste, int min, int max) => Karistir(liste).Take(Math.Min(liste.Count, Sayi(min, max))).ToList();

    /// <summary>Makine adedi: çoğunlukla 1, bazen 2-5, ara sıra 6-15.</summary>
    public int MakineAdedi() => _r.NextDouble() switch { < 0.55 => 1, < 0.85 => Sayi(2, 5), _ => Sayi(6, 15) };

    public static readonly string[] KadinAdlari = ["Ayşe", "Fatma", "Elif", "Zeynep", "Merve", "Esra", "Derya", "Hatice", "Selin", "Büşra", "Gizem", "Sevgi", "Emine", "Aslı", "Ceren"];
    public static readonly string[] ErkekAdlari = ["Mehmet", "Ahmet", "Mustafa", "Ali", "Hüseyin", "Hasan", "İbrahim", "Murat", "Emre", "Burak", "Serkan", "Okan", "Yusuf", "Kemal", "Cem"];
    public static readonly string[] Soyadlari = ["Yılmaz", "Kaya", "Demir", "Şahin", "Çelik", "Yıldız", "Aydın", "Öztürk", "Arslan", "Doğan", "Kılıç", "Aslan", "Çetin", "Kara", "Koç", "Kurt", "Özdemir", "Polat", "Erdoğan", "Güneş"];
    public static readonly string[] Mahalleler = ["Cumhuriyet", "Atatürk", "Yenimahalle", "Fatih", "Bahçelievler", "Esentepe", "Gazi", "Hürriyet", "Kocatepe", "Sanayi", "Organize", "Yeşilyurt"];
    public static readonly string[] Sokaklar = ["Tarım Sk.", "Bereket Cd.", "Çınar Sk.", "Lale Sk.", "Fabrika Cd.", "Sanayi Cd.", "Hasat Sk.", "Bağlar Cd.", "Pınar Sk.", "Değirmen Sk."];
    public static readonly string[] Unvanlar = ["Genel Müdür", "Yönetim Kurulu Başkanı", "Finans Müdürü", "Proje Koordinatörü", "Üretim Müdürü", "Şirket Müdürü"];
    public static readonly string[] Markalar = ["Tümosan", "Hattat", "Alpler", "Özen Makine", "Başak", "Kuhn", "Lely", "GEA", "Bühler", "Tetra Pak", "Krone", "Pöttinger", "Ulusoy", "Gökhan Makine"];

    public string KadinAdi() => Sec(KadinAdlari);
    public string ErkekAdi() => Sec(ErkekAdlari);
    public string Soyadi() => Sec(Soyadlari);
    public string AdSoyad(bool kadin) => $"{(kadin ? KadinAdi() : ErkekAdi())} {Soyadi()}";
    public string Telefon() => $"05{Sayi(30, 59)} {Sayi(100, 999)} {Sayi(10, 99)} {Sayi(10, 99)}";
    public string Adres(string ilce, string il) => $"{Sec(Mahalleler)} Mah. {Sec(Sokaklar)} No:{Sayi(1, 150)} {ilce}/{il}";
    public string Eposta(string ad) => $"{Ascii(ad).ToLowerInvariant().Replace(' ', '.')}{Sayi(1, 99)}@ornektest.com.tr";
    public DateTime DogumTarihi(int minYas, int maxYas) => DateTime.Today.AddYears(-Sayi(minYas, maxYas)).AddDays(-Sayi(0, 360));

    public static string Ascii(string metin) => new(metin
        .Replace('ç', 'c').Replace('ğ', 'g').Replace('ı', 'i').Replace('ö', 'o').Replace('ş', 's').Replace('ü', 'u')
        .Replace('Ç', 'C').Replace('Ğ', 'G').Replace('İ', 'I').Replace('Ö', 'O').Replace('Ş', 'S').Replace('Ü', 'U')
        .Where(c => char.IsLetterOrDigit(c) || c == ' ').ToArray());

    /// <summary>Geçerli (algoritmaya uygun) TCKN üretir.</summary>
    public string Tckn()
    {
        while (true)
        {
            int[] d = new int[11];
            d[0] = Sayi(1, 9);
            for (int i = 1; i < 9; i++) d[i] = Sayi(0, 9);
            int tek = d[0] + d[2] + d[4] + d[6] + d[8], cift = d[1] + d[3] + d[5] + d[7];
            d[9] = ((tek * 7 - cift) % 10 + 10) % 10;
            d[10] = d.Take(10).Sum() % 10;
            string tckn = string.Concat(d);
            if (OrtakFonksiyonlar.TCKNGecerliMi(tckn)) return tckn;
        }
    }

    /// <summary>Geçerli (algoritmaya uygun) VKN üretir.</summary>
    public string Vkn()
    {
        while (true)
        {
            int[] d = Enumerable.Range(0, 9).Select(_ => Sayi(0, 9)).ToArray();
            int toplam = 0;
            for (int i = 0; i < 9; i++)
            {
                int basamak = (d[i] + 9 - i) % 10;
                toplam += basamak == 9 ? 9 : basamak * (1 << (9 - i)) % 9;
            }
            string vkn = string.Concat(d) + ((10 - toplam % 10) % 10);
            if (OrtakFonksiyonlar.VKNGecerliMi(vkn)) return vkn;
        }
    }

    /// <summary>Değer zinciri adına göre ürün, girdi ve faaliyet örnekleri.</summary>
    public sealed record Sektor(string Anahtar, string[] Urunler, string[] Girdiler, string Faaliyet, string Tesis);

    public static readonly Sektor[] Sektorler =
    [
        new("Arıcılık", ["Süzme çiçek balı", "Petek bal", "Polen", "Propolis ekstraktı", "Arı sütü"], ["Ham bal", "Cam kavanoz", "Etiket", "Polen (ham)"], "bal süzme, dolum ve paketleme", "Bal işleme ve paketleme tesisi"),
        new("Baklagil", ["Nohut unu", "Kırmızı mercimek", "Bezelye proteini", "Kavrulmuş nohut", "Bulgur-mercimek karışımı"], ["Nohut", "Mercimek", "Kuru bezelye", "Ambalaj torbası"], "baklagil temizleme, kırma, öğütme ve paketleme", "Baklagil işleme tesisi"),
        new("Domates", ["Domates salçası", "Domates püresi", "Kurutulmuş domates", "Domates suyu", "Doğranmış domates konservesi"], ["Taze domates", "Tuz", "Cam kavanoz", "Teneke kutu"], "domates yıkama, ayıklama, pişirme, konsantre etme ve dolum", "Domates işleme tesisi"),
        new("Kanatlı", ["Bütün piliç", "Piliç but", "Piliç göğüs fileto", "Yumurta (koli)", "Piliç kanat"], ["Canlı piliç", "Kanatlı yemi", "Ambalaj köpüğü", "Streç film"], "kesim, parçalama, soğutma ve paketleme", "Kanatlı kesim ve işleme tesisi"),
        new("Kırmızı Et", ["Dana karkas", "Kuzu karkas", "Kıyma", "Sucuk", "Pastırma"], ["Canlı büyükbaş", "Canlı küçükbaş", "Baharat", "Vakum ambalaj"], "kesim, parçalama, olgunlaştırma ve paketleme", "Kırmızı et kesim ve işleme tesisi"),
        new("Mantar", ["Kültür mantarı", "İstiridye mantarı", "Mantar konservesi", "Kurutulmuş mantar"], ["Mantar kompostu", "Misel", "Torf", "Ambalaj kasası"], "kompost hazırlama, yetiştirme, hasat ve paketleme", "Mantar üretim tesisi"),
        new("Meyve ve Sebze", ["Elma suyu konsantresi", "Kurutulmuş kayısı", "Dondurulmuş sebze", "Meyve püresi", "Taze paketli sebze"], ["Taze elma", "Kayısı", "Karışık sebze", "Karton koli"], "yıkama, kalibrasyon, kurutma, dondurma ve paketleme", "Meyve-sebze işleme ve paketleme tesisi"),
        new("Sert Kabuklu", ["İç fındık", "Kavrulmuş fındık", "Fındık ezmesi", "İç ceviz", "Badem içi"], ["Kabuklu fındık", "Kabuklu ceviz", "Badem", "Vakumlu ambalaj"], "kırma, ayıklama, kavurma ve paketleme", "Sert kabuklu meyve işleme tesisi"),
        new("Su Ürünleri", ["Levrek fileto", "Çipura", "Alabalık", "Dondurulmuş balık", "Füme somon"], ["Canlı balık", "Balık yemi", "Buz", "Strafor kutu"], "yetiştiricilik, işleme, dondurma ve paketleme", "Su ürünleri işleme tesisi"),
        new("Süt", ["Beyaz peynir", "Kaşar peyniri", "Yoğurt", "Ayran", "Tereyağı"], ["Çiğ inek sütü", "Peynir mayası", "Tuz", "Plastik kova"], "süt kabul, pastörizasyon, peynir ve yoğurt üretimi", "Süt işleme tesisi"),
        new("Tahıl", ["Buğday unu", "Bulgur", "Makarna", "Kepek", "İrmik"], ["Buğday", "Arpa", "Çuval", "Ambalaj filmi"], "temizleme, tavlama, öğütme ve paketleme", "Un ve bulgur fabrikası"),
        new("Tıbbi ve Aromatik", ["Kekik yağı", "Kurutulmuş kekik", "Lavanta yağı", "Adaçayı (paket)", "Bitki çayı karışımı"], ["Taze kekik", "Lavanta", "Adaçayı", "Cam şişe"], "kurutma, distilasyon ve paketleme", "Aromatik bitki işleme tesisi"),
        new("Yağlı Tohum", ["Ayçiçek yağı", "Kanola yağı", "Küspe", "Soğuk sıkım yağ"], ["Ayçiçeği tohumu", "Kanola tohumu", "PET şişe", "Koli"], "tohum temizleme, presleme, rafinasyon ve dolum", "Yağ fabrikası"),
        new("Yem", ["Süt yemi (pelet)", "Besi yemi", "Kanatlı yemi", "Buzağı başlangıç yemi"], ["Mısır", "Arpa", "Soya küspesi", "Vitamin-mineral premiks"], "hammadde kabul, kırma, karıştırma, peletleme ve paketleme", "Yem fabrikası"),
        new("Zeytin", ["Natürel sızma zeytinyağı", "Siyah sofralık zeytin", "Yeşil kırma zeytin", "Zeytin ezmesi"], ["Zeytin", "Tuz", "Cam şişe", "Teneke"], "zeytin yıkama, sıkım, salamura ve dolum", "Zeytinyağı ve sofralık zeytin tesisi"),
        new("", ["Kurutulmuş meyve", "Organik reçel", "Glütensiz un", "Bitkisel süt", "Meyve cipsi"], ["Taze meyve", "Şeker", "Cam kavanoz", "Karton kutu"], "işleme, kurutma ve paketleme", "Gıda işleme tesisi")
    ];

    public static Sektor SektorBul(string degerZinciriAdi) =>
        Sektorler.FirstOrDefault(x => x.Anahtar.Length > 0 && degerZinciriAdi.Contains(x.Anahtar, StringComparison.CurrentCultureIgnoreCase))
        ?? Sektorler[^1];

    public static readonly string[] YedekMakineler = ["Paketleme Makinesi", "Konveyör", "Soğutma Grubu", "Yıkama Makinesi", "Kurutma Fırını", "Dolum Makinesi", "Forklift (Elektrikli)", "Jeneratör",
        "Kompresör", "Etiketleme Makinesi", "Transpalet", "Metal Dedektörü", "Kodlama Makinesi", "Streçleme Makinesi", "Paletleme Makinesi", "Otomatik Tartım Sistemi", "CIP Sistemi",
        "Kalibrasyon (Boylama) Makinesi", "Vakumlu Paketleme Makinesi", "Su Arıtma/Hazırlama Ünitesi", "Buhar Kazanı", "Endüstriyel Tip Soğutma Grubu", "Hassas Terazi", "Mikser", "Proses Tankı"];
    /// <summary>Her ürünün 10 girdisini tamamlamak için sektörden bağımsız yardımcı madde ve işletme malzemeleri.</summary>
    public static readonly string[] GenelGirdiler = ["Ambalaj filmi", "Karton koli", "Etiket", "Palet", "Streç film", "Koruyucu katkı maddesi", "Temizlik ve dezenfeksiyon kimyasalı",
        "Su arıtma kimyasalı", "Laboratuvar sarf malzemesi", "Yardımcı katkı maddesi", "Kapak", "Ambalaj bandı", "Kalite kontrol kiti", "Koruyucu kıyafet ve eldiven"];
    public static readonly string[] Binalar = ["Üretim binası", "Soğuk hava deposu", "Ham madde deposu", "Mamul deposu", "İdari bina", "Laboratuvar binası", "Kazan dairesi", "Paketleme binası"];
    public static readonly string[] Mahaller = ["Üretim alanı", "Depo", "Ofis", "Soyunma odası", "Laboratuvar", "Teknik oda", "Yükleme alanı", "WC/Lavabo"];
    public static readonly string[] IstihdamBirimleri = ["Üretim", "Paketleme", "Kalite Kontrol", "Depo ve Lojistik", "İdari İşler", "Bakım-Onarım", "Satış ve Pazarlama"];
    public static readonly string[] Gorevler = ["Makine operatörü", "Üretim işçisi", "Gıda mühendisi", "Kalite kontrol uzmanı", "Depo sorumlusu", "Forklift operatörü", "Muhasebe uzmanı", "Satış temsilcisi", "Bakım teknisyeni"];

    public string Cumle(params string[] parcalar) => string.Join(" ", parcalar.Where(x => !string.IsNullOrWhiteSpace(x)));

    /// <summary>Türkçe binlik ayraçlı tamsayı metni (ör. 12.345.678).</summary>
    public static string ParaMetni(decimal deger) => Math.Round(deger).ToString("#,##0", Tr);

    /// <summary>İçinde metin bulunan geçerli, küçük bir PDF dosyası.</summary>
    public static byte[] Pdf(string baslik)
    {
        string metin = Ascii(baslik).Replace("(", "").Replace(")", "");
        string akis = $"BT /F1 18 Tf 72 760 Td (TEST BELGESI) Tj 0 -30 Td /F1 12 Tf ({metin}) Tj 0 -20 Td (Otomatik test verisi - {DateTime.Now:dd.MM.yyyy HH:mm}) Tj ET";
        string[] nesneler =
        [
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 4 0 R /Resources << /Font << /F1 5 0 R >> >> >>",
            $"<< /Length {akis.Length} >>\nstream\n{akis}\nendstream",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>"
        ];
        StringBuilder sb = new("%PDF-1.4\n");
        List<int> konumlar = [];
        for (int i = 0; i < nesneler.Length; i++)
        {
            konumlar.Add(sb.Length);
            sb.Append($"{i + 1} 0 obj\n{nesneler[i]}\nendobj\n");
        }
        int xref = sb.Length;
        sb.Append($"xref\n0 {nesneler.Length + 1}\n0000000000 65535 f \n");
        foreach (int k in konumlar) sb.Append($"{k:D10} 00000 n \n");
        sb.Append($"trailer\n<< /Size {nesneler.Length + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF\n");
        return Encoding.ASCII.GetBytes(sb.ToString());
    }

    public static string DosyaAdi(string baslik) => Ascii(baslik).Replace(' ', '_') + ".pdf";
}
