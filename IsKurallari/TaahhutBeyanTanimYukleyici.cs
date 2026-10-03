using Microsoft.Data.SqlClient;
using TarimDonusum.Models;
using TarimDonusum.Tablolar;

namespace TarimDonusum.IsKurallari;

public static class TaahhutBeyanTanimYukleyici
{
    /// <summary>
    /// Uygulama açılışında çalışır. CevreselSosyalAnketAktarici'den sonra çağrılmalıdır:
    /// ilk kurulumda çevresel-sosyal taahhüt metni oradan yüklenen değerle eklenir.
    /// </summary>
    public static async Task IlkDegerleVeYukleAsync(IConfiguration configuration, ILogger logger)
    {
        string connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        await using SqlConnection connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        TABTaahhutBeyanTanim tablo = new TABTaahhutBeyanTanim(connection);
        await tablo.VarsayilanlariEkleAsync(TaahhutBeyanTanimSaglayici.Varsayilanlar(CevreselSosyalTaahhutTanimSaglayici.Metin));
        List<TaahhutBeyanTanim> tanimlar = await tablo.ListeleAsync();
        TaahhutBeyanTanimSaglayici.Guncelle(tanimlar);
        logger.LogInformation("Taahhüt/beyan tanımları yüklendi. Adet: {Adet}", tanimlar.Count);
    }
}
