using TarimDonusum.Araclar;
using TarimDonusum.Models;

namespace TarimDonusum.Raporlar;

public sealed class RPROB_BasvuruSahibi(string uygulamaRootPath, bool onBasvuruCiktisi = false) : RPROBTemel(uygulamaRootPath)
{
    protected override string SablonAdi => "BasvuruSahibi.xltx";
    protected override string GeciciDosyaOnEki => onBasvuruCiktisi ? "on-basvuru-sahibi" : "basvuru-sahibi";
    protected override string CiktiDosyaOnEki => onBasvuruCiktisi ? "OnBasvuruSahibi" : "BasvuruSahibi";

    protected override void Doldur(Tablo tablo, Basvuru basvuru)
    {
        Firma f = basvuru.basvuruFirma.firma;
        BasvuruIrtibat i = basvuru.irtibat;
        BasvuruFirma bf = basvuru.basvuruFirma;

        Yaz(tablo, 5, 2, f.ticaretUnvani); Yaz(tablo, 5, 4, f.ticaretSicilNo);
        Yaz(tablo, 6, 2, f.kurulusTarihi?.ToString("dd.MM.yyyy")); Yaz(tablo, 6, 4, EvetHayir(bf.sonIkiYildirFaalMi));
        Yaz(tablo, 7, 2, f.telefon); Yaz(tablo, 7, 4, f.webSitesi);
        Yaz(tablo, 8, 2, f.vergiKimlikNo); Yaz(tablo, 8, 4, f.mersisNo);
        Yaz(tablo, 9, 2, f.naceKodu); Yaz(tablo, 9, 4, f.kepAdresi);
        Yaz(tablo, 10, 2, f.eposta); Yaz(tablo, 10, 4, BasvuruSahibiTuru(bf.basvuruSahibiTuru));
        Yaz(tablo, 11, 2, HukukiTur(bf.hukukiTurSirketTuru)); Yaz(tablo, 11, 4, i.kisi);
        Yaz(tablo, 12, 2, i.unvan); Yaz(tablo, 12, 4, i.telefon);
        Yaz(tablo, 13, 2, i.ePosta);
        Yaz(tablo, 13, 4, KisileriBirlestir(basvuru.AdliSicilKisileri, x => GorevEsit(x.gorev, "Temsil ve ilzama yetkili")));
        Yaz(tablo, 15, 2, f.faaliyetKonusu);
        Yaz(tablo, 16, 2, i.adres);
        Yaz(tablo, 17, 2, KisileriBirlestir(basvuru.AdliSicilKisileri, x =>
            GorevEsit(x.gorev, "Y\u00f6netim kurulu \u00fcye") ||
            GorevEsit(x.gorev, "Adli Sicil Kontrol\u00fcne Tabi Ek Ki\u015fi")));

        if (onBasvuruCiktisi)
        {
            tablo.SatirSil(18, 19);
            return;
        }

        Yaz(tablo, 20, 2, EvetHayir(bf.onBasvuruSonrasiDegisiklikVarMi));
        Yaz(tablo, 20, 4, bf.onBasvuruSonrasiDegisiklikSebebi);
    }

    private static void Yaz(Tablo tablo, int satir, int sutun, string? deger) => tablo.HucreDegerYaz(satir - 1, sutun - 1, deger ?? "");
    private static string EvetHayir(bool? deger) => deger.HasValue ? (deger.Value ? "Evet" : "Hay\u0131r") : "";
    private static bool GorevEsit(string? gorev, string aranan) => string.Equals(gorev?.Trim(), aranan, StringComparison.OrdinalIgnoreCase);
    private static string KisileriBirlestir(IEnumerable<BasvuruAdliSicilKisi> kisiler, Func<BasvuruAdliSicilKisi, bool> filtre) =>
        string.Join(Environment.NewLine, kisiler.Where(filtre).OrderBy(x => x.siraNo)
            .Select(x => $"{x.ad} {x.soyad} - TCKN: {x.tckn} - G\u00f6rev: {x.gorev}"));

    private static string BasvuruSahibiTuru(enumBasvuruSahibiTuru? tur) => tur switch
    {
        enumBasvuruSahibiTuru.Isletme => "\u0130\u015fletme",
        enumBasvuruSahibiTuru.UreticiOrgutu => "\u00dcretici \u00f6rg\u00fct\u00fc",
        enumBasvuruSahibiTuru.Kooperatif => "Kooperatif",
        enumBasvuruSahibiTuru.Birlik => "Birlik",
        enumBasvuruSahibiTuru.Diger => "Di\u011fer",
        _ => ""
    };

    private static string HukukiTur(enumHukukiTurSirketTuru? tur) => tur switch
    {
        enumHukukiTurSirketTuru.AnonimSirket => "Anonim \u015firket",
        enumHukukiTurSirketTuru.LimitedSirket => "Limited \u015firket",
        enumHukukiTurSirketTuru.KollektifSirket => "Kollektif \u015firket",
        enumHukukiTurSirketTuru.KomanditSirket => "Komandit \u015firket",
        enumHukukiTurSirketTuru.UreticiOrgutuKooperatifBirlik => "\u00dcretici \u00f6rg\u00fct\u00fc / kooperatif / birlik",
        enumHukukiTurSirketTuru.Diger => "Di\u011fer",
        _ => ""
    };
}