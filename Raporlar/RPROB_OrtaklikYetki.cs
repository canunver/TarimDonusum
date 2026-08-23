using TarimDonusum.Araclar;
using TarimDonusum.Models;

namespace TarimDonusum.Raporlar;

public sealed class RPROB_OrtaklikYetki(string uygulamaRootPath) : RPROBTemel(uygulamaRootPath)
{
    protected override string SablonAdi => "OrtaklikYetki.xltx";
    protected override string GeciciDosyaOnEki => "ortaklik-yetki";
    protected override string CiktiDosyaOnEki => "OrtaklikYetki";

    protected override void Doldur(Tablo tablo, Basvuru basvuru)
    {
        List<BasvuruOrtak> ortaklar = (basvuru.ortaklik?.ortaklar ?? []).OrderBy(x => x.siraNo).ToList();
        int ekOrtakSatiri = Math.Max(0, ortaklar.Count - 15);
        if (ekOrtakSatiri > 0)
        {
            tablo.SatirAc(23, ekOrtakSatiri);
            for (int i = 0; i < ekOrtakSatiri; i++) tablo.HucreKopyala(22, 0, 22, 11, 23 + i, 0);
        }

        int ortakSonSatir = Math.Max(23, 8 + ortaklar.Count);
        tablo.HucreFormulYaz(4, 1, $"IF(COUNTA(B9:B{ortakSonSatir},E9:E{ortakSonSatir})=0,\"\",SUM(E9:E{ortakSonSatir}))");
        tablo.HucreFormulYaz(4, 3, $"IF(COUNTA(B9:B{ortakSonSatir},E9:E{ortakSonSatir})=0,\"\",SUMIF(F9:F{ortakSonSatir},\"Özel\",E9:E{ortakSonSatir}))");
        tablo.HucreFormulYaz(4, 5, $"IF(COUNTA(B9:B{ortakSonSatir},E9:E{ortakSonSatir})=0,\"\",SUMIF(L9:L{ortakSonSatir},\"Kadın\",E9:E{ortakSonSatir})+SUMIF(L9:L{ortakSonSatir},\"Her ikisi\",E9:E{ortakSonSatir}))");
        tablo.HucreFormulYaz(4, 7, $"IF(COUNTA(B9:B{ortakSonSatir},E9:E{ortakSonSatir})=0,\"\",SUMIF(L9:L{ortakSonSatir},\"40 yaş altı\",E9:E{ortakSonSatir})+SUMIF(L9:L{ortakSonSatir},\"Her ikisi\",E9:E{ortakSonSatir}))");
        tablo.HucreFormulYaz(4, 9, "IF(OR(F5>0.5,H5>0.5),\"EVET\",\"HAYIR\")");
        Yaz(tablo, 5, 12, ortaklar.Any(x => !string.IsNullOrWhiteSpace(x.iliskiTuru)) ? "Evet" : "Hayır");

        for (int i = 0; i < ortaklar.Count; i++)
        {
            BasvuruOrtak ortak = ortaklar[i]; int satir = 9 + i;
            Yaz(tablo, satir, 1, i + 1); Yaz(tablo, satir, 2, ortak.adUnvan); Yaz(tablo, satir, 3, ortak.tcknVkn); Yaz(tablo, satir, 4, ortak.kisiTuru);
            Yaz(tablo, satir, 5, ortak.payOrani.GetValueOrDefault() / 100m); Yaz(tablo, satir, 6, ortak.ozelKamuNiteligi);
            Yaz(tablo, satir, 7, GercekKisiMi(ortak) ? ortak.cinsiyet : ""); Yaz(tablo, satir, 8, Yas(ortak.dogumTarihi));
            Yaz(tablo, satir, 9, string.IsNullOrWhiteSpace(ortak.nihaiFaydalaniciBilgisi) ? "Hayır" : "Evet"); Yaz(tablo, satir, 10, ortak.nihaiFaydalaniciBilgisi);
            Yaz(tablo, satir, 11, ortak.uboKycBelgeAdi); Yaz(tablo, satir, 12, ortak.sahiplikNiteligi);
        }

        List<BasvuruAdliSicilKisi> kisiler = (basvuru.AdliSicilKisileri ?? []).OrderBy(x => x.siraNo).ToList();
        int kisiBaslangicSatiri = 27 + ekOrtakSatiri;
        int ekKisiSatiri = Math.Max(0, kisiler.Count - 10);
        if (ekKisiSatiri > 0)
        {
            int eklemeSatiri = 36 + ekOrtakSatiri;
            tablo.SatirAc(eklemeSatiri, ekKisiSatiri);
            for (int i = 0; i < ekKisiSatiri; i++) tablo.HucreKopyala(eklemeSatiri - 1, 0, eklemeSatiri - 1, 6, eklemeSatiri + i, 0);
        }
        for (int i = 0; i < kisiler.Count; i++)
        {
            BasvuruAdliSicilKisi kisi = kisiler[i]; int satir = kisiBaslangicSatiri + i;
            Yaz(tablo, satir, 1, i + 1); Yaz(tablo, satir, 2, $"{kisi.ad} {kisi.soyad}".Trim()); Yaz(tablo, satir, 3, kisi.gorev);
            Yaz(tablo, satir, 4, kisi.yetkiKapsami); Yaz(tablo, satir, 5, kisi.imzaYetkiDosyaAdi); Yaz(tablo, satir, 6, kisi.dosyaAdi); Yaz(tablo, satir, 7, kisi.aciklama);
        }
    }

    private static bool GercekKisiMi(BasvuruOrtak ortak) => string.Equals(ortak.kisiTuru, "Gerçek Kişi", StringComparison.OrdinalIgnoreCase);
    private static int? Yas(DateTime? dogumTarihi) { if (!dogumTarihi.HasValue) return null; DateTime bugun=DateTime.Today,dogum=dogumTarihi.Value.Date; int yas=bugun.Year-dogum.Year; if(dogum>bugun.AddYears(-yas))yas--; return Math.Max(0,yas); }
    private static void Yaz(Tablo tablo,int satir,int sutun,string? deger)=>tablo.HucreDegerYaz(satir-1,sutun-1,deger??"");
    private static void Yaz(Tablo tablo,int satir,int sutun,decimal deger)=>tablo.HucreDegerYaz(satir-1,sutun-1,deger);
    private static void Yaz(Tablo tablo,int satir,int sutun,int deger)=>tablo.HucreDegerYaz(satir-1,sutun-1,deger);
    private static void Yaz(Tablo tablo,int satir,int sutun,int? deger) { if (deger.HasValue) tablo.HucreDegerYaz(satir-1,sutun-1,deger.Value); else tablo.HucreDegerYaz(satir-1,sutun-1,""); }
}