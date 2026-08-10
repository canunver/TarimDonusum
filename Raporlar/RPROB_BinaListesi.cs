using System.Text.Json;
using TarimDonusum.Araclar;
using TarimDonusum.Models;

namespace TarimDonusum.Raporlar;

public sealed class RPROB_BinaListesi(string uygulamaRootPath) : RPROBTemel(uygulamaRootPath)
{
    protected override string SablonAdi => "BinaListesi.xltx";
    protected override string GeciciDosyaOnEki => "bina-listesi";
    protected override string CiktiDosyaOnEki => "BinaListesi";

    protected override void Doldur(Tablo tablo, Basvuru basvuru)
    {
        int fs = -1, fr = -1, fc = -1, r2 = -1, c2 = -1, hs = -1, hr = -1, hc = -1, hr2 = -1, hc2 = -1;
        tablo.HucreAdAdresCoz("FormatSatir", ref fs, ref fr, ref fc, ref r2, ref c2);
        tablo.HucreAdAdresCoz("BaslaSatir", ref hs, ref hr, ref hc, ref hr2, ref hc2);
        if (fs < 0 || hs < 0) throw new InvalidOperationException("Bina/yapı listesi şablonunda FormatSatir veya BaslaSatir isimli bölgesi bulunamadı.");
        tablo.AktifSheetDegistir(fs); int min = tablo.SatirYukseklikAl(fr);
        List<Satir> satirlar = Oku(basvuru.dbCtpTeknikProje.dbCtpTeknikProjeJson);
        for (int i = 0; i < satirlar.Count; i++)
        {
            int hedef = hr + i; tablo.SatirAc(hs, hedef, 1); tablo.HucreKopyala(fs, fr, fc, fr, fc + 6, hs, hedef, hc); tablo.AktifSheetDegistir(hs);
            Satir s = satirlar[i]; bool yok = s.YatirimSekli == "Değişiklik yok / Kullanılacak";
            string[] d = [s.Ad, X(s.Durum,"Mevcut"), X(s.Durum,"Yeni"), yok ? "-" : X(s.YatirimSekli,"Yeni yapım"), yok ? "-" : X(s.YatirimSekli,"Genişletme / Modernizasyon"), X(s.Destek,"Evet"), X(s.Destek,"Hayır")];
            for (int c = 0; c < d.Length; c++) tablo.HucreDegerYaz(hedef, hc + c, d[c]);
            tablo.SatirYukseklikAyarla(hedef, hedef, -1, 0, min);
        }
        tablo.AktifSheetDegistir(fs); tablo.SatirSil(fr, fr);
    }
    private static string X(string deger, string beklenen) => deger == beklenen ? "X" : "";
    private static List<Satir> Oku(string? json)
    {
        List<Satir> sonuc = []; if (string.IsNullOrWhiteSpace(json)) return sonuc;
        using JsonDocument b = JsonDocument.Parse(json);
        if (!b.RootElement.TryGetProperty("buildingRows", out JsonElement d) || d.ValueKind != JsonValueKind.Array) return sonuc;
        foreach (JsonElement x in d.EnumerateArray()) { Satir s = new(RaporJson.DegerMetni(x,"name"), RaporJson.DegerMetni(x,"assetStatus"), RaporJson.DegerMetni(x,"investmentType"), RaporJson.DegerMetni(x,"supportRequested")); if (!string.IsNullOrWhiteSpace(s.Ad)) sonuc.Add(s); }
        return sonuc;
    }
    private sealed record Satir(string Ad, string Durum, string YatirimSekli, string Destek);
}
