using System.Text.Json;
using TarimDonusum.Araclar;
using TarimDonusum.Models;

namespace TarimDonusum.Raporlar;

public sealed class RPROB_MakineEkipman(string uygulamaRootPath) : RPROBTemel(uygulamaRootPath)
{
    protected override string SablonAdi => "MakineEkipman.xltx";
    protected override string GeciciDosyaOnEki => "makine-ekipman";
    protected override string CiktiDosyaOnEki => "MakineEkipman";

    protected override void Doldur(Tablo tablo, Basvuru basvuru)
    {
        int fs = -1, fr = -1, fc = -1, r2 = -1, c2 = -1, hs = -1, hr = -1, hc = -1, hr2 = -1, hc2 = -1;
        tablo.HucreAdAdresCoz("FormatSatir", ref fs, ref fr, ref fc, ref r2, ref c2);
        tablo.HucreAdAdresCoz("BaslaSatir", ref hs, ref hr, ref hc, ref hr2, ref hc2);
        if (fs < 0 || hs < 0) throw new InvalidOperationException("Makine-ekipman şablonunda FormatSatir veya BaslaSatir isimli bölgesi bulunamadı.");
        tablo.AktifSheetDegistir(fs);
        int min = tablo.SatirYukseklikAl(fr);
        List<Satir> satirlar = Oku(basvuru.dbCtpTeknikProje.dbCtpTeknikProjeJson);
        for (int i = 0; i < satirlar.Count; i++)
        {
            int hedef = hr + i;
            tablo.HucreKopyala(fs, fr, fc, fr, fc + 9, hs, hedef, hc);
            tablo.AktifSheetDegistir(hs);
            Satir s = satirlar[i];
            string[] degerler = [(i + 1).ToString(), s.Ad, s.Adet, s.Amac, X(s.Durum, "Mevcut"), X(s.Durum, "Yeni"), X(s.Kullanilacak, "Evet"), X(s.Kullanilacak, "Hayır"), X(s.Destek, "Evet"), X(s.Destek, "Hayır")];
            for (int c = 0; c < degerler.Length; c++) tablo.HucreDegerYaz(hedef, hc + c, degerler[c]);
            tablo.SatirYukseklikAyarla(hedef, hedef, -1, 0, min);
        }
        tablo.AktifSheetDegistir(fs); tablo.SatirSil(fr, fr);
    }
    private static string X(string deger, string beklenen) => deger == beklenen ? "X" : "";
    private static List<Satir> Oku(string? json)
    {
        List<Satir> sonuc = []; if (string.IsNullOrWhiteSpace(json)) return sonuc;
        using JsonDocument b = JsonDocument.Parse(json);
        if (!b.RootElement.TryGetProperty("machineryRows", out JsonElement d) || d.ValueKind != JsonValueKind.Array) return sonuc;
        foreach (JsonElement x in d.EnumerateArray()) { Satir s = new(RaporJson.DegerMetni(x,"name"), RaporJson.DegerMetni(x,"qty"), RaporJson.DegerMetni(x,"purpose"), RaporJson.DegerMetni(x,"assetStatus"), RaporJson.DegerMetni(x,"useInInvestment"), RaporJson.DegerMetni(x,"supportRequested")); if (!string.IsNullOrWhiteSpace(s.Ad) || !string.IsNullOrWhiteSpace(s.Amac)) sonuc.Add(s); }
        return sonuc;
    }
    private sealed record Satir(string Ad, string Adet, string Amac, string Durum, string Kullanilacak, string Destek);
}
