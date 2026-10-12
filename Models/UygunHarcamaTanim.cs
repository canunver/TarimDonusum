namespace TarimDonusum.Models;

/// <summary>Tanımlar ekranından yönetilen, başvuruda makine-ekipman kaydına seçilen uygun harcama kalemi.</summary>
public class UygunHarcamaTanim
{
    public int id { get; set; }
    public string kod { get; set; } = "";
    public string ad { get; set; } = "";
    public bool aktif { get; set; } = true;
}

public static class UygunHarcamaTanimSaglayici
{
    private static IReadOnlyList<UygunHarcamaTanim> _tum = [];

    public static IReadOnlyList<UygunHarcamaTanim> Tum => Volatile.Read(ref _tum);

    public static IReadOnlyList<UygunHarcamaTanim> Aktifler => Tum.Where(x => x.aktif).OrderBy(x => x.kod, StringComparer.OrdinalIgnoreCase).ToList();

    public static void Guncelle(IEnumerable<UygunHarcamaTanim> liste) => Volatile.Write(ref _tum, liste.ToList());

    /// <summary>Pasif kayıtlar dahil; daha önce seçilmiş kalem yine bulunabilsin.</summary>
    public static UygunHarcamaTanim? Bul(int? id) => id is > 0 ? Tum.FirstOrDefault(x => x.id == id) : null;

    public static string Etiket(UygunHarcamaTanim x) => $"{x.kod} - {x.ad}";

    public static string Etiket(int? id) => Bul(id) is UygunHarcamaTanim x ? Etiket(x) : "";
}
