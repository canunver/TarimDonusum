using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;

namespace TarimDonusum.Servisler;

public enum VKNDogrulamaDurumu { ServisYok = 1, Dogru = 2, Hatali = 3 }

public sealed record VKNDogrulamaSonucu(VKNDogrulamaDurumu Durum, string? Hata = null);

public sealed class VKNServisi(HttpClient httpClient, IConfiguration configuration, ILogger<VKNServisi> logger)
{
    public const string Uyusmazlik = "Girdiğiniz bilgiler ile VKN servisinden gelen bilgiler uyuşmuyor. Lütfen doğru bilgileri giriniz.";
    public const string ErisimHatasi = "Firma servisine erişilemedi veya geçerli bir yanıt alınamadı. Lütfen daha sonra tekrar deneyiniz.";

    public async Task<VKNDogrulamaSonucu> VKNDogrulaAsync(string? vkn, string? firmaAdi,
        int kullaniciId, int firmaId, string bolum, CancellationToken cancellationToken = default)
    {
        string? adres = configuration["VKNServis"];
        if (string.IsNullOrWhiteSpace(adres)) return new(VKNDogrulamaDurumu.ServisYok);

        Guid sorguId = Guid.NewGuid();
        logger.LogInformation("Firma sorgusu başladı. SorguId: {SorguId}, KullaniciId: {KullaniciId}, VKN: {VKN}, FirmaId: {FirmaId}, Bolum: {Bolum}, Zaman: {Zaman}",
            sorguId, kullaniciId, vkn, firmaId, bolum, DateTimeOffset.UtcNow);
        VKNDogrulamaSonucu sonuc;
        try
        {
            if (!Uri.TryCreate(adres, UriKind.Absolute, out var uri) || (uri.Scheme != "https" && uri.Scheme != "http"))
                throw new HttpRequestException("Geçersiz firma servisi adresi.");

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(15));
            using var response = await httpClient.PostAsJsonAsync(uri, new
            {
                vkn = vkn?.Trim(), firmaAdi = firmaAdi?.Trim()
            }, timeout.Token);
            response.EnsureSuccessStatusCode();
            var firma = await response.Content.ReadFromJsonAsync<FirmaServisYaniti>(cancellationToken: timeout.Token);
            if (firma == null || string.IsNullOrWhiteSpace(firma.Vkn) || string.IsNullOrWhiteSpace(firma.FirmaAdi))
                throw new JsonException("Eksik veya geçersiz firma servisi yanıtı.");

            bool dogru = !string.IsNullOrWhiteSpace(vkn) && vkn.Trim() == firma.Vkn.Trim()
                && !string.IsNullOrWhiteSpace(firmaAdi) && IsimNormalize(firmaAdi) == IsimNormalize(firma.FirmaAdi);
            sonuc = dogru ? new(VKNDogrulamaDurumu.Dogru)
                : new(VKNDogrulamaDurumu.Hatali, Hata: Uyusmazlik);
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or JsonException or NotSupportedException)
        {
            // Yanıt gövdesi ve exception mesajı kişisel veri / servis sırrı içerebilir.
            logger.LogWarning("Firma servisi çağrı hatası. SorguId: {SorguId}, HataTuru: {HataTuru}", sorguId, ex.GetType().Name);
            sonuc = new(VKNDogrulamaDurumu.Hatali, Hata: ErisimHatasi);
        }
        logger.LogInformation("Firma sorgusu tamamlandı. SorguId: {SorguId}, Durum: {Durum}, Hata: {Hata}, Zaman: {Zaman}",
            sorguId, sonuc.Durum, sonuc.Hata, DateTimeOffset.UtcNow);
        return sonuc;
    }

    private static string IsimNormalize(string? value) => string.Join(" ",
        (value ?? "").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)).ToUpper(CultureInfo.GetCultureInfo("tr-TR"));

    private sealed record FirmaServisYaniti(string? Vkn, string? FirmaAdi);
}
