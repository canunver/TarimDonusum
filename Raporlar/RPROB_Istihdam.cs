using TarimDonusum.Araclar;
using TarimDonusum.Models;

namespace TarimDonusum.Raporlar;

public sealed class RPROB_Istihdam(string uygulamaRootPath) : RPROBTemel(uygulamaRootPath)
{
    protected override string SablonAdi => "TamZamanliIstihdam.xlsx";
    protected override string GeciciDosyaOnEki => "tam-zamanli-istihdam";
    protected override string CiktiDosyaOnEki => "TamZamanliIstihdam";

    private const string SablonYili = "2026";
    private const int IlkSatir = 21, SablonSatirSayisi = 15, SonSutun = 11;

    protected override void Doldur(Tablo t, Basvuru b)
    {
        BasvuruIstihdam i = b.istihdam;
        List<BasvuruIstihdamSatir> satirlar = (i.satirlar ?? []).OrderBy(x => x.siraNo).ThenBy(x => x.id).ToList();
        int basvuruYili = b.basvuruFirma.donem.yil;
        int sonYil = basvuruYili - 1;

        // Şablondaki yıl ifadeleri dönem yılına göre güncellenir.
        foreach ((int r, int c) in new[] { (0, 0), (18, 0), (19, 0), (20, 4), (20, 7), (37, 0), (38, 0), (45, 0) })
            t.HucreDegerYaz(r, c, t.HucreDegerAl(r, c).Replace(SablonYili, sonYil.ToString()));

        BasvuruUygulamaAdresi? adres = b.YatirimAdresleri.OrderBy(x => x.siraNo).ThenBy(x => x.id).FirstOrDefault();
        t.HucreDegerYaz(4, 2, b.basvuruFirma.firma.ticaretUnvani ?? "");
        t.HucreDegerYaz(4, 8, KimlikBilgisi(b));
        t.HucreDegerYaz(5, 2, adres?.ilAdi ?? b.basvuruFirma.il.ad ?? "");
        t.HucreDegerYaz(5, 6, adres?.ilceAdi ?? "");
        Yaz(t, 5, 10, b.HesaplananTalepEdilenFinansmanTutari);

        t.HucreDegerYaz(8, 0, basvuruYili - 2); t.HucreDegerYaz(8, 1, i.oncekiYilKadin); t.HucreDegerYaz(8, 2, i.oncekiYilErkek);
        t.HucreDegerYaz(9, 0, sonYil); t.HucreDegerYaz(9, 1, i.sonYilKadin); t.HucreDegerYaz(9, 2, i.sonYilErkek);

        int satirSayisi = Math.Max(SablonSatirSayisi, satirlar.Count);
        int kayma = satirSayisi - SablonSatirSayisi;
        if (kayma > 0)
        {
            // Satırlar aralığın içine açılır; böylece özet formüllerindeki aralıklar da genişler.
            double yukseklik = t.SatirGercekYukseklikAl(IlkSatir);
            int acilacakYer = IlkSatir + SablonSatirSayisi - 1;
            t.SatirAc(acilacakYer, kayma);
            for (int r = acilacakYer; r < acilacakYer + kayma; r++)
            {
                t.HucreKopyala(IlkSatir, 0, IlkSatir, SonSutun, r, 0);
                t.HucreBirlestir(r, 8, r, SonSutun);
                t.SatirGercekYukseklikAyarla(r, r, yukseklik);
            }
        }

        string belge = string.IsNullOrWhiteSpace(i.sgkDosyaAdi) ? "" : $"Belge var: {i.sgkDosyaAdi}";
        for (int n = 0; n < satirSayisi; n++)
        {
            int r = IlkSatir + n;
            BasvuruIstihdamSatir? x = n < satirlar.Count ? satirlar[n] : null;
            Yaz(t, r, 0, x?.birimUnite);
            Yaz(t, r, 1, x?.gorevUretimHatti);
            Yaz(t, r, 2, x?.cinsiyet);
            Yaz(t, r, 3, x?.yasDurumu);
            Yaz(t, r, 4, x?.mevcutCalisan);
            Yaz(t, r, 5, x?.netCalisanArtisi);
            Yaz(t, r, 7, x?.bazAylikBrutUcret);
            Yaz(t, r, 8, x == null ? null : belge);
        }

        t.HucreDegerYaz(39 + kayma, 0, i.gerekceVarsayimlarDogrulamaYaklasimi ?? "");
        t.HucreDegerYaz(47 + kayma, 0, b.irtibat.kisi ?? "");
        t.HucreDegerYaz(47 + kayma, 4, b.irtibat.unvan ?? "");
        t.HucreDegerYaz(47 + kayma, 7, DateTime.Today.ToString("dd.MM.yyyy"));

        t.CalculateFormula();
    }

    // Boş değer hücreyi temizler; boş metin yazılırsa şablon formülleri satırı dolu sayar.
    private static void Yaz(Tablo t, int r, int c, string? v) => t.HucreDegerYaz(r, c, string.IsNullOrEmpty(v) ? null! : v);

    private static void Yaz(Tablo t, int r, int c, decimal? v)
    {
        if (v.HasValue) t.HucreDegerYaz(r, c, v.Value);
        else Yaz(t, r, c, (string?)null);
    }

    private static string KimlikBilgisi(Basvuru b)
    {
        string v = b.basvuruFirma.firma.vergiKimlikNo?.Trim() ?? "", m = b.basvuruFirma.firma.mersisNo?.Trim() ?? "";
        return string.Join(" / ", new[] { v, m }.Where(x => !string.IsNullOrWhiteSpace(x)));
    }
}
