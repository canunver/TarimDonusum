using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Localization;
using TarimDonusum.Models;

namespace TarimDonusum.Tablolar
{
    public class TABPozDonemFiyat : TABTablo
    {
        public TABPozDonemFiyat(SqlConnection connection, IStringLocalizer<SharedResource>? localizer = null, SqlTransaction? transaction = null)
            : base(connection, localizer, transaction) { }

        public async Task<List<PozDonemFiyat>> ListeleAsync(int donemId)
        {
            const string sql = @"SELECT ISNULL(f.Id,0),@DonemId,p.Id,f.BirimFiyat,p.PozNo,p.Ad,p.Birim
                                 FROM dbo.Poz p
                                 LEFT JOIN dbo.PozDonemFiyat f ON f.PozId=p.Id AND f.DonemId=@DonemId
                                 WHERE p.Aktif=1 OR f.Id IS NOT NULL
                                 ORDER BY p.PozNo,p.Ad;";
            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@DonemId", donemId);
            await using SqlDataReader reader = await command.ExecuteReaderAsync();
            List<PozDonemFiyat> liste = new();
            while (await reader.ReadAsync())
                liste.Add(new PozDonemFiyat
                {
                    id = reader.GetInt32(0), donemId = reader.GetInt32(1), pozId = reader.GetInt32(2),
                    birimFiyat = reader.IsDBNull(3) ? null : reader.GetDecimal(3), pozNo = reader.GetString(4),
                    pozAdi = reader.GetString(5), birim = reader.GetString(6)
                });
            return liste;
        }

        public async Task KaydetAsync(PozDonemFiyat model)
        {
            const string sql = @"MERGE dbo.PozDonemFiyat AS hedef
                                 USING(SELECT @DonemId DonemId,@PozId PozId) AS kaynak
                                 ON hedef.DonemId=kaynak.DonemId AND hedef.PozId=kaynak.PozId
                                 WHEN MATCHED THEN UPDATE SET BirimFiyat=@BirimFiyat
                                 WHEN NOT MATCHED THEN INSERT(DonemId,PozId,BirimFiyat) VALUES(@DonemId,@PozId,@BirimFiyat);";
            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@DonemId", model.donemId);
            command.Parameters.AddWithValue("@PozId", model.pozId);
            command.Parameters.AddWithValue("@BirimFiyat", model.birimFiyat!.Value);
            await command.ExecuteNonQueryAsync();
        }

        public async Task SilAsync(int donemId, int pozId)
        {
            const string sql = "DELETE dbo.PozDonemFiyat WHERE DonemId=@DonemId AND PozId=@PozId;";
            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@DonemId", donemId);
            command.Parameters.AddWithValue("@PozId", pozId);
            await command.ExecuteNonQueryAsync();
        }
    }
}
