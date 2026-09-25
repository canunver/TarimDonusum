using System.Text.Json;

namespace TarimDonusum.Models;

public static class CevreselSosyalTaahhut
{
    public const string Alan = "cevreselSosyalTaahhutOnayi";

    public static bool? Oku(string? json, string alan)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind == JsonValueKind.Object && doc.RootElement.TryGetProperty(alan, out var value))
                return value.ValueKind == JsonValueKind.True ? true : value.ValueKind == JsonValueKind.False ? false : null;
        }
        catch (JsonException) { }
        return null;
    }

    public static bool OnayliMi(Basvuru basvuru) => Oku(basvuru.TaahhutBeyanlarJson, Alan)
        ?? basvuru.YatirimAdresleri.Any(adres => Oku(adres.cevreselSosyalJson, "declaration") == true);
}
