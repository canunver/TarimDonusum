namespace TarimDonusum.Models;

public sealed class DonemTahminAyari
{
    public int donemId { get; set; }
    public int donemYili { get; set; }
    public List<DonemTahminSatiri> tahminler { get; set; } = [];
}

public sealed class DonemTahminSatiri
{
    public int yil { get; set; }
    public decimal? kurTahminiTL { get; set; }
    public decimal? enflasyonTahminiYuzde { get; set; }
}

/// <summary>
/// Dönem kur ve enflasyon tahminleri (dönem yılı = yatırım dönemi + izleyen 7 yıl).
/// Uygulama açılışında yüklenir, Tanımlar ekranından kaydedildiğinde güncellenir; Geri Ödeme ve Karlılık hesapları kullanır.
/// </summary>
public static class DonemTahminSaglayici
{
    public const int YilSayisi = 8;

    private static IReadOnlyDictionary<int, IReadOnlyList<DonemTahminSatiri>> _tum = new Dictionary<int, IReadOnlyList<DonemTahminSatiri>>();

    public static IReadOnlyList<DonemTahminSatiri> Donem(int donemId) =>
        Volatile.Read(ref _tum).TryGetValue(donemId, out IReadOnlyList<DonemTahminSatiri>? liste) ? liste : [];

    public static void Yukle(IEnumerable<(int donemId, DonemTahminSatiri satir)> kayitlar) =>
        Volatile.Write(ref _tum, kayitlar.GroupBy(x => x.donemId)
            .ToDictionary(x => x.Key, x => (IReadOnlyList<DonemTahminSatiri>)x.Select(y => y.satir).OrderBy(y => y.yil).ToList()));

    public static void Guncelle(int donemId, IEnumerable<DonemTahminSatiri> tahminler)
    {
        Dictionary<int, IReadOnlyList<DonemTahminSatiri>> yeni = new(Volatile.Read(ref _tum))
        {
            [donemId] = tahminler.OrderBy(x => x.yil).ToList()
        };
        Volatile.Write(ref _tum, yeni);
    }
}
