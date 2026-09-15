using TarimDonusum.Araclar;
using TarimDonusum.Models;

namespace TarimDonusum.Raporlar;

public sealed class RPROB_MetrajCetveli(string uygulamaRootPath, int binaId) : RPROBTemel(uygulamaRootPath)
{
    public BasvuruMetrajVerisi? Veri { get; set; }

    protected override string SablonAdi => "MetrajCetveli.xltx";
    protected override string GeciciDosyaOnEki => "metraj-cetveli";
    protected override string CiktiDosyaOnEki => "MetrajCetveli";

    protected override void Doldur(Tablo tablo, Basvuru basvuru)
    {
        BasvuruMetrajBina bina = Veri?.binalar.FirstOrDefault(x => x.id == binaId)
            ?? throw new InvalidOperationException("Yazdırılacak bina veya metraj bilgileri bulunamadı.");

        SayfaDoldur(tablo, 0, bina, agirlik: false, sonSutun: 10);
        SayfaDoldur(tablo, 1, bina, agirlik: true, sonSutun: 12);
    }

    private static void SayfaDoldur(Tablo tablo, int sheet, BasvuruMetrajBina bina, bool agirlik, int sonSutun)
    {
        List<BasvuruMetrajBolum> bolumler = bina.bolumler
            .OrderBy(x => x.siraNo).ThenBy(x => x.id)
            .Select(x => new BasvuruMetrajBolum
            {
                id = x.id, kategori = x.kategori, siraNo = x.siraNo, ad = x.ad,
                pozlar = x.pozlar.Where(p => AgirlikPozu(p) == agirlik)
                    .OrderBy(p => p.siraNo).ThenBy(p => p.id).ToList()
            })
            .Where(x => x.pozlar.Count > 0).ToList();

        tablo.AktifSheetDegistir(sheet);
        tablo.SayfaYonuAta(SayfaYonu.YATAY);
        tablo.YanyanaSayfaSayisi(1);
        double kategoriSatirYuksekligi = tablo.SatirGercekYukseklikAl(2);
        double ilkBaslikSatirYuksekligi = tablo.SatirGercekYukseklikAl(3);
        double ikinciBaslikSatirYuksekligi = tablo.SatirGercekYukseklikAl(4);
        tablo.HucreDegerYaz(1, 0, $"Yapı/Bina Adı: {bina.ad}");

        if (bolumler.Count == 0)
        {
            tablo.HucreDegerYaz(2, 0, "Kayıt bulunmamaktadır.");
            for (int satir = 3; satir <= 5; satir++)
                for (int sutun = 0; sutun <= sonSutun; sutun++) tablo.HucreDegerYaz(satir, sutun, "");
            return;
        }

        int gerekenSatir = 2 + bolumler.Sum(b => 3 + b.pozlar.Sum(p => 1 + Math.Max(1, p.detaylar.Count) + 1) + 1);
        if (gerekenSatir > 6) tablo.SatirAc(sheet, 6, gerekenSatir - 6);

        int hedef = 2;
        foreach (BasvuruMetrajBolum bolum in bolumler)
        {
            SatirKopyalaVeTemizle(tablo, sheet, 2, hedef, sonSutun);
            tablo.SatirGercekYukseklikAyarla(hedef, hedef, kategoriSatirYuksekligi);
            tablo.HucreDegerYaz(hedef, 0, $"{(int)bolum.kategori}. KATEGORİ: {bolum.ad}");
            hedef++;

            tablo.HucreKopyala(sheet, 3, 0, 4, sonSutun, sheet, hedef, 0);
            tablo.SatirGercekYukseklikAyarla(hedef, hedef, ilkBaslikSatirYuksekligi);
            tablo.SatirGercekYukseklikAyarla(hedef + 1, hedef + 1, ikinciBaslikSatirYuksekligi);
            BasliklariBirlestir(tablo, hedef, sonSutun);
            hedef += 2;

            foreach (BasvuruMetrajPoz poz in bolum.pozlar)
            {
                SatirKopyalaVeTemizle(tablo, sheet, 5, hedef, sonSutun);
                tablo.HucreDegerYaz(hedef, 0, poz.siraNo);
                tablo.HucreDegerYaz(hedef, 1, poz.pozNo);
                tablo.HucreDegerYaz(hedef, 2, poz.pozAdi);
                tablo.HucreDegerYaz(hedef, 3, poz.birim);
                tablo.KoyuYap(hedef, 0, hedef, 3, true);
                hedef++;

                List<BasvuruMetrajDetay> detaylar = poz.detaylar.OrderBy(x => x.siraNo).ThenBy(x => x.id).ToList();
                if (detaylar.Count == 0) detaylar.Add(new BasvuruMetrajDetay { siraNo = 1 });
                foreach (BasvuruMetrajDetay detay in detaylar)
                {
                    SatirKopyalaVeTemizle(tablo, sheet, 5, hedef, sonSutun);
                    tablo.KoyuYap(hedef, 0, hedef, sonSutun, false);
                    DetayYaz(tablo, hedef, poz, detay, agirlik);
                    hedef++;
                }

                SatirKopyalaVeTemizle(tablo, sheet, 5, hedef, sonSutun);
                tablo.HucreDegerYaz(hedef, 2, $"{poz.pozNo} Poz Toplamı");
                tablo.KoyuYap(hedef, 0, hedef, sonSutun, true);
                tablo.HucreDegerYaz(hedef, sonSutun, poz.miktar);
                if (agirlik) tablo.HucreFormatla(hedef, sonSutun, hedef, sonSutun, "#,##0.00####");
                hedef++;
            }

            for (int sutun = 0; sutun <= sonSutun; sutun++) tablo.HucreDegerYaz(hedef, sutun, "");
            hedef++;
        }
    }

    private static void BasliklariBirlestir(Tablo tablo, int baslikSatiri, int sonSutun)
    {
        for (int sutun = 0; sutun <= 4; sutun++)
            tablo.HucreBirlestir(baslikSatiri, sutun, baslikSatiri + 1, sutun);

        tablo.HucreBirlestir(baslikSatiri, 5, baslikSatiri, 7);
        tablo.DuseyHizala(baslikSatiri, 5, baslikSatiri, 7, 2);
        tablo.YatayHizala(baslikSatiri, 5, baslikSatiri, 7, 2);

        for (int sutun = 8; sutun <= sonSutun; sutun++)
            tablo.HucreBirlestir(baslikSatiri, sutun, baslikSatiri + 1, sutun);
    }

    private static void DetayYaz(Tablo tablo, int satir, BasvuruMetrajPoz poz, BasvuruMetrajDetay detay, bool agirlik)
    {
        tablo.HucreDegerYaz(satir, 2, detay.aciklama);
        Yaz(tablo, satir, 4, detay.adet);
        Yaz(tablo, satir, 5, detay.en);
        Yaz(tablo, satir, 6, detay.boy);
        Yaz(tablo, satir, 7, detay.yukseklik);
        tablo.HucreDegerYaz(satir, 8, detay.benzerSayisi);
        if (agirlik)
        {
            tablo.HucreDegerYaz(satir, 9, detay.urunCinsi);
            Yaz(tablo, satir, 10, detay.birimAgirlik);
            tablo.HucreDegerYaz(satir, 11, detay.Miktar(poz.hesaplamaTuru, poz.birim));
            tablo.HucreFormatla(satir, 10, satir, 12, "#,##0.00####");
        }
        else tablo.HucreDegerYaz(satir, 9, detay.Miktar(poz.hesaplamaTuru, poz.birim));
    }

    private static void Yaz(Tablo tablo, int satir, int sutun, decimal? deger)
    {
        if (deger.HasValue) tablo.HucreDegerYaz(satir, sutun, deger.Value);
    }

    private static bool AgirlikPozu(BasvuruMetrajPoz poz) =>
        poz.hesaplamaTuru == (int)enumPozHesaplamaTuru.Agirlik ||
        string.Equals(poz.birim?.Trim(), "Kg", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(poz.birim?.Trim(), "Ton", StringComparison.OrdinalIgnoreCase);

    private static void SatirKopyalaVeTemizle(Tablo tablo, int sheet, int kaynak, int hedef, int sonSutun)
    {
        if (kaynak != hedef) tablo.HucreKopyala(sheet, kaynak, 0, kaynak, sonSutun, sheet, hedef, 0);
        tablo.AktifSheetDegistir(sheet);
        for (int sutun = 0; sutun <= sonSutun; sutun++) tablo.HucreDegerYaz(hedef, sutun, "");
    }
}
