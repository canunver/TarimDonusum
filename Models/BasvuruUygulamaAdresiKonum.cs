namespace TarimDonusum.Models;

public class BasvuruUygulamaAdresiKonum
{
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

    public void Dogrula(Sonuc sonuc, int satir)
    {
        if (minEnlem is < -90 or > 90 || maxEnlem is < -90 or > 90)
            sonuc.HataEkle($"Konum tablosu {satir}. satır: Enlem -90 ile 90 arasında olmalıdır.");
        if (minBoylam is < -180 or > 180 || maxBoylam is < -180 or > 180)
            sonuc.HataEkle($"Konum tablosu {satir}. satır: Boylam -180 ile 180 arasında olmalıdır.");
        if (minEnlem > maxEnlem || minBoylam > maxBoylam)
            sonuc.HataEkle($"Konum tablosu {satir}. satır: Minimum değer maksimum değerden büyük olamaz.");
        if ((ada?.Length ?? 0) > 30 || (parsel?.Length ?? 0) > 30 || (mahalle?.Length ?? 0) > 200)
            sonuc.HataEkle($"Konum tablosu {satir}. satır: Ada ve parsel en fazla 30, mahalle en fazla 200 karakter olmalıdır.");
    }
}
