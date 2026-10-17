using Microsoft.Data.SqlClient;
using TarimDonusum.Models;
using TarimDonusum.Tablolar;

namespace TarimDonusum.IsKurallari;

public static class SiralamaPuaniTanimYukleyici
{
    /// <summary>Uygulama açılışında çalışır; tablolar boşsa Excel'deki kriterleri ekler ve tanımları belleğe yükler.</summary>
    public static async Task IlkDegerleVeYukleAsync(IConfiguration configuration, ILogger logger)
    {
        string connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        await using SqlConnection connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        TABSiralamaPuaniTanim tablo = new TABSiralamaPuaniTanim(connection);
        await tablo.VarsayilanlariEkleAsync(SiralamaPuaniTanimSaglayici.Varsayilanlar);
        List<SiralamaPuaniKonu> konular = await tablo.ListeleAsync();
        SiralamaPuaniTanimSaglayici.Guncelle(konular);
        logger.LogInformation("Sıralama puanı kriterleri yüklendi. Konu: {Adet}", konular.Count);
    }
}
