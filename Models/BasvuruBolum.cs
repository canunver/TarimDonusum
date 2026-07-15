namespace TarimDonusum.Models
{
    public enum enumBasvuruBolum
    {
        Tanimsiz = 0,
        Firma = 1,
        Irtibat = 2,
        Yatirim = 3,
        UygulamaAdresi = 4,
        Finans = 5,
        Mali = 6,
        Belgeler = 7,
        Denetim = 8,
        Ortaklik = 9
    }

    public sealed record BasvuruBolumTanim(
        enumBasvuruBolum Bolum,
        int Sira,
        string BaslikResourceKey,
        string PartialView,
        bool DenetciBolumu = false);

    public static class BasvuruBolumleri
    {
        private static readonly IReadOnlyList<BasvuruBolumTanim> Tanimlar =
        [
            new(enumBasvuruBolum.Firma, 10, "Basvuru.Step.1", "Bolumler/_Firma"),
            new(enumBasvuruBolum.Mali, 20, "Basvuru.Step.6", "Bolumler/_Mali"),
            new(enumBasvuruBolum.Ortaklik, 30, "Basvuru.Step.Ortaklik", "Bolumler/_Ortaklik"),
            new(enumBasvuruBolum.Yatirim, 40, "Basvuru.Step.3", "Bolumler/_Yatirim"),
            new(enumBasvuruBolum.UygulamaAdresi, 50, "Basvuru.Step.4", "Bolumler/_UygulamaAdresi"),
            new(enumBasvuruBolum.Finans, 60, "Basvuru.Step.5", "Bolumler/_Finans"),
            new(enumBasvuruBolum.Belgeler, 70, "Basvuru.Step.7", "Bolumler/_Belgeler"),
            new(enumBasvuruBolum.Denetim, 80, "Basvuru.Step.8", "Bolumler/_Denetim", true)
        ];

        public static IReadOnlyList<BasvuruBolumTanim> Tum(bool denetciGorunumu)
        {
            return Tanimlar
                .Where(x => denetciGorunumu || !x.DenetciBolumu)
                .OrderBy(x => x.Sira)
                .ToList();
        }

        public static BasvuruBolumTanim? Bul(enumBasvuruBolum bolum, bool denetciGorunumu)
        {
            return Tum(denetciGorunumu).FirstOrDefault(x => x.Bolum == bolum);
        }
    }
}
