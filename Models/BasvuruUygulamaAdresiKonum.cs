namespace TarimDonusum.Models;

public enum enumKoordinatTuru
{
    ITRF96 = 1,
    ED50 = 2
}

public class BasvuruUygulamaAdresiKonum
{
    // Türkiye'nin coğrafi kapsama alanı. Her iki sistemde de değerler enlem/boylam (derece) olarak alınır.
    private const decimal TurkiyeMinEnlem = 35.80m;
    private const decimal TurkiyeMaxEnlem = 42.20m;
    private const decimal TurkiyeMinBoylam = 25.50m;
    private const decimal TurkiyeMaxBoylam = 45.00m;

    public int id { get; set; }
    public int adresId { get; set; }
    public int siraNo { get; set; }
    public decimal? minEnlem { get; set; }
    public decimal? maxEnlem { get; set; }
    public decimal? minBoylam { get; set; }
    public decimal? maxBoylam { get; set; }
    public string? ada { get; set; }
    public string? parsel { get; set; }
    public string? mahalle { get; set; }
    public enumKoordinatTuru koordinatTuru { get; set; } = enumKoordinatTuru.ITRF96;

    public void Dogrula(Sonuc sonuc, int satir)
    {
        if (string.IsNullOrWhiteSpace(mahalle))
            sonuc.HataEkle($"Konum tablosu {satir}. satır: Mahalle boş olamaz.");
        if (!int.TryParse(ada, out int adaNo) || adaNo <= 0)
            sonuc.HataEkle($"Konum tablosu {satir}. satır: Ada sıfırdan büyük bir tam sayı olmalıdır.");
        if (!int.TryParse(parsel, out int parselNo) || parselNo <= 0)
            sonuc.HataEkle($"Konum tablosu {satir}. satır: Parsel sıfırdan büyük bir tam sayı olmalıdır.");
        string koordinatTuruAdi = koordinatTuru == enumKoordinatTuru.ED50 ? "ED50" : "ITRF 96";
        if (minEnlem is < TurkiyeMinEnlem or > TurkiyeMaxEnlem || maxEnlem is < TurkiyeMinEnlem or > TurkiyeMaxEnlem)
            sonuc.HataEkle($"Konum tablosu {satir}. satır ({koordinatTuruAdi}): Enlem Türkiye sınırları için {TurkiyeMinEnlem:0.00} ile {TurkiyeMaxEnlem:0.00} arasında olmalıdır.");
        if (minBoylam is < TurkiyeMinBoylam or > TurkiyeMaxBoylam || maxBoylam is < TurkiyeMinBoylam or > TurkiyeMaxBoylam)
            sonuc.HataEkle($"Konum tablosu {satir}. satır ({koordinatTuruAdi}): Boylam Türkiye sınırları için {TurkiyeMinBoylam:0.00} ile {TurkiyeMaxBoylam:0.00} arasında olmalıdır.");
        if (minEnlem > maxEnlem || minBoylam > maxBoylam)
            sonuc.HataEkle($"Konum tablosu {satir}. satır: Minimum değer maksimum değerden büyük olamaz.");
        if ((ada?.Length ?? 0) > 30 || (parsel?.Length ?? 0) > 30 || (mahalle?.Length ?? 0) > 200)
            sonuc.HataEkle($"Konum tablosu {satir}. satır: Ada ve parsel en fazla 30, mahalle en fazla 200 karakter olmalıdır.");
        if (koordinatTuru is not enumKoordinatTuru.ITRF96 and not enumKoordinatTuru.ED50)
            sonuc.HataEkle($"Konum tablosu {satir}. satır: Koordinat türü ITRF 96 veya ED50 olmalıdır.");
    }
}
