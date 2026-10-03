namespace TarimDonusum.Models;

/// <summary>Ön başvurunun Taahhüt/Beyan sayfasında gösterilen, Tanımlar ekranından yönetilen madde.</summary>
public class TaahhutBeyanTanim
{
    public int id { get; set; }
    /// <summary>Başvurudaki TaahhutBeyanlarJson içinde onayın saklandığı alan adı. Kaydedildikten sonra değişmez.</summary>
    public string anahtar { get; set; } = "";
    public int siraNo { get; set; }
    public string metin { get; set; } = "";
    public bool zorunlu { get; set; } = true;
    public bool aktif { get; set; } = true;
}

public static class TaahhutBeyanTanimSaglayici
{
    public static IReadOnlyList<TaahhutBeyanTanim> Varsayilanlar(string cevreselSosyalMetin) =>
    [
        new() { anahtar = "dogrulukBeyani", siraNo = 1, zorunlu = true, metin = "Başvuru bilgilerinin ve yüklenen belgelerin doğru ve eksiksiz olduğunu beyan ederim." },
        new() { anahtar = "cifteFinansmanBeyani", siraNo = 2, zorunlu = true, metin = "Aynı yatırım veya aynı harcama kalemleri için çifte finansman bulunmadığını beyan ederim." },
        new() { anahtar = "bankaVeriPaylasimRizasi", siraNo = 3, zorunlu = true, metin = "Ziraat Bankası ve ilgili finansal kurumlarla ön finansal/kambiyo uygunluk kontrolü için gerekli veri paylaşımına açık rıza verdiğimi kabul ederim." },
        new() { anahtar = "izlemeDenetimKabulu", siraNo = 4, zorunlu = true, metin = "İzleme, raporlama, kontrol, denetim, yerinde inceleme ve belge doğrulama süreçlerini kabul ederim." },
        new() { anahtar = CevreselSosyalTaahhut.Alan, siraNo = 5, zorunlu = false, metin = cevreselSosyalMetin }
    ];

    private static IReadOnlyList<TaahhutBeyanTanim> _tum = Varsayilanlar(CevreselSosyalTaahhutTanimSaglayici.VarsayilanMetin);

    public static IReadOnlyList<TaahhutBeyanTanim> Tum => Volatile.Read(ref _tum);

    public static IReadOnlyList<TaahhutBeyanTanim> Aktifler =>
        Tum.Where(x => x.aktif).OrderBy(x => x.siraNo).ThenBy(x => x.id).ToList();

    public static void Guncelle(IEnumerable<TaahhutBeyanTanim> tanimlar)
    {
        IReadOnlyList<TaahhutBeyanTanim> yeni = tanimlar.ToList();
        Volatile.Write(ref _tum, yeni);
    }

    public static bool OnayliMi(Basvuru basvuru, string anahtar) =>
        anahtar == CevreselSosyalTaahhut.Alan
            ? CevreselSosyalTaahhut.OnayliMi(basvuru)
            : CevreselSosyalTaahhut.Oku(basvuru.TaahhutBeyanlarJson, anahtar) == true;
}
