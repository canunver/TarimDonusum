using System.Text.Json;
using TarimDonusum.Araclar;
using TarimDonusum.Models;

namespace TarimDonusum.Raporlar;

public sealed class RPROB_OnBilgiler(string uygulamaRootPath) : RPROBTemel(uygulamaRootPath)
{
    protected override string SablonAdi => "OnBilgiler.xltx";
    protected override string GeciciDosyaOnEki => "on-bilgiler";
    protected override string CiktiDosyaOnEki => "OnBilgiler";

    protected override void Doldur(Tablo tablo, Basvuru basvuru)
    {
        tablo.HucreAdBulYaz("YatrimAd", basvuru.yatirim.yatirimAdi ?? "");
        tablo.HucreAdBulYaz("SahipAd", basvuru.basvuruFirma.firma.ticaretUnvani ?? "");
        tablo.HucreAdBulYaz("YatirimAmaci", basvuru.yatirim.yatiriminAmaci ?? "");
        using JsonDocument belge = JsonDocument.Parse(string.IsNullOrWhiteSpace(basvuru.dbCtpTeknikProje.dbCtpTeknikProjeJson) ? "{}" : basvuru.dbCtpTeknikProje.dbCtpTeknikProjeJson);
        JsonElement kok = belge.RootElement;
        ListeYaz(tablo, kok, "existingProducts", "BaslaSatirMevcut", ["product", "capacity"]);
        ListeYaz(tablo, kok, "plannedProducts", "BaslaSatirUretilecek", ["product", "capacity"]);
        ListeYaz(tablo, kok, "inputs", "BaslaSatirGirdiler", ["input", "need"]);
        ListeYaz(tablo, kok, "solarRows", "BaslaSatirEnerjiSayi", ["type", "panels", "panelPower", "totalPower"]);
        ListeYaz(tablo, kok, "installedRows", "BaslaSatirEnerjiGuc", ["type", "power"]);
    }

    private static void ListeYaz(Tablo tablo, JsonElement kok, string listeAlani, string bolge, string[] alanlar)
    {
        List<string[]> satirlar = [];
        if (kok.TryGetProperty(listeAlani, out JsonElement liste) && liste.ValueKind == JsonValueKind.Array)
            foreach (JsonElement kayit in liste.EnumerateArray())
            {
                string[] degerler = alanlar.Select(a => RaporJson.DegerMetni(kayit, a)).ToArray();
                if (degerler.Any(d => !string.IsNullOrWhiteSpace(d))) satirlar.Add(degerler);
            }
        if (satirlar.Count == 0) return;
        int sh = -1, bas = -1, sut = -1, r2 = -1, c2 = -1;
        tablo.HucreAdAdresCoz(bolge, ref sh, ref bas, ref sut, ref r2, ref c2);
        if (sh < 0) throw new InvalidOperationException($"Ön bilgiler şablonunda {bolge} isimli bölgesi bulunamadı.");
        tablo.AktifSheetDegistir(sh);
        double yukseklik = tablo.SatirGercekYukseklikAl(bas);
        for (int i = 0; i < satirlar.Count; i++)
        {
            int hedef = bas + i;
            if (i > 0)
            {
                tablo.SatirAc(sh, hedef, 1);
                tablo.HucreKopyala(sh, bas, sut, bas, sut + 3, sh, hedef, sut);
                tablo.SatirGercekYukseklikAyarla(hedef, hedef, yukseklik);
            }
            for (int c = 0; c < satirlar[i].Length; c++) tablo.HucreDegerYaz(hedef, sut + c, satirlar[i][c]);
        }
    }
}
