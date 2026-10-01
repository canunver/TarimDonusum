using System.Text.Json.Nodes;

namespace TarimDonusum.Models;

public static class BasvuruAdresAsamaFiltresi
{
    public static void Uygula(Basvuru b, int adresId, enumDegerZinciriAsamaTuru asama)
    {
        b.YatirimOnBilgileri = b.YatirimOnBilgileri.Where(x => x.uygulamaAdresiId == adresId && x.degerZinciriAsamaTuru == asama).ToList();
        b.Makineler = b.Makineler.Where(x => x.uygulamaAdresiId == adresId && x.degerZinciriAsamaTuru == asama).ToList();
        b.Binalar = b.Binalar.Where(x => x.uygulamaAdresiId == adresId && x.degerZinciriAsamaTuru == asama).ToList();
        // Eski JSON listeleri boş sonuçta başka adresin kayıtlarını yeniden eklememeli.
        var json = JsonNode.Parse(string.IsNullOrWhiteSpace(b.dbCtpTeknikProje.dbCtpTeknikProjeJson) ? "{}" : b.dbCtpTeknikProje.dbCtpTeknikProjeJson) as JsonObject ?? new();
        foreach (string key in new[] { "existingProducts", "plannedProducts", "inputs", "machineryRows", "buildingRows", "solarRows", "installedRows" })
            json[key] = new JsonArray();
        b.dbCtpTeknikProje.dbCtpTeknikProjeJson = json.ToJsonString();
        var pikk = JsonNode.Parse(string.IsNullOrWhiteSpace(b.uygunHarcama.pikkListesiJson) ? "{}" : b.uygunHarcama.pikkListesiJson) as JsonObject ?? new();
        pikk["constructionRows"] = new JsonArray();
        b.uygunHarcama.pikkListesiJson = pikk.ToJsonString();
    }
}
