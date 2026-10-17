using TarimDonusum.Models;

namespace TarimDonusum.TestVeri;

/// <summary>
/// TEST: Bir test başvurusunun bütün sayfalarındaki tutarların türetildiği ortak finansal profil.
/// Yatırım yaklaşık 2 milyon EUR; firma bu yatırımı ve krediyi taşıyabilecek büyüklükte ve kârlıdır:
/// ürünlerin 1. yıl satış geliri, girdi ve işletme giderlerinin toplamından belirgin şekilde yüksektir.
/// </summary>
public sealed class TestFinansProfili
{
    /// <summary>Ön başvuru EUR çevrim kuru (TL).</summary>
    public decimal Kur { get; init; }
    public decimal ToplamYatirimEur { get; init; }
    /// <summary>Bütçe ve Giderler sayfasındaki C - Toplam Yatırım Tutarı (TL).</summary>
    public decimal ToplamYatirimTl { get; init; }
    /// <summary>Firmanın son yıl net satışları (TL).</summary>
    public decimal SonYilNetSatis { get; init; }
    public decimal OncekiYilNetSatis { get; init; }
    public decimal SonYilAktif { get; init; }
    public decimal OncekiYilAktif { get; init; }
    public int CalisanSayisi { get; init; }
    /// <summary>Yatırım sonrası 1. yıl tüm ürünlerin satış geliri (TL).</summary>
    public decimal BirinciYilGelir { get; init; }
    /// <summary>Ürünlerin bugünkü satış geliri (TL); mevcut ürünler bu tutarı paylaşır.</summary>
    public decimal MevcutGelir { get; init; }
    /// <summary>Teknik Proje (Girdiler) sayfasındaki girdilerin yıllık maliyeti (TL).</summary>
    public decimal GirdiMaliyeti { get; init; }
    /// <summary>Giderler sayfasındaki girdi dışı işletme giderleri (TL).</summary>
    public decimal DigerIsletmeGiderleri { get; init; }

    public static TestFinansProfili Olustur(TestVeriKaynaklari r, Donem donem)
    {
        decimal kur = donem.onBasvuruCevrimKuru.GetValueOrDefault();
        if (kur <= 0) throw new TestVeriHatasi("Bütçe", [$"{donem.ad} dönemi için ön başvuru EUR çevrim kuru tanımlı değil."]);
        decimal eur = r.Tutar(1_800_000, 2_200_000, 10_000);
        decimal c = Math.Round(eur * kur / 1000) * 1000;

        // Dönemin mali ölçek alt sınırı varsa net satış onun üzerinde tutulur.
        decimal altSinir = donem.minimumYatirimTutari.GetValueOrDefault() * 1.05m;
        decimal sonSatis = r.Tutar(Math.Max(altSinir, c * 1.6m), Math.Max(altSinir, c * 1.6m) * 2m, 100_000);
        decimal oncekiSatis = Math.Round(sonSatis * r.Sayi(84, 96) / 100m / 100_000) * 100_000;
        decimal gelir = Math.Round(sonSatis * r.Sayi(125, 150) / 100m / 1000) * 1000;
        decimal isletmeGideri = Math.Round(gelir * r.Sayi(70, 76) / 100m / 1000) * 1000;
        decimal girdi = Math.Round(isletmeGideri * r.Sayi(62, 70) / 100m / 1000) * 1000;
        return new TestFinansProfili
        {
            Kur = kur,
            ToplamYatirimEur = eur,
            ToplamYatirimTl = c,
            SonYilNetSatis = sonSatis,
            OncekiYilNetSatis = Math.Max(altSinir, oncekiSatis),
            SonYilAktif = Math.Max(altSinir, Math.Round(sonSatis * r.Sayi(80, 110) / 100m / 100_000) * 100_000),
            OncekiYilAktif = Math.Max(altSinir, Math.Round(oncekiSatis * r.Sayi(78, 105) / 100m / 100_000) * 100_000),
            CalisanSayisi = (int)Math.Clamp(Math.Round(sonSatis / r.Tutar(3_500_000, 5_000_000, 1000)), 20, 200),
            BirinciYilGelir = gelir,
            MevcutGelir = Math.Round(sonSatis * r.Sayi(85, 95) / 100m / 1000) * 1000,
            GirdiMaliyeti = girdi,
            DigerIsletmeGiderleri = isletmeGideri - girdi
        };
    }

    /// <summary>Toplamı rastgele ağırlıklarla paylara böler; payların toplamı tam olarak toplama eşittir.</summary>
    public static List<decimal> Bol(TestVeriKaynaklari r, decimal toplam, int adet, int minAgirlik = 5, int maxAgirlik = 30)
    {
        if (adet <= 0) return [];
        List<decimal> agirlik = Enumerable.Range(0, adet).Select(_ => (decimal)r.Sayi(minAgirlik, maxAgirlik)).ToList();
        decimal agirlikToplami = agirlik.Sum();
        List<decimal> paylar = agirlik.Select(x => Math.Round(toplam * x / agirlikToplami)).ToList();
        paylar[^1] += toplam - paylar.Sum();
        return paylar;
    }

    /// <summary>Bütçe JSON'undaki bir kalemin tutarı (ör. "A2" makine, "A1" yapım işleri).</summary>
    public static decimal ButceKalemi(string? yatirimOzetiJson, string kod)
    {
        if (string.IsNullOrWhiteSpace(yatirimOzetiJson)) return 0;
        try
        {
            using System.Text.Json.JsonDocument belge = System.Text.Json.JsonDocument.Parse(yatirimOzetiJson);
            if (!belge.RootElement.TryGetProperty("investmentBudgetData", out var butce) || !butce.TryGetProperty(kod, out var satir)
                || !satir.TryGetProperty("amount", out var tutar)) return 0;
            string metin = tutar.ValueKind == System.Text.Json.JsonValueKind.String ? tutar.GetString() ?? "" : tutar.ToString();
            return decimal.TryParse(metin.Replace(".", "").Replace(',', '.'), System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out decimal d) ? d : 0;
        }
        catch (System.Text.Json.JsonException) { return 0; }
    }
}
