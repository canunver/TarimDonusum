using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Localization;
using TarimDonusum.Araclar;
using TarimDonusum.Models;

namespace TarimDonusum.Tablolar
{
    public class TABIlce : TABTablo
    {
        public TABIlce(SqlConnection connection, IStringLocalizer<SharedResource>? localizer = null, SqlTransaction? transaction = null)
            : base(connection, localizer, transaction)
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

        public async Task<int> EkleAsync(Ilce ilce)
        {
            const string sql = @"INSERT INTO dbo.Ilce (IlId, Ad, Aktif)
                                 OUTPUT INSERTED.Id VALUES (@IlId, @Ad, @Aktif);";
            await using SqlCommand command = KomutOlustur(sql);
            ParametreleriEkle(command, ilce);
            return Convert.ToInt32(await command.ExecuteScalarAsync());
        }

        public async Task<bool> GuncelleAsync(Ilce ilce)
        {
            const string sql = @"UPDATE dbo.Ilce SET Ad=@Ad, Aktif=@Aktif
                                 WHERE Id=@Id AND IlId=@IlId;";
            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@Id", ilce.Id);
            ParametreleriEkle(command, ilce);
            return await command.ExecuteNonQueryAsync() > 0;
        }

        private static void ParametreleriEkle(SqlCommand command, Ilce ilce)
        {
            command.Parameters.AddWithValue("@IlId", ilce.IlId);
            command.Parameters.AddWithValue("@Ad", ilce.Ad.Trim());
            command.Parameters.AddWithValue("@Aktif", ilce.Aktif ? 1 : 0);
        }
    }
}
