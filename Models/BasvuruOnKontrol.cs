namespace TarimDonusum.Models;

/// <summary>
/// Başvurunun incelemeye gönderilmeden önce, ön başvuru gönderim kontrollerine ek olarak geçmesi gereken kontroller.
/// Hem "Özet - Ön Kontrol" sayfası hem de incelemeye gönderim iş kuralı bu listeyi kullanır.
/// </summary>
public static class BasvuruOnKontrol
{
    public static List<string> Eksikler(Basvuru b)
    {
        List<string> eksikler = [];
        if (b.kayitTuru != enumBasvuruKayitTuru.Basvuru) return eksikler;

        // Excel'den yüklenen veya alan eklenmeden önce girilmiş makinelerde uygun harcama boş kalabilir.
        List<string> uygunHarcamasizlar = b.Makineler
            .Where(x => x.uygunHarcamaId is null)
            .OrderBy(x => x.siraNo)
            .Select(x => string.IsNullOrWhiteSpace(x.ad) ? $"{x.siraNo}. sıra" : x.ad.Trim())
            .ToList();
        if (uygunHarcamasizlar.Count > 0)
            eksikler.Add("Teknik Proje (Makine Ekipman) sayfasında uygun harcama seçilmemiş makine/ekipman var: " + string.Join(", ", uygunHarcamasizlar));

        return eksikler;
    }
}
