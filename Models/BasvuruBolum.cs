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
        Ortaklik = 9,
        UygunHarcama = 10,
        YatirimOzeti = 11,
        CevreselSosyal = 12,
        TaahhutBeyan = 13,
        Ozet = 14,
        DbCtpTeknikProje = 15,
        SistemSonuclari = 16,
        UzmanSonuclari = 17,
        Karar = 18
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
            new(enumBasvuruBolum.UygulamaAdresi, 40, "Basvuru.Step.4", "Bolumler/_UygulamaAdresi"),
            new(enumBasvuruBolum.Yatirim, 50, "Basvuru.Step.3", "Bolumler/_Yatirim"),
            new(enumBasvuruBolum.UygunHarcama, 60, "Basvuru.Step.UygunHarcama", "Bolumler/_UygunHarcama"),
            new(enumBasvuruBolum.Finans, 70, "Basvuru.Step.5", "Bolumler/_Finans"),
            new(enumBasvuruBolum.YatirimOzeti, 80, "Basvuru.Step.YatirimOzeti", "Bolumler/_YatirimOzeti"),
            new(enumBasvuruBolum.DbCtpTeknikProje, 90, "Basvuru.Step.DbCtp", "Bolumler/_DbCtpTeknikProje"),
            new(enumBasvuruBolum.Belgeler, 100, "Basvuru.Step.7", "Bolumler/_Belgeler"),
            new(enumBasvuruBolum.CevreselSosyal, 110, "Basvuru.Step.CevreselSosyal", "Bolumler/_CevreselSosyal"),
            new(enumBasvuruBolum.TaahhutBeyan, 120, "Basvuru.Step.TaahhutBeyan", "Bolumler/_TaahhutBeyan"),
            new(enumBasvuruBolum.Ozet, 130, "Basvuru.Step.Ozet", "Bolumler/_Ozet"),
            new(enumBasvuruBolum.SistemSonuclari, 140, "Basvuru.Step.SystemResults", "Bolumler/_SistemSonuclari", true),
            new(enumBasvuruBolum.UzmanSonuclari, 150, "Basvuru.Step.ExpertResults", "Bolumler/_UzmanSonuclari", true),
            new(enumBasvuruBolum.Karar, 160, "Basvuru.Step.Decision", "Bolumler/_Denetim", true)
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
