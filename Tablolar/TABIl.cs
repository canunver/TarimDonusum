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

        private static Il Oku(SqlDataReader reader)
        {
            return new Il
            {
                Id = reader.GetInt32(0),
                Kod = OrtakFonksiyonlar.Int32Yap(reader.GetValue(1)),
                Ad = reader.GetString(2),
                Aktif = OrtakFonksiyonlar.Int32Yap(reader.GetValue(3)) == 1
            };
        }
    }
}
