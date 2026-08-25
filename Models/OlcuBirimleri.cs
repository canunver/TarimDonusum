namespace TarimDonusum.Models;

public static class OlcuBirimleri
{
    public static readonly IReadOnlyList<string> Tum = ["Adet", "Ton", "Litre", "Kişi/Yıl", "kWh", "Yıl"];

    public static bool GecerliMi(string? birim) =>
        !string.IsNullOrWhiteSpace(birim) && Tum.Contains(birim, StringComparer.OrdinalIgnoreCase);
}
