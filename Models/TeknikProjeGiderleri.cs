using System.Text.Json;
using System.Text.Json.Nodes;

namespace TarimDonusum.Models;

public static class TeknikProjeGiderleri
{
    private static bool TeknikProjeGrubuMu(JsonObject row) => Enum.GetValues<GirdiGiderTuru>().Any(tur =>
        string.Equals(row["parentExpenseGroup"]?.ToString()?.Trim(), GirdiGiderTurleri.Anahtar(tur), StringComparison.OrdinalIgnoreCase)
        || string.Equals(row["group"]?.ToString()?.Trim(), GirdiGiderTurleri.Ad(tur), StringComparison.OrdinalIgnoreCase)
        || string.Equals(row["item"]?.ToString()?.Trim(), GirdiGiderTurleri.Ad(tur), StringComparison.OrdinalIgnoreCase));

    public static string? Birlestir(string? json, IEnumerable<BasvuruYatirimOnBilgi> kayitlar)
    {
        var girdiler = kayitlar.Where(x => x.tur == enumYatirimOnBilgiTuru.Girdi
            && x.giderTuru.HasValue && Enum.IsDefined(x.giderTuru.Value)).OrderBy(x => x.siraNo).ToList();
        if (girdiler.Count == 0 && string.IsNullOrWhiteSpace(json)) return json;
        JsonObject root;
        try { root = string.IsNullOrWhiteSpace(json) ? new() : JsonNode.Parse(json) as JsonObject ?? new(); }
        catch (JsonException) { return json; }
        var eskiSatirlar = (root["operatingExpenseRows"] as JsonArray)?.OfType<JsonObject>().ToList() ?? [];
        
        if (root["operatingExpenseRows"] == null) root["operatingExpenseDefaultsRequired"] = true;
        var satirlar = new JsonArray();
        foreach (var girdi in girdiler)
        {
            // Sıra numarası revizyon kopyalarında ve Excel aktarımında fiyatı korur.
            var eski = eskiSatirlar.FirstOrDefault(x => x["sourceGirdiId"]?.ToString() == girdi.id.ToString())
                ?? eskiSatirlar.FirstOrDefault(x => x["sourceGirdiSiraNo"]?.ToString() == girdi.siraNo.ToString());
            decimal sabit = Math.Clamp(girdi.sabitOran ?? 0, 0, 100);
            satirlar.Add(new JsonObject
            {
                ["sourceGirdiId"] = girdi.id, ["sourceGirdiSiraNo"] = girdi.siraNo,
                ["parentExpenseGroup"] = GirdiGiderTurleri.Anahtar(girdi.giderTuru!.Value),
                ["group"] = GirdiGiderTurleri.Ad(girdi.giderTuru.Value), ["item"] = girdi.ad,
                ["qty"] = girdi.miktar, ["unit"] = girdi.birim,
                ["unitPrice"] = girdi.birimFiyat.HasValue ? JsonValue.Create(girdi.birimFiyat.Value) : eski?["unitPrice"]?.DeepClone() ?? JsonValue.Create(""),
                ["fixedPct"] = sabit, ["variablePct"] = 100 - sabit
            });
        }
        foreach (var satir in eskiSatirlar.Where(x => x["sourceGirdiId"] == null && !TeknikProjeGrubuMu(x))) satirlar.Add(satir.DeepClone());
        root["operatingExpenseRows"] = satirlar;
        return root.ToJsonString();
    }
}
