using System.Globalization;
using TarimDonusum.Araclar;
using TarimDonusum.Models;

namespace TarimDonusum.Raporlar;

public sealed class RPROB_TedarikciEntegrasyonu(string uygulamaRootPath) : RPROBTemel(uygulamaRootPath)
{
    protected override string SablonAdi => "TedarikciEntegrasyonu.xlsx";
    protected override string GeciciDosyaOnEki => "tedarikci-entegrasyonu";
    protected override string CiktiDosyaOnEki => "TedarikciEntegrasyonu";

    private const int FormSayfasi = 0, OzetSayfasi = 1;
    private const int IlkSatir = 14, SablonSatirSayisi = 15, SonSutun = 15;
    private const int OzetIlkSatir = 2, OzetSablonSatirSayisi = 16, OzetSonSutun = 5;

    protected override void Doldur(Tablo t, Basvuru b)
    {
        List<BasvuruTedarikciEntegrasyonu> satirlar = b.TedarikciEntegrasyonlari.OrderBy(x => x.urunId).ThenBy(x => x.id).ToList();
        Dictionary<int, string> urunler = b.YatirimOnBilgileri.Where(x => x.tur is enumYatirimOnBilgiTuru.MevcutUrun or enumYatirimOnBilgiTuru.UretilecekUrun).ToDictionary(x => x.id, x => x.ad);
        BasvuruUygulamaAdresi? adres = b.YatirimAdresleri.OrderBy(x => x.siraNo).ThenBy(x => x.id).FirstOrDefault();

        t.AktifSheetDegistir(FormSayfasi);
        t.HucreDegerYaz(4, 1, (b.BasvuruAnaId > 0 ? b.BasvuruAnaId : b.Id).ToString());
        t.HucreDegerYaz(4, 5, b.basvuruFirma.firma.ticaretUnvani ?? "");
        t.HucreDegerYaz(4, 11, b.yatirim.yatirimAdi ?? "");
        t.HucreDegerYaz(5, 1, string.Join(" / ", new[] { adres?.ilAdi ?? b.basvuruFirma.il.ad, adres?.ilceAdi ?? "" }.Where(x => !string.IsNullOrWhiteSpace(x))));
        Yaz(t, 5, 5, b.HesaplananTalepEdilenFinansmanTutari);
        t.HucreDegerYaz(5, 11, DateTime.Today.ToString("dd.MM.yyyy"));

        int satirSayisi = Math.Max(SablonSatirSayisi, satirlar.Count);
        int kayma = SatirlariGenislet(t, IlkSatir, SablonSatirSayisi, satirSayisi, SonSutun);
        for (int n = 0; n < satirSayisi; n++)
        {
            int r = IlkSatir + n;
            BasvuruTedarikciEntegrasyonu? x = n < satirlar.Count ? satirlar[n] : null;
            t.HucreDegerYaz(r, 0, n + 1);
            Yaz(t, r, 1, x == null ? null : urunler.GetValueOrDefault(x.urunId));
            Yaz(t, r, 2, x?.tarimsalUrun);
            Yaz(t, r, 3, x?.ilAdi);
            Yaz(t, r, 4, x?.ilceAdi);
            Yaz(t, r, 5, x?.birim);
            Yaz(t, r, 6, x?.mevcutYillikMiktar);
            Yaz(t, r, 7, x?.mevcutBirimFiyat);
            Yaz(t, r, 8, x?.hedefYillikMiktar);
            Yaz(t, r, 9, x?.hedefBirimFiyat);
            Yaz(t, r, 10, x?.mevcutKayitliCiftci);
            Yaz(t, r, 11, x?.eklenecekKayitliCiftci);
            Yaz(t, r, 12, x == null ? null : x.tedarikSekli == 1 ? "Sözleşmeli tedarik" : "Niyet / protokol");
            Yaz(t, r, 13, x == null ? null : BelgeAciklamasi(x));
            // SEGE kademesi sistemdeki ilçe kaydından gelir; ilçede kademe yoksa şablondaki il + ilçe eşleştirme formülü kalır.
            if (x?.segeKademesi is >= 1 and <= 6)
                t.HucreDegerYaz(r, 14, x.segeKademesi.Value);
        }

        t.HucreDegerYaz(32 + kayma, 0, b.tedarikciEntegrasyonuAciklama ?? "");
        t.HucreDegerYaz(39 + kayma, 1, b.irtibat.kisi ?? "");
        t.HucreDegerYaz(39 + kayma, 5, b.irtibat.unvan ?? "");
        t.HucreDegerYaz(39 + kayma, 11, DateTime.Today.ToString("dd.MM.yyyy"));

        // Özet sayfasındaki dinamik dizi formülleri (SORT/UNIQUE/FILTER) yerine ürün adları değer olarak yazılır.
        List<string> tarimsalUrunler = satirlar.Select(x => x.tarimsalUrun).Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct().OrderBy(x => x, StringComparer.Create(new CultureInfo("tr-TR"), true)).ToList();
        t.AktifSheetDegistir(OzetSayfasi);
        int ozetSatirSayisi = Math.Max(OzetSablonSatirSayisi, tarimsalUrunler.Count);
        SatirlariGenislet(t, OzetIlkSatir, OzetSablonSatirSayisi, ozetSatirSayisi, OzetSonSutun);
        // Aspose, form sayfasına satır eklenince diğer sayfalardaki aralıkları güncellemediği için aralıklar yeniden yazılır.
        int s = IlkSatir + satirSayisi;
        string C = $"'Başvuru Formu'!$C$15:$C${s}", K = $"'Başvuru Formu'!$K$15:$K${s}", O = $"'Başvuru Formu'!$O$15:$O${s}", P = $"'Başvuru Formu'!$P$15:$P${s}";
        for (int n = 0; n < ozetSatirSayisi; n++)
        {
            int r = OzetIlkSatir + n, er = r + 1;
            Yaz(t, r, 0, n < tarimsalUrunler.Count ? tarimsalUrunler[n] : null);
            t.HucreFormulYaz(r, 1, $"=IF(A{er}=\"\",\"\",IF(COUNTIF({C},A{er})<>COUNTIFS({C},A{er},{P},\">0\"),\"\",SUMIF({C},A{er},{P})))");
            t.HucreFormulYaz(r, 2, $"=IF(OR(A{er}=\"\",B{er}=\"\"),\"\",SUMIFS({P},{C},A{er},{O},\">=4\",{O},\"<=6\"))");
            t.HucreFormulYaz(r, 5, $"=IF(A{er}=\"\",\"\",SUMIF({C},A{er},{K}))");
        }

        t.AktifSheetDegistir(FormSayfasi);
        t.CalculateFormula();
    }

    private static int SatirlariGenislet(Tablo t, int ilkSatir, int sablonSatirSayisi, int satirSayisi, int sonSutun)
    {
        int kayma = satirSayisi - sablonSatirSayisi;
        if (kayma <= 0) return 0;
        // Satırlar aralığın içine açılır; böylece formüllerdeki aralıklar da genişler.
        double yukseklik = t.SatirGercekYukseklikAl(ilkSatir);
        int acilacakYer = ilkSatir + sablonSatirSayisi - 1;
        t.SatirAc(acilacakYer, kayma);
        for (int r = acilacakYer; r < acilacakYer + kayma; r++)
        {
            t.HucreKopyala(ilkSatir, 0, ilkSatir, sonSutun, r, 0);
            t.SatirGercekYukseklikAyarla(r, r, yukseklik);
        }
        return kayma;
    }

    private static string BelgeAciklamasi(BasvuruTedarikciEntegrasyonu x)
    {
        string belge = string.IsNullOrWhiteSpace(x.dayanakBelgeDosyaAdi) ? "Belge yok" : $"Belge var: {x.dayanakBelgeDosyaAdi}";
        return string.IsNullOrWhiteSpace(x.kisaAciklama) ? belge : $"{belge} / {x.kisaAciklama}";
    }

    // Boş değer hücreyi temizler; boş metin yazılırsa şablon formülleri satırı dolu sayar.
    private static void Yaz(Tablo t, int r, int c, string? v) => t.HucreDegerYaz(r, c, string.IsNullOrEmpty(v) ? null! : v);

    private static void Yaz(Tablo t, int r, int c, decimal? v)
    {
        if (v.HasValue) t.HucreDegerYaz(r, c, v.Value);
        else Yaz(t, r, c, (string?)null);
    }

    private static void Yaz(Tablo t, int r, int c, int? v)
    {
        if (v.HasValue) t.HucreDegerYaz(r, c, v.Value);
        else Yaz(t, r, c, (string?)null);
    }
}
