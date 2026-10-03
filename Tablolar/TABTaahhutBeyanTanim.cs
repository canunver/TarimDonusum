using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Localization;
using TarimDonusum.Models;

namespace TarimDonusum.Tablolar;

public class TABTaahhutBeyanTanim : TABTablo
{
    public TABTaahhutBeyanTanim(SqlConnection connection, IStringLocalizer<SharedResource>? localizer = null, SqlTransaction? transaction = null)
        : base(connection, localizer, transaction) { }

    public async Task<List<TaahhutBeyanTanim>> ListeleAsync()
    {
        const string sql = @"SELECT Id,Anahtar,SiraNo,Metin,Zorunlu,Aktif
            FROM dbo.TaahhutBeyanTanim ORDER BY SiraNo,Id;";
        await using SqlCommand command = KomutOlustur(sql);
        await using SqlDataReader reader = await command.ExecuteReaderAsync();
        List<TaahhutBeyanTanim> liste = [];
        while (await reader.ReadAsync())
        {
            liste.Add(new TaahhutBeyanTanim
            {
                id = reader.GetInt32(0),
                anahtar = reader.GetString(1),
                siraNo = reader.GetInt32(2),
                metin = reader.GetString(3),
                zorunlu = reader.GetInt32(4) == 1,
                aktif = reader.GetInt32(5) == 1
            });
        }
        return liste;
    }

    /// <summary>Yeni maddede anahtar "taahhut_{Id}" olarak üretilir; mevcut maddenin anahtarı korunur.</summary>
    public async Task<int> KaydetAsync(TaahhutBeyanTanim tanim)
    {
        const string sql = @"IF @Id=0
            BEGIN
                INSERT dbo.TaahhutBeyanTanim(Anahtar,SiraNo,Metin,Zorunlu,Aktif)
                VALUES(CONVERT(NVARCHAR(100),NEWID()),@SiraNo,@Metin,@Zorunlu,@Aktif);
                DECLARE @YeniId INT = CONVERT(INT,SCOPE_IDENTITY());
                UPDATE dbo.TaahhutBeyanTanim SET Anahtar=CONCAT(N'taahhut_',@YeniId) WHERE Id=@YeniId;
                SELECT @YeniId;
            END
            ELSE
            BEGIN
                UPDATE dbo.TaahhutBeyanTanim
                SET SiraNo=@SiraNo,Metin=@Metin,Zorunlu=@Zorunlu,Aktif=@Aktif,GuncellemeTarihi=SYSUTCDATETIME()
                WHERE Id=@Id;
                SELECT @Id;
            END";
        await using SqlCommand command = KomutOlustur(sql);
        command.Parameters.AddWithValue("@Id", tanim.id);
        command.Parameters.AddWithValue("@SiraNo", tanim.siraNo);
        command.Parameters.AddWithValue("@Metin", tanim.metin);
        command.Parameters.AddWithValue("@Zorunlu", tanim.zorunlu ? 1 : 0);
        command.Parameters.AddWithValue("@Aktif", tanim.aktif ? 1 : 0);
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    /// <summary>Tablo boşsa, daha önce sayfada sabit duran maddeleri kendi anahtarlarıyla ekler.</summary>
    public async Task VarsayilanlariEkleAsync(IEnumerable<TaahhutBeyanTanim> varsayilanlar)
    {
        await using SqlCommand say = KomutOlustur("SELECT COUNT(1) FROM dbo.TaahhutBeyanTanim;");
        if (Convert.ToInt32(await say.ExecuteScalarAsync()) > 0) return;

        foreach (TaahhutBeyanTanim tanim in varsayilanlar)
        {
            await using SqlCommand ekle = KomutOlustur(@"INSERT dbo.TaahhutBeyanTanim(Anahtar,SiraNo,Metin,Zorunlu,Aktif)
                VALUES(@Anahtar,@SiraNo,@Metin,@Zorunlu,1);");
            ekle.Parameters.AddWithValue("@Anahtar", tanim.anahtar);
            ekle.Parameters.AddWithValue("@SiraNo", tanim.siraNo);
            ekle.Parameters.AddWithValue("@Metin", tanim.metin);
            ekle.Parameters.AddWithValue("@Zorunlu", tanim.zorunlu ? 1 : 0);
            await ekle.ExecuteNonQueryAsync();
        }
    }
}
