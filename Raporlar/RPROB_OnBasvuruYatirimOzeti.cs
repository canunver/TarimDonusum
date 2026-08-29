using System.Text.Json;
using TarimDonusum.Araclar;
using TarimDonusum.Models;

namespace TarimDonusum.Raporlar;

public sealed class RPROB_OnBasvuruYatirimOzeti(string uygulamaRootPath) : RPROBTemel(uygulamaRootPath)
{
    protected override string SablonAdi => "YatirimOzeti.xltx";
    protected override string GeciciDosyaOnEki => "on-basvuru-yatirim-ozeti";
    protected override string CiktiDosyaOnEki => "YatirimOzeti";

    protected override void Doldur(Tablo tablo, Basvuru basvuru)
    {
        int formatSheet = -1, formatSatir1 = -1, formatSutun1 = -1, formatSatir2 = -1, formatSutun2 = -1;
        int hedefSheet = -1, hedefSatir = -1, hedefSutun = -1, hedefSatir2 = -1, hedefSutun2 = -1;
        tablo.HucreAdAdresCoz("FormatSatir", ref formatSheet, ref formatSatir1, ref formatSutun1, ref formatSatir2, ref formatSutun2);
        tablo.HucreAdAdresCoz("BaslaSatir", ref hedefSheet, ref hedefSatir, ref hedefSutun, ref hedefSatir2, ref hedefSutun2);
        if (formatSheet < 0 || hedefSheet < 0)
            throw new InvalidOperationException("Yatırım özeti şablonunda FormatSatir veya BaslaSatir isimli bölgesi bulunamadı.");

        const int urunSatirSayisi = 5;
        const int urunSutunSayisi = 14;
        formatSatir2 = Math.Max(formatSatir2, formatSatir1 + urunSatirSayisi - 1);
        formatSutun2 = Math.Max(formatSutun2, formatSutun1 + urunSutunSayisi - 1);
        tablo.AktifSheetDegistir(formatSheet);
        double formatIlkSatirYuksekligi = tablo.SatirGercekYukseklikAl(formatSatir1);

        List<UrunSatiri> urunler = UrunleriOku(basvuru.yatirimOzeti.yatirimOzetiJson);
        for (int urunNo = 0; urunNo < urunler.Count; urunNo++)
        {
            int urunBaslangicSatiri = hedefSatir + urunNo * urunSatirSayisi;
            tablo.HucreKopyala(formatSheet, formatSatir1, formatSutun1, formatSatir2, formatSutun2,
                hedefSheet, urunBaslangicSatiri, hedefSutun);
            tablo.AktifSheetDegistir(hedefSheet);
            tablo.SatirGercekYukseklikAyarla(urunBaslangicSatiri, urunBaslangicSatiri, formatIlkSatirYuksekligi);

            UrunSatiri urun = urunler[urunNo];
            tablo.HucreDegerYaz(urunBaslangicSatiri, hedefSutun, urunNo + 1);
            tablo.HucreDegerYaz(urunBaslangicSatiri, hedefSutun + 1, urun.Ad);
            tablo.HucreDegerYaz(urunBaslangicSatiri, hedefSutun + 2, urun.Birim);

            string[] gostergeler = ["capacity", "production", "sales", "price"];
            for (int gostergeNo = 0; gostergeNo < gostergeler.Length; gostergeNo++)
            {
                List<decimal> degerler = urun.Veriler.GetValueOrDefault(gostergeler[gostergeNo]) ?? [];
                for (int yil = 0; yil < Math.Min(11, degerler.Count); yil++)
                    tablo.HucreDegerYaz(urunBaslangicSatiri + gostergeNo + 1, hedefSutun + yil + 3, degerler[yil]);
            }
        }

        tablo.AktifSheetDegistir(formatSheet);
        tablo.SatirSil(formatSatir1, formatSatir2);
    }

    private static List<UrunSatiri> UrunleriOku(string? json)
    {
        List<UrunSatiri> urunler = [];
        if (string.IsNullOrWhiteSpace(json)) return urunler;
        using JsonDocument belge = JsonDocument.Parse(json);
        if (!belge.RootElement.TryGetProperty("productionRows", out JsonElement satirlar) || satirlar.ValueKind != JsonValueKind.Array)
            return urunler;

        foreach (JsonElement satir in satirlar.EnumerateArray())
        {
            Dictionary<string, List<decimal>> veriler = new(StringComparer.OrdinalIgnoreCase);
            if (satir.TryGetProperty("data", out JsonElement veri) && veri.ValueKind == JsonValueKind.Object)
            {
                foreach (string gosterge in new[] { "capacity", "production", "sales", "price" })
                {
                    List<decimal> degerler = [];
                    if (veri.TryGetProperty(gosterge, out JsonElement dizi) && dizi.ValueKind == JsonValueKind.Array)
                        foreach (JsonElement deger in dizi.EnumerateArray()) degerler.Add(RPROB_ProjeButcesi.SayiOku(deger));
                    veriler[gosterge] = degerler;
                }
            }
            urunler.Add(new(RaporJson.Metin(satir, "name"), RaporJson.Metin(satir, "unit"), veriler));
        }
        return urunler;
    }

    private sealed record UrunSatiri(string Ad, string Birim, Dictionary<string, List<decimal>> Veriler);
}

public sealed class RPROB_YatirimOzetiYonlendirici(string uygulamaRootPath) : IRPROB
{
    private IRPROB? secilenRapor;
    public string SablonDosyasi => secilenRapor?.SablonDosyasi ?? Path.Combine(uygulamaRootPath, "Sablonlar", "YatirimOzeti.xltx");

    public RaporDosyasi Olustur(Basvuru basvuru, int basvuruId)
    {
        secilenRapor = basvuru.kayitTuru == enumBasvuruKayitTuru.Basvuru
            ? new RPROB_YatirimOzeti(uygulamaRootPath)
            : new RPROB_OnBasvuruYatirimOzeti(uygulamaRootPath);
        return secilenRapor.Olustur(basvuru, basvuruId);
    }
}
