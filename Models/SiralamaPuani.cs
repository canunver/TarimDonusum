using System.Globalization;

namespace TarimDonusum.Models;

/// <summary>Sıralama puanı koşulu: terim değeri belirli bir değere eşit mi, yoksa bir aralıkta mı.</summary>
public enum enumSiralamaKosulTuru : int
{
    Esit = 1,
    Aralik = 2
}

public enum enumSiralamaTerimTuru : int
{
    Sayi = 1,
    Yuzde = 2,
    EvetHayir = 3,
    Secim = 4
}

/// <summary>Tanımlar > Sıralama Puanı Kriterleri: konu (ör. "Coğrafi hedefleme").</summary>
public class SiralamaPuaniKonu
{
    public int id { get; set; }
    public int siraNo { get; set; }
    public string ad { get; set; } = "";
    public decimal? azamiPuan { get; set; }
    public bool aktif { get; set; } = true;
    public List<SiralamaPuaniAltKriter> altKriterler { get; set; } = new();
}

/// <summary>Konunun alt kriteri; puanı, bağlı terimin başvurudaki değerinin sağladığı koşuldan gelir.</summary>
public class SiralamaPuaniAltKriter
{
    public int id { get; set; }
    public int konuId { get; set; }
    public int siraNo { get; set; }
    public string ad { get; set; } = "";
    /// <summary><see cref="SiralamaPuaniTerimleri"/> içindeki terimin kodu; boşsa kriter uygulamadan hesaplanamaz.</summary>
    public string? terimKodu { get; set; }
    /// <summary>Ön başvuruda gösterilecek metin; {deger} terimin başvurudaki değeriyle değiştirilir. Boşsa sağlanan koşulun metni gösterilir.</summary>
    public string sonucMetni { get; set; } = "";
    public string veriKaynagi { get; set; } = "";
    public bool aktif { get; set; } = true;
    public List<SiralamaPuaniKosul> kosullar { get; set; } = new();
}

public class SiralamaPuaniKosul
{
    public int id { get; set; }
    public int altKriterId { get; set; }
    public int siraNo { get; set; }
    public string metin { get; set; } = "";
    public enumSiralamaKosulTuru kosulTuru { get; set; } = enumSiralamaKosulTuru.Aralik;
    /// <summary>Eşit koşulunda aranan değer (Evet/Hayır terimlerinde 1/0, seçim terimlerinde seçenek değeri).</summary>
    public decimal? deger { get; set; }
    public decimal? altSinir { get; set; }
    public bool altDahil { get; set; } = true;
    public decimal? ustSinir { get; set; }
    public bool ustDahil { get; set; }
    public decimal puan { get; set; }

    public bool Saglar(decimal x)
    {
        if (kosulTuru == enumSiralamaKosulTuru.Esit) return deger.HasValue && x == deger.Value;
        if (!altSinir.HasValue && !ustSinir.HasValue) return false;
        if (altSinir.HasValue && (altDahil ? x < altSinir.Value : x <= altSinir.Value)) return false;
        if (ustSinir.HasValue && (ustDahil ? x > ustSinir.Value : x >= ustSinir.Value)) return false;
        return true;
    }
}

public sealed record SiralamaTerimSecenegi(decimal deger, string ad);

/// <summary>
/// Kriterlerin bağlanabildiği, değeri başvuru verisinden hesaplanan terim. Terimler kodda tanımlanır
/// (her biri uygulamadaki bir veriden hesaplanır); koşullar ve puanlar Tanımlar sayfasından girilir.
/// Hesap birden fazla değer döndürebilir (ör. her yatırım adresi için); kriter en yüksek puanı veren değerle değerlendirilir.
/// </summary>
public sealed record SiralamaPuaniTerim(string kod, string ad, enumSiralamaTerimTuru tur, string aciklama, IReadOnlyList<SiralamaTerimSecenegi> secenekler, Func<Basvuru, IReadOnlyList<decimal>> hesapla)
{
    private static readonly CultureInfo Tr = new("tr-TR");

    public string DegerYaz(decimal x) => tur switch
    {
        enumSiralamaTerimTuru.Yuzde => "%" + x.ToString("#,##0.##", Tr),
        enumSiralamaTerimTuru.EvetHayir => x == 1 ? "Evet" : "Hayır",
        enumSiralamaTerimTuru.Secim => secenekler.FirstOrDefault(s => s.deger == x)?.ad ?? x.ToString("0.##", Tr),
        _ => x.ToString("#,##0.##", Tr)
    };
}

public static class SiralamaPuaniTerimleri
{
    private static readonly IReadOnlyList<SiralamaTerimSecenegi> EvetHayir = [new(1, "Evet"), new(0, "Hayır")];

    public static readonly IReadOnlyList<SiralamaPuaniTerim> Tum =
    [
        new("ILCE_SEGE", "Yatırım yeri ilçesinin SEGE kademesi", enumSiralamaTerimTuru.Sayi,
            "Yatırım Bilgileri'ndeki her uygulama adresinin ilçe SEGE kademesi (1–6).", [], IlceSege),
        new("SEGE46_TEDARIK_ORANI", "SEGE 4–6 ilçelerinden tedarik oranı (%)", enumSiralamaTerimTuru.Yuzde,
            "Tedarikçi Entegrasyonu'nda SEGE 4–6 ilçelerinden yapılan tedariklerin hedef yıllık tedarik değeri içindeki payı.", [], Sege46TedarikOrani),
        new("CIFTCI_90K", "90.000 Avro eşdeğeri RFF tutarı başına ilave kayıtlı çiftçi / birincil üretici", enumSiralamaTerimTuru.Sayi,
            "Tedarikçi Entegrasyonu'ndaki eklenecek kayıtlı çiftçi toplamı / (talep edilen RFF tutarı / 90.000).", [], Ciftci90k),
        new("ISTIHDAM_90K", "90.000 Avro eşdeğeri RFF tutarı başına ilave tam zamanlı eşdeğer istihdam", enumSiralamaTerimTuru.Sayi,
            "İstihdam sayfasındaki planlanan net ek tam zamanlı istihdam / (talep edilen RFF tutarı / 90.000).", [], Istihdam90k),
        new("KADIN_ISTIHDAM", "Kadın çalışan sayısı erkek çalışan sayısından fazla (son iki yılda ortalama 20'den fazla çalışan)", enumSiralamaTerimTuru.EvetHayir,
            "İstihdam sayfasındaki son iki yılın kadın / erkek tam zamanlı istihdam sayıları.", EvetHayir, KadinIstihdam),
        new("KADIN_GENC_SAHIPLIK", "Kadınların veya 40 yaş altı bireylerin sahiplik oranı (%)", enumSiralamaTerimTuru.Yuzde,
            "Ortaklık bilgilerinde sahiplik niteliği kadın ve/veya 40 yaş altı olan ortakların pay oranları toplamı.", [], KadinGencSahiplik),
        new("ORGANIZE_ALAN", "Yatırım yerinin bulunduğu organize alan", enumSiralamaTerimTuru.Secim,
            "Yatırım Bilgileri'ndeki her uygulama adresinin kümelenme / organize alan türü.",
            [
                new((int)enumKumelenmeOrganizeAlanTuru.OrganizeTarimBolgesi, "Organize Tarım Bölgesi"),
                new((int)enumKumelenmeOrganizeAlanTuru.IhtisasGidaBolgesi, "İhtisas Gıda Bölgesi"),
                new((int)enumKumelenmeOrganizeAlanTuru.OrganizeSanayiBolgesi, "Organize Sanayi Bölgesi"),
                new((int)enumKumelenmeOrganizeAlanTuru.DigerOrganizeAlan, "Diğer organize alan"),
                new((int)enumKumelenmeOrganizeAlanTuru.OrganizeAlanDisinda, "Organize alan dışında")
            ], OrganizeAlan),
        new("YENILENEBILIR_ENERJI", "Yatırım yenilenebilir enerji üretimi veya kullanımı içeriyor", enumSiralamaTerimTuru.EvetHayir,
            "Teknik Proje (Girdiler) sayfasındaki yenilenebilir enerji bilgilerinde güç / panel girilmiş olması.", EvetHayir, YenilenebilirEnerji),
        new("IHRACAT_SATIS_ORANI", "Son iki yılda ihracatın satışlara oranı (%)", enumSiralamaTerimTuru.Yuzde,
            "Mali verilerdeki son iki yılın ihracat satışları toplamı / net satışlar toplamı. Ön başvuruda ihracat bilgisi alınmıyorsa veri bulunmaz.", [], IhracatSatisOrani)
    ];

    public static SiralamaPuaniTerim? Bul(string? kod) => Tum.FirstOrDefault(x => string.Equals(x.kod, kod, StringComparison.Ordinal));

    private static decimal? Per90k(Basvuru b, decimal pay)
    {
        decimal rff = b.HesaplananTalepEdilenFinansmanTutari.GetValueOrDefault();
        return rff > 0 ? Math.Round(pay / (rff / 90000m), 4) : null;
    }

    private static IReadOnlyList<decimal> Tek(decimal? x) => x.HasValue ? [x.Value] : [];

    private static IReadOnlyList<decimal> IlceSege(Basvuru b) =>
        b.YatirimAdresleri.Select(a => int.TryParse(a.segeKademesi, out int s) && s is >= 1 and <= 6 ? (decimal?)s : null)
            .Where(x => x.HasValue).Select(x => x!.Value).Distinct().ToList();

    // Tedarikçi Entegrasyonu sayfasındaki hesapla aynı: tüm kayıtların SEGE'si ve tedarik değeri girilmiş olmalıdır.
    private static IReadOnlyList<decimal> Sege46TedarikOrani(Basvuru b)
    {
        var k = b.TedarikciEntegrasyonlari;
        if (k.Count == 0 || k.Any(x => x.segeKademesi is not (>= 1 and <= 6) || x.hedefYillikTedarikDegeri <= 0)) return [];
        decimal toplam = k.Sum(x => x.hedefYillikTedarikDegeri);
        return toplam > 0 ? [Math.Round(k.Where(x => x.segeKademesi >= 4).Sum(x => x.hedefYillikTedarikDegeri) / toplam * 100m, 2)] : [];
    }

    private static IReadOnlyList<decimal> Ciftci90k(Basvuru b) =>
        b.TedarikciEntegrasyonlari.Count == 0 ? [] : Tek(Per90k(b, b.TedarikciEntegrasyonlari.Sum(x => (decimal)x.eklenecekKayitliCiftci)));

    private static IReadOnlyList<decimal> Istihdam90k(Basvuru b) =>
        Tek(Per90k(b, b.istihdam.satirlar.Sum(x => x.netCalisanArtisi)));

    private static IReadOnlyList<decimal> KadinIstihdam(Basvuru b)
    {
        var i = b.istihdam;
        int kadin = i.oncekiYilKadin + i.sonYilKadin, erkek = i.oncekiYilErkek + i.sonYilErkek;
        if (kadin + erkek == 0) return [];
        return [(kadin + erkek) / 2m > 20 && kadin > erkek ? 1 : 0];
    }

    private static IReadOnlyList<decimal> KadinGencSahiplik(Basvuru b)
    {
        var ortaklar = b.ortaklik.ortaklar;
        if (ortaklar.Count == 0) return [];
        return [ortaklar.Where(o => o.sahiplikNiteligi is "Kadın" or "40 yaş altı" or "Her ikisi").Sum(o => o.payOrani.GetValueOrDefault())];
    }

    private static IReadOnlyList<decimal> OrganizeAlan(Basvuru b) =>
        b.YatirimAdresleri.Where(a => a.kumelenmeOrganizeAlanTuru != enumKumelenmeOrganizeAlanTuru.Tanimsiz)
            .Select(a => (decimal)(int)a.kumelenmeOrganizeAlanTuru).Distinct().ToList();

    private static IReadOnlyList<decimal> YenilenebilirEnerji(Basvuru b) =>
        [b.YatirimOnBilgileri.Any(x => x.tur is enumYatirimOnBilgiTuru.EnerjiKullanimi or enumYatirimOnBilgiTuru.KuruluGuc
            && (x.toplamGuc.GetValueOrDefault() > 0 || x.tekPanelGucu.GetValueOrDefault() > 0 || x.miktar.GetValueOrDefault() > 0)) ? 1 : 0];

    private static IReadOnlyList<decimal> IhracatSatisOrani(Basvuru b)
    {
        var m = b.mali;
        if (!m.oncekiYilIhracatSatis.HasValue && !m.sonYilIhracatSatis.HasValue) return [];
        decimal satis = m.oncekiYilNetSatis.GetValueOrDefault() + m.sonYilNetSatis.GetValueOrDefault();
        if (satis <= 0) return [];
        return [Math.Round((m.oncekiYilIhracatSatis.GetValueOrDefault() + m.sonYilIhracatSatis.GetValueOrDefault()) / satis * 100m, 2)];
    }
}

public static class SiralamaPuaniTanimSaglayici
{
    private static IReadOnlyList<SiralamaPuaniKonu> _tum = [];

    public static IReadOnlyList<SiralamaPuaniKonu> Tum => Volatile.Read(ref _tum);

    public static void Guncelle(IEnumerable<SiralamaPuaniKonu> konular) =>
        Volatile.Write(ref _tum, konular.OrderBy(x => x.siraNo).ThenBy(x => x.id).ToList());

    private static SiralamaPuaniKosul A(string metin, decimal? alt, bool altDahil, decimal? ust, bool ustDahil, decimal puan) =>
        new() { metin = metin, kosulTuru = enumSiralamaKosulTuru.Aralik, altSinir = alt, altDahil = altDahil, ustSinir = ust, ustDahil = ustDahil, puan = puan };

    private static SiralamaPuaniKosul E(string metin, decimal deger, decimal puan) =>
        new() { metin = metin, kosulTuru = enumSiralamaKosulTuru.Esit, deger = deger, puan = puan };

    private static SiralamaPuaniAltKriter K(string ad, string? terim, string sonucMetni, string veriKaynagi, params SiralamaPuaniKosul[] kosullar)
    {
        for (int i = 0; i < kosullar.Length; i++) kosullar[i].siraNo = i + 1;
        return new() { ad = ad, terimKodu = terim, sonucMetni = sonucMetni, veriKaynagi = veriKaynagi, kosullar = kosullar.ToList() };
    }

    private static SiralamaPuaniKonu Konu(int siraNo, string ad, decimal azami, params SiralamaPuaniAltKriter[] altKriterler)
    {
        for (int i = 0; i < altKriterler.Length; i++) altKriterler[i].siraNo = i + 1;
        return new() { siraNo = siraNo, ad = ad, azamiPuan = azami, altKriterler = altKriterler.ToList() };
    }

    /// <summary>Tablo boşken eklenen kriterler: "Sıralama Puanı (Ön başvuru Formu Sonrası)" Excel'i, Alternatif 1.</summary>
    public static IReadOnlyList<SiralamaPuaniKonu> Varsayilanlar =>
    [
        Konu(1, "Coğrafi hedefleme", 15,
            K("Yatırımın bulunduğu SEGE ilçesi", "ILCE_SEGE", "Yatırım SEGE {deger} ilçesinde yer alıyor", "Başvuru formundan alınır",
                E("Yatırım SEGE 4 ilçesinde yer alıyorsa", 4, 3), E("Yatırım SEGE 5 ilçesinde yer alıyorsa", 5, 5), E("Yatırım SEGE 6 ilçesinde yer alıyorsa", 6, 7.5m)),
            K("SEGE 4–6 ilçelerinden tedarik", "SEGE46_TEDARIK_ORANI", "İlgili tarımsal girdilerin {deger} kadarı SEGE 4–6 ilçelerinden tedarik ediliyor", "Tedarikçi Entegrasyon Planından alınır",
                A("İlgili tarımsal girdilerin %1–20'si SEGE 4–6 ilçelerinden tedarik ediliyorsa", 0, false, 20, true, 1),
                A("İlgili tarımsal girdilerin %21–50'si SEGE 4–6 ilçelerinden tedarik ediliyorsa", 20, false, 50, true, 4),
                A("İlgili tarımsal girdilerin %51–100'ü SEGE 4–6 ilçelerinden tedarik ediliyorsa", 50, false, 100, true, 7.5m))),
        Konu(2, "Tedarik zinciri entegrasyonu", 20,
            K("İlave kayıtlı çiftçi / birincil üretici entegrasyonu", "CIFTCI_90K", "90.000 Avro eşdeğeri RFF tutarı başına {deger} ilave kayıtlı çiftçi / birincil üretici", "Tedarikçi Entegrasyon Planından alınır",
                A("90.000 Avro eşdeğeri RFF tutarı başına 2'den az ilave kayıtlı çiftçi / birincil üretici", null, true, 2, false, 0),
                A("90.000 Avro eşdeğeri RFF tutarı başına 2 ila <4 ilave kayıtlı çiftçi / birincil üretici", 2, true, 4, false, 6),
                A("90.000 Avro eşdeğeri RFF tutarı başına 4 ila <6 ilave kayıtlı çiftçi / birincil üretici", 4, true, 6, false, 12),
                A("90.000 Avro eşdeğeri RFF tutarı başına 6 veya daha fazla ilave kayıtlı çiftçi / birincil üretici", 6, true, null, false, 20))),
        Konu(3, "Daha fazla ve daha iyi ücretli istihdam yaratma", 20,
            K("İlave tam zamanlı eşdeğer istihdam", "ISTIHDAM_90K", "90.000 Avro eşdeğeri RFF tutarı başına {deger} ilave tam zamanlı eşdeğer yıllık iş", "Tam Zamanlı İstihdam Formundan alınır",
                A("90.000 Avro eşdeğeri RFF tutarı başına 0,25'ten az ilave tam zamanlı eşdeğer yıllık iş", null, true, 0.25m, false, 0),
                A("90.000 Avro eşdeğeri RFF tutarı başına 0,25 ila <0,50 ilave tam zamanlı eşdeğer yıllık iş", 0.25m, true, 0.5m, false, 5),
                A("90.000 Avro eşdeğeri RFF tutarı başına 0,50 ila <0,75 ilave tam zamanlı eşdeğer yıllık iş", 0.5m, true, 0.75m, false, 10),
                A("90.000 Avro eşdeğeri RFF tutarı başına 0,75 ila <1,00 ilave tam zamanlı eşdeğer yıllık iş", 0.75m, true, 1, false, 15),
                A("90.000 Avro eşdeğeri RFF tutarı başına 1,00 veya daha fazla ilave tam zamanlı eşdeğer yıllık iş", 1, true, null, false, 20))),
        Konu(4, "Kadın / genç kapsayıcılığı", 5,
            K("Kadın istihdam düzeyi", "KADIN_ISTIHDAM", "", "Tam Zamanlı İstihdam Formundan alınır",
                E("Son iki yılda 20'den fazla çalışanı olan başvuru sahiplerinde kadın çalışan sayısı erkek çalışan sayısından fazlaysa", 1, 2.5m)),
            K("Kadın veya genç sahipliği", "KADIN_GENC_SAHIPLIK", "Şirket veya üretici örgütünde kadınların veya 40 yaş altı bireylerin sahiplik oranı {deger}", "Başvuru formundan alınır",
                A("Şirket veya üretici örgütünde kadınların veya 40 yaş altı bireylerin sahiplik oranı %50'den fazlaysa", 50, false, null, false, 2.5m))),
        Konu(5, "Kümelenmenin desteklenmesi", 10,
            K("Organize üretim veya sanayi alanında yer alma", "ORGANIZE_ALAN", "", "Başvuru formundan alınır",
                E("Yatırım Organize Tarım Bölgesi'nde yer alıyorsa", (int)enumKumelenmeOrganizeAlanTuru.OrganizeTarimBolgesi, 100),
                E("Yatırım İhtisas Gıda Bölgesi'nde yer alıyorsa", (int)enumKumelenmeOrganizeAlanTuru.IhtisasGidaBolgesi, 10),
                E("Yatırım Organize Sanayi Bölgesi'nde yer alıyorsa", (int)enumKumelenmeOrganizeAlanTuru.OrganizeSanayiBolgesi, 5))),
        Konu(6, "Yatırım konusunun sürdürülebilirlik, verimli kaynak kullanımı ve döngüsel ekonomi katkısı", 10,
            K("Atık, yan ürün, artık veya ikincil materyallerin değerlendirilmesi", null, "", "",
                E("Yatırım atık, yan ürün, artık veya ikincil materyallerin değerlendirilmesini içeriyorsa", 1, 4)),
            K("Yenilenebilir enerji üretimi veya kullanımı", "YENILENEBILIR_ENERJI", "", "",
                E("Yatırım yenilenebilir enerji üretimi veya kullanımını içeriyorsa", 1, 3)),
            K("İklim dayanıklılığı, sera gazı emisyon azaltımı ile enerji, su ve girdilerin etkin kullanımı", null, "", "",
                E("İklim dayanıklılığı ve iklim değişikliğine uyum", 1, 3),
                E("Sera gazı emisyonlarının azaltılması ve enerji verimliliği", 1, 3),
                E("Su kullanımı, atık su yönetimi, kirliliğin azaltılması ve girdi kullanımının azaltılması", 1, 3))),
        Konu(7, "Rekabetçilik", 20,
            K("Öncelikli desteklenecek yatırım konuları ile uyum", null, "", "Paylaşılacak Rekabetçilik Formundan alınır",
                E("Yatırım Öncelikli Desteklenecek Yatırım Listesi'nde yer alıyorsa", 1, 4)),
            K("Son iki yılda ihracat / satış oranı", "IHRACAT_SATIS_ORANI", "İhracatın satışlara oranı {deger}", "Paylaşılacak Rekabetçilik Formundan alınır",
                A("İhracatın satışlara oranı %1–20 arasındaysa", 1, true, 20, true, 1),
                A("İhracatın satışlara oranı %20–50 arasındaysa", 20, false, 50, true, 4),
                A("İhracatın satışlara oranı %50'den fazlaysa", 50, false, null, false, 6)),
            K("Yüksek ithalat bağımlılığı olan ürünleri hedefleme", null, "", "Paylaşılacak Rekabetçilik Formundan alınır",
                E("Yatırım Yüksek İthalat Bağımlılığı Olan Ürünler Listesi'nde yer alan ürünleri hedefliyorsa", 1, 10)))
    ];
}

public sealed class SiralamaPuaniSatiri
{
    public string konu { get; init; } = "";
    public int konuId { get; init; }
    public string altKriter { get; init; } = "";
    public string metin { get; init; } = "";
    public decimal puan { get; init; }
    /// <summary>Kriterin terimi yok ya da başvuruda terimin hesaplanacağı veri bulunmuyor.</summary>
    public bool veriYok { get; init; }
    public bool kosulSaglandi { get; init; }
}

public static class SiralamaPuaniHesaplayici
{
    public static List<SiralamaPuaniSatiri> Hesapla(Basvuru b, IReadOnlyList<SiralamaPuaniKonu>? konular = null)
    {
        List<SiralamaPuaniSatiri> satirlar = [];
        foreach (SiralamaPuaniKonu konu in (konular ?? SiralamaPuaniTanimSaglayici.Tum).Where(x => x.aktif).OrderBy(x => x.siraNo).ThenBy(x => x.id))
        {
            string konuAdi = $"{konu.siraNo}. {konu.ad}";
            foreach (SiralamaPuaniAltKriter k in konu.altKriterler.Where(x => x.aktif).OrderBy(x => x.siraNo).ThenBy(x => x.id))
            {
                string altKriterAdi = $"{konu.siraNo}.{k.siraNo} {k.ad}";
                SiralamaPuaniTerim? terim = SiralamaPuaniTerimleri.Bul(k.terimKodu);
                IReadOnlyList<decimal> degerler = terim == null ? [] : terim.hesapla(b);
                if (terim == null || degerler.Count == 0)
                {
                    satirlar.Add(new() { konu = konuAdi, konuId = konu.id, altKriter = altKriterAdi, veriYok = true,
                        metin = terim == null ? "Bu kriter için uygulamada hesaplanan bir veri bulunmuyor." : "Bu kriterin hesaplanacağı bilgi başvuruda girilmemiş." });
                    continue;
                }
                // Birden fazla değer varsa (ör. birden fazla yatırım adresi) en yüksek puanı veren değer esas alınır.
                var adaylar = degerler.Select(d => (deger: d, kosul: k.kosullar.OrderBy(x => x.siraNo).Where(x => x.Saglar(d)).OrderByDescending(x => x.puan).FirstOrDefault())).ToList();
                var secilen = adaylar.Where(x => x.kosul != null).OrderByDescending(x => x.kosul!.puan).FirstOrDefault();
                decimal deger = secilen.kosul != null ? secilen.deger : degerler.Max();
                string degerMetni = terim.DegerYaz(deger);
                string metin = !string.IsNullOrWhiteSpace(k.sonucMetni)
                    ? k.sonucMetni.Replace("{deger}", degerMetni, StringComparison.Ordinal)
                    : secilen.kosul?.metin ?? $"Koşul sağlanmıyor ({terim.ad}: {degerMetni})";
                satirlar.Add(new() { konu = konuAdi, konuId = konu.id, altKriter = altKriterAdi, metin = metin, puan = secilen.kosul?.puan ?? 0, kosulSaglandi = secilen.kosul != null });
            }
        }
        return satirlar;
    }
}
