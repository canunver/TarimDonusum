namespace TarimDonusum.Models;

public static class ParaBirimleri
{
    public static readonly IReadOnlyList<string> Tum = ["TL", "USD", "EUR", "GBP"];

    public static bool GecerliMi(string? paraBirimi) =>
        !string.IsNullOrWhiteSpace(paraBirimi) && Tum.Contains(paraBirimi, StringComparer.OrdinalIgnoreCase);
}
