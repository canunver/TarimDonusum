using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Localization;
using TarimDonusum.Models;

namespace TarimDonusum.Tablolar;

public class TABUygunHarcamaTanim : TABTablo
{
    public TABUygunHarcamaTanim(SqlConnection connection, IStringLocalizer<SharedResource>? localizer = null, SqlTransaction? transaction = null)
        : base(connection, localizer, transaction) { }

    public async Task<List<UygunHarcamaTanim>> ListeleAsync()
    {
        const string sql = @"SELECT Id,Kod,Ad,Aktif FROM dbo.UygunHarcamaTanim ORDER BY Kod,Id;";
        await using SqlCommand command = KomutOlustur(sql);
        await using SqlDataReader reader = await command.ExecuteReaderAsync();
        List<UygunHarcamaTanim> liste = [];
        while (await reader.ReadAsync())
        {
            liste.Add(new UygunHarcamaTanim
            {
                id = reader.GetInt32(0),
                kod = reader.GetString(1),
                ad = reader.GetString(2),
                aktif = reader.GetInt32(3) == 1
            });
        }
        return liste;
    }

    public async Task<bool> KodKullaniliyorMuAsync(string kod, int haricId)
    {
        await using SqlCommand command = KomutOlustur("SELECT COUNT(1) FROM dbo.UygunHarcamaTanim WHERE Kod=@Kod AND Id<>@Id;");
        command.Parameters.AddWithValue("@Kod", kod);
        command.Parameters.AddWithValue("@Id", haricId);
        return Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
    }

    public async Task<int> KaydetAsync(UygunHarcamaTanim harcama)
    {
        const string sql = @"IF @Id=0
            BEGIN
                INSERT dbo.UygunHarcamaTanim(Kod,Ad,Aktif) VALUES(@Kod,@Ad,@Aktif);
                SELECT CONVERT(INT,SCOPE_IDENTITY());
            END
            ELSE
            BEGIN
                UPDATE dbo.UygunHarcamaTanim SET Kod=@Kod,Ad=@Ad,Aktif=@Aktif,GuncellemeTarihi=SYSUTCDATETIME() WHERE Id=@Id;
                SELECT @Id;
            END";
        await using SqlCommand command = KomutOlustur(sql);
        command.Parameters.AddWithValue("@Id", harcama.id);
        command.Parameters.AddWithValue("@Kod", harcama.kod);
        command.Parameters.AddWithValue("@Ad", harcama.ad);
        command.Parameters.AddWithValue("@Aktif", harcama.aktif ? 1 : 0);
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }
}
