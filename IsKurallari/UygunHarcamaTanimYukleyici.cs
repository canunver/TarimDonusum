using Microsoft.Data.SqlClient;
using TarimDonusum.Models;
using TarimDonusum.Tablolar;

namespace TarimDonusum.IsKurallari;

public static class UygunHarcamaTanimYukleyici
{
    /// <summary>Uygulama açılışında çalışır; uygun harcama listesini belleğe yükler.</summary>
    public static async Task YukleAsync(IConfiguration configuration, ILogger logger)
    {
        string connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        await using SqlConnection connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        List<UygunHarcamaTanim> liste = await new TABUygunHarcamaTanim(connection).ListeleAsync();
        UygunHarcamaTanimSaglayici.Guncelle(liste);
        logger.LogInformation("Uygun harcama listesi yüklendi. Adet: {Adet}", liste.Count);
    }
}
