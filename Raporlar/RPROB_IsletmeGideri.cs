using System.Text.Json;
using TarimDonusum.Araclar;
using TarimDonusum.Models;

namespace TarimDonusum.Raporlar;

public sealed class RPROB_IsletmeGideri(string uygulamaRootPath) : RPROBTemel(uygulamaRootPath)
{
    protected override string SablonAdi => "IsletmeGideri.xltx";
    protected override string GeciciDosyaOnEki => "isletme-giderleri";
    protected override string CiktiDosyaOnEki => "IsletmeGiderleri";

    protected override void Doldur(Tablo tablo, Basvuru basvuru)
    {
        int fs = -1, fr = -1, fc = -1, fr2 = -1, fc2 = -1;
        int hs = -1, hr = -1, hc = -1, hr2 = -1, hc2 = -1;
        tablo.HucreAdAdresCoz("FormatSatir", ref fs, ref fr, ref fc, ref fr2, ref fc2);
        tablo.HucreAdAdresCoz("BaslaSatir", ref hs, ref hr, ref hc, ref hr2, ref hc2);
        if (fs < 0 || hs < 0) throw new InvalidOperationException("İşletme gideri şablonunda FormatSatir veya BaslaSatir isimli bölgesi bulunamadı.");

        tablo.AktifSheetDegistir(fs);
        double[] yukseklikler = [tablo.SatirGercekYukseklikAl(fr), tablo.SatirGercekYukseklikAl(fr + 1), tablo.SatirGercekYukseklikAl(fr + 2)];
        List<GiderSatiri> giderler = GiderleriOku(basvuru.yatirimOzeti.yatirimOzetiJson);
        decimal genelToplam = giderler.Sum(x => x.Toplam);
        int satir = hr;

        foreach (IGrouping<string, GiderSatiri> grup in giderler.GroupBy(x => x.Grup))
        {
            decimal toplam = grup.Sum(x => x.Toplam);
            FormatKopyala(tablo, fs, fr, fc, hs, satir, hc, yukseklikler[0]);
            tablo.HucreDegerYaz(satir, hc + 1, grup.Key);
            tablo.HucreDegerYaz(satir, hc + 5, toplam);
            tablo.HucreDegerYaz(satir, hc + 8, genelToplam == 0 ? 0 : toplam / genelToplam * 100);
            tablo.HucreDegerYaz(satir, hc + 9, grup.Sum(x => x.SabitTutar));
            tablo.HucreDegerYaz(satir, hc + 10, grup.Sum(x => x.DegiskenTutar));
            satir++;

            foreach (GiderSatiri gider in grup)
            {
                FormatKopyala(tablo, fs, fr + 1, fc, hs, satir, hc, yukseklikler[1]);
                tablo.HucreDegerYaz(satir, hc + 1, gider.Unsur);
                tablo.HucreDegerYaz(satir, hc + 2, gider.Miktar);
                tablo.HucreDegerYaz(satir, hc + 3, gider.Birim);
                tablo.HucreDegerYaz(satir, hc + 4, gider.BirimFiyat);
                tablo.HucreDegerYaz(satir, hc + 5, gider.Toplam);
                tablo.HucreDegerYaz(satir, hc + 6, gider.SabitYuzde);
                tablo.HucreDegerYaz(satir, hc + 7, gider.DegiskenYuzde);
                tablo.HucreDegerYaz(satir, hc + 8, genelToplam == 0 ? 0 : gider.Toplam / genelToplam * 100);
                tablo.HucreDegerYaz(satir, hc + 9, gider.SabitTutar);
                tablo.HucreDegerYaz(satir, hc + 10, gider.DegiskenTutar);
                satir++;
            }
        }

        FormatKopyala(tablo, fs, fr + 2, fc, hs, satir, hc, yukseklikler[2]);
        tablo.HucreDegerYaz(satir, hc + 1, "TOPLAM İŞLETME GİDERLERİ");
        tablo.HucreDegerYaz(satir, hc + 5, genelToplam);
        tablo.HucreDegerYaz(satir, hc + 8, genelToplam == 0 ? 0 : 100);
        tablo.HucreDegerYaz(satir, hc + 9, giderler.Sum(x => x.SabitTutar));
        tablo.HucreDegerYaz(satir, hc + 10, giderler.Sum(x => x.DegiskenTutar));
        tablo.AktifSheetDegistir(fs);
        tablo.SatirSil(fr, fr + 2);
    }

    private static void FormatKopyala(Tablo tablo, int ks, int kr, int kc, int hs, int hr, int hc, double yukseklik)
    {
        tablo.HucreKopyala(ks, kr, kc, kr, kc + 11, hs, hr, hc);
        tablo.AktifSheetDegistir(hs);
        tablo.SatirGercekYukseklikAyarla(hr, hr, yukseklik);
    }

    private static List<GiderSatiri> GiderleriOku(string? json)
    {
        List<GiderSatiri> sonuc = [];
        if (string.IsNullOrWhiteSpace(json)) return sonuc;
        using JsonDocument belge = JsonDocument.Parse(json);
        if (!belge.RootElement.TryGetProperty("operatingExpenseRows", out JsonElement satirlar) || satirlar.ValueKind != JsonValueKind.Array) return sonuc;
        foreach (JsonElement s in satirlar.EnumerateArray())
        {
            decimal miktar = RaporJson.Sayi(s, "qty"), fiyat = RaporJson.Sayi(s, "unitPrice");
            decimal sabit = RaporJson.Sayi(s, "fixedPct"), degisken = RaporJson.Sayi(s, "variablePct"), toplam = miktar * fiyat;
            sonuc.Add(new(RaporJson.Metin(s, "group"), RaporJson.Metin(s, "item"), miktar, RaporJson.Metin(s, "unit"), fiyat,
                toplam, sabit, degisken, toplam * sabit / 100, toplam * degisken / 100));
        }
        return sonuc;
    }

    private sealed record GiderSatiri(string Grup, string Unsur, decimal Miktar, string Birim, decimal BirimFiyat,
        decimal Toplam, decimal SabitYuzde, decimal DegiskenYuzde, decimal SabitTutar, decimal DegiskenTutar);
}

internal static class RaporJson
{
    internal static string Metin(JsonElement nesne, string alan) =>
        nesne.TryGetProperty(alan, out JsonElement deger) && deger.ValueKind == JsonValueKind.String ? deger.GetString() ?? "" : "";
    internal static decimal Sayi(JsonElement nesne, string alan) =>
        nesne.TryGetProperty(alan, out JsonElement deger) ? RPROB_ProjeButcesi.SayiOku(deger) : 0;
    internal static string DegerMetni(JsonElement nesne, string alan)
    {
        if (!nesne.TryGetProperty(alan, out JsonElement deger)) return "";
        return deger.ValueKind switch
        {
            JsonValueKind.String => deger.GetString() ?? "",
            JsonValueKind.Number => deger.GetRawText(),
            JsonValueKind.True => "Evet",
            JsonValueKind.False => "Hayır",
            _ => ""
        };
    }
}
