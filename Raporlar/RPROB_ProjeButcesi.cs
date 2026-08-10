using System.Globalization;
using System.Text.Json;
using TarimDonusum.Araclar;
using TarimDonusum.Models;

namespace TarimDonusum.Raporlar;

public sealed class RPROB_ProjeButcesi(string uygulamaRootPath) : RPROBTemel(uygulamaRootPath)
{
    protected override string SablonAdi => "ProjeButcesi.xltx";
    protected override string GeciciDosyaOnEki => "proje-butcesi";
    protected override string CiktiDosyaOnEki => "ProjeButcesi";

    protected override void Doldur(Tablo tablo, Basvuru basvuru)
    {
        Dictionary<string, decimal> tutarlar = TutarlariOku(basvuru.yatirimOzeti.yatirimOzetiJson);
        Dictionary<string, int> satirlar = new()
        {
            ["A1"] = 3, ["A2"] = 4, ["A3"] = 5, ["A4"] = 6,
            ["B1"] = 8, ["B2"] = 9, ["B3"] = 10, ["B4"] = 11,
            ["B5"] = 12, ["B6"] = 13, ["B7"] = 14,
            ["D"] = 16, ["E"] = 17, ["F"] = 18, ["G"] = 19
        };

        foreach ((string anahtar, int satir) in satirlar)
            tablo.HucreDegerYaz(satir, 8, tutarlar.GetValueOrDefault(anahtar));

        tablo.HucreFormulYaz(20, 8, "ROUNDUP(SUM(I17:I20),2)");
        tablo.CalculateFormula();
    }

    internal static decimal SayiOku(JsonElement tutar)
    {
        if (tutar.ValueKind == JsonValueKind.Number && tutar.TryGetDecimal(out decimal sayi))
            return sayi;

        string metin = tutar.ValueKind == JsonValueKind.String ? tutar.GetString() ?? "" : "";
        metin = metin.Replace("TL", "", StringComparison.OrdinalIgnoreCase).Trim();
        if (decimal.TryParse(metin, NumberStyles.Any, CultureInfo.GetCultureInfo("tr-TR"), out sayi))
            return sayi;
        return decimal.TryParse(metin, NumberStyles.Any, CultureInfo.InvariantCulture, out sayi) ? sayi : 0;
    }

    private static Dictionary<string, decimal> TutarlariOku(string? json)
    {
        Dictionary<string, decimal> tutarlar = new(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(json))
            return tutarlar;

        using JsonDocument belge = JsonDocument.Parse(json);
        if (!belge.RootElement.TryGetProperty("investmentBudgetData", out JsonElement butce) ||
            butce.ValueKind != JsonValueKind.Object)
            return tutarlar;

        foreach (JsonProperty satir in butce.EnumerateObject())
        {
            if (satir.Value.ValueKind == JsonValueKind.Object &&
                satir.Value.TryGetProperty("amount", out JsonElement tutar))
                tutarlar[satir.Name] = SayiOku(tutar);
        }

        return tutarlar;
    }
}
