namespace TarimDonusum.Models;

/// <summary>
/// Başvurunun İzleme Göstergeleri sayfasında listelenen, Tanımlar ekranından yönetilen gösterge.
/// </summary>
public class IzlemeGostergesiTanim
{
    public int id { get; set; }
    /// <summary>Başvuru kayıtlarının (BasvuruIzlemeGostergesi.GostergeKodu) bağlandığı anahtar. Kaydedildikten sonra değişmez.</summary>
    public string kod { get; set; } = "";
    public int siraNo { get; set; }
    public string ad { get; set; } = "";
    public string birim { get; set; } = "";
    public string veriKaynagi { get; set; } = "";
    public string siklik { get; set; } = "";
    public bool aktif { get; set; } = true;
}

public static class IzlemeGostergesiTanimSaglayici
{
    /// <summary>Tablo boşken eklenen, daha önce sayfada sabit duran göstergeler (kodları kayıtlı başvuru verileriyle uyumludur).</summary>
    public static IReadOnlyList<IzlemeGostergesiTanim> Varsayilanlar =>
    [
        Yeni(1, "ciftci_ticari", "Aktif ticari düzenlemeye katılan çiftçiler", "Çiftçi sayısı", "Sözleşme, alım anlaşması, fatura", "Altı aylık"),
        Yeni(2, "ciftci_erisim", "AE/PO üzerinden hizmet veya mal erişen çiftçiler", "Çiftçi sayısı", "Hizmet/girdi kayıtları", "Altı aylık"),
        Yeni(3, "ciftci_ilave", "İlave kayıtlı çiftçi / birincil üretici", "Kişi", "Tekil kayıt, ÇKS, ticari düzenleme", "Altı aylık"),
        Yeni(4, "tzi_mevcut", "Mevcut tam zamanlı eşdeğer istihdam", "TZİ", "SGK, bordro, zaman kaydı", "Yıllık"),
        Yeni(5, "tzi_ilave", "İlave tam zamanlı eşdeğer istihdam", "TZİ", "SGK, bordro, zaman kaydı", "Yıllık"),
        Yeni(6, "mbj", "MBJ iş eşdeğeri", "MBJ iş eşdeğeri", "İşgücü geliri ve referans FTE geliri", "Yıllık"),
        Yeni(7, "isgucu_toplam", "Toplam işgücü geliri", "TL/yıl", "Bordro, ücret ödeme kayıtları", "Yıllık"),
        Yeni(8, "isgucu_ortalama", "Ortalama TZİ işgücü geliri", "TL/FTE-yıl", "Bordro ve TZİ hesabı", "Yıllık"),
        Yeni(9, "kapasite", "Üretim / işleme kapasitesi", "Ürün bazlı birim", "Teknik proje, kapasite raporu", "Yıllık"),
        Yeni(10, "satis", "Satış geliri", "TL/yıl", "Mali tablolar, satış kayıtları", "Yıllık"),
        Yeni(11, "ihracat", "İhracat geliri", "TL/yıl", "Gümrük ve mali kayıtlar", "Yıllık"),
        Yeni(12, "ihracat_oran", "İhracat / satış oranı", "%", "Mali ve ihracat kayıtları", "Yıllık"),
        Yeni(13, "sertifika", "Sertifikasyon / kalite sistemi", "Sayı / durum", "Sertifika, denetim raporu", "Yıllık"),
        Yeni(14, "izlenebilirlik", "İzlenebilirlik kapsamı", "% veya ürün sayısı", "İzlenebilirlik sistemi", "Yıllık"),
        Yeni(15, "enerji", "Enerji kullanımı / verimlilik", "kWh/birim ürün", "Fatura, sayaç, izleme sistemi", "Yıllık"),
        Yeni(16, "su", "Su kullanımı / verimlilik", "m3/birim ürün", "Sayaç, fatura, izleme kaydı", "Yıllık"),
        Yeni(17, "atik", "Atık / yan ürün değerlendirme", "ton/yıl veya %", "Atık, satış/bertaraf kayıtları", "Yıllık"),
        Yeni(18, "sera_gazi", "Sera gazı emisyonu", "tCO2e/yıl", "Hesap/rapor ve faaliyet verisi", "Yıllık"),
        Yeni(19, "operasyon", "Ana yatırım unsurlarının operasyonel durumu", "Sayı / durum", "Fatura, kabul, fotoğraf, yerinde kontrol", "Üç aylık"),
        Yeni(20, "ilerleme", "Yatırım uygulama ilerlemesi", "%", "İlerleme raporu", "Üç aylık")
    ];

    private static IzlemeGostergesiTanim Yeni(int siraNo, string kod, string ad, string birim, string veriKaynagi, string siklik) =>
        new() { siraNo = siraNo, kod = kod, ad = ad, birim = birim, veriKaynagi = veriKaynagi, siklik = siklik };

    private static IReadOnlyList<IzlemeGostergesiTanim> _tum = Varsayilanlar;

    public static IReadOnlyList<IzlemeGostergesiTanim> Tum => Volatile.Read(ref _tum);

    public static void Guncelle(IEnumerable<IzlemeGostergesiTanim> tanimlar) =>
        Volatile.Write(ref _tum, tanimlar.OrderBy(x => x.siraNo).ThenBy(x => x.id).ToList());

    /// <summary>Pasif göstergeler dahil; daha önce girilmiş değerlerin göstergesi yine bulunabilsin.</summary>
    public static IzlemeGostergesiTanim? Bul(string? kod) => Tum.FirstOrDefault(x => string.Equals(x.kod, kod, StringComparison.Ordinal));

    /// <summary>Başvuruda gösterilen göstergeler: aktif olanlar ile pasife alınmış ama bu başvuruda değeri girilmiş olanlar.</summary>
    public static IReadOnlyList<IzlemeGostergesiTanim> BasvuruIcin(Basvuru b)
    {
        HashSet<string> girilenler = b.IzlemeGostergeleri.Select(x => x.gostergeKodu).ToHashSet(StringComparer.Ordinal);
        return Tum.Where(x => x.aktif || girilenler.Contains(x.kod)).ToList();
    }
}
