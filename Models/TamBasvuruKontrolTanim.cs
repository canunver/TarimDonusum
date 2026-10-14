namespace TarimDonusum.Models;

/// <summary>
/// Tam başvuru tamlık kontrol formunun "A. Belge / Form Envanteri ve Ön Kontrol" bölümündeki kontrol satırı.
/// Uzman her satırı Evet / Hayır (+ açıklama) ile değerlendirir; sonuç TAM / EKSİK olarak hesaplanır.
/// </summary>
public class TamBasvuruBelgeKontrolTanim
{
    public int id { get; set; }
    public int siraNo { get; set; }
    public string soru { get; set; } = "";
    /// <summary>Koşullu satırlar başvuruya uygulanmıyorsa Evet işaretlenir ve açıklamaya gerekçe yazılır.</summary>
    public bool kosullu { get; set; }
    public bool aktif { get; set; } = true;
}

/// <summary>
/// Tam başvuru tamlık kontrol formunun "B. Detaylı Tamlık ve Uygunluk Soruları" bölümündeki soru.
/// Uzman her soruyu Evet / Hayır (+ açıklama) ile değerlendirir; sonuç UYGUN / UYGUN DEĞİL olarak hesaplanır.
/// </summary>
public class TamBasvuruUygunlukSorusuTanim
{
    public int id { get; set; }
    public int siraNo { get; set; }
    public string soru { get; set; } = "";
    /// <summary>"Nasıl kontrol edilir" açıklaması / uygulama esası.</summary>
    public string kontrolAciklamasi { get; set; } = "";
    public string kaynakBelge { get; set; } = "";
    public bool aktif { get; set; } = true;
}

public static class TamBasvuruBelgeKontrolTanimSaglayici
{
    private static IReadOnlyList<TamBasvuruBelgeKontrolTanim> _tum = TamBasvuruKontrolVarsayilanlari.BelgeKontrolleri;

    public static IReadOnlyList<TamBasvuruBelgeKontrolTanim> Tum => Volatile.Read(ref _tum);

    public static IReadOnlyList<TamBasvuruBelgeKontrolTanim> Aktifler => Tum.Where(x => x.aktif).ToList();

    public static void Guncelle(IEnumerable<TamBasvuruBelgeKontrolTanim> tanimlar) =>
        Volatile.Write(ref _tum, tanimlar.OrderBy(x => x.siraNo).ThenBy(x => x.id).ToList());
}

public static class TamBasvuruUygunlukSorusuTanimSaglayici
{
    private static IReadOnlyList<TamBasvuruUygunlukSorusuTanim> _tum = TamBasvuruKontrolVarsayilanlari.UygunlukSorulari;

    public static IReadOnlyList<TamBasvuruUygunlukSorusuTanim> Tum => Volatile.Read(ref _tum);

    public static IReadOnlyList<TamBasvuruUygunlukSorusuTanim> Aktifler => Tum.Where(x => x.aktif).ToList();

    public static void Guncelle(IEnumerable<TamBasvuruUygunlukSorusuTanim> tanimlar) =>
        Volatile.Write(ref _tum, tanimlar.OrderBy(x => x.siraNo).ThenBy(x => x.id).ToList());
}

/// <summary>Uzmanın bir kontrol satırına verdiği cevap ("Evet", "Hayır" veya boş) ve açıklaması.</summary>
public class TamBasvuruKontrolCevap
{
    public int tanimId { get; set; }
    public string cevap { get; set; } = "";
    public string aciklama { get; set; } = "";
}

/// <summary>Belge / form ön kontrolü (belgeKontrolu=true) veya uygunluk soruları cevaplarının kaydı.</summary>
public class TamBasvuruKontrolKayit
{
    public int basvuruId { get; set; }
    public bool belgeKontrolu { get; set; }
    public List<TamBasvuruKontrolCevap> cevaplar { get; set; } = new();
}

public static class TamBasvuruKontrolCevaplari
{
    /// <summary>Cevap JSON'unda kaydı bulunan tanım Id'leri; pasife alınmış ama cevaplanmış satırlar yine gösterilir.</summary>
    public static HashSet<int> CevaplananIdler(string? json)
    {
        HashSet<int> idler = [];
        if (string.IsNullOrWhiteSpace(json)) return idler;
        try
        {
            using System.Text.Json.JsonDocument belge = System.Text.Json.JsonDocument.Parse(json);
            if (belge.RootElement.ValueKind != System.Text.Json.JsonValueKind.Object) return idler;
            foreach (System.Text.Json.JsonProperty alan in belge.RootElement.EnumerateObject())
                if (int.TryParse(alan.Name, out int id)) idler.Add(id);
        }
        catch (System.Text.Json.JsonException) { }
        return idler;
    }

    public static string JsonVeyaBos(string? json) => string.IsNullOrWhiteSpace(json) ? "{}" : json;
}
