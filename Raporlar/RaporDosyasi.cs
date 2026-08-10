using TarimDonusum.Araclar;
using TarimDonusum.Models;

namespace TarimDonusum.Raporlar;

public sealed record RaporDosyasi(byte[] Icerik, string DosyaAdi)
{
    public const string ExcelMimeTuru = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
}

public interface IRPROB
{
    string SablonDosyasi { get; }
    RaporDosyasi Olustur(Basvuru basvuru, int basvuruId);
}

public abstract class RPROBTemel(string uygulamaRootPath) : IRPROB
{
    protected abstract string SablonAdi { get; }
    protected abstract string GeciciDosyaOnEki { get; }
    protected abstract string CiktiDosyaOnEki { get; }
    protected abstract void Doldur(Tablo tablo, Basvuru basvuru);

    public string SablonDosyasi => Path.Combine(uygulamaRootPath, "Sablonlar", SablonAdi);

    public RaporDosyasi Olustur(Basvuru basvuru, int basvuruId)
    {
        if (!File.Exists(SablonDosyasi))
            throw new FileNotFoundException($"Rapor şablonu bulunamadı: {SablonAdi}", SablonDosyasi);

        string geciciDosya = Path.Combine(Path.GetTempPath(), $"{GeciciDosyaOnEki}-{Guid.NewGuid():N}.xlsx");
        Tablo? tablo = null;
        try
        {
            tablo = OrtakFonksiyonlar.NewTablo();
            tablo.DosyaAc(SablonDosyasi, geciciDosya);
            Doldur(tablo, basvuru);
            tablo.DosyaSaklaTamYol();
            tablo.DosyaKapat();
            tablo = null;

            return new RaporDosyasi(
                File.ReadAllBytes(geciciDosya),
                $"{CiktiDosyaOnEki}-{basvuruId}.xlsx");
        }
        finally
        {
            if (tablo != null)
                tablo.DosyaKapat();
            if (File.Exists(geciciDosya))
                File.Delete(geciciDosya);
        }
    }
}
