using System.Text.Json;

namespace TarimDonusum.Models;

public static class CevreselSosyalTaahhut
{
    public const string Alan = "cevreselSosyalTaahhutOnayi";

    public static bool? Oku(string? json, string alan)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind == JsonValueKind.Object && doc.RootElement.TryGetProperty(alan, out var value))
                return value.ValueKind == JsonValueKind.True ? true : value.ValueKind == JsonValueKind.False ? false : null;
        }
        catch (JsonException) { }
        return null;
    }

    public static bool OnayliMi(Basvuru basvuru) => Oku(basvuru.TaahhutBeyanlarJson, Alan)
        ?? basvuru.YatirimAdresleri.Any(adres => Oku(adres.cevreselSosyalJson, "declaration") == true);
}

public static class CevreselSosyalTaahhutTanimSaglayici
{
    public const string VarsayilanMetin = "İşbu formda tarafımdan sunulan tüm bilgilerin tamamen doğru, eksiksiz ve mevcut saha koşullarıyla (ve/veya yeni yatırımlar için onaylı proje tasarım parametreleriyle) tam uyumlu olduğunu; yatırımın uygulama ve işletme dönemlerinde Türkiye Cumhuriyeti mevzuatına ve projeye uygulanabilir Uluslararası Finans Kuruluşu (Dünya Bankası) Çevresel ve Sosyal Çerçeve (ESF) standartlarına tam olarak uyacağımı kabul, beyan ve taahhüt ederim.\n\nHerhangi bir uyumsuzluk veya gerçeğe aykırı beyanın tespit edilmesi halinde (çocuk işçiliği, zorla tahliye, yasaklı kimyasalların kullanımı, resmi kurumlara yanlış beyanda bulunulması vb. dahil) düzeltici faaliyet uygulanabileceğini, kredi desteğinin askıya alınabileceğini veya alt projemin sonlandırılabileceğini kabul ederim.\n\nAlt projem için belirlenecek risk kategorisine bağlı olarak hazırlanması gerekebilecek ilave çevresel ve sosyal raporların hazırlanması ve sahada uygulanmasına ilişkin tüm maliyetlerin tarafıma ait olduğunu peşinen kabul ve taahhüt ederim.";

    private static string _metin = VarsayilanMetin;
    public static string Metin => Volatile.Read(ref _metin);
    public static void Guncelle(string? metin)
    {
        if (!string.IsNullOrWhiteSpace(metin)) Volatile.Write(ref _metin, metin.Trim());
    }
}
