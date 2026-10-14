using Microsoft.Data.SqlClient;
using TarimDonusum.Models;
using TarimDonusum.Tablolar;

namespace TarimDonusum.IsKurallari;

public static class TamBasvuruKontrolTanimYukleyici
{
    /// <summary>
    /// Uygulama açılışında çalışır; tablolar boşsa tamlık/uygunluk formundaki başlangıç tanımlarını ekler
    /// ve tanımları belleğe yükler.
    /// </summary>
    public static async Task IlkDegerleVeYukleAsync(IConfiguration configuration, ILogger logger)
    {
        string connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        await using SqlConnection connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        TABTamBasvuruBelgeKontrolTanim belgeTablosu = new TABTamBasvuruBelgeKontrolTanim(connection);
        await belgeTablosu.VarsayilanlariEkleAsync(TamBasvuruKontrolVarsayilanlari.BelgeKontrolleri);
        List<TamBasvuruBelgeKontrolTanim> belgeler = await belgeTablosu.ListeleAsync();
        TamBasvuruBelgeKontrolTanimSaglayici.Guncelle(belgeler);

        TABTamBasvuruUygunlukSorusuTanim soruTablosu = new TABTamBasvuruUygunlukSorusuTanim(connection);
        await soruTablosu.VarsayilanlariEkleAsync(TamBasvuruKontrolVarsayilanlari.UygunlukSorulari);
        List<TamBasvuruUygunlukSorusuTanim> sorular = await soruTablosu.ListeleAsync();
        TamBasvuruUygunlukSorusuTanimSaglayici.Guncelle(sorular);

        logger.LogInformation("Tam başvuru kontrol tanımları yüklendi. Belge kontrolü: {Belge}, Uygunluk sorusu: {Soru}", belgeler.Count, sorular.Count);
    }
}
