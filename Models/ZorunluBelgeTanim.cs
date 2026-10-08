using System.Text.RegularExpressions;

namespace TarimDonusum.Models;

/// <summary>
/// Ön başvurunun Zorunlu Belgeler sayfasında ve tüzel kişi ortak belgelerinde istenen, Tanımlar ekranından yönetilen belge.
/// </summary>
public class ZorunluBelgeTanim
{
    public int id { get; set; }
    /// <summary>Yüklenen dosyaların bağlandığı belge numarası (dosya no). Kaydedildikten sonra değişmez.</summary>
    public int belgeNo { get; set; }
    public int siraNo { get; set; }
    /// <summary>Belge adı; [dönem], [dönem-1] gibi yer tutucular başvurunun dönem yılına göre yazılır.</summary>
    public string ad { get; set; } = "";
    /// <summary>Yalnızca kooperatif ve üretici örgütü başvurularında istenir; tüzel kişi ortaklardan istenmez.</summary>
    public bool sadeceKooperatif { get; set; }
    public bool aktif { get; set; } = true;
}

public static class ZorunluBelgeTanimSaglayici
{
    // [dönem], [dönem-1], [dönem+1] (büyük/küçük harf ve "donem" yazımı da kabul edilir)
    private static readonly Regex YerTutucu = new(@"\[\s*d[öo]nem\s*(?:([+-])\s*(\d{1,2}))?\s*\]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    /// <summary>Tablo boşken eklenen, daha önce sayfada sabit duran belgeler (numaraları yüklenmiş dosyalarla uyumludur).</summary>
    public static IReadOnlyList<ZorunluBelgeTanim> Varsayilanlar =>
    [
        new() { belgeNo = 1, siraNo = 1, ad = "[dönem-1] yılına ait gelir tablosu" },
        new() { belgeNo = 2, siraNo = 2, ad = "[dönem-1] yılına ait bilanço" },
        new() { belgeNo = 3, siraNo = 3, ad = "[dönem-1] yılına ait detaylı mizan" },
        new() { belgeNo = 4, siraNo = 4, ad = "Ara dönem raporları" },
        new() { belgeNo = 5, siraNo = 5, ad = "İflas, konkordato ve tasfiye sürecinin olmadığına dair belge" },
        new() { belgeNo = 6, siraNo = 6, ad = "Ticaret sicil gazetesi, kuruluş ve mevcut durum" },
        new() { belgeNo = 7, siraNo = 7, ad = "Noter onaylı imza sirküleri", aktif = false },
        new() { belgeNo = 9, siraNo = 8, ad = "[dönem-2] yılına ait gelir tablosu" },
        new() { belgeNo = 10, siraNo = 9, ad = "[dönem-2] yılına ait bilanço" },
        new() { belgeNo = 11, siraNo = 10, ad = "[dönem-2] yılına ait detaylı mizan" },
        new() { belgeNo = 8, siraNo = 11, ad = "Güncel tüzük / ana sözleşme", sadeceKooperatif = true }
    ];

    private static IReadOnlyList<ZorunluBelgeTanim> _tum = Varsayilanlar;

    public static IReadOnlyList<ZorunluBelgeTanim> Tum => Volatile.Read(ref _tum);

    public static IReadOnlyList<ZorunluBelgeTanim> Aktifler => Tum.Where(x => x.aktif).OrderBy(x => x.siraNo).ThenBy(x => x.belgeNo).ToList();

    public static void Guncelle(IEnumerable<ZorunluBelgeTanim> tanimlar) => Volatile.Write(ref _tum, tanimlar.ToList());

    /// <summary>Başvuru sahibinden istenen belgeler; kooperatife özel belgeler yalnızca kooperatif ve üretici örgütlerinde gelir.</summary>
    public static IReadOnlyList<ZorunluBelgeTanim> BasvuruIcin(Basvuru b)
    {
        bool kooperatif = b.basvuruFirma.basvuruSahibiTuru is enumBasvuruSahibiTuru.UreticiOrgutu or enumBasvuruSahibiTuru.Kooperatif;
        return Aktifler.Where(x => kooperatif || !x.sadeceKooperatif).ToList();
    }

    /// <summary>Tüzel kişi ortaklardan istenen belgeler.</summary>
    public static IReadOnlyList<ZorunluBelgeTanim> OrtakIcin => Aktifler.Where(x => !x.sadeceKooperatif).ToList();

    /// <summary>Pasif belgeler dahil; daha önce yüklenmiş dosyaların türü yine bulunabilsin.</summary>
    public static ZorunluBelgeTanim? Bul(int belgeNo) => Tum.FirstOrDefault(x => x.belgeNo == belgeNo);

    /// <summary>Yer tutucuları dönem yılına göre yazar: [dönem] = dönem yılı, [dönem-1] = bir önceki yıl...</summary>
    public static string YillariYaz(string ad, int donemYili) =>
        YerTutucu.Replace(ad ?? "", m =>
        {
            int fark = m.Groups[2].Success ? int.Parse(m.Groups[2].Value) : 0;
            return (m.Groups[1].Value == "-" ? donemYili - fark : donemYili + fark).ToString();
        });

    public static string Ad(Basvuru b, int belgeNo, string varsayilan = "") =>
        YillariYaz(Bul(belgeNo)?.ad ?? varsayilan, FirmaGecmisYilKurallari.DonemYili(b));

    /// <summary>Belge adındaki en eski yıl dönem yılından kaç yıl önce (ör. [dönem-2] için 2); geçmiş yıl yoksa 0.</summary>
    public static int EnEskiYilFarki(int belgeNo)
    {
        string ad = Bul(belgeNo)?.ad ?? "";
        return YerTutucu.Matches(ad).Select(m => m.Groups[1].Value == "-" && m.Groups[2].Success ? int.Parse(m.Groups[2].Value) : 0).DefaultIfEmpty(0).Max();
    }
}
