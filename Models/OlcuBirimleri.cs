namespace TarimDonusum.Models;

public static class OlcuBirimleri
{
    public static readonly IReadOnlyList<string> Tum = ["Adet", "Kg", "Ton", "Litre", "W", "kW", "kWp", "kWe", "Kişi/Yıl", "kWh", "Yıl"];

    public static bool GecerliMi(string? birim) =>
        Standartlastir(birim) != null;

    public static string? Standartlastir(string? birim)
    {
        string deger = (birim ?? "").Trim();
        string? standart = Tum.FirstOrDefault(x => string.Equals(x, deger, StringComparison.OrdinalIgnoreCase));
        if (standart != null) return standart;

        return deger.ToLowerInvariant() switch
        {
            "adet." or "ad" or "pcs" => "Adet",
            "kilogram" or "kilogramme" => "Kg",
            "t" or "tonne" => "Ton",
            "l" or "lt" or "ltr" => "Litre",
            "kwp" => "kWp",
            "kwe" => "kWe",
            "kwh" => "kWh",
            "kişi/yil" or "kisi/yıl" or "kisi/yil" => "Kişi/Yıl",
            "yil" => "Yıl",
            _ => null
        };
    }
}
