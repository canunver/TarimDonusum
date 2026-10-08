using Microsoft.Data.SqlClient;
using TarimDonusum.Models;
using TarimDonusum.Tablolar;

namespace TarimDonusum.IsKurallari;

public static class ZorunluBelgeTanimYukleyici
{
    /// <summary>Uygulama açılışında çalışır; tablo boşsa varsayılan belgeleri ekler ve tanımları belleğe yükler.</summary>
    public static async Task IlkDegerleVeYukleAsync(IConfiguration configuration, ILogger logger)
    {
        string connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        await using SqlConnection connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        TABZorunluBelgeTanim tablo = new TABZorunluBelgeTanim(connection);
        await tablo.VarsayilanlariEkleAsync(ZorunluBelgeTanimSaglayici.Varsayilanlar);
        List<ZorunluBelgeTanim> tanimlar = await tablo.ListeleAsync();
        ZorunluBelgeTanimSaglayici.Guncelle(tanimlar);
        logger.LogInformation("Zorunlu belge tanımları yüklendi. Adet: {Adet}", tanimlar.Count);
    }
}
