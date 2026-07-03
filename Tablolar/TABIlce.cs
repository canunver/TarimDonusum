using Microsoft.Data.SqlClient;
using TarimDonusum.Araclar;
using TarimDonusum.Models;

namespace TarimDonusum.Tablolar
{
    public class TABIlce : TABTablo
    {
        public TABIlce(SqlConnection connection, SqlTransaction? transaction = null)
            : base(connection, transaction)
        {
        }

        public async Task<List<Ilce>> ListeleAsync(int? ilId = null, bool sadeceAktif = true)
        {
            const string sql = @"
                SELECT Id, IlId, Ad, Aktif
                FROM dbo.Ilce
                WHERE (@IlId IS NULL OR IlId = @IlId)
                    AND (@SadeceAktif = 0 OR Aktif = 1)
                ORDER BY Ad;";

            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@IlId", ilId.HasValue && ilId.Value > 0 ? ilId.Value : DBNull.Value);
            command.Parameters.AddWithValue("@SadeceAktif", sadeceAktif ? 1 : 0);

            await using SqlDataReader reader = await command.ExecuteReaderAsync();
            List<Ilce> liste = new List<Ilce>();
            while (await reader.ReadAsync())
            {
                liste.Add(new Ilce
                {
                    Id = reader.GetInt32(0),
                    IlId = reader.GetInt32(1),
                    Ad = reader.GetString(2),
                    Aktif = OrtakFonksiyonlar.Int32Yap(reader.GetValue(3)) == 1
                });
            }

            return liste;
        }

        public async Task<Ilce?> OkuAsync(int id)
        {
            const string sql = @"
                SELECT Id, IlId, Ad, Aktif
                FROM dbo.Ilce
                WHERE Id = @Id;";

            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@Id", id);

            await using SqlDataReader reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return null;

            return new Ilce
            {
                Id = reader.GetInt32(0),
                IlId = reader.GetInt32(1),
                Ad = reader.GetString(2),
                Aktif = OrtakFonksiyonlar.Int32Yap(reader.GetValue(3)) == 1
            };
        }
    }
}
