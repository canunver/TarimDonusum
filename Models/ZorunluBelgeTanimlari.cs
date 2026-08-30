using System.IO.Compression;
using System.Xml.Linq;

namespace TarimDonusum.Models;

public sealed record ZorunluBelgeTanimi(int No, string Grup, string Ad, string Uygulanabilirlik, string Aciklama);

public static class ZorunluBelgeTanimlari
{
    private static readonly Lazy<IReadOnlyList<ZorunluBelgeTanimi>> Liste = new(Yukle);
    public static IReadOnlyList<ZorunluBelgeTanimi> Tum => Liste.Value;

    private static IReadOnlyList<ZorunluBelgeTanimi> Yukle()
    {
        string dosya = Path.Combine(AppContext.BaseDirectory, "Sablonlar", "ZorunluBelgeler.xlsx");
        if (!File.Exists(dosya)) dosya = Path.Combine(Directory.GetCurrentDirectory(), "Sablonlar", "ZorunluBelgeler.xlsx");
        using ZipArchive zip = ZipFile.OpenRead(dosya);
        XNamespace x = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        XDocument ortak = XDocument.Load(zip.GetEntry("xl/sharedStrings.xml")!.Open());
        List<string> metinler = ortak.Descendants(x + "si").Select(si => string.Concat(si.Descendants(x + "t").Select(t => t.Value))).ToList();
        XDocument sayfa = XDocument.Load(zip.GetEntry("xl/worksheets/sheet1.xml")!.Open());
        string Oku(XElement satir, string sutun)
        {
            XElement? c = satir.Elements(x + "c").FirstOrDefault(h => ((string?)h.Attribute("r"))?.StartsWith(sutun, StringComparison.Ordinal) == true);
            string v = c?.Element(x + "v")?.Value ?? "";
            return c != null && (string?)c.Attribute("t") == "s" && int.TryParse(v, out int i) ? metinler[i] : v;
        }
        string AciklamaGuncelle(string aciklama) => aciklama switch
        {
            "06 ve 08 sayfalarıyla uyumlu." => "Finansman ve Yatırım Özeti sayfalarıyla uyumlu.",
            "09 sayfasıyla uyumlu." => "Teknik Proje sayfasıyla uyumlu.",
            _ => aciklama
        };
        return sayfa.Descendants(x + "row").Skip(1).Select(r => new ZorunluBelgeTanimi(
            int.TryParse(Oku(r, "A"), out int no) ? no : 0, Oku(r, "B"), Oku(r, "C"), Oku(r, "D"), AciklamaGuncelle(Oku(r, "E"))))
            .Where(x => x.No > 0).ToList();
    }
}
