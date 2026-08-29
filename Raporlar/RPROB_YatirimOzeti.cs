using System.Text.Json;
using TarimDonusum.Araclar;
using TarimDonusum.Models;

namespace TarimDonusum.Raporlar;

public sealed class RPROB_YatirimOzeti(string uygulamaRootPath) : RPROBTemel(uygulamaRootPath)
{
    protected override string SablonAdi => "YatirimOzetTablosu.xltx";
    protected override string GeciciDosyaOnEki => "yatirim-ozeti";
    protected override string CiktiDosyaOnEki => "YatirimOzeti";

    protected override void Doldur(Tablo tablo, Basvuru basvuru)
    {
        YatirimOzetiVerisi veri = Oku(basvuru.yatirimOzeti.yatirimOzetiJson);
        ButceyiYaz(tablo, veri.Butce);

        const int urunIlkSatir = 20; // Excel 21
        const int urunBlokSatirSayisi = 4;
        int urunBlokSayisi = Math.Max(1, veri.Urunler.Count);
        int urunKaymasi = (urunBlokSayisi - 1) * urunBlokSatirSayisi;
        if (urunKaymasi > 0)
        {
            tablo.SatirAc(urunIlkSatir + urunBlokSatirSayisi, urunKaymasi);
            for (int blok = 1; blok < urunBlokSayisi; blok++)
            {
                int hedef = urunIlkSatir + blok * urunBlokSatirSayisi;
                tablo.HucreKopyala(urunIlkSatir, 0, urunIlkSatir + 3, 13, hedef, 0);
                for (int satir = 0; satir < urunBlokSatirSayisi; satir++)
                    tablo.SatirGercekYukseklikAyarla(hedef + satir, hedef + satir, tablo.SatirGercekYukseklikAl(urunIlkSatir + satir));
            }
        }

        for (int urunNo = 0; urunNo < veri.Urunler.Count; urunNo++)
        {
            UrunSatiri urun = veri.Urunler[urunNo];
            int baslangic = urunIlkSatir + urunNo * urunBlokSatirSayisi;
            tablo.HucreDegerYaz(baslangic, 0, urun.Ad);
            tablo.HucreDegerYaz(baslangic, 2, urun.Birim);

            string[] gostergeler = ["capacity", "production", "sales", "price"];
            for (int gostergeNo = 0; gostergeNo < gostergeler.Length; gostergeNo++)
            {
                List<decimal> degerler = urun.Veriler.GetValueOrDefault(gostergeler[gostergeNo]) ?? [];
                for (int donem = 0; donem < Math.Min(11, degerler.Count); donem++)
                    tablo.HucreDegerYaz(baslangic + gostergeNo, 3 + donem, degerler[donem]);
            }
        }

        int giderIlkSatir = 27 + urunKaymasi; // Excel 28 + ürün kayması
        int giderSatirSayisi = Math.Max(1, veri.Giderler.Count);
        int giderKaymasi = giderSatirSayisi - 1;
        double giderSatirYuksekligi = tablo.SatirGercekYukseklikAl(giderIlkSatir);
        if (giderKaymasi > 0)
        {
            tablo.SatirAc(giderIlkSatir + 1, giderKaymasi);
            for (int i = 1; i < giderSatirSayisi; i++)
            {
                tablo.HucreKopyala(giderIlkSatir, 0, giderIlkSatir, 9, giderIlkSatir + i, 0);
                tablo.SatirGercekYukseklikAyarla(giderIlkSatir + i, giderIlkSatir + i, giderSatirYuksekligi);
            }
        }

        for (int i = 0; i < veri.Giderler.Count; i++)
        {
            GiderSatiri gider = veri.Giderler[i];
            int satir = giderIlkSatir + i;
            tablo.HucreDegerYaz(satir, 0, gider.Grup);
            tablo.HucreDegerYaz(satir, 1, gider.Unsur);
            tablo.HucreDegerYaz(satir, 2, gider.Miktar);
            tablo.HucreDegerYaz(satir, 3, gider.Birim);
            tablo.HucreDegerYaz(satir, 4, gider.BirimFiyat);
            tablo.HucreDegerYaz(satir, 5, gider.Toplam);
            tablo.HucreDegerYaz(satir, 6, gider.SabitYuzde / 100m);
            tablo.HucreDegerYaz(satir, 7, gider.DegiskenYuzde / 100m);
            tablo.HucreDegerYaz(satir, 8, gider.SabitTutar);
            tablo.HucreDegerYaz(satir, 9, gider.DegiskenTutar);
        }

        int toplamSatiri = 29 + urunKaymasi + giderKaymasi; // Excel 30 + kaymalar
        tablo.HucreDegerYaz(toplamSatiri, 5, veri.Giderler.Sum(x => x.Toplam));
        tablo.HucreDegerYaz(toplamSatiri, 8, veri.Giderler.Sum(x => x.SabitTutar));
        tablo.HucreDegerYaz(toplamSatiri, 9, veri.Giderler.Sum(x => x.DegiskenTutar));
    }

    private static void ButceyiYaz(Tablo tablo, Dictionary<string, decimal> tutarlar)
    {
        decimal uygun = Topla(tutarlar, "A1", "A2", "A3", "A4");
        decimal uygunOlmayan = Topla(tutarlar, "B1", "B2", "B3", "B4", "B5", "B6", "B7");
        decimal toplamYatirim = uygun + uygunOlmayan;
        decimal ozkaynak = tutarlar.GetValueOrDefault("D");
        decimal banka = tutarlar.GetValueOrDefault("E");
        decimal dunyaBankasi = tutarlar.GetValueOrDefault("F");
        decimal diger = tutarlar.GetValueOrDefault("G");
        decimal finansmanToplami = ozkaynak + banka + dunyaBankasi + diger;

        tablo.HucreDegerYaz(4, 3, uygun);
        tablo.HucreDegerYaz(5, 3, tutarlar.GetValueOrDefault("A1"));
        tablo.HucreDegerYaz(6, 3, tutarlar.GetValueOrDefault("A2"));
        tablo.HucreDegerYaz(7, 3, tutarlar.GetValueOrDefault("A3"));
        tablo.HucreDegerYaz(8, 3, tutarlar.GetValueOrDefault("A4"));
        tablo.HucreDegerYaz(9, 3, uygunOlmayan);
        tablo.HucreDegerYaz(10, 3, toplamYatirim);
        tablo.HucreDegerYaz(11, 3, ozkaynak);
        tablo.HucreDegerYaz(12, 3, banka);
        tablo.HucreDegerYaz(13, 3, dunyaBankasi);
        tablo.HucreDegerYaz(14, 3, diger);
        tablo.HucreDegerYaz(15, 3, finansmanToplami);
        tablo.HucreDegerYaz(16, 3, finansmanToplami - toplamYatirim);
    }

    private static decimal Topla(Dictionary<string, decimal> tutarlar, params string[] anahtarlar) =>
        anahtarlar.Sum(tutarlar.GetValueOrDefault);

    private static YatirimOzetiVerisi Oku(string? json)
    {
        Dictionary<string, decimal> butce = new(StringComparer.OrdinalIgnoreCase);
        List<UrunSatiri> urunler = [];
        List<GiderSatiri> giderler = [];
        if (string.IsNullOrWhiteSpace(json)) return new(butce, urunler, giderler);

        using JsonDocument belge = JsonDocument.Parse(json);
        JsonElement kok = belge.RootElement;
        if (kok.TryGetProperty("investmentBudgetData", out JsonElement butceJson) && butceJson.ValueKind == JsonValueKind.Object)
        {
            foreach (JsonProperty satir in butceJson.EnumerateObject())
                if (satir.Value.ValueKind == JsonValueKind.Object && satir.Value.TryGetProperty("amount", out JsonElement tutar))
                    butce[satir.Name] = RPROB_ProjeButcesi.SayiOku(tutar);
        }

        if (kok.TryGetProperty("productionRows", out JsonElement urunJson) && urunJson.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement satir in urunJson.EnumerateArray())
            {
                Dictionary<string, List<decimal>> veriler = new(StringComparer.OrdinalIgnoreCase);
                if (satir.TryGetProperty("data", out JsonElement veri) && veri.ValueKind == JsonValueKind.Object)
                {
                    foreach (string gosterge in new[] { "capacity", "production", "sales", "price" })
                    {
                        List<decimal> degerler = [];
                        if (veri.TryGetProperty(gosterge, out JsonElement dizi) && dizi.ValueKind == JsonValueKind.Array)
                            foreach (JsonElement deger in dizi.EnumerateArray()) degerler.Add(RPROB_ProjeButcesi.SayiOku(deger));
                        veriler[gosterge] = degerler;
                    }
                }
                urunler.Add(new(RaporJson.Metin(satir, "name"), RaporJson.Metin(satir, "unit"), veriler));
            }
        }

        if (kok.TryGetProperty("operatingExpenseRows", out JsonElement giderJson) && giderJson.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement satir in giderJson.EnumerateArray())
            {
                decimal miktar = RaporJson.Sayi(satir, "qty");
                decimal birimFiyat = RaporJson.Sayi(satir, "unitPrice");
                decimal sabit = RaporJson.Sayi(satir, "fixedPct");
                decimal degisken = RaporJson.Sayi(satir, "variablePct");
                decimal toplam = miktar * birimFiyat;
                giderler.Add(new(RaporJson.Metin(satir, "group"), RaporJson.Metin(satir, "item"), miktar,
                    RaporJson.Metin(satir, "unit"), birimFiyat, toplam, sabit, degisken,
                    toplam * sabit / 100m, toplam * degisken / 100m));
            }
        }

        return new(butce, urunler, giderler);
    }

    private sealed record YatirimOzetiVerisi(Dictionary<string, decimal> Butce, List<UrunSatiri> Urunler, List<GiderSatiri> Giderler);
    private sealed record UrunSatiri(string Ad, string Birim, Dictionary<string, List<decimal>> Veriler);
    private sealed record GiderSatiri(string Grup, string Unsur, decimal Miktar, string Birim, decimal BirimFiyat,
        decimal Toplam, decimal SabitYuzde, decimal DegiskenYuzde, decimal SabitTutar, decimal DegiskenTutar);
}
