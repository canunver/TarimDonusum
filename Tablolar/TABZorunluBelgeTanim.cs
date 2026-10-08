using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Localization;
using TarimDonusum.Models;

namespace TarimDonusum.Tablolar;

public class TABZorunluBelgeTanim : TABTablo
{
    public TABZorunluBelgeTanim(SqlConnection connection, IStringLocalizer<SharedResource>? localizer = null, SqlTransaction? transaction = null)
        : base(connection, localizer, transaction) { }

    public async Task<List<ZorunluBelgeTanim>> ListeleAsync()
    {
        const string sql = @"SELECT Id,BelgeNo,SiraNo,Ad,SadeceKooperatif,Aktif FROM dbo.ZorunluBelgeTanim ORDER BY SiraNo,BelgeNo;";
        await using SqlCommand command = KomutOlustur(sql);
        await using SqlDataReader reader = await command.ExecuteReaderAsync();
        List<ZorunluBelgeTanim> liste = [];
        while (await reader.ReadAsync())
        {
            liste.Add(new ZorunluBelgeTanim
            {
                id = reader.GetInt32(0),
                belgeNo = reader.GetInt32(1),
                siraNo = reader.GetInt32(2),
                ad = reader.GetString(3),
                sadeceKooperatif = reader.GetInt32(4) == 1,
                aktif = reader.GetInt32(5) == 1
            });
        }
        return liste;
    }

    /// <summary>Yeni belgeye en büyük belge numarasının bir fazlası verilir; mevcut belgenin numarası korunur.</summary>
    public async Task<int> KaydetAsync(ZorunluBelgeTanim tanim)
    {
        const string sql = @"IF @Id=0
            BEGIN
                INSERT dbo.ZorunluBelgeTanim(BelgeNo,SiraNo,Ad,SadeceKooperatif,Aktif)
                SELECT ISNULL(MAX(BelgeNo),0)+1,@SiraNo,@Ad,@SadeceKooperatif,@Aktif FROM dbo.ZorunluBelgeTanim WITH (UPDLOCK, HOLDLOCK);
                SELECT CONVERT(INT,SCOPE_IDENTITY());
            END
            ELSE
            BEGIN
                UPDATE dbo.ZorunluBelgeTanim
                SET SiraNo=@SiraNo,Ad=@Ad,SadeceKooperatif=@SadeceKooperatif,Aktif=@Aktif,GuncellemeTarihi=SYSUTCDATETIME()
                WHERE Id=@Id;
                SELECT @Id;
            END";
        await using SqlCommand command = KomutOlustur(sql);
        command.Parameters.AddWithValue("@Id", tanim.id);
        command.Parameters.AddWithValue("@SiraNo", tanim.siraNo);
        command.Parameters.AddWithValue("@Ad", tanim.ad);
        command.Parameters.AddWithValue("@SadeceKooperatif", tanim.sadeceKooperatif ? 1 : 0);
        command.Parameters.AddWithValue("@Aktif", tanim.aktif ? 1 : 0);
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    /// <summary>Tablo boşsa, daha önce sayfada sabit duran belgeleri kendi numaralarıyla ekler.</summary>
    public async Task VarsayilanlariEkleAsync(IEnumerable<ZorunluBelgeTanim> varsayilanlar)
    {
        await using SqlCommand say = KomutOlustur("SELECT COUNT(1) FROM dbo.ZorunluBelgeTanim;");
        if (Convert.ToInt32(await say.ExecuteScalarAsync()) > 0) return;

        foreach (ZorunluBelgeTanim tanim in varsayilanlar)
        {
            await using SqlCommand ekle = KomutOlustur(@"INSERT dbo.ZorunluBelgeTanim(BelgeNo,SiraNo,Ad,SadeceKooperatif,Aktif)
                VALUES(@BelgeNo,@SiraNo,@Ad,@SadeceKooperatif,@Aktif);");
            ekle.Parameters.AddWithValue("@BelgeNo", tanim.belgeNo);
            ekle.Parameters.AddWithValue("@SiraNo", tanim.siraNo);
            ekle.Parameters.AddWithValue("@Ad", tanim.ad);
            ekle.Parameters.AddWithValue("@SadeceKooperatif", tanim.sadeceKooperatif ? 1 : 0);
            ekle.Parameters.AddWithValue("@Aktif", tanim.aktif ? 1 : 0);
            await ekle.ExecuteNonQueryAsync();
        }
    }
}
