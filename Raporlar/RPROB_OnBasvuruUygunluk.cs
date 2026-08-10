using System.Text.Json;
using TarimDonusum.Models;

namespace TarimDonusum.Raporlar;

public sealed class RPROB_OnBasvuruUygunluk(string uygulamaRootPath) : KontrolRaporuTemel(uygulamaRootPath)
{
    protected override string SablonAdi => "OnBasvuruUygunluk.xltx";
    protected override string GeciciDosyaOnEki => "on-basvuru-uygunluk";
    protected override string CiktiDosyaOnEki => "OnBasvuruUygunluk";
    protected override string? JsonAl(Basvuru basvuru) => basvuru.DenetimAnketi;
    protected override string SonucAl(JsonElement satir) => RaporJson.Metin(satir, "sonuc");
}
