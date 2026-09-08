using System.Drawing;
using System.Text.Json;
using Aspose.Cells;
using TarimDonusum.IsKurallari;
using TarimDonusum.Models;

namespace TarimDonusum.Raporlar;

public sealed class RPROB_OnBasvuruCevreselSosyal(int uygulamaAdresiId) : IRPROB
{
    public string SablonDosyasi => "";

    public RaporDosyasi Olustur(Basvuru basvuru, int basvuruId)
    {
        BasvuruUygulamaAdresi adres = basvuru.YatirimAdresleri.FirstOrDefault(x => x.id == uygulamaAdresiId)
            ?? throw new InvalidOperationException("Seçilen yatırım adresi bulunamadı.");
        if (string.IsNullOrWhiteSpace(adres.cevreselSosyalJson))
            throw new InvalidOperationException("Seçilen yatırım adresinin çevresel ve sosyal anketi henüz doldurulmamış.");

        using JsonDocument belge = JsonDocument.Parse(adres.cevreselSosyalJson);
        JsonElement kok = belge.RootElement;
        string mod = RaporJson.Metin(kok, "mode");
        if (string.IsNullOrWhiteSpace(mod))
            mod = KapsamBelirle(adres);

        Dictionary<string, string> cevaplar = new(StringComparer.OrdinalIgnoreCase);
        if (kok.TryGetProperty("answers", out JsonElement cevapNesnesi) && cevapNesnesi.ValueKind == JsonValueKind.Object)
            foreach (JsonProperty cevap in cevapNesnesi.EnumerateObject())
                cevaplar[cevap.Name] = cevap.Value.ValueKind == JsonValueKind.String ? cevap.Value.GetString() ?? "" : cevap.Value.ToString();

        IReadOnlyDictionary<string, string> otomatikCevaplar = CevreselSosyalVeriFormuTanimlari.OtomatikCevaplar(basvuru);
        Dictionary<string, string> adresCevaplari = new(otomatikCevaplar, StringComparer.OrdinalIgnoreCase)
        {
            ["1.3"] = string.Join(" / ", new[] { adres.ilAdi, adres.ilceAdi, adres.tamAdres }.Where(x => !string.IsNullOrWhiteSpace(x))),
            ["1.5"] = string.Join(Environment.NewLine, new[]
            {
                basvuru.yatirim.yatiriminAmaci?.Trim() ?? "",
                adres.yatirimFaaliyetleri?.Trim() ?? ""
            }.Where(x => x.Length > 0)),
            ["2.2"] = HarcamaTurleri(adres.harcamaTurleri),
            ["6.1"] = adres.kullanimHakkiDosyaId.GetValueOrDefault() > 0 ? "Var" : "Yok",
            ["6.2"] = adres.yatirimYeriStatusuAd?.Trim() ?? ""
        };
        List<(string Kapsam, string SayfaAdi)> sayfalar = mod switch
        {
            "both" => [("existing", "Mevcut Tesis"), ("planned", "Planlanan Yatırım")],
            "existing" => [("existing", "Mevcut Tesis")],
            _ => [("planned", "Planlanan Yatırım")]
        };

        Workbook kitap = new();
        while (kitap.Worksheets.Count < sayfalar.Count)
            kitap.Worksheets.Add();
        while (kitap.Worksheets.Count > sayfalar.Count)
            kitap.Worksheets.RemoveAt(kitap.Worksheets.Count - 1);

        for (int i = 0; i < sayfalar.Count; i++)
            SayfaDoldur(kitap.Worksheets[i], sayfalar[i].Kapsam, sayfalar[i].SayfaAdi, adres, cevaplar, adresCevaplari);

        using MemoryStream akis = new();
        kitap.Save(akis, SaveFormat.Xlsx);
        return new RaporDosyasi(akis.ToArray(), $"OnBasvuru-Cevresel-Sosyal-{basvuruId}-Adres-{adres.siraNo}.xlsx");
    }

    private static void SayfaDoldur(
        Worksheet sayfa,
        string kapsam,
        string sayfaAdi,
        BasvuruUygulamaAdresi adres,
        IReadOnlyDictionary<string, string> cevaplar,
        IReadOnlyDictionary<string, string> otomatikCevaplar)
    {
        sayfa.Name = sayfaAdi;
        Cells hucreler = sayfa.Cells;
        hucreler.Merge(0, 0, 1, 5);
        hucreler[0, 0].PutValue($"Ön Başvuru Çevresel ve Sosyal Formu - {sayfaAdi}");
        hucreler.Merge(1, 0, 1, 5);
        hucreler[1, 0].PutValue($"Yatırım adresi: {adres.siraNo}. {adres.ilAdi} / {adres.ilceAdi} - {adres.tamAdres}");

        string[] basliklar = ["Sorular", "Cevap", "Ek Bilgiler", "Dosya Adı"];
        for (int sutun = 0; sutun < basliklar.Length; sutun++)
            hucreler[2, sutun + 1].PutValue(basliklar[sutun]);

        int satir = 3;
        List<int> bolumSatirlari = [];
        foreach (CevreselSosyalSoruGrubu grup in CevreselSosyalAnketTanimlari.Tum)
        {
            List<CevreselSosyalSoru> sorular = grup.Questions.Where(x => SoruKapsamdaMi(x, kapsam)).ToList();
            if (sorular.Count == 0)
                continue;

            bolumSatirlari.Add(satir);
            hucreler.Merge(satir, 0, 1, 5);
            hucreler[satir, 0].PutValue($"{grup.Id}. {grup.Title}");
            satir++;

            foreach (CevreselSosyalSoru soru in sorular)
            {
                string baglam = string.Equals(soru.Scope, "global", StringComparison.OrdinalIgnoreCase) ? "global" : kapsam;
                string anahtar = AlanId(soru.Id, baglam, "answer");
                string cevap = otomatikCevaplar.TryGetValue(soru.Id, out string? otomatik) ? otomatik : Deger(cevaplar, anahtar);
                if (string.Equals(soru.AnswerType, "staff", StringComparison.OrdinalIgnoreCase))
                    cevap = CalisanCevabi(soru.Id, baglam, cevaplar);
                string ekBilgi = Deger(cevaplar, AlanId(soru.Id, baglam, "explain"));
                string dosyaAdi = Deger(cevaplar, AlanId(soru.Id, baglam, "doc"));
                if (soru.Id == "6.1" && string.IsNullOrWhiteSpace(dosyaAdi))
                    dosyaAdi = adres.kullanimHakkiDosyaAdi?.Trim() ?? "";
                bool dosyaYuklenecek = soru.DocOn?.Contains(cevap, StringComparer.OrdinalIgnoreCase) == true;

                hucreler[satir, 1].PutValue($"{soru.Id} {soru.Text}");
                hucreler[satir, 2].PutValue(cevap);
                hucreler[satir, 3].PutValue(ekBilgi);
                hucreler[satir, 4].PutValue(!dosyaYuklenecek ? "-" : string.IsNullOrWhiteSpace(dosyaAdi) ? "Yüklenmedi" : dosyaAdi);
                satir++;
            }
        }

        hucreler.SetColumnWidth(0, 5);
        hucreler.SetColumnWidth(1, 66);
        hucreler.SetColumnWidth(2, 28);
        hucreler.SetColumnWidth(3, 34);
        hucreler.SetColumnWidth(4, 24);

        Style baslik = kitapStili(sayfa.Workbook, true, Color.FromArgb(31, 78, 121), Color.White);
        Style govde = kitapStili(sayfa.Workbook, false, Color.White, Color.Black);
        Style ustBaslik = kitapStili(sayfa.Workbook, true, Color.FromArgb(226, 239, 218), Color.FromArgb(31, 78, 121));
        Style bolumBasligi = kitapStili(sayfa.Workbook, true, Color.FromArgb(242, 242, 242), Color.Black);
        bolumBasligi.Font.Size = 12;
        ustBaslik.Font.Size = 14;
        hucreler.CreateRange(0, 0, 1, 5).ApplyStyle(ustBaslik, new StyleFlag { All = true });
        hucreler.CreateRange(1, 0, 1, 5).ApplyStyle(govde, new StyleFlag { All = true });
        hucreler.CreateRange(2, 1, 1, 4).ApplyStyle(baslik, new StyleFlag { All = true });
        if (satir > 3)
            hucreler.CreateRange(3, 0, satir - 3, 5).ApplyStyle(govde, new StyleFlag { All = true });
        foreach (int bolumSatiri in bolumSatirlari)
            hucreler.CreateRange(bolumSatiri, 0, 1, 5).ApplyStyle(bolumBasligi, new StyleFlag { All = true });
        sayfa.AutoFitRows();

        sayfa.FreezePanes(3, 0, 3, 0);
        sayfa.PageSetup.Orientation = PageOrientationType.Landscape;
        sayfa.PageSetup.FitToPagesWide = 1;
        sayfa.PageSetup.FitToPagesTall = 0;
        sayfa.PageSetup.PrintTitleRows = "$1:$3";
        sayfa.PageSetup.PrintArea = $"A1:E{satir}";
    }

    private static Style kitapStili(Workbook kitap, bool kalin, Color arkaPlan, Color yazi)
    {
        Style stil = kitap.CreateStyle();
        stil.Font.IsBold = kalin;
        stil.Font.Color = yazi;
        stil.Pattern = BackgroundType.Solid;
        stil.ForegroundColor = arkaPlan;
        stil.IsTextWrapped = true;
        stil.VerticalAlignment = TextAlignmentType.Top;
        stil.Borders[BorderType.TopBorder].LineStyle = CellBorderType.Thin;
        stil.Borders[BorderType.BottomBorder].LineStyle = CellBorderType.Thin;
        stil.Borders[BorderType.LeftBorder].LineStyle = CellBorderType.Thin;
        stil.Borders[BorderType.RightBorder].LineStyle = CellBorderType.Thin;
        return stil;
    }

    private static bool SoruKapsamdaMi(CevreselSosyalSoru soru, string kapsam) =>
        string.Equals(soru.Scope, "global", StringComparison.OrdinalIgnoreCase)
        || soru.Contexts == null
        || soru.Contexts.Count == 0
        || soru.Contexts.Contains(kapsam, StringComparer.OrdinalIgnoreCase);

    private static string AlanId(string soruId, string kapsam, string alan) => $"csf_{soruId.Replace('.', '_')}_{kapsam}_{alan}";
    private static string Deger(IReadOnlyDictionary<string, string> cevaplar, string anahtar) => cevaplar.TryGetValue(anahtar, out string? deger) ? deger : "";

    private static string CalisanCevabi(string soruId, string kapsam, IReadOnlyDictionary<string, string> cevaplar)
    {
        (string Kod, string Ad)[] gruplar = [("dogrudan", "Doğrudan"), ("yuklenici", "Yüklenici"), ("tedarikci", "Birincil tedarikçi"), ("gocmen", "Göçmen")];
        List<string> satirlar = [];
        foreach ((string kod, string ad) in gruplar)
        {
            string kadin = Deger(cevaplar, AlanId(soruId, kapsam, $"{kod}_kadin"));
            string erkek = Deger(cevaplar, AlanId(soruId, kapsam, $"{kod}_erkek"));
            string toplam = Deger(cevaplar, AlanId(soruId, kapsam, $"{kod}_toplam"));
            if (!string.IsNullOrWhiteSpace(kadin) || !string.IsNullOrWhiteSpace(erkek) || !string.IsNullOrWhiteSpace(toplam))
                satirlar.Add($"{ad}: Kadın {kadin}, Erkek {erkek}, Toplam {toplam}");
        }
        return string.Join(Environment.NewLine, satirlar);
    }

    private static string KapsamBelirle(BasvuruUygulamaAdresi adres)
    {
        bool yeni = adres.yatirimTurleri.Contains(1);
        bool mevcut = adres.yatirimTurleri.Any(x => x is 2 or 3 or 4);
        return yeni && mevcut ? "both" : mevcut ? "existing" : "planned";
    }

    private static string HarcamaTurleri(IEnumerable<int> turler)
    {
        static string Ad(int deger) => ((enumHarcamaTuru)deger) switch
        {
            enumHarcamaTuru.YapimIsleri => "Yapım işi",
            enumHarcamaTuru.MakineEkipman => "Makine Ekipman",
            enumHarcamaTuru.Danismanlik => "Danışmanlık",
            enumHarcamaTuru.TedarikciGelistirmeHarcamalari => "Tedarikçi Geliştirme Harcamaları",
            enumHarcamaTuru.YazilimDonanım => "Yazılım / Donanım",
            _ => ""
        };
        return string.Join(", ", turler.Select(Ad).Where(x => x.Length > 0));
    }
}
