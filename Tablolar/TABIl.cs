using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Localization;
using TarimDonusum.Araclar;
using TarimDonusum.Models;

namespace TarimDonusum.Tablolar
{
    public class TABIl : TABTablo
    {
        public TABIl(SqlConnection connection, IStringLocalizer<SharedResource> localizer, SqlTransaction? transaction = null)
            : base(connection, localizer, transaction)
        {
        }

        public async Task<Il?> OkuAsync(int id)
        {
            const string sql = @"
                SELECT Id, Kod, Ad, Aktif
                FROM dbo.Il
                WHERE Id = @Id;";

            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@Id", id);

            await using SqlDataReader reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return null;

            return Oku(reader);
        }

        public async Task<List<Il>> ListeleAsync(bool sadeceAktif = true)
        {
            const string sql = @"
                SELECT Id, Kod, Ad, Aktif
                FROM dbo.Il
                WHERE @SadeceAktif = 0 OR Aktif = 1
                ORDER BY Ad;";

            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@SadeceAktif", sadeceAktif ? 1 : 0);

            await using SqlDataReader reader = await command.ExecuteReaderAsync();
            List<Il> liste = new List<Il>();
            while (await reader.ReadAsync())
            {
                liste.Add(Oku(reader));
            }

            return liste;
        }

        public async Task<int> EkleAsync(Il il)
        {
            const string sql = @"INSERT INTO dbo.Il (Kod, Ad, Aktif)
                                 OUTPUT INSERTED.Id VALUES (@Kod, @Ad, @Aktif);";
            await using SqlCommand command = KomutOlustur(sql);
            ParametreleriEkle(command, il);
            return Convert.ToInt32(await command.ExecuteScalarAsync());
        }

        public async Task<bool> GuncelleAsync(Il il)
        {
            const string sql = @"UPDATE dbo.Il SET Kod=@Kod, Ad=@Ad, Aktif=@Aktif WHERE Id=@Id;";
            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@Id", il.id);
            ParametreleriEkle(command, il);
            return await command.ExecuteNonQueryAsync() > 0;
        }

        private static void ParametreleriEkle(SqlCommand command, Il il)
        {
            command.Parameters.AddWithValue("@Kod", il.kod);
            command.Parameters.AddWithValue("@Ad", il.ad.Trim());
            command.Parameters.AddWithValue("@Aktif", il.aktif ? 1 : 0);
        }

        private static Il Oku(SqlDataReader reader)
        {
            return new Il
            {
                id = reader.GetInt32(0),
                kod = OrtakFonksiyonlar.Int32Yap(reader.GetValue(1)),
                ad = reader.GetString(2),
                aktif = OrtakFonksiyonlar.Int32Yap(reader.GetValue(3)) == 1
            };
        }
    }
}
