using System.Text.Json;
using TarimDonusum.Araclar;
using TarimDonusum.Models;

namespace TarimDonusum.Raporlar;

public sealed class RPROB_OnBasvuruTamlik(string uygulamaRootPath) : KontrolRaporuTemel(uygulamaRootPath)
{
    protected override string SablonAdi => "OnBasvuruTamlik.xltx";
    protected override string GeciciDosyaOnEki => "on-basvuru-tamlik";
    protected override string CiktiDosyaOnEki => "OnBasvuruTamlik";
    protected override string? JsonAl(Basvuru basvuru) => basvuru.SistemDenetimAnketi;
    protected override string SonucAl(JsonElement satir)
    {
        string sonuc = RaporJson.Metin(satir, "sonuc");
        return string.Equals(sonuc, "Tam", StringComparison.OrdinalIgnoreCase) ? "E" : "H";
    }
}

public abstract class KontrolRaporuTemel(string uygulamaRootPath) : RPROBTemel(uygulamaRootPath)
{
    protected abstract string? JsonAl(Basvuru basvuru);
    protected abstract string SonucAl(JsonElement satir);

    protected override void Doldur(Tablo tablo, Basvuru basvuru)
    {
        int fs = -1, fr = -1, fc = -1, fr2 = -1, fc2 = -1;
        int hs = -1, hr = -1, hc = -1, hr2 = -1, hc2 = -1;
        tablo.HucreAdAdresCoz("FormatSatir", ref fs, ref fr, ref fc, ref fr2, ref fc2);
        tablo.HucreAdAdresCoz("BaslaSatir", ref hs, ref hr, ref hc, ref hr2, ref hc2);
        if (fs < 0 || hs < 0) throw new InvalidOperationException("Kontrol raporu şablonunda FormatSatir veya BaslaSatir isimli bölgesi bulunamadı.");
        tablo.AktifSheetDegistir(fs);
        int minimumYukseklik = tablo.SatirYukseklikAl(fr);
        List<KontrolSatiri> satirlar = SatirlariOku(JsonAl(basvuru));
        for (int i = 0; i < satirlar.Count; i++)
        {
            int hedef = hr + i;
            tablo.HucreKopyala(fs, fr, fc, fr, fc + 5, hs, hedef, hc);
            tablo.AktifSheetDegistir(hs);
            KontrolSatiri s = satirlar[i];
            tablo.HucreDegerYaz(hedef, hc, s.No);
            tablo.HucreDegerYaz(hedef, hc + 1, s.Konu);
            tablo.HucreDegerYaz(hedef, hc + 2, s.Soru);
            tablo.HucreDegerYaz(hedef, hc + 3, s.Kaynak);
            tablo.HucreDegerYaz(hedef, hc + 4, s.Sonuc);
            tablo.HucreDegerYaz(hedef, hc + 5, s.Aciklama);
            tablo.SatirYukseklikAyarla(hedef, hedef, -1, 0, minimumYukseklik);
        }
        tablo.AktifSheetDegistir(fs);
        tablo.SatirSil(fr, fr);
    }

    private List<KontrolSatiri> SatirlariOku(string? json)
    {
        List<KontrolSatiri> sonuc = [];
        if (string.IsNullOrWhiteSpace(json)) return sonuc;
        using JsonDocument belge = JsonDocument.Parse(json);
        if (belge.RootElement.ValueKind != JsonValueKind.Array) return sonuc;
        foreach (JsonElement s in belge.RootElement.EnumerateArray())
        {
            int no = s.TryGetProperty("no", out JsonElement n) && n.TryGetInt32(out int d) ? d : 0;
            sonuc.Add(new(no, RaporJson.Metin(s, "konu"), RaporJson.Metin(s, "soru"), RaporJson.Metin(s, "kaynak"), SonucAl(s), RaporJson.Metin(s, "aciklama")));
        }
        return sonuc;
    }

    private sealed record KontrolSatiri(int No, string Konu, string Soru, string Kaynak, string Sonuc, string Aciklama);
}
