namespace TarimDonusum.Models;

public sealed record IzlemeGostergesiTanimi(string Kod,string Ad,string Birim,string VeriKaynagi,string Siklik);

public static class IzlemeGostergesiTanimlari
{
    public static readonly IReadOnlyList<IzlemeGostergesiTanimi> Tum=
    [
        new("ciftci_ticari","Aktif ticari düzenlemeye katılan çiftçiler","Çiftçi sayısı","Sözleşme, alım anlaşması, fatura","Altı aylık"),
        new("ciftci_erisim","AE/PO üzerinden hizmet veya mal erişen çiftçiler","Çiftçi sayısı","Hizmet/girdi kayıtları","Altı aylık"),
        new("ciftci_ilave","İlave kayıtlı çiftçi / birincil üretici","Kişi","Tekil kayıt, ÇKS, ticari düzenleme","Altı aylık"),
        new("tzi_mevcut","Mevcut tam zamanlı eşdeğer istihdam","TZİ","SGK, bordro, zaman kaydı","Yıllık"),
        new("tzi_ilave","İlave tam zamanlı eşdeğer istihdam","TZİ","SGK, bordro, zaman kaydı","Yıllık"),
        new("mbj","MBJ iş eşdeğeri","MBJ iş eşdeğeri","İşgücü geliri ve referans FTE geliri","Yıllık"),
        new("isgucu_toplam","Toplam işgücü geliri","TL/yıl","Bordro, ücret ödeme kayıtları","Yıllık"),
        new("isgucu_ortalama","Ortalama TZİ işgücü geliri","TL/FTE-yıl","Bordro ve TZİ hesabı","Yıllık"),
        new("kapasite","Üretim / işleme kapasitesi","Ürün bazlı birim","Teknik proje, kapasite raporu","Yıllık"),
        new("satis","Satış geliri","TL/yıl","Mali tablolar, satış kayıtları","Yıllık"),
        new("ihracat","İhracat geliri","TL/yıl","Gümrük ve mali kayıtlar","Yıllık"),
        new("ihracat_oran","İhracat / satış oranı","%","Mali ve ihracat kayıtları","Yıllık"),
        new("sertifika","Sertifikasyon / kalite sistemi","Sayı / durum","Sertifika, denetim raporu","Yıllık"),
        new("izlenebilirlik","İzlenebilirlik kapsamı","% veya ürün sayısı","İzlenebilirlik sistemi","Yıllık"),
        new("enerji","Enerji kullanımı / verimlilik","kWh/birim ürün","Fatura, sayaç, izleme sistemi","Yıllık"),
        new("su","Su kullanımı / verimlilik","m3/birim ürün","Sayaç, fatura, izleme kaydı","Yıllık"),
        new("atik","Atık / yan ürün değerlendirme","ton/yıl veya %","Atık, satış/bertaraf kayıtları","Yıllık"),
        new("sera_gazi","Sera gazı emisyonu","tCO2e/yıl","Hesap/rapor ve faaliyet verisi","Yıllık"),
        new("operasyon","Ana yatırım unsurlarının operasyonel durumu","Sayı / durum","Fatura, kabul, fotoğraf, yerinde kontrol","Üç aylık"),
        new("ilerleme","Yatırım uygulama ilerlemesi","%","İlerleme raporu","Üç aylık")
    ];
    public static IzlemeGostergesiTanimi? Bul(string? kod)=>Tum.FirstOrDefault(x=>string.Equals(x.Kod,kod,StringComparison.Ordinal));
}
