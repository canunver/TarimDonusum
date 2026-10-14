using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Localization;
using TarimDonusum.Models;

namespace TarimDonusum.Tablolar;

public class TABIzlemeGostergesiTanim : TABTablo
{
    public TABIzlemeGostergesiTanim(SqlConnection connection, IStringLocalizer<SharedResource>? localizer = null, SqlTransaction? transaction = null)
        : base(connection, localizer, transaction) { }

    public async Task<List<IzlemeGostergesiTanim>> ListeleAsync()
    {
        const string sql = @"SELECT Id,Kod,SiraNo,Ad,Birim,VeriKaynagi,Siklik,Aktif FROM dbo.IzlemeGostergesiTanim ORDER BY SiraNo,Id;";
        await using SqlCommand command = KomutOlustur(sql);
        await using SqlDataReader reader = await command.ExecuteReaderAsync();
        List<IzlemeGostergesiTanim> liste = [];
        while (await reader.ReadAsync())
        {
            liste.Add(new IzlemeGostergesiTanim
            {
                id = reader.GetInt32(0),
                kod = reader.GetString(1),
                siraNo = reader.GetInt32(2),
                ad = reader.GetString(3),
                birim = reader.GetString(4),
                veriKaynagi = reader.GetString(5),
                siklik = reader.GetString(6),
                aktif = reader.GetInt32(7) == 1
            });
        }
        return liste;
    }

    /// <summary>Yeni göstergeye "gosterge{Id}" kodu verilir; mevcut göstergenin kodu korunur.</summary>
    public async Task<int> KaydetAsync(IzlemeGostergesiTanim tanim)
    {
        const string sql = @"IF @Id=0
            BEGIN
                DECLARE @YeniId INT;
                INSERT dbo.IzlemeGostergesiTanim(Kod,SiraNo,Ad,Birim,VeriKaynagi,Siklik,Aktif)
                VALUES(CONVERT(NVARCHAR(50),NEWID()),@SiraNo,@Ad,@Birim,@VeriKaynagi,@Siklik,@Aktif);
                SET @YeniId=CONVERT(INT,SCOPE_IDENTITY());
                UPDATE dbo.IzlemeGostergesiTanim SET Kod=CONCAT(N'gosterge',@YeniId) WHERE Id=@YeniId;
                SELECT @YeniId;
            END
            ELSE
            BEGIN
                UPDATE dbo.IzlemeGostergesiTanim
                SET SiraNo=@SiraNo,Ad=@Ad,Birim=@Birim,VeriKaynagi=@VeriKaynagi,Siklik=@Siklik,Aktif=@Aktif,GuncellemeTarihi=SYSUTCDATETIME()
                WHERE Id=@Id;
                SELECT @Id;
            END";
        await using SqlCommand command = KomutOlustur(sql);
        command.Parameters.AddWithValue("@Id", tanim.id);
        command.Parameters.AddWithValue("@SiraNo", tanim.siraNo);
        command.Parameters.AddWithValue("@Ad", tanim.ad);
        command.Parameters.AddWithValue("@Birim", tanim.birim);
        command.Parameters.AddWithValue("@VeriKaynagi", tanim.veriKaynagi);
        command.Parameters.AddWithValue("@Siklik", tanim.siklik);
        command.Parameters.AddWithValue("@Aktif", tanim.aktif ? 1 : 0);
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    /// <summary>Tablo boşsa, daha önce sayfada sabit duran göstergeleri kendi kodlarıyla ekler.</summary>
    public async Task VarsayilanlariEkleAsync(IEnumerable<IzlemeGostergesiTanim> varsayilanlar)
    {
        await using SqlCommand say = KomutOlustur("SELECT COUNT(1) FROM dbo.IzlemeGostergesiTanim;");
        if (Convert.ToInt32(await say.ExecuteScalarAsync()) > 0) return;

        foreach (IzlemeGostergesiTanim tanim in varsayilanlar)
        {
            await using SqlCommand ekle = KomutOlustur(@"INSERT dbo.IzlemeGostergesiTanim(Kod,SiraNo,Ad,Birim,VeriKaynagi,Siklik,Aktif)
                VALUES(@Kod,@SiraNo,@Ad,@Birim,@VeriKaynagi,@Siklik,@Aktif);");
            ekle.Parameters.AddWithValue("@Kod", tanim.kod);
            ekle.Parameters.AddWithValue("@SiraNo", tanim.siraNo);
            ekle.Parameters.AddWithValue("@Ad", tanim.ad);
            ekle.Parameters.AddWithValue("@Birim", tanim.birim);
            ekle.Parameters.AddWithValue("@VeriKaynagi", tanim.veriKaynagi);
            ekle.Parameters.AddWithValue("@Siklik", tanim.siklik);
            ekle.Parameters.AddWithValue("@Aktif", tanim.aktif ? 1 : 0);
            await ekle.ExecuteNonQueryAsync();
        }
    }
}
