using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Localization;
using TarimDonusum.Models;

namespace TarimDonusum.Tablolar;

public class TABTamBasvuruUygunlukSorusuTanim : TABTablo
{
    public TABTamBasvuruUygunlukSorusuTanim(SqlConnection connection, IStringLocalizer<SharedResource>? localizer = null, SqlTransaction? transaction = null)
        : base(connection, localizer, transaction) { }

    public async Task<List<TamBasvuruUygunlukSorusuTanim>> ListeleAsync()
    {
        const string sql = @"SELECT Id,SiraNo,Soru,KontrolAciklamasi,KaynakBelge,Aktif FROM dbo.TamBasvuruUygunlukSorusuTanim ORDER BY SiraNo,Id;";
        await using SqlCommand command = KomutOlustur(sql);
        await using SqlDataReader reader = await command.ExecuteReaderAsync();
        List<TamBasvuruUygunlukSorusuTanim> liste = [];
        while (await reader.ReadAsync())
        {
            liste.Add(new TamBasvuruUygunlukSorusuTanim
            {
                id = reader.GetInt32(0),
                siraNo = reader.GetInt32(1),
                soru = reader.GetString(2),
                kontrolAciklamasi = reader.GetString(3),
                kaynakBelge = reader.GetString(4),
                aktif = reader.GetInt32(5) == 1
            });
        }
        return liste;
    }

    public async Task<int> KaydetAsync(TamBasvuruUygunlukSorusuTanim tanim)
    {
        const string sql = @"IF @Id=0
            BEGIN
                INSERT dbo.TamBasvuruUygunlukSorusuTanim(SiraNo,Soru,KontrolAciklamasi,KaynakBelge,Aktif)
                VALUES(@SiraNo,@Soru,@KontrolAciklamasi,@KaynakBelge,@Aktif);
                SELECT CONVERT(INT,SCOPE_IDENTITY());
            END
            ELSE
            BEGIN
                UPDATE dbo.TamBasvuruUygunlukSorusuTanim
                SET SiraNo=@SiraNo,Soru=@Soru,KontrolAciklamasi=@KontrolAciklamasi,KaynakBelge=@KaynakBelge,Aktif=@Aktif,GuncellemeTarihi=SYSUTCDATETIME()
                WHERE Id=@Id;
                SELECT @Id;
            END";
        await using SqlCommand command = KomutOlustur(sql);
        command.Parameters.AddWithValue("@Id", tanim.id);
        ParametreleriEkle(command, tanim);
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    /// <summary>Tablo boşsa Excel formundaki başlangıç sorularını ekler.</summary>
    public async Task VarsayilanlariEkleAsync(IEnumerable<TamBasvuruUygunlukSorusuTanim> varsayilanlar)
    {
        await using SqlCommand say = KomutOlustur("SELECT COUNT(1) FROM dbo.TamBasvuruUygunlukSorusuTanim;");
        if (Convert.ToInt32(await say.ExecuteScalarAsync()) > 0) return;

        foreach (TamBasvuruUygunlukSorusuTanim tanim in varsayilanlar)
        {
            await using SqlCommand ekle = KomutOlustur(@"INSERT dbo.TamBasvuruUygunlukSorusuTanim(SiraNo,Soru,KontrolAciklamasi,KaynakBelge,Aktif)
                VALUES(@SiraNo,@Soru,@KontrolAciklamasi,@KaynakBelge,@Aktif);");
            ParametreleriEkle(ekle, tanim);
            await ekle.ExecuteNonQueryAsync();
        }
    }

    private static void ParametreleriEkle(SqlCommand command, TamBasvuruUygunlukSorusuTanim tanim)
    {
        command.Parameters.AddWithValue("@SiraNo", tanim.siraNo);
        command.Parameters.AddWithValue("@Soru", tanim.soru);
        command.Parameters.AddWithValue("@KontrolAciklamasi", tanim.kontrolAciklamasi);
        command.Parameters.AddWithValue("@KaynakBelge", tanim.kaynakBelge);
        command.Parameters.AddWithValue("@Aktif", tanim.aktif ? 1 : 0);
    }
}
