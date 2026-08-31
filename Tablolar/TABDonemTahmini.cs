using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Localization;
using TarimDonusum.Models;

namespace TarimDonusum.Tablolar;

public sealed class TABDonemTahmini(SqlConnection connection, IStringLocalizer<SharedResource>? localizer = null, SqlTransaction? transaction = null)
    : TABTablo(connection, localizer, transaction)
{
    public async Task<List<DonemTahminSatiri>> ListeleAsync(int donemId)
    {
        const string sql = @"SELECT Yil, KurTahminiTL, EnflasyonTahminiYuzde
                             FROM dbo.DonemTahmini WHERE DonemId=@DonemId ORDER BY Yil;";
        await using SqlCommand command=KomutOlustur(sql); command.Parameters.AddWithValue("@DonemId",donemId);
        await using SqlDataReader reader=await command.ExecuteReaderAsync(); List<DonemTahminSatiri> liste=[];
        while(await reader.ReadAsync()) liste.Add(new(){yil=reader.GetInt32(0),kurTahminiTL=NullOkuDecimal(reader,1),enflasyonTahminiYuzde=NullOkuDecimal(reader,2)});
        return liste;
    }

    public async Task KaydetAsync(int donemId, IEnumerable<DonemTahminSatiri> tahminler)
    {
        const string sql=@"MERGE dbo.DonemTahmini AS H
USING(SELECT @DonemId DonemId,@Yil Yil) AS K ON H.DonemId=K.DonemId AND H.Yil=K.Yil
WHEN MATCHED THEN UPDATE SET KurTahminiTL=@Kur,EnflasyonTahminiYuzde=@Enflasyon
WHEN NOT MATCHED THEN INSERT(DonemId,Yil,KurTahminiTL,EnflasyonTahminiYuzde) VALUES(@DonemId,@Yil,@Kur,@Enflasyon);";
        foreach(DonemTahminSatiri t in tahminler){await using SqlCommand c=KomutOlustur(sql);c.Parameters.AddWithValue("@DonemId",donemId);c.Parameters.AddWithValue("@Yil",t.yil);c.Parameters.AddWithValue("@Kur",t.kurTahminiTL!.Value);c.Parameters.AddWithValue("@Enflasyon",t.enflasyonTahminiYuzde!.Value);await c.ExecuteNonQueryAsync();}
    }
}
