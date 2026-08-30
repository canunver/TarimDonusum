using TarimDonusum.Araclar;
using TarimDonusum.Models;

namespace TarimDonusum.Raporlar;

public sealed class RPROB_TedarikciEntegrasyonu(string uygulamaRootPath) : RPROBTemel(uygulamaRootPath)
{
    protected override string SablonAdi => "TedarikciEntegrasyonu.xltx";
    protected override string GeciciDosyaOnEki => "tedarikci-entegrasyonu";
    protected override string CiktiDosyaOnEki => "TedarikciEntegrasyonu";

    protected override void Doldur(Tablo t, Basvuru b)
    {
        List<BasvuruTedarikciEntegrasyonu> satirlar = b.TedarikciEntegrasyonlari.OrderBy(x => x.urunId).ThenBy(x => x.id).ToList();
        Dictionary<int, string> urunler = b.YatirimOnBilgileri.Where(x => x.tur == enumYatirimOnBilgiTuru.UretilecekUrun).ToDictionary(x => x.id, x => x.ad);
        BasvuruUygulamaAdresi? adres = b.YatirimAdresleri.OrderBy(x => x.siraNo).ThenBy(x => x.id).FirstOrDefault();

        Yaz(t, 4, 2, b.BasvuruAnaId > 0 ? b.BasvuruAnaId : b.Id);
        Yaz(t, 4, 6, b.basvuruFirma.firma.ticaretUnvani);
        Yaz(t, 4, 10, b.yatirim.yatirimAdi);
        Yaz(t, 5, 2, string.Join(" / ", new[] { adres?.ilAdi ?? b.basvuruFirma.il.ad, adres?.ilceAdi ?? "" }.Where(x => !string.IsNullOrWhiteSpace(x))));
        Yaz(t, 5, 6, b.finans.talepEdilenDestekTutari.GetValueOrDefault());
        Yaz(t, 5, 10, DateTime.Today);

        int mevcutCiftci = satirlar.Sum(x => x.mevcutKayitliCiftci);
        int eklenecekCiftci = satirlar.Sum(x => x.eklenecekKayitliCiftci);
        decimal destek = b.finans.talepEdilenDestekTutari.GetValueOrDefault();
        decimal? yeniCiftciYuzBin = destek > 0 ? eklenecekCiftci * 100000m / destek : null;
        decimal toplamHedef = satirlar.Sum(x => x.hedefYillikMiktar);
        bool segeVerisiTam = satirlar.All(x => x.segeKademesi is >= 1 and <= 6 && x.hedefYillikMiktar >= 0);
        decimal? segePayi = toplamHedef > 0 && segeVerisiTam ? satirlar.Where(x => x.segeKademesi is >= 4 and <= 6).Sum(x => x.hedefYillikMiktar) / toplamHedef : null;
        decimal? segePuani = segePayi.HasValue ? segePayi.Value switch { <= 0 => 0m, <= .20m => 1m, <= .50m => 4m, _ => 7.5m } : null;

        Yaz(t, 8, 2, mevcutCiftci); Yaz(t, 8, 4, eklenecekCiftci);
        Yaz(t, 8, 6, yeniCiftciYuzBin); Yaz(t, 8, 10, segePayi); Yaz(t, 8, 13, segePuani);
        Yaz(t, 9, 2, yeniCiftciYuzBin.HasValue && yeniCiftciYuzBin.Value >= 2 ? "SAĞLANIYOR" : "SAĞLANMIYOR");

        const int sablonSatiri = 13;
        int satirSayisi = Math.Max(1, satirlar.Count);
        if (satirSayisi > 1) t.SatirAc(sablonSatiri + 1, satirSayisi - 1);
        double yukseklik = t.SatirGercekYukseklikAl(sablonSatiri);
        for (int i = 0; i < satirSayisi; i++)
        {
            int r = sablonSatiri + i;
            if (i > 0) t.HucreKopyala(sablonSatiri, 0, sablonSatiri, 12, r, 0);
            for (int c = 0; c < 13; c++) t.HucreDegerYaz(r, c, "");
            t.SatirGercekYukseklikAyarla(r, r, yukseklik * 2);
            if (i >= satirlar.Count) continue;
            BasvuruTedarikciEntegrasyonu x = satirlar[i];
            t.HucreDegerYaz(r, 0, i + 1);
            t.HucreDegerYaz(r, 1, urunler.GetValueOrDefault(x.urunId, ""));
            t.HucreDegerYaz(r, 2, x.tarimsalUrun); t.HucreDegerYaz(r, 3, x.ilAdi); t.HucreDegerYaz(r, 4, x.ilceAdi);
            t.HucreDegerYaz(r, 5, "Ton"); t.HucreDegerYaz(r, 6, x.mevcutYillikMiktar); t.HucreDegerYaz(r, 7, x.hedefYillikMiktar);
            t.HucreDegerYaz(r, 8, x.mevcutKayitliCiftci); t.HucreDegerYaz(r, 9, x.eklenecekKayitliCiftci);
            t.HucreDegerYaz(r, 10, x.tedarikSekli == 1 ? "Sözleşmeli tedarik" : $"Niyet / protokol{Environment.NewLine}etiketi");
            string belgeDurumu = string.IsNullOrWhiteSpace(x.dayanakBelgeDosyaAdi) ? "Belge Yok" : "Belge Var";
            t.HucreDegerYaz(r, 11, string.IsNullOrWhiteSpace(x.kisaAciklama) ? belgeDurumu : $"{belgeDurumu} / {x.kisaAciklama}");
            t.HucreDegerYaz(r, 12, x.segeKademesi.GetValueOrDefault());
        }

        int kayma = satirSayisi - 1;
        t.HucreDegerYaz(15 + kayma, 0, b.tedarikciEntegrasyonuAciklama);
        t.HucreDegerYaz(21 + kayma, 1, b.irtibat.kisi);
        t.HucreDegerYaz(21 + kayma, 5, b.irtibat.unvan);
        t.HucreDegerYaz(21 + kayma, 9, DateTime.Today.ToString("dd.MM.yyyy"));
    }

    private static void Yaz(Tablo t, int r, int c, string? v) => t.HucreDegerYaz(r - 1, c - 1, v ?? "");
    private static void Yaz(Tablo t, int r, int c, int v) => t.HucreDegerYaz(r - 1, c - 1, v);
    private static void Yaz(Tablo t, int r, int c, decimal v) => t.HucreDegerYaz(r - 1, c - 1, v);
    private static void Yaz(Tablo t, int r, int c, decimal? v) { if (v.HasValue) Yaz(t, r, c, v.Value); else Yaz(t, r, c, ""); }
    private static void Yaz(Tablo t, int r, int c, DateTime v) => t.HucreDegerYaz(r - 1, c - 1, v);
}
