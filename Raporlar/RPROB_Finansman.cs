using TarimDonusum.Araclar;
using TarimDonusum.Models;

namespace TarimDonusum.Raporlar;

public sealed class RPROB_Finansman(string uygulamaRootPath) : RPROBTemel(uygulamaRootPath)
{
    protected override string SablonAdi => "Finansman.xltx";
    protected override string GeciciDosyaOnEki => "finansman";
    protected override string CiktiDosyaOnEki => "Finansman";

    protected override void Doldur(Tablo tablo, Basvuru basvuru)
    {
        BasvuruFinans f = basvuru.finans;
        Donem donem = basvuru.basvuruFirma.donem;
        decimal kur = basvuru.kayitTuru == enumBasvuruKayitTuru.OnBasvuru
            ? donem.onBasvuruCevrimKuru.GetValueOrDefault()
            : donem.basvuruCevrimKuru.GetValueOrDefault();
        decimal altLimitTl = donem.minimumYatirimTutari.GetValueOrDefault();
        decimal ustLimitTl = donem.maksimumYatirimTutari.GetValueOrDefault();
        decimal altLimitEur = kur > 0 ? Math.Round(altLimitTl / kur, 2) : 0;
        decimal ustLimitEur = kur > 0 ? Math.Round(ustLimitTl / kur, 2) : 0;
        decimal toplamButceTl = basvuru.yatirimOzeti.toplamYatirimButcesiTl;
        decimal? toplamYatirimEur = toplamButceTl > 0 && kur > 0
            ? Math.Round(toplamButceTl / kur, 2)
            : null;
        decimal? talepEdilenRffEur = toplamYatirimEur.HasValue && f.talepEdilenFinansmanOrani.HasValue
            ? Math.Round(toplamYatirimEur.Value * f.talepEdilenFinansmanOrani.Value / 100m, 2)
            : null;

        Yaz(tablo, 5, 2, toplamYatirimEur);
        Yaz(tablo, 5, 4, talepEdilenRffEur);
        Yaz(tablo, 7, 2, f.talepEdilenVadeSuresiAy);
        Yaz(tablo, 7, 4, f.odemeSuresiAy);
        Yaz(tablo, 8, 2, f.finansmanParaBirimi);
        Yaz(tablo, 8, 4, f.digerFinansmanKaynaklari);
        Yaz(tablo, 9, 2, f.oncekiRffOnayliTutar);
        Yaz(tablo, 9, 4, f.oncekiRffSozlesmesiKapaliMi);
        Yaz(tablo, 10, 1, $"Kümülatif {IsimBul.MetneCevirKurussuz(ustLimitEur)} EUR tavan kontrolü");
        Yaz(tablo, 10, 3, $"{IsimBul.MetneCevirKurussuz(altLimitEur)}–{IsimBul.MetneCevirKurussuz(ustLimitEur)} EUR limit kontrolü");
        tablo.HucreFormulYaz(9, 1, $"IF(OR(B9=\"\",D5=\"\"),\"\",IF(B9+D5<={FormulSayisi(ustLimitEur)},\"UYGUN\",\"TAVAN AŞILIYOR\"))");
        tablo.HucreFormulYaz(9, 3, $"IF(D5=\"\",\"\",IF(AND(D5>={FormulSayisi(altLimitEur)},D5<={FormulSayisi(ustLimitEur)}),\"UYGUN\",\"UYGUN DEĞİL\"))");
        Yaz(tablo, 11, 2, f.bankaTeminatMektubuSaglanabilirMi);
        Yaz(tablo, 14, 1, f.digerFinansmanKaynaklariAciklama);
    }

    private static string FormulSayisi(decimal deger) =>
        deger.ToString(System.Globalization.CultureInfo.InvariantCulture);

    private static void Yaz(Tablo tablo, int satir, int sutun, string? deger) =>
        tablo.HucreDegerYaz(satir - 1, sutun - 1, deger ?? "");

    private static void Yaz(Tablo tablo, int satir, int sutun, decimal? deger)
    {
        if (deger.HasValue)
            tablo.HucreDegerYaz(satir - 1, sutun - 1, Convert.ToDouble(deger.Value));
        else
            tablo.HucreDegerYaz(satir - 1, sutun - 1, "");
    }

    private static void Yaz(Tablo tablo, int satir, int sutun, int? deger)
    {
        if (deger.HasValue)
            tablo.HucreDegerYaz(satir - 1, sutun - 1, Convert.ToDouble(deger.Value));
        else
            tablo.HucreDegerYaz(satir - 1, sutun - 1, "");
    }
}
