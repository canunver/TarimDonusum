using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;

namespace TarimDonusum.Servisler;

public enum TCKNDogrulamaDurumu { ServisYok = 1, Dogru = 2, Hatali = 3 }

// Asenkron metotlarda out kullanılamadığı için cinsiyet sonuç nesnesiyle döner.
public sealed record TCKNDogrulamaSonucu(TCKNDogrulamaDurumu Durum, string? Cinsiyet = null, string? Hata = null);

public sealed class TCKNServisi(HttpClient httpClient, IConfiguration configuration, ILogger<TCKNServisi> logger)
{
    public const string Uyusmazlik = "Girdiğiniz bilgiler ile MERNİS servisinden gelen bilgiler uyuşmuyor. Lütfen doğru bilgileri giriniz.";
    public const string ErisimHatasi = "Kimlik servisine erişilemedi veya geçerli bir yanıt alınamadı. Lütfen daha sonra tekrar deneyiniz.";

    public async Task<TCKNDogrulamaSonucu> TCKNDogrulaAsync(string? tckn, string? adSoyad,
        DateTime? dogumTarihi, int kullaniciId, int basvuruId, string bolum, CancellationToken cancellationToken = default)
    {
        string? adres = configuration["TCKNServis"];
        if (string.IsNullOrWhiteSpace(adres)) return new(TCKNDogrulamaDurumu.ServisYok);

        Guid sorguId = Guid.NewGuid();
        logger.LogInformation("Kimlik sorgusu başladı. SorguId: {SorguId}, KullaniciId: {KullaniciId}, TCKN: {TCKN}, BasvuruId: {BasvuruId}, Bolum: {Bolum}, Zaman: {Zaman}",
            sorguId, kullaniciId, tckn, basvuruId, bolum, DateTimeOffset.UtcNow);
        TCKNDogrulamaSonucu sonuc;
        try
        {
            if (!Uri.TryCreate(adres, UriKind.Absolute, out var uri) || (uri.Scheme != "https" && uri.Scheme != "http"))
                throw new HttpRequestException("Geçersiz kimlik servisi adresi.");

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(15));
            using var response = await httpClient.PostAsJsonAsync(uri, new
            {
                tckn = tckn?.Trim(), adSoyad = adSoyad?.Trim(),
                dogumTarihi = dogumTarihi?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            }, timeout.Token);
            response.EnsureSuccessStatusCode();
            var kisi = await response.Content.ReadFromJsonAsync<KimlikServisYaniti>(cancellationToken: timeout.Token);
            if (kisi == null || string.IsNullOrWhiteSpace(kisi.Tckn) || string.IsNullOrWhiteSpace(kisi.Ad)
                || string.IsNullOrWhiteSpace(kisi.Soyad) || kisi.DogumTarihi == null
                || !(kisi.Cinsiyet is "Kadın" or "Erkek"))
                throw new JsonException("Eksik veya geçersiz kimlik servisi yanıtı.");

            bool dogru = !string.IsNullOrWhiteSpace(tckn) && tckn.Trim() == kisi.Tckn.Trim()
                && IsimNormalize(adSoyad) == IsimNormalize($"{kisi.Ad} {kisi.Soyad}")
                && dogumTarihi.HasValue && dogumTarihi.Value.Date == kisi.DogumTarihi.Value.Date;
            sonuc = dogru ? new(TCKNDogrulamaDurumu.Dogru, kisi.Cinsiyet)
                : new(TCKNDogrulamaDurumu.Hatali, Hata: Uyusmazlik);
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or JsonException or NotSupportedException)
        {
            // Yanıt gövdesi ve exception mesajı kişisel veri / servis sırrı içerebilir.
            logger.LogWarning("Kimlik servisi çağrı hatası. SorguId: {SorguId}, HataTuru: {HataTuru}", sorguId, ex.GetType().Name);
            sonuc = new(TCKNDogrulamaDurumu.Hatali, Hata: ErisimHatasi);
        }
        logger.LogInformation("Kimlik sorgusu tamamlandı. SorguId: {SorguId}, Durum: {Durum}, Hata: {Hata}, Zaman: {Zaman}",
            sorguId, sonuc.Durum, sonuc.Hata, DateTimeOffset.UtcNow);
        return sonuc;
    }

    private static string IsimNormalize(string? value) => string.Join(" ",
        (value ?? "").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)).ToUpper(CultureInfo.GetCultureInfo("tr-TR"));

    private sealed record KimlikServisYaniti(string? Tckn, string? Ad, string? Soyad, DateTime? DogumTarihi, string? Cinsiyet);
}
