namespace TarimDonusum.Models;

public sealed class BasvuruBilancoGelir
{
    public int basvuruId { get; set; }
    public List<BasvuruBilancoGelirSatiri> satirlar { get; set; } = [];
}

public sealed class BasvuruBilancoGelirSatiri
{
    public string kod { get; set; } = "";
    public decimal? yil_1 { get; set; }
    public decimal? yil_2 { get; set; }
    public decimal? yil_3 { get; set; }
}

public sealed record BilancoGelirSatirTanimi(string Kod, string Aciklama, int ExcelSatiri, bool Eksi = false);

public static class BilancoGelirTanimlari
{
    public static readonly IReadOnlyList<BilancoGelirSatirTanimi> GirisSatirlari =
    [
        new("DONEN_VARLIKLAR", "Dönen Varlıklar", 5), new("TICARI_ALACAKLAR", "Ticari Alacaklar", 6),
        new("STOKLAR", "Stoklar", 7), new("DURAN_VARLIKLAR", "Duran Varlıklar", 8),
        new("KISA_VADELI_YABANCI_KAYNAKLAR", "Kısa Vadeli Yabancı Kaynaklar", 11),
        new("UZUN_VADELI_YABANCI_KAYNAKLAR", "Uzun Vadeli Yabancı Kaynaklar", 12), new("OZ_KAYNAKLAR", "Öz Kaynaklar", 13),
        new("BRUT_SATISLAR", "Brüt Satışlar", 17), new("SATIS_INDIRIMLERI", "Satış İndirimleri (-)", 18, true),
        new("SATISLARIN_MALIYETI", "Satışların Maliyeti (-)", 20, true), new("FAALIYET_GIDERLERI", "Faaliyet Giderleri (-)", 22, true),
        new("DIGER_OLAGAN_GELIR_KARLAR", "Diğer Faaliyetlerden Olağan Gelir ve Kârlar", 24),
        new("DIGER_OLAGAN_GIDER_ZARARLAR", "Diğer Faaliyetlerden Olağan Gider ve Zararlar (-)", 25, true),
        new("FINANSMAN_GIDERLERI", "Finansman Giderleri (-)", 26, true), new("OLAGANDISI_GELIR_KARLAR", "Olağandışı Gelir ve Kârlar", 28),
        new("OLAGANDISI_GIDER_ZARARLAR", "Olağandışı Gider ve Zararlar (-)", 29, true),
        new("VERGI_YASAL_YUKUMLULUK", "Dönem Kârı Vergi ve Diğer Yasal Yükümlülük Karşılıkları (-)", 31, true),
        new("DONEM_AMORTISMAN", "Dönem Amortisman Tutarı", 33)
    ];
}

public sealed record BilancoGelirHesapSonucu(decimal AktifToplami, decimal PasifToplami, decimal NetSatislar,
    decimal BrutSatisKari, decimal FaaliyetKari, decimal OlaganKar, decimal DonemKari, decimal NetKar,
    decimal NetIsletmeSermayesi, decimal? CariOran, decimal? LikiditeOrani, decimal? FinansmanOrani,
    decimal? FaaliyetKarliligi, decimal? BilancoKarliligi, decimal? OzsermayeKarliligi, decimal? AktifKarliligi, decimal Favok);

public static class BilancoGelirHesaplayici
{
    public static BilancoGelirHesapSonucu Hesapla(IEnumerable<BasvuruBilancoGelirSatiri> satirlar, int yilSirasi)
    {
        Dictionary<string, BasvuruBilancoGelirSatiri> d = satirlar.ToDictionary(x => x.kod, StringComparer.OrdinalIgnoreCase);
        decimal V(string kod) => !d.TryGetValue(kod, out var s) ? 0 : yilSirasi switch { 1 => s.yil_1 ?? 0, 2 => s.yil_2 ?? 0, _ => s.yil_3 ?? 0 };
        static decimal? Bol(decimal pay, decimal payda, decimal carpan = 1) => payda == 0 ? null : pay / payda * carpan;
        decimal donen=V("DONEN_VARLIKLAR"), aktif=donen+V("DURAN_VARLIKLAR"), kvyk=V("KISA_VADELI_YABANCI_KAYNAKLAR"), uvyk=V("UZUN_VADELI_YABANCI_KAYNAKLAR"), oz=V("OZ_KAYNAKLAR");
        decimal pasif=kvyk+uvyk+oz, netSatis=V("BRUT_SATISLAR")-V("SATIS_INDIRIMLERI"), brutKar=netSatis-V("SATISLARIN_MALIYETI"), faaliyet=brutKar-V("FAALIYET_GIDERLERI");
        decimal olagan=faaliyet+V("DIGER_OLAGAN_GELIR_KARLAR")-V("DIGER_OLAGAN_GIDER_ZARARLAR")-V("FINANSMAN_GIDERLERI"), donem=olagan+V("OLAGANDISI_GELIR_KARLAR")-V("OLAGANDISI_GIDER_ZARARLAR"), net=donem-V("VERGI_YASAL_YUKUMLULUK");
        return new(aktif,pasif,netSatis,brutKar,faaliyet,olagan,donem,net,donen-kvyk,Bol(donen,kvyk),Bol(donen-V("STOKLAR"),kvyk),Bol(kvyk+uvyk,oz,100),Bol(faaliyet,netSatis,100),Bol(donem,kvyk,100),Bol(net,oz,100),Bol(net,aktif,100),faaliyet+V("DONEM_AMORTISMAN"));
    }
}
