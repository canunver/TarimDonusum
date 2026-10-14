using Microsoft.Data.SqlClient;
using TarimDonusum.Models;
using TarimDonusum.Tablolar;

namespace TarimDonusum.IsKurallari;

public static class IzlemeGostergesiTanimYukleyici
{
    /// <summary>Uygulama açılışında çalışır; tablo boşsa varsayılan göstergeleri ekler ve tanımları belleğe yükler.</summary>
    public static async Task IlkDegerleVeYukleAsync(IConfiguration configuration, ILogger logger)
    {
        string connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        await using SqlConnection connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        TABIzlemeGostergesiTanim tablo = new TABIzlemeGostergesiTanim(connection);
        await tablo.VarsayilanlariEkleAsync(IzlemeGostergesiTanimSaglayici.Varsayilanlar);
        List<IzlemeGostergesiTanim> tanimlar = await tablo.ListeleAsync();
        IzlemeGostergesiTanimSaglayici.Guncelle(tanimlar);
        logger.LogInformation("İzleme göstergesi tanımları yüklendi. Adet: {Adet}", tanimlar.Count);
    }
}
