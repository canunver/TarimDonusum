using System.Net.Http.Json;
using System.Text.Json;

namespace TarimDonusum.Servisler;

public sealed class SMSServisi(HttpClient client, IConfiguration configuration, ILogger<SMSServisi> logger)
{
    public bool Etkin => !string.IsNullOrWhiteSpace(configuration["SMSServis"]);
    public const string GonderimHatasi = "SMS gönderilemedi veya SMS servisine erişilemedi. Lütfen daha sonra tekrar deneyiniz.";

    // Başarıda null, gönderim hatasında kullanıcıya gösterilecek mesaj döner.
    public async Task<string?> KodGonderAsync(string telefon, string kod, CancellationToken cancellationToken = default)
    {
        if (!Etkin) return "SMS doğrulaması etkin değil.";
        var sorguId = Guid.NewGuid();
        try
        {
            if (!Uri.TryCreate(configuration["SMSServis"], UriKind.Absolute, out var uri)
                || (uri.Scheme != "https" && uri.Scheme != "http"))
                throw new HttpRequestException("Geçersiz SMS servisi adresi.");
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(15));
            using var response = await client.PostAsJsonAsync(uri, new
            {
                telefon, mesaj = $"TKDK Başvuru Portalı doğrulama kodunuz: {kod}. Kod 3 dakika geçerlidir."
            }, timeout.Token);
            response.EnsureSuccessStatusCode();
            var sonuc = await response.Content.ReadFromJsonAsync<SMSYaniti>(cancellationToken: timeout.Token);
            if (sonuc?.Basarili != true) throw new JsonException("SMS servisi gönderimi onaylamadı.");
            logger.LogInformation("SMS gönderildi. SorguId: {SorguId}", sorguId);
            return null;
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or JsonException or NotSupportedException)
        {
            logger.LogWarning("SMS gönderilemedi. SorguId: {SorguId}, HataTuru: {HataTuru}", sorguId, ex.GetType().Name);
            return GonderimHatasi;
        }
    }

    private sealed record SMSYaniti(bool Basarili);
}
