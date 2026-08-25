using TarimDonusum.Araclar;
using TarimDonusum.Models;

namespace TarimDonusum.Raporlar;

public sealed class RPROB_DegerZinciri(string uygulamaRootPath) : RPROBTemel(uygulamaRootPath)
{
    protected override string SablonAdi => "DegerZinciri.xltx";
    protected override string GeciciDosyaOnEki => "deger-zinciri";
    protected override string CiktiDosyaOnEki => "DegerZinciri";

    protected override void Doldur(Tablo tablo, Basvuru basvuru)
    {
        BasvuruYatirim y = basvuru.yatirim;
        List<DegerZinciriAsama> asamalar = (y.degerZinciriAsamalari ?? [])
            .OrderBy(x => x.siraNo)
            .ThenBy(x => x.ad)
            .ToList();
        string degerZinciriAdi = asamalar.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.dz?.ad))?.dz.ad ?? "";

        Yaz(tablo, 5, 2, degerZinciriAdi);
        Yaz(tablo, 5, 6, y.ilDegerZinciriEslesmesi);
        Yaz(tablo, 6, 2, y.tarimGidaBaglantiTuru);
        Yaz(tablo, 6, 6, y.tarimGidaBaglantiAciklamasi);
        Yaz(tablo, 7, 2, y.yatirimAlaniTipolojisi);
        Yaz(tablo, 7, 6, y.degerZinciriUygunlukAciklamasi);

        const int formatSatiri = 10;
        int yazilacakSatirSayisi = Math.Max(1, asamalar.Count);
        if (yazilacakSatirSayisi > 1)
        {
            tablo.SatirAc(11, yazilacakSatirSayisi - 1);
            double satirYuksekligi = tablo.SatirGercekYukseklikAl(formatSatiri);
            for (int i = 1; i < yazilacakSatirSayisi; i++)
            {
                tablo.HucreKopyala(formatSatiri, 0, formatSatiri, 7, formatSatiri + i, 0);
                tablo.HucreBirlestir(formatSatiri + i, 3, formatSatiri + i, 7);
                tablo.SatirGercekYukseklikAyarla(formatSatiri + i, formatSatiri + i, satirYuksekligi);
            }
        }
        if (asamalar.Count == 0)
        {
            Yaz(tablo, 11, 1, "");
            Yaz(tablo, 11, 2, "");
            Yaz(tablo, 11, 3, "");
            Yaz(tablo, 11, 4, "");
        }
        for (int i = 0; i < asamalar.Count; i++)
        {
            DegerZinciriAsama asama = asamalar[i];
            int satir = 11 + i;
            Yaz(tablo, satir, 1, asama.secili ? "Evet" : "Hayır");
            Yaz(tablo, satir, 2, asama.ad);
            Yaz(tablo, satir, 3, asama.aciklama);
            Yaz(tablo, satir, 4, asama.secili ? asama.yapilacakFaaliyetler : "");
        }

        int rekabetBaslikSatiri = 12 + yazilacakSatirSayisi;
        Yaz(tablo, rekabetBaslikSatiri + 1, 3, y.oncelikliYatirimUyumu);
        Yaz(tablo, rekabetBaslikSatiri + 1, 6, y.oncelikliYatirimKonuKodu);
        Yaz(tablo, rekabetBaslikSatiri + 2, 3, y.ithalatBagimliligiUyumu);
        Yaz(tablo, rekabetBaslikSatiri + 2, 6, y.ithalatBagimliligiUrunKodu);
        Yaz(tablo, rekabetBaslikSatiri + 3, 3, y.hedefUrunlerPazarCiktisi);
        Yaz(tablo, rekabetBaslikSatiri + 3, 6, y.rekabetcilikAciklamasi);
    }

    private static void Yaz(Tablo tablo, int satir, int sutun, string? deger) =>
        tablo.HucreDegerYaz(satir - 1, sutun - 1, deger ?? "");
}
