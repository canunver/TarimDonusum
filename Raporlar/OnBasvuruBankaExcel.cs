using System.Globalization;
using System.IO.Compression;
using System.Xml.Linq;
using TarimDonusum.Models;

namespace TarimDonusum.Raporlar;

public static class OnBasvuruBankaExcel
{
    private static readonly XNamespace X = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    // Durum filtresi son sürüm seçiminden sonra uygulanır; eski inceleme sürümleri alınmaz.
    public static List<Basvuru> SonIncelemedekiSurumler(IEnumerable<Basvuru> versiyonlar) => versiyonlar
        .GroupBy(x => x.BasvuruAnaId)
        .Select(g => g.OrderByDescending(x => x.basvuruFirma.revizyonNo).ThenByDescending(x => x.Id).First())
        .Where(x => x.kayitTuru == enumBasvuruKayitTuru.OnBasvuru
            && x.durum == enumBasvuruDurum.OnBasvuruIncelemeDurumu)
        .OrderBy(x => x.BasvuruAnaId).ToList();

    public static byte[] Olustur(string sablonDosyasi, IReadOnlyList<Basvuru> basvurular)
    {
        if (basvurular.Count == 0)
            throw new InvalidOperationException("Yazdırılacak, son sürümü incelemede olan ön başvuru bulunamadı.");
        using var stream = new MemoryStream();
        using (var source = File.OpenRead(sablonDosyasi)) source.CopyTo(stream);
        // XLSX paketinin yalnızca veri, biçim ve yazdırma ayarları güncellenir.
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Update, leaveOpen: true))
        {
            XDocument sheet = Oku(zip, "xl/worksheets/sheet1.xml");
            XDocument styles = Oku(zip, "xl/styles.xml");
            int textStyle = BicimEkle(styles, "@"), moneyStyle = BicimEkle(styles, "#,##0.00"),
                shareStyle = BicimEkle(styles, "0.00");
            XElement root = sheet.Root!;
            XElement data = root.Element(X + "sheetData")!;
            XElement oldHeader = data.Element(X + "row")!;
            int headerStyle = (int?)oldHeader.Element(X + "c")?.Attribute("s") ?? textStyle;
            data.RemoveNodes();
            string[] headings = ["VKN", "REF", "KREDI_TUTAR", "HISSE_ORAN", "HISSE_KIMLIK"];
            data.Add(new XElement(X + "row", new XAttribute("r", 1), new XAttribute("ht", 30), new XAttribute("customHeight", 1),
                headings.Select((h, i) => Metin($"{(char)('A' + i)}1", h, headerStyle))));
            XElement columns = new(X + "cols");
            int[] widths = [18, 28, 24, 18, 22];
            for (int i = 0; i < widths.Length; i++)
                columns.Add(new XElement(X + "col", new XAttribute("min", i + 1), new XAttribute("max", i + 1),
                    new XAttribute("width", widths[i]), new XAttribute("customWidth", 1)));
            if (root.Element(X + "cols") is { } oldColumns) oldColumns.ReplaceWith(columns);
            else data.AddBeforeSelf(columns);
            int nextRow = 2;
            foreach (Basvuru b in basvurular.OrderBy(x => x.basvuruFirma.firma.vergiKimlikNo, StringComparer.Ordinal)
                .ThenBy(x => x.basvuruFirma.basvuruNo, StringComparer.Ordinal))
            {
                string referans = b.basvuruFirma.basvuruNo ?? "";
                decimal? kredi = b.HesaplananTalepEdilenFinansmanTutari;
                if (string.IsNullOrWhiteSpace(referans) || kredi == null)
                    throw new InvalidOperationException($"{(string.IsNullOrWhiteSpace(referans) ? b.BasvuruAnaId.ToString() : referans)} numaralı başvurunun referans numarası veya kredi tutarının hesaplanması için gereken bilgiler eksik. Excel oluşturulamadı.");
                var hisseler = b.ortaklik.ortaklar.OrderBy(x => x.siraNo).ThenBy(x => x.id)
                    .Select(x => (Kimlik: x.tcknVkn, Oran: x.payOrani)).ToList();
                decimal? halkaOrani = b.ortaklik.halkaAciklikOrani;
                if (halkaOrani is < 0 or > 100)
                    throw new InvalidOperationException($"{referans} numaralı başvurunun halka açıklık oranı 0–100 arasında olmalıdır.");
                if (halkaOrani > 0) hisseler.Add(("H", halkaOrani));
                // Ortak bilgisi bulunmayan başvuruyu sessizce atlama; eksik alanları boş bırak.
                if (hisseler.Count == 0) hisseler.Add((null, null));
                foreach (var hisse in hisseler)
                {
                    if (hisse.Oran is < 0 or > 100)
                        throw new InvalidOperationException($"{referans} numaralı başvurunun hisse oranı 0–100 arasında olmalıdır.");
                    int row = nextRow++;
                    data.Add(new XElement(X + "row", new XAttribute("r", row), new XAttribute("ht", 24), new XAttribute("customHeight", 1),
                        Metin($"A{row}", b.basvuruFirma.firma.vergiKimlikNo, textStyle),
                        Metin($"B{row}", referans, textStyle), Sayi($"C{row}", kredi, moneyStyle),
                        Sayi($"D{row}", hisse.Oran, shareStyle), Metin($"E{row}", hisse.Kimlik, textStyle)));
                }
            }
            string area = $"A1:E{nextRow - 1}";
            root.Element(X + "dimension")!.SetAttributeValue("ref", area);
            root.Element(X + "autoFilter")?.Remove();
            data.AddAfterSelf(new XElement(X + "autoFilter", new XAttribute("ref", area)));
            XElement? view = root.Element(X + "sheetViews")?.Element(X + "sheetView");
            if (view != null)
            {
                view.Element(X + "pane")?.Remove();
                view.AddFirst(new XElement(X + "pane", new XAttribute("ySplit", 1), new XAttribute("topLeftCell", "A2"),
                    new XAttribute("activePane", "bottomLeft"), new XAttribute("state", "frozen")));
            }
            XElement? setup = root.Element(X + "pageSetup");
            if (setup != null)
            {
                setup.SetAttributeValue("orientation", "landscape");
                setup.SetAttributeValue("fitToWidth", 1);
                setup.SetAttributeValue("fitToHeight", 0);
            }
            XElement? properties = root.Element(X + "sheetPr");
            if (properties == null) { properties = new XElement(X + "sheetPr"); root.AddFirst(properties); }
            properties.Element(X + "pageSetUpPr")?.Remove();
            properties.Add(new XElement(X + "pageSetUpPr", new XAttribute("fitToPage", 1)));
            XDocument book = Oku(zip, "xl/workbook.xml");
            XElement? names = book.Root!.Element(X + "definedNames");
            if (names == null) { names = new XElement(X + "definedNames"); book.Root.Element(X + "sheets")!.AddAfterSelf(names); }
            names.Elements(X + "definedName").Where(n => (int?)n.Attribute("localSheetId") == 0
                && ((string?)n.Attribute("name") is "_xlnm.Print_Area" or "_xlnm.Print_Titles")).Remove();
            string sheetName = ((string?)book.Root.Element(X + "sheets")!.Element(X + "sheet")!.Attribute("name") ?? "Sayfa1").Replace("'", "''");
            names.Add(new XElement(X + "definedName", new XAttribute("name", "_xlnm.Print_Area"), new XAttribute("localSheetId", 0), $"'{sheetName}'!$A$1:$E${nextRow - 1}"),
                new XElement(X + "definedName", new XAttribute("name", "_xlnm.Print_Titles"), new XAttribute("localSheetId", 0), $"'{sheetName}'!$1:$1"));
            Yaz(zip, "xl/worksheets/sheet1.xml", sheet);
            Yaz(zip, "xl/styles.xml", styles);
            Yaz(zip, "xl/workbook.xml", book);
        }
        return stream.ToArray();
    }

    private static XElement Metin(string adres, string? value, int style) => new(X + "c", new XAttribute("r", adres),
        new XAttribute("s", style), new XAttribute("t", "inlineStr"),
        new XElement(X + "is", new XElement(X + "t", new XAttribute(XNamespace.Xml + "space", "preserve"), value ?? "")));
    private static XElement Sayi(string adres, decimal? value, int style) => new(X + "c", new XAttribute("r", adres),
        new XAttribute("s", style), value.HasValue ? new XElement(X + "v", value.Value.ToString(CultureInfo.InvariantCulture)) : null);
    private static int BicimEkle(XDocument doc, string format)
    {
        XElement root = doc.Root!;
        XElement? formats = root.Element(X + "numFmts");
        if (formats == null) { formats = new XElement(X + "numFmts"); root.AddFirst(formats); }
        int id = Math.Max(164, formats.Elements().Select(x => (int?)x.Attribute("numFmtId") ?? 0).DefaultIfEmpty(163).Max() + 1);
        formats.Add(new XElement(X + "numFmt", new XAttribute("numFmtId", id), new XAttribute("formatCode", format)));
        formats.SetAttributeValue("count", formats.Elements().Count());
        XElement xfs = root.Element(X + "cellXfs")!;
        int index = xfs.Elements().Count();
        XElement xf = new(xfs.Elements().First());
        xf.SetAttributeValue("numFmtId", id); xf.SetAttributeValue("applyNumberFormat", 1); xf.SetAttributeValue("applyAlignment", 1);
        xf.Element(X + "alignment")?.Remove();
        xf.Add(new XElement(X + "alignment", new XAttribute("vertical", "center"), new XAttribute("wrapText", 1)));
        xfs.Add(xf); xfs.SetAttributeValue("count", index + 1);
        return index;
    }
    private static XDocument Oku(ZipArchive zip, string path)
    {
        using var stream = zip.GetEntry(path)!.Open();
        return XDocument.Load(stream);
    }
    private static void Yaz(ZipArchive zip, string path, XDocument document)
    {
        zip.GetEntry(path)?.Delete();
        using var stream = zip.CreateEntry(path).Open();
        document.Save(stream);
    }
}
