using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Localization;
using TarimDonusum.Models;

namespace TarimDonusum.Tablolar;

public class TABTamBasvuruBelgeKontrolTanim : TABTablo
{
    public TABTamBasvuruBelgeKontrolTanim(SqlConnection connection, IStringLocalizer<SharedResource>? localizer = null, SqlTransaction? transaction = null)
        : base(connection, localizer, transaction) { }

    public async Task<List<TamBasvuruBelgeKontrolTanim>> ListeleAsync()
    {
        const string sql = @"SELECT Id,SiraNo,Soru,Kosullu,Aktif FROM dbo.TamBasvuruBelgeKontrolTanim ORDER BY SiraNo,Id;";
        await using SqlCommand command = KomutOlustur(sql);
        await using SqlDataReader reader = await command.ExecuteReaderAsync();
        List<TamBasvuruBelgeKontrolTanim> liste = [];
        while (await reader.ReadAsync())
        {
            liste.Add(new TamBasvuruBelgeKontrolTanim
            {
                id = reader.GetInt32(0),
                siraNo = reader.GetInt32(1),
                soru = reader.GetString(2),
                kosullu = reader.GetInt32(3) == 1,
                aktif = reader.GetInt32(4) == 1
            });
        }
        return liste;
    }

    public async Task<int> KaydetAsync(TamBasvuruBelgeKontrolTanim tanim)
    {
        const string sql = @"IF @Id=0
            BEGIN
                INSERT dbo.TamBasvuruBelgeKontrolTanim(SiraNo,Soru,Kosullu,Aktif) VALUES(@SiraNo,@Soru,@Kosullu,@Aktif);
                SELECT CONVERT(INT,SCOPE_IDENTITY());
            END
            ELSE
            BEGIN
                UPDATE dbo.TamBasvuruBelgeKontrolTanim
                SET SiraNo=@SiraNo,Soru=@Soru,Kosullu=@Kosullu,Aktif=@Aktif,GuncellemeTarihi=SYSUTCDATETIME()
                WHERE Id=@Id;
                SELECT @Id;
            END";
        await using SqlCommand command = KomutOlustur(sql);
        command.Parameters.AddWithValue("@Id", tanim.id);
        ParametreleriEkle(command, tanim);
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    /// <summary>Tablo boşsa Excel formundaki başlangıç kontrollerini ekler.</summary>
    public async Task VarsayilanlariEkleAsync(IEnumerable<TamBasvuruBelgeKontrolTanim> varsayilanlar)
    {
        await using SqlCommand say = KomutOlustur("SELECT COUNT(1) FROM dbo.TamBasvuruBelgeKontrolTanim;");
        if (Convert.ToInt32(await say.ExecuteScalarAsync()) > 0) return;

        foreach (TamBasvuruBelgeKontrolTanim tanim in varsayilanlar)
        {
            await using SqlCommand ekle = KomutOlustur(@"INSERT dbo.TamBasvuruBelgeKontrolTanim(SiraNo,Soru,Kosullu,Aktif)
                VALUES(@SiraNo,@Soru,@Kosullu,@Aktif);");
            ParametreleriEkle(ekle, tanim);
            await ekle.ExecuteNonQueryAsync();
        }
    }

    private static void ParametreleriEkle(SqlCommand command, TamBasvuruBelgeKontrolTanim tanim)
    {
        command.Parameters.AddWithValue("@SiraNo", tanim.siraNo);
        command.Parameters.AddWithValue("@Soru", tanim.soru);
        command.Parameters.AddWithValue("@Kosullu", tanim.kosullu ? 1 : 0);
        command.Parameters.AddWithValue("@Aktif", tanim.aktif ? 1 : 0);
    }
}
