using System.IO.Compression;
using System.Xml.Linq;

namespace TarimDonusum.Models;

public sealed record CevreselSosyalVeriSorusu(string Kod, string Konu, string Soru, string Kapsam, int ExcelSatiri, IReadOnlyList<string> Secenekler);

public static class CevreselSosyalVeriFormuTanimlari
{
    private static readonly Lazy<IReadOnlyList<CevreselSosyalVeriSorusu>> Sorular = new(Yukle);
    public static IReadOnlyList<CevreselSosyalVeriSorusu> Tum => Sorular.Value;

    private static IReadOnlyList<CevreselSosyalVeriSorusu> Yukle()
    {
        string dosya = Path.Combine(AppContext.BaseDirectory, "Sablonlar", "CevreselSosyalVeriFormu.xltx");
        if (!File.Exists(dosya))
            dosya = Path.Combine(Directory.GetCurrentDirectory(), "Sablonlar", "CevreselSosyalVeriFormu.xltx");
        using ZipArchive zip = ZipFile.OpenRead(dosya);
        XNamespace x = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        XDocument ortak = XDocument.Load(zip.GetEntry("xl/sharedStrings.xml")!.Open());
        List<string> metinler = ortak.Descendants(x + "si").Select(si => string.Concat(si.Descendants(x + "t").Select(t => t.Value))).ToList();
        XDocument sayfa = XDocument.Load(zip.GetEntry("xl/worksheets/sheet1.xml")!.Open());
        Dictionary<int, IReadOnlyList<string>> secenekler = [];
        foreach (XElement dogrulama in sayfa.Descendants(x + "dataValidation").Where(v => (string?)v.Attribute("type") == "list"))
        {
            string formul = dogrulama.Element(x + "formula1")?.Value.Trim().Trim('"') ?? "";
            IReadOnlyList<string> liste = formul.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (string basvuru in ((string?)dogrulama.Attribute("sqref") ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                string[] sinirlar = basvuru.Split(':');
                int Satir(string adres) => int.Parse(new string(adres.Where(char.IsDigit).ToArray()));
                int ilk = Satir(sinirlar[0]), son = Satir(sinirlar[^1]);
                for (int satir = ilk; satir <= son; satir++) secenekler[satir] = liste;
            }
        }
        string Oku(XElement satir, string sutun)
        {
            XElement? hucre = satir.Elements(x + "c").FirstOrDefault(c => ((string?)c.Attribute("r"))?.StartsWith(sutun, StringComparison.Ordinal) == true);
            if (hucre == null) return "";
            string deger = hucre.Element(x + "v")?.Value ?? "";
            return (string?)hucre.Attribute("t") == "s" && int.TryParse(deger, out int i) ? metinler[i] : deger;
        }
        return sayfa.Descendants(x + "row")
            .Where(r => int.TryParse((string?)r.Attribute("r"), out int no) && no is >= 4 and <= 63)
            .Select(r =>
            {
                int satir = (int)r.Attribute("r")!;
                return new CevreselSosyalVeriSorusu(Oku(r, "A"), Oku(r, "B"), Oku(r, "C"), Oku(r, "D"), satir, secenekler.GetValueOrDefault(satir) ?? []);
            })
            .ToList();
    }

    public static IReadOnlyDictionary<string, string> OtomatikCevaplar(Basvuru b)
    {
        string adres = string.Join("; ", (b.YatirimAdresleri ?? []).OrderBy(x => x.siraNo)
            .Select(x => string.Join(" / ", new[] { x.ilAdi, x.ilceAdi, x.tamAdres }.Where(y => !string.IsNullOrWhiteSpace(y)))));
        string Tur(int v) => ((enumYatirimTuru)v) switch
        {
            enumYatirimTuru.Yeni => "Yeni",
            enumYatirimTuru.KapasiteArtirimi => "Kapasite Artırımı",
            enumYatirimTuru.Modernizasyon => "Modernizasyon",
            enumYatirimTuru.TeknolojiYenileme => "Teknoloji Yenileme",
            _ => ""
        };
        return new Dictionary<string, string>
        {
            ["1.1"] = b.basvuruFirma.firma.ticaretUnvani ?? "",
            ["1.2"] = b.yatirim.yatirimAdi ?? "",
            ["1.3"] = adres,
            ["1.4"] = string.Join(", ", (b.yatirim.yatirimTurleri ?? []).Select(Tur).Where(x => x.Length > 0)),
            ["2.2"] = HarcamaTurleri(b),
            ["3.2"] = PersonelPlani(b)
        };
    }

    private static string HarcamaTurleri(Basvuru b)
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
        return string.Join(", ", (b.yatirim.harcamaTurleri ?? []).Select(Ad).Where(x => x.Length > 0));
    }

    private static string PersonelPlani(Basvuru b)
    {
        static string Sayi(decimal deger) => deger.ToString("0.##", System.Globalization.CultureInfo.GetCultureInfo("tr-TR"));
        return string.Join(Environment.NewLine, (b.istihdam.satirlar ?? [])
            .OrderBy(x => x.siraNo).ThenBy(x => x.id)
            .Select(x =>
            {
                string grup = string.Join(" / ", new[] { x.birimUnite, x.gorevUretimHatti, x.cinsiyet, x.yasDurumu }.Where(y => !string.IsNullOrWhiteSpace(y)));
                return $"{grup}: yatırım öncesi {Sayi(x.mevcutCalisan)}, yatırım sonrası {Sayi(x.mevcutCalisan + x.netCalisanArtisi)}";
            }));
    }
}
