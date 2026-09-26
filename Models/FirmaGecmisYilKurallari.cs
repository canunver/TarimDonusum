namespace TarimDonusum.Models;

public static class FirmaGecmisYilKurallari
{
    public static int DonemYili(Basvuru b) => b.basvuruFirma.donem?.yil > 0 ? b.basvuruFirma.donem.yil : DateTime.Today.Year;
    public static bool Girilebilir(Basvuru b, int yilGeri) => !b.basvuruFirma.firma.kurulusTarihi.HasValue
        || b.basvuruFirma.firma.kurulusTarihi.Value.Year <= DonemYili(b) - yilGeri;

    public static string Hata(Basvuru b) => b.basvuruFirma.firma.kurulusTarihi?.Year == DateTime.Today.Year
        ? "Bu yıl kurulmuş bir firma önceki yıl bilgilerini giremez veya bu yıllara ait belge yükleyemez."
        : "Firma kuruluş tarihinden önceki yıllara ait bilgi girilemez veya belge yüklenemez.";

    public static string BelgeAdi(Basvuru b, int dosyaNo, string varsayilan)
    {
        string? tur = dosyaNo switch { 1 or 9 => "gelir tablosu", 2 or 10 => "bilanço", 3 or 11 => "detaylı mizan", _ => null };
        return tur == null ? varsayilan : $"{DonemYili(b) - (dosyaNo <= 3 ? 1 : 2)} yılına ait {tur}";
    }

    public static bool BelgeEngelli(Basvuru b, string formAd, int dosyaNo)
    {
        if (string.Equals(formAd, "Basvuru_ZorunluBelge", StringComparison.OrdinalIgnoreCase))
            return dosyaNo is 1 or 2 or 3 ? !Girilebilir(b, 1) : dosyaNo is 9 or 10 or 11 && !Girilebilir(b, 2);
        if (string.Equals(formAd, "Basvuru_MaliBelge", StringComparison.OrdinalIgnoreCase) && dosyaNo is >= 1 and <= 8)
            return !Girilebilir(b, dosyaNo % 2 == 1 ? 2 : 1);
        if (string.Equals(formAd, "Basvuru_ZorunluBelgeMerkezi", StringComparison.OrdinalIgnoreCase) && dosyaNo == 16)
            return !Girilebilir(b, 1);
        return false;
    }

    public static void Dogrula(Basvuru b, BasvuruMali mali, Sonuc sonuc)
    {
        bool oncekiVeri = new decimal?[] { mali.oncekiYilNetSatis, mali.oncekiYilAktifToplami, mali.oncekiYilIhracatSatis, mali.oncekiYilCalisanSayisi }.Any(x => x.HasValue);
        bool sonVeri = new decimal?[] { mali.sonYilNetSatis, mali.sonYilAktifToplami, mali.sonYilIhracatSatis, mali.sonYilCalisanSayisi }.Any(x => x.HasValue);
        if ((!Girilebilir(b, 2) && oncekiVeri) || (!Girilebilir(b, 1) && sonVeri)) sonuc.HataEkle(Hata(b));
    }
}
