using System.Text.Json;
using TarimDonusum.Araclar;
using TarimDonusum.Models;

namespace TarimDonusum.Raporlar;

public sealed class RPROB_Beyan(string uygulamaRootPath) : RPROBTemel(uygulamaRootPath)
{
    protected override string SablonAdi => "Beyan.xltx";
    protected override string GeciciDosyaOnEki => "taahhut-beyan";
    protected override string CiktiDosyaOnEki => "TaahhutBeyan";

    protected override void Doldur(Tablo tablo, Basvuru basvuru)
    {
        string yetkili = "", gorevi = "";
        Dictionary<int, (int Kabul, string Aciklama)> satirlar = [];
        if (!string.IsNullOrWhiteSpace(basvuru.TaahhutBeyanlarJson))
        {
            using JsonDocument belge = JsonDocument.Parse(basvuru.TaahhutBeyanlarJson);
            JsonElement kok = belge.RootElement;
            yetkili = RaporJson.Metin(kok, "yetkiliKisi");
            gorevi = RaporJson.Metin(kok, "gorevi");
            if (kok.TryGetProperty("satirlar", out JsonElement liste) && liste.ValueKind == JsonValueKind.Array)
                foreach (JsonElement x in liste.EnumerateArray())
                {
                    int no = x.TryGetProperty("no", out JsonElement n) && n.TryGetInt32(out int sira) ? sira : 0;
                    int kabul = x.TryGetProperty("kabul", out JsonElement k) && k.TryGetInt32(out int secim) ? secim : 0;
                    if (no > 0) satirlar[no] = (kabul, RaporJson.Metin(x, "aciklama"));
                }
        }
        for (int no = 1; no <= BeyanTanimlari.Maddeler.Count; no++)
        {
            (int kabul, string aciklama) = satirlar.GetValueOrDefault(no);
            tablo.HucreDegerYaz(no + 3, 2, kabul == 1 ? "Evet" : kabul == 2 ? "Hayır" : "");
            tablo.HucreDegerYaz(no + 3, 3, aciklama ?? "");
        }
        tablo.HucreDegerYaz(27, 1, yetkili);
        tablo.HucreDegerYaz(28, 1, gorevi);
        tablo.HucreDegerYaz(29, 1, basvuru.TaahhutAciklama ?? "");
    }
}
