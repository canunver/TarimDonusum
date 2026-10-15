using Microsoft.Data.SqlClient;
using TarimDonusum.Models;
using TarimDonusum.Tablolar;

namespace TarimDonusum.IsKurallari;

public static class DonemTahminYukleyici
{
    /// <summary>Uygulama açılışında tüm dönemlerin kur ve enflasyon tahminlerini belleğe yükler.</summary>
    public static async Task YukleAsync(IConfiguration configuration, ILogger logger)
    {
        string connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        await using SqlConnection connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        List<(int donemId, DonemTahminSatiri satir)> kayitlar = await new TABDonemTahmini(connection).TumunuListeleAsync();
        DonemTahminSaglayici.Yukle(kayitlar);
        logger.LogInformation("Dönem kur ve enflasyon tahminleri yüklendi. Kayıt: {Adet}", kayitlar.Count);
    }
}
