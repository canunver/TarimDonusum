using System.Globalization;
using System.Text.Json;

namespace TarimDonusum.Models;

/// <summary>Nakit akış tablosu: yatırım dönemi (kaynaklar ve yatırım giderleri) ile 1.–7. yıllar.</summary>
public sealed class NakitAkisTablosu
{
    public const int YilSayisi = 7;

    public decimal TargetKredisi { get; set; }
    public decimal Ozkaynaklar { get; set; }
    public decimal DigerKaynaklar { get; set; }
    public decimal BankaKredisi { get; set; }
    public decimal YatirimGiderleri { get; set; }
    public decimal IsletmeSermayesi { get; set; }
    /// <summary>Enflasyon uygulanmadan önceki baz gelir (ürünlerin 1. yıl satış bedeli) ve işletme gideri; yalnızca bilgi amaçlı gösterilir, toplamlara girmez.</summary>
    public decimal BazGelir { get; set; }
    public decimal BazIsletmeGideri { get; set; }
    /// <summary>Ödemesiz süre yatırım dönemi yılı içinde biterse o yıla düşen TARGET taksiti (EUR ve TL).</summary>
    public decimal YatirimDonemiTargetOdemesiEur { get; set; }
    public decimal YatirimDonemiTargetOdemesi { get; set; }
    public decimal YatirimDonemiNetNakit => TargetKredisi + Ozkaynaklar + DigerKaynaklar + BankaKredisi - YatirimGiderleri - IsletmeSermayesi - YatirimDonemiTargetOdemesi;

    public decimal[] Gelirler { get; } = new decimal[YilSayisi];
    public decimal[] IsletmeGiderleri { get; } = new decimal[YilSayisi];
    public decimal[] Amortisman { get; } = new decimal[YilSayisi];
    public decimal[] TargetKrediOdemesiEur { get; } = new decimal[YilSayisi];
    public decimal[] TargetKrediOdemesi { get; } = new decimal[YilSayisi];
    public decimal[] VergiOncesiKar { get; } = new decimal[YilSayisi];
    public decimal[] VergiOdemesi { get; } = new decimal[YilSayisi];
    public decimal[] NetNakitAkisi { get; } = new decimal[YilSayisi];
    public decimal[] KumulatifNetNakit { get; } = new decimal[YilSayisi];

    /// <summary>Yatırım dönemi dahil kümülatif net nakit akışının hiçbir yılda eksiye düşmemesi.</summary>
    public bool KumulatifPozitif => YatirimDonemiNetNakit >= 0 && KumulatifNetNakit.All(x => x >= 0);
}

public sealed record GeriOdemeSenaryosu(int No, string Ad, decimal Oran, NakitAkisTablosu Tablo)
{
    public bool Dayanikli => Tablo.KumulatifPozitif;
}

public sealed class GeriOdemeKarlilikSonucu
{
    /// <summary>Yatırım dönemi yılı (dönem yılı) ve izleyen 7 yıl.</summary>
    public int[] Yillar { get; set; } = [];
    public decimal?[] Kur { get; set; } = [];
    public decimal?[] Enflasyon { get; set; } = [];
    public List<string> Uyarilar { get; } = [];

    /// <summary>TARGET kredisi geri ödeme bilgileri (Finansman sayfası).</summary>
    public decimal TargetEur { get; set; }
    public int VadeAy { get; set; }
    public int GeriOdemesizAy { get; set; }
    public int YatirimSuresiAy { get; set; }
    public decimal AylikTaksitEur => VadeAy > 0 ? TargetEur / VadeAy : 0;

    public NakitAkisTablosu Temel { get; set; } = new();
    public List<GeriOdemeSenaryosu> Senaryolar { get; } = [];

    public decimal[] Ebitda { get; } = new decimal[NakitAkisTablosu.YilSayisi];
    public decimal[] EbitdaCiroOrani { get; } = new decimal[NakitAkisTablosu.YilSayisi];
    public decimal[] Bsko { get; } = new decimal[NakitAkisTablosu.YilSayisi];
    public decimal? OrtalamaBsko { get; set; }
    public decimal? BasabasNoktasi { get; set; }
    public bool ProjePozitif { get; set; }
}

/// <summary>
/// "GeriOdeme" ve "Karlilik" Excel sayfalarındaki hesapların karşılığı. Excel'in Yatırım Özeti Tablosu'na bağlı hücreleri
/// başvurudaki verilerle eşlenir:
/// gelir = ürünlerin 1. yıl satış bedeli toplamı; işletme gideri (toplam/sabit/değişken) = Giderler sayfası;
/// yatırım bütçesi ve kaynaklar = Bütçe sayfası (A, A1, A2, B, B5, D, E, F, G);
/// TARGET kredisi (EUR) ve geri ödeme takvimi = Finansman sayfası (talep edilen RFF tutarı, vade, geri ödemesiz dönem, yatırım süresi);
/// kur ve enflasyon = Tanımlar > Kur ve Enflasyon Tahminleri (dönem yılı ve izleyen 7 yıl).
/// </summary>
public static class GeriOdemeKarlilikHesaplayici
{
    private const decimal VergiOrani = 0.2m;
    private const decimal BinaAmortismanOrani = 0.05m;
    private const decimal MakineAmortismanOrani = 0.2m;
    private const int MakineAmortismanYili = 5;

    private sealed record Girdiler(
        decimal Gelir, decimal Gider, decimal SabitGider, decimal DegiskenGider,
        decimal A, decimal A1, decimal A2, decimal B, decimal B5, decimal D, decimal E, decimal F, decimal G,
        decimal TargetEur, int VadeAy, int OdemesizAy, decimal[] Kur, decimal[] Enflasyon);

    public static GeriOdemeKarlilikSonucu Hesapla(Basvuru b)
    {
        GeriOdemeKarlilikSonucu sonuc = new();
        int donemYili = b.basvuruFirma.donem?.yil > 0 ? b.basvuruFirma.donem.yil : DateTime.Today.Year;
        IReadOnlyList<DonemTahminSatiri> tahminler = DonemTahminSaglayici.Donem(b.basvuruFirma.donemId);
        sonuc.Yillar = Enumerable.Range(donemYili, DonemTahminSaglayici.YilSayisi).ToArray();
        sonuc.Kur = sonuc.Yillar.Select(y => tahminler.FirstOrDefault(x => x.yil == y)?.kurTahminiTL).ToArray();
        sonuc.Enflasyon = sonuc.Yillar.Select(y => tahminler.FirstOrDefault(x => x.yil == y)?.enflasyonTahminiYuzde).ToArray();
        List<int> eksikYillar = sonuc.Yillar.Where((_, i) => sonuc.Kur[i] is null || sonuc.Enflasyon[i] is null).ToList();
        if (eksikYillar.Count > 0)
            sonuc.Uyarilar.Add($"{string.Join(", ", eksikYillar)} yılları için kur veya enflasyon tahmini girilmemiş (Tanımlar > Kur ve Enflasyon Tahminleri); eksik değerler 0 kabul edildi.");

        Girdiler g = GirdileriOku(b, sonuc);
        sonuc.Temel = Tablo(g, gelirCarpani: 1, giderCarpani: 1, kurArtisi: 0, vergiYalnizKarda: false);
        sonuc.Senaryolar.Add(new(1, "Toplam Giderlerde Artış", 0.05m, Tablo(g, 1, 1.05m, 0, true)));
        sonuc.Senaryolar.Add(new(2, "Toplam Giderlerde Artış", 0.10m, Tablo(g, 1, 1.10m, 0, true)));
        sonuc.Senaryolar.Add(new(3, "Toplam Gelirde Azalış", 0.05m, Tablo(g, 0.95m, 1, 0, true)));
        sonuc.Senaryolar.Add(new(4, "Toplam Gelirde Azalış", 0.10m, Tablo(g, 0.90m, 1, 0, true)));
        sonuc.Senaryolar.Add(new(5, "Kur Artışı (Enflasyona göre)", 0.05m, Tablo(g, 1, 1, 0.05m, true)));
        sonuc.Senaryolar.Add(new(6, "Kur Artışı (Enflasyona göre)", 0.10m, Tablo(g, 1, 1, 0.10m, true)));

        // EBITDA (FAVÖK) - BSKO göstergeleri temel nakit akış tablosundan hesaplanır.
        NakitAkisTablosu t = sonuc.Temel;
        for (int i = 0; i < NakitAkisTablosu.YilSayisi; i++)
        {
            sonuc.Ebitda[i] = t.VergiOncesiKar[i];
            sonuc.EbitdaCiroOrani[i] = t.Gelirler[i] == 0 ? 0 : t.VergiOncesiKar[i] / t.Gelirler[i];
            sonuc.Bsko[i] = t.TargetKrediOdemesi[i] == 0 ? 0 : t.VergiOncesiKar[i] / t.TargetKrediOdemesi[i];
        }
        int pozitifBsko = sonuc.Bsko.Count(x => x > 0);
        sonuc.OrtalamaBsko = pozitifBsko == 0 ? null : sonuc.Bsko.Sum() / pozitifBsko;
        decimal katkiPayi = g.Gelir - g.DegiskenGider;
        sonuc.BasabasNoktasi = katkiPayi <= 0 ? null : g.SabitGider / katkiPayi;
        sonuc.ProjePozitif = sonuc.EbitdaCiroOrani.Sum() / NakitAkisTablosu.YilSayisi > 0
            && sonuc.OrtalamaBsko > 1
            && sonuc.BasabasNoktasi < 1
            && sonuc.Senaryolar.Where(x => x.No is 1 or 3 or 5).All(x => x.Dayanikli);
        return sonuc;
    }

    private static NakitAkisTablosu Tablo(Girdiler g, decimal gelirCarpani, decimal giderCarpani, decimal kurArtisi, bool vergiYalnizKarda)
    {
        NakitAkisTablosu t = new()
        {
            TargetKredisi = g.F,
            Ozkaynaklar = g.D,
            DigerKaynaklar = g.G,
            BankaKredisi = g.E,
            YatirimGiderleri = g.A + g.B - g.B5,
            IsletmeSermayesi = g.B5,
            BazGelir = g.Gelir,
            BazIsletmeGideri = g.Gider
        };
        // TARGET taksitleri: ödemesiz süre (geri ödemesiz dönem + yatırım süresi) 1. yıldan itibaren sayılır ve tam yıla yukarı
        // yuvarlanır (ör. 20 ay -> 2 yıl). Sonraki yılın başından vade kadar ay boyunca her ay "RFF tutarı / vade" EUR ödenir;
        // aylar yıllara toplanır ve yıllık EUR toplamı o yılın kuruyla TL'ye çevrilir. Yatırım döneminde ödeme yoktur.
        decimal[] yillikEur = YillikTaksitlerEur(g);
        t.YatirimDonemiTargetOdemesiEur = yillikEur[0];
        t.YatirimDonemiTargetOdemesi = yillikEur[0] * g.Kur[0];
        decimal gelir = g.Gelir, gider = g.Gider, kumulatif = t.YatirimDonemiNetNakit;
        for (int i = 0; i < NakitAkisTablosu.YilSayisi; i++)
        {
            int yil = i + 1;
            // n. yılın geliri/gideri bir önceki yılın (ilk yılda yatırım dönemi yılının) enflasyonu kadar artar.
            decimal enflasyon = g.Enflasyon[i] / 100m;
            gelir *= 1 + enflasyon;
            gider *= 1 + enflasyon;
            t.Gelirler[i] = gelir * gelirCarpani;
            t.IsletmeGiderleri[i] = gider * giderCarpani;
            t.Amortisman[i] = g.A1 * BinaAmortismanOrani + (yil <= MakineAmortismanYili ? g.A2 * MakineAmortismanOrani : 0);

            t.TargetKrediOdemesiEur[i] = yillikEur[yil];
            t.TargetKrediOdemesi[i] = yillikEur[yil] * g.Kur[yil] * (decimal)Math.Pow(1 + (double)kurArtisi, yil);

            t.VergiOncesiKar[i] = t.Gelirler[i] - t.IsletmeGiderleri[i] - t.Amortisman[i] - t.TargetKrediOdemesi[i];
            t.VergiOdemesi[i] = vergiYalnizKarda && t.VergiOncesiKar[i] <= 0 ? 0 : t.VergiOncesiKar[i] * VergiOrani;
            t.NetNakitAkisi[i] = t.VergiOncesiKar[i] - t.VergiOdemesi[i];
            kumulatif += t.NetNakitAkisi[i];
            t.KumulatifNetNakit[i] = kumulatif;
        }
        return t;
    }

    /// <summary>Ödemesiz süre (ay) tam yıla yukarı yuvarlanır: 0 ay -> 0, 1-12 ay -> 1, 13-24 ay -> 2 yıl ...</summary>
    public static int OdemesizYil(int odemesizAy) => odemesizAy <= 0 ? 0 : (odemesizAy + 11) / 12;

    /// <summary>Yatırım dönemi yılı (0, her zaman 0) ve 1.-7. yıllar için yıllık TARGET taksit toplamı (EUR).</summary>
    private static decimal[] YillikTaksitlerEur(Girdiler g)
    {
        decimal[] yillik = new decimal[NakitAkisTablosu.YilSayisi + 1];
        if (g.VadeAy <= 0 || g.TargetEur <= 0) return yillik;
        decimal aylikTaksit = g.TargetEur / g.VadeAy;
        int ilkOdemeAyi = OdemesizYil(g.OdemesizAy) * 12; // 1. yılın başından itibaren 0 tabanlı ay
        for (int k = 0; k < g.VadeAy; k++)
        {
            int yilIndeksi = (ilkOdemeAyi + k) / 12 + 1; // 1 = 1. yıl
            if (yilIndeksi < yillik.Length) yillik[yilIndeksi] += aylikTaksit;
        }
        return yillik;
    }

    private static Girdiler GirdileriOku(Basvuru b, GeriOdemeKarlilikSonucu sonuc)
    {
        decimal gelir = b.YatirimOnBilgileri
            .Where(x => x.tur is enumYatirimOnBilgiTuru.MevcutUrun or enumYatirimOnBilgiTuru.UretilecekUrun)
            .Sum(x => (x.satisMiktari ?? 0) * (x.birimSatisFiyati ?? 0));
        if (gelir == 0) sonuc.Uyarilar.Add("Ürünler ve Satış Bilgileri sayfasında 1. yıl satış bedeli bulunmuyor; gelirler 0 kabul edildi.");

        Dictionary<string, decimal> butce = new(StringComparer.OrdinalIgnoreCase);
        decimal gider = 0, sabit = 0, degisken = 0;
        bool giderVar = false;
        if (!string.IsNullOrWhiteSpace(b.yatirimOzeti.yatirimOzetiJson))
        {
            try
            {
                using JsonDocument belge = JsonDocument.Parse(b.yatirimOzeti.yatirimOzetiJson);
                JsonElement kok = belge.RootElement;
                if (kok.TryGetProperty("investmentBudgetData", out JsonElement butceVerisi) && butceVerisi.ValueKind == JsonValueKind.Object)
                    foreach (JsonProperty satir in butceVerisi.EnumerateObject())
                        if (satir.Value.ValueKind == JsonValueKind.Object && satir.Value.TryGetProperty("amount", out JsonElement tutar))
                            butce[satir.Name] = Sayi(tutar);

                // Giderler sayfasıyla aynı şekilde her zaman satırlardan hesaplanır (okuma sırasında Teknik Proje (Girdiler) satırları
                // da listeye eklenir). Kayıtlı "totals" son kaydın anlık görüntüsü olup eskiyebileceği için kullanılmaz.
                if (kok.TryGetProperty("operatingExpenseRows", out JsonElement satirlar) && satirlar.ValueKind == JsonValueKind.Array)
                {
                    foreach (JsonElement satir in satirlar.EnumerateArray())
                    {
                        decimal toplam = Alan(satir, "qty") * Alan(satir, "unitPrice");
                        decimal sabitPay = Math.Clamp(Alan(satir, "fixedPct"), 0, 100) / 100m;
                        gider += toplam; sabit += toplam * sabitPay; degisken += toplam * (1 - sabitPay);
                    }
                    giderVar = true;
                }
            }
            catch (JsonException) { }
        }
        if (butce.Count == 0) sonuc.Uyarilar.Add("Bütçe sayfasında yatırım bütçesi ve finansman kaynakları girilmemiş.");
        if (!giderVar || gider == 0) sonuc.Uyarilar.Add("Giderler sayfasında işletme gideri bulunmuyor; işletme giderleri 0 kabul edildi.");

        decimal B(string kod) => butce.GetValueOrDefault(kod);
        decimal a = B("A1") + B("A2") + B("A3") + B("A4");
        decimal bToplam = B("B1") + B("B2") + B("B3") + B("B4") + B("B5") + B("B6") + B("B7");

        // TARGET kredisi = Finansman sayfasındaki "Talep edilen RFF tutarı (EUR)".
        sonuc.TargetEur = b.HesaplananTalepEdilenFinansmanTutari ?? 0;
        sonuc.VadeAy = Math.Max(0, b.finans.talepEdilenVadeSuresiAy ?? 0);
        sonuc.GeriOdemesizAy = Math.Max(0, b.finans.odemeSuresiAy ?? 0);
        sonuc.YatirimSuresiAy = Math.Max(0, b.finans.yatirimSuresiAy ?? 0);
        int odemesizAy = sonuc.GeriOdemesizAy + sonuc.YatirimSuresiAy;
        if (sonuc.TargetEur > 0 && sonuc.VadeAy == 0)
            sonuc.Uyarilar.Add("Finansman sayfasında talep edilen vade girilmemiş; TARGET kredi ödemesi hesaplanamadı.");
        int kapsamDisiAy = Math.Max(0, OdemesizYil(odemesizAy) * 12 + sonuc.VadeAy - NakitAkisTablosu.YilSayisi * 12);
        if (sonuc.TargetEur > 0 && sonuc.VadeAy > 0 && kapsamDisiAy > 0)
            sonuc.Uyarilar.Add($"TARGET kredisinin son {Math.Min(kapsamDisiAy, sonuc.VadeAy)} aylık taksiti tablonun kapsadığı 7. yıldan sonraya düşüyor ve tabloda gösterilmiyor.");

        decimal[] kur = sonuc.Kur.Select(x => x ?? 0).ToArray();
        decimal[] enflasyon = sonuc.Enflasyon.Select(x => x ?? 0).ToArray();
        return new Girdiler(gelir, gider, sabit, degisken, a, B("A1"), B("A2"), bToplam, B("B5"), B("D"), B("E"), B("F"), B("G"),
            sonuc.TargetEur, sonuc.VadeAy, odemesizAy, kur, enflasyon);
    }

    private static decimal Alan(JsonElement nesne, string ad) =>
        nesne.ValueKind == JsonValueKind.Object && nesne.TryGetProperty(ad, out JsonElement deger) ? Sayi(deger) : 0;

    /// <summary>Sayı veya Türkçe biçimli metin ("1.234,56") olarak kaydedilmiş tutarı okur.</summary>
    private static decimal Sayi(JsonElement deger)
    {
        if (deger.ValueKind == JsonValueKind.Number && deger.TryGetDecimal(out decimal sayi)) return sayi;
        if (deger.ValueKind != JsonValueKind.String) return 0;
        // Sayfadaki YatirimOzetiSayi ile aynı kural: virgül varsa Türkçe biçim; "2.000.000" gibi değerde nokta binlik ayırıcıdır.
        string metin = (deger.GetString() ?? "").Trim();
        if (metin.Contains(',')) metin = metin.Replace(".", "").Replace(',', '.');
        else if (System.Text.RegularExpressions.Regex.IsMatch(metin, @"^\d{1,3}(\.\d{3})+$")) metin = metin.Replace(".", "");
        return decimal.TryParse(metin, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal sonuc) ? sonuc : 0;
    }
}

/// <summary>Nakit akış tablosu görünümünün modeli (Geri Ödeme sayfası ve Karlılık senaryo sekmeleri).</summary>
public sealed record NakitAkisGorunum(NakitAkisTablosu Tablo, int[] Yillar, bool SenaryoTablosu);
