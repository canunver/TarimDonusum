using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Localization;
using TarimDonusum.Araclar;
using TarimDonusum.Models;

namespace TarimDonusum.Tablolar;

public class TABNace : TABTablo
{
    public TABNace(SqlConnection connection, IStringLocalizer<SharedResource>? localizer = null, SqlTransaction? transaction = null)
        : base(connection, localizer, transaction) { }

    public async Task<List<Nace>> AraAsync(string? metin, bool sadeceAktif = true)
    {
        const string sql = @"SELECT TOP (100) Kod,Ad,Aktif FROM dbo.Nace
            WHERE (@SadeceAktif=0 OR Aktif=1) AND (@Metin=N'' OR Kod LIKE N'%' + @Metin + N'%' OR Ad LIKE N'%' + @Metin + N'%')
            ORDER BY Kod;";
        await using SqlCommand c=KomutOlustur(sql);
        c.Parameters.AddWithValue("@Metin", (metin??"").Trim()); c.Parameters.AddWithValue("@SadeceAktif", sadeceAktif?1:0);
        await using SqlDataReader r=await c.ExecuteReaderAsync(); List<Nace> liste=[];
        while(await r.ReadAsync()) liste.Add(new(){kod=r.GetString(0),ad=r.GetString(1),aktif=OrtakFonksiyonlar.Int32Yap(r.GetValue(2))==1});
        return liste;
    }

    public async Task<Nace?> OkuAsync(string kod)
    {
        const string sql="SELECT Kod,Ad,Aktif FROM dbo.Nace WHERE Kod=@Kod;";
        await using SqlCommand c=KomutOlustur(sql);c.Parameters.AddWithValue("@Kod",kod);await using SqlDataReader r=await c.ExecuteReaderAsync();
        return await r.ReadAsync()?new(){kod=r.GetString(0),ad=r.GetString(1),aktif=OrtakFonksiyonlar.Int32Yap(r.GetValue(2))==1}:null;
    }

    public async Task<string> KaydetAsync(Nace x)
    {
        string sql=!string.IsNullOrWhiteSpace(x.eskiKod)?"UPDATE dbo.Nace SET Kod=@Kod,Ad=@Ad,Aktif=@Aktif WHERE Kod=@EskiKod; SELECT @Kod;":"INSERT dbo.Nace(Kod,Ad,Aktif) VALUES(@Kod,@Ad,@Aktif); SELECT @Kod;";
        await using SqlCommand c=KomutOlustur(sql);c.Parameters.AddWithValue("@EskiKod",x.eskiKod??"");c.Parameters.AddWithValue("@Kod",x.kod);c.Parameters.AddWithValue("@Ad",x.ad);c.Parameters.AddWithValue("@Aktif",x.aktif?1:0);
        return Convert.ToString(await c.ExecuteScalarAsync()) ?? x.kod;
    }
}
