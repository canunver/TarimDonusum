using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Localization;
using TarimDonusum.Models;

namespace TarimDonusum.Tablolar;

public sealed class TABBasvuruBilancoGelir(SqlConnection connection, IStringLocalizer<SharedResource>? localizer = null, SqlTransaction? transaction = null) : TABTablo(connection, localizer, transaction)
{
    public async Task<List<BasvuruBilancoGelirSatiri>> OkuAsync(int basvuruId)
    {
        const string sql = "SELECT Kod,Yil_1,Yil_2,Yil_3 FROM dbo.BasvuruBilancoGelir WHERE BasvuruId=@BasvuruId ORDER BY Kod";
        await using SqlCommand command=KomutOlustur(sql); command.Parameters.AddWithValue("@BasvuruId",basvuruId);
        await using SqlDataReader reader=await command.ExecuteReaderAsync(); List<BasvuruBilancoGelirSatiri> sonuc=[];
        while(await reader.ReadAsync()) sonuc.Add(new(){kod=reader.GetString(0),yil_1=NullOkuDecimal(reader,1),yil_2=NullOkuDecimal(reader,2),yil_3=NullOkuDecimal(reader,3)});
        return sonuc;
    }
    public async Task KaydetAsync(BasvuruBilancoGelir model)
    {
        const string sql=@"MERGE dbo.BasvuruBilancoGelir AS H USING(SELECT @BasvuruId BasvuruId,@Kod Kod) AS K ON H.BasvuruId=K.BasvuruId AND H.Kod=K.Kod WHEN MATCHED THEN UPDATE SET Yil_1=@Yil_1,Yil_2=@Yil_2,Yil_3=@Yil_3 WHEN NOT MATCHED THEN INSERT(BasvuruId,Kod,Yil_1,Yil_2,Yil_3) VALUES(@BasvuruId,@Kod,@Yil_1,@Yil_2,@Yil_3);";
        foreach(var s in model.satirlar){await using SqlCommand c=KomutOlustur(sql);c.Parameters.AddWithValue("@BasvuruId",model.basvuruId);c.Parameters.AddWithValue("@Kod",s.kod);c.Parameters.AddWithValue("@Yil_1",(object?)s.yil_1??DBNull.Value);c.Parameters.AddWithValue("@Yil_2",(object?)s.yil_2??DBNull.Value);c.Parameters.AddWithValue("@Yil_3",(object?)s.yil_3??DBNull.Value);await c.ExecuteNonQueryAsync();}
    }
}
