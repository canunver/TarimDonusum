using System.Text.Json;
using TarimDonusum.Araclar;
using TarimDonusum.Models;

namespace TarimDonusum.Raporlar;

public sealed class RPROB_CevreselSosyalVeriFormu(string uygulamaRootPath) : RPROBTemel(uygulamaRootPath)
{
    protected override string SablonAdi => "CevreselSosyalVeriFormu.xltx";
    protected override string GeciciDosyaOnEki => "cevresel-sosyal-veri-formu";
    protected override string CiktiDosyaOnEki => "CevreselSosyalVeriFormu";

    protected override void Doldur(Tablo tablo, Basvuru basvuru)
    {
        Dictionary<string, (string Yanit, string Onlem)> cevaplar = new(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(basvuru.cevreselSosyal.cevreselSosyalJson))
        {
            using JsonDocument belge = JsonDocument.Parse(basvuru.cevreselSosyal.cevreselSosyalJson);
            if (belge.RootElement.TryGetProperty("cevaplar", out JsonElement liste) && liste.ValueKind == JsonValueKind.Object)
                foreach (JsonProperty x in liste.EnumerateObject())
                    cevaplar[x.Name] = (RaporJson.Metin(x.Value, "yanit"), RaporJson.Metin(x.Value, "aciklamaOnlem"));
        }

        IReadOnlyDictionary<string, string> otomatik = CevreselSosyalVeriFormuTanimlari.OtomatikCevaplar(basvuru);
        foreach (CevreselSosyalVeriSorusu soru in CevreselSosyalVeriFormuTanimlari.Tum)
        {
            (string yanit, string onlem) = cevaplar.GetValueOrDefault(soru.Kod);
            if (otomatik.TryGetValue(soru.Kod, out string? bilinen)) { yanit = bilinen; onlem = ""; }
            tablo.HucreDegerYaz(soru.ExcelSatiri - 1, 4, yanit ?? "");
            tablo.HucreDegerYaz(soru.ExcelSatiri - 1, 5, onlem ?? "");
        }
        tablo.HucreDegerYaz(2, 5, "Açıklama/Önlem");
    }
}
