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
        Karar = 18,
        BasvuruSahibi = 19,
        BasvuruMaliVeriler = 20,
        BasvuruOrtaklikYetki = 21,
        BasvuruYatirimBilgileri = 22,
        BasvuruDegerZinciri = 23,
        BasvuruFinansman = 24,
        BasvuruMakineEkipman = 25,
        BasvuruYatirimOnBilgileri = 26,
        BasvuruYatirimdaKullanilacakEkipmanlar = 27,
        BasvuruUrunSurecMakine = 28,
        BasvuruBinaListesi = 29,
        BasvuruIstihdam = 30,
        BasvuruTedarikciEntegrasyonu = 31,
        BasvuruTaahhutBeyan = 32,
        BasvuruCevreselSosyal = 33,
        BasvuruZorunluBelgeler = 34,
        BasvuruOzeti = 35,
        BasvuruIzlemeGostergeleri = 36
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
            new(enumBasvuruBolum.BasvuruSahibi, 15, "Basvuru.Step.1", "Basvuru/_BasvuruSahibi"),
            new(enumBasvuruBolum.BasvuruMaliVeriler, 16, "Basvuru.ApplicationFinancial.Title", "Basvuru/_MaliVeriler"),
            new(enumBasvuruBolum.BasvuruOrtaklikYetki, 17, "Basvuru.ApplicationPartnershipAuthority.Title", "Basvuru/_OrtaklikYetki"),
            new(enumBasvuruBolum.BasvuruYatirimBilgileri, 18, "Basvuru.ApplicationInvestment.Title", "Basvuru/_YatirimBilgileri"),
            new(enumBasvuruBolum.BasvuruDegerZinciri, 19, "Basvuru.Step.3", "Basvuru/_DegerZinciri"),
            new(enumBasvuruBolum.BasvuruFinansman, 20, "Basvuru.Step.5", "Basvuru/_Finansman"),
            new(enumBasvuruBolum.BasvuruYatirimOnBilgileri, 21, "Basvuru.Step.YatirimOnBilgileri", "Basvuru/_YatirimOnBilgileri"),
            new(enumBasvuruBolum.BasvuruYatirimdaKullanilacakEkipmanlar, 22, "Basvuru.Step.YatirimdaKullanilacakEkipmanlar", "Basvuru/_YatirimdaKullanilacakEkipmanlar"),
            new(enumBasvuruBolum.BasvuruMakineEkipman, 23, "Basvuru.Step.MakineEkipman", "Basvuru/_MakineEkipman"),
            new(enumBasvuruBolum.BasvuruUrunSurecMakine, 24, "Basvuru.Step.UrunSurecMakine", "Basvuru/_UrunSurecMakine"),
            new(enumBasvuruBolum.BasvuruBinaListesi, 25, "Basvuru.Step.BinaListesi", "Basvuru/_BinaListesi"),
            new(enumBasvuruBolum.BasvuruIstihdam, 26, "Basvuru.Step.Istihdam", "Basvuru/_Istihdam"),
            new(enumBasvuruBolum.BasvuruTedarikciEntegrasyonu, 27, "Tedarikçi Entegrasyonu", "Basvuru/_TedarikciEntegrasyonu"),
            new(enumBasvuruBolum.BasvuruTaahhutBeyan, 28, "Taahhüt / Beyan", "Basvuru/_TaahhutBeyan"),
            new(enumBasvuruBolum.BasvuruCevreselSosyal, 29, "Çevresel ve Sosyal Veri Formu", "Basvuru/_CevreselSosyalVeriFormu"),
            new(enumBasvuruBolum.BasvuruZorunluBelgeler, 30, "Zorunlu Belgeler", "Basvuru/_ZorunluBelgeler"),
            new(enumBasvuruBolum.BasvuruOzeti, 31, "Başvuru Özeti", "Basvuru/_BasvuruOzeti"),
            new(enumBasvuruBolum.BasvuruIzlemeGostergeleri, 32, "İzleme Göstergeleri", "Basvuru/_IzlemeGostergeleri"),
            new(enumBasvuruBolum.Mali, 20, "Basvuru.Step.6", "Bolumler/_Mali"),
            new(enumBasvuruBolum.Ortaklik, 30, "Basvuru.Step.Ortaklik", "Bolumler/_Ortaklik"),
            new(enumBasvuruBolum.UygulamaAdresi, 40, "Basvuru.Step.4", "Bolumler/_UygulamaAdresi"),
            new(enumBasvuruBolum.Yatirim, 50, "Basvuru.Step.3", "Bolumler/_Yatirim"),
            new(enumBasvuruBolum.Finans, 60, "Basvuru.Step.5", "Bolumler/_Finans"),
            new(enumBasvuruBolum.YatirimOzeti, 70, "Basvuru.Step.YatirimOzeti", "Bolumler/_YatirimOzeti"),
            new(enumBasvuruBolum.DbCtpTeknikProje, 80, "Basvuru.Step.DbCtp", "Bolumler/_DbCtpTeknikProje"),
            new(enumBasvuruBolum.Belgeler, 90, "Basvuru.Step.7", "Bolumler/_Belgeler"),
            new(enumBasvuruBolum.CevreselSosyal, 100, "Basvuru.Step.CevreselSosyal", "Bolumler/_CevreselSosyal"),
            new(enumBasvuruBolum.TaahhutBeyan, 110, "Basvuru.Step.TaahhutBeyan", "Bolumler/_TaahhutBeyan"),
            new(enumBasvuruBolum.Ozet, 120, "Basvuru.Step.Ozet", "Bolumler/_Ozet"),
            new(enumBasvuruBolum.SistemSonuclari, 130, "Basvuru.Step.SystemResults", "Bolumler/_SistemSonuclari", true),
            new(enumBasvuruBolum.UzmanSonuclari, 140, "Basvuru.Step.ExpertResults", "Bolumler/_UzmanSonuclari", true),
            new(enumBasvuruBolum.Karar, 150, "Basvuru.Step.Decision", "Bolumler/_Denetim", true)
        ];

        public static IReadOnlyList<BasvuruBolumTanim> Tum(bool denetciGorunumu, enumBasvuruKayitTuru kayitTuru = enumBasvuruKayitTuru.OnBasvuru)
        {
            if (kayitTuru == enumBasvuruKayitTuru.Basvuru)
            {
                return Tanimlar
                    .Where(x => x.Bolum == enumBasvuruBolum.BasvuruSahibi || x.Bolum == enumBasvuruBolum.BasvuruMaliVeriler || x.Bolum == enumBasvuruBolum.BasvuruOrtaklikYetki || x.Bolum == enumBasvuruBolum.BasvuruYatirimBilgileri || x.Bolum == enumBasvuruBolum.BasvuruDegerZinciri || x.Bolum == enumBasvuruBolum.BasvuruFinansman || x.Bolum == enumBasvuruBolum.BasvuruYatirimOnBilgileri || x.Bolum == enumBasvuruBolum.BasvuruYatirimdaKullanilacakEkipmanlar || x.Bolum == enumBasvuruBolum.BasvuruMakineEkipman || x.Bolum == enumBasvuruBolum.BasvuruUrunSurecMakine || x.Bolum == enumBasvuruBolum.BasvuruBinaListesi || x.Bolum == enumBasvuruBolum.BasvuruIstihdam || x.Bolum == enumBasvuruBolum.BasvuruTedarikciEntegrasyonu || x.Bolum == enumBasvuruBolum.BasvuruTaahhutBeyan || x.Bolum == enumBasvuruBolum.BasvuruCevreselSosyal || x.Bolum == enumBasvuruBolum.BasvuruZorunluBelgeler || x.Bolum == enumBasvuruBolum.BasvuruOzeti || x.Bolum == enumBasvuruBolum.BasvuruIzlemeGostergeleri || x.Bolum == enumBasvuruBolum.YatirimOzeti)
                    .ToList();
            }

            return Tanimlar
                .Where(x => x.Bolum != enumBasvuruBolum.BasvuruSahibi
                    && x.Bolum != enumBasvuruBolum.BasvuruMaliVeriler
                    && x.Bolum != enumBasvuruBolum.BasvuruOrtaklikYetki
                    && x.Bolum != enumBasvuruBolum.BasvuruYatirimBilgileri
                    && x.Bolum != enumBasvuruBolum.BasvuruDegerZinciri
                    && x.Bolum != enumBasvuruBolum.BasvuruFinansman
                    && x.Bolum != enumBasvuruBolum.BasvuruYatirimOnBilgileri
                    && x.Bolum != enumBasvuruBolum.BasvuruYatirimdaKullanilacakEkipmanlar
                    && x.Bolum != enumBasvuruBolum.BasvuruMakineEkipman)
                .Where(x => x.Bolum != enumBasvuruBolum.BasvuruUrunSurecMakine)
                .Where(x => x.Bolum != enumBasvuruBolum.BasvuruBinaListesi)
                .Where(x => x.Bolum != enumBasvuruBolum.BasvuruIstihdam)
                .Where(x => x.Bolum != enumBasvuruBolum.BasvuruTedarikciEntegrasyonu)
                .Where(x => x.Bolum != enumBasvuruBolum.BasvuruTaahhutBeyan)
                .Where(x => x.Bolum != enumBasvuruBolum.BasvuruCevreselSosyal)
                .Where(x => x.Bolum != enumBasvuruBolum.BasvuruZorunluBelgeler)
                .Where(x => x.Bolum != enumBasvuruBolum.BasvuruOzeti)
                .Where(x => x.Bolum != enumBasvuruBolum.BasvuruIzlemeGostergeleri)
                .Where(x => denetciGorunumu || !x.DenetciBolumu)
                .OrderBy(x => x.Sira)
                .ToList();
        }

        public static BasvuruBolumTanim? Bul(enumBasvuruBolum bolum, bool denetciGorunumu, enumBasvuruKayitTuru kayitTuru = enumBasvuruKayitTuru.OnBasvuru)
        {
            return Tum(denetciGorunumu, kayitTuru).FirstOrDefault(x => x.Bolum == bolum);
        }
    }
}
