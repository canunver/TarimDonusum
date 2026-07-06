using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Localization;
using TarimDonusum.Araclar;
using TarimDonusum.Models;

namespace TarimDonusum.Tablolar
{
    public class TABDonem : TABTablo
    {
        public TABDonem(SqlConnection connection, IStringLocalizer<SharedResource>? localizer = null, SqlTransaction? transaction = null)
            : base(connection, localizer, transaction)
        {
        }

        public async Task<Donem?> OkuAsync(int id)
        {
            const string sql = @"
                SELECT
                    Id,
                    Ad,
                    BasvuruyaAcikMi,
                    BasvuruBaslangicTarihi,
                    BasvuruBitisTarihi,
                    OnBasvuruBitisTarihi,
                    MinimumYatirimTutari,
                    MaksimumYatirimTutari,
                    MaksimumDestekTutari,
                    DestekOrani,
                    Aciklama,
                    KayitTarihi,
                    GuncellemeTarihi
                FROM dbo.Donem
                WHERE Id = @Id;";

            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@Id", id);

            await using SqlDataReader reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return null;

            return Oku(reader);
        }

        public async Task<List<Donem>> ListeleAsync()
        {
            const string sql = @"
                SELECT
                    Id,
                    Yil,
                    Ad,
                    BasvuruyaAcikMi,
                    BasvuruBaslangicTarihi,
                    BasvuruBitisTarihi,
                    OnBasvuruBitisTarihi,
                    MinimumYatirimTutari,
                    MaksimumYatirimTutari,
                    MaksimumDestekTutari,
                    DestekOrani,
                    Aciklama
                FROM dbo.Donem
                ORDER BY BasvuruyaAcikMi DESC, Yil DESC, Id DESC;";

            await using SqlCommand command = KomutOlustur(sql);
            await using SqlDataReader reader = await command.ExecuteReaderAsync();

            List<Donem> liste = new List<Donem>();
            while (await reader.ReadAsync())
            {
                liste.Add(Oku(reader));
            }

            return liste;
        }

        private static Donem Oku(SqlDataReader reader)
        {
            Donem d = new Donem();
            int kol = 0;
            d.Id = reader.GetInt32(kol++);
            d.Yil = NullDuzeltInt(reader, kol++);
            d.Ad = reader.GetString(kol++);
            d.BasvuruyaAcikMi = BoolYap(NullDuzeltInt(reader, kol++));
            d.BasvuruBaslangicTarihi = reader.GetDateTime(kol++);
            d.BasvuruBitisTarihi = reader.GetDateTime(kol++);
            d.OnBasvuruBitisTarihi = reader.GetDateTime(kol++);
            d.MinimumYatirimTutari = reader.GetDecimal(kol++);
            d.MaksimumYatirimTutari = reader.GetDecimal(kol++);
            d.MaksimumDestekTutari = reader.GetDecimal(kol++);
            d.DestekOrani = reader.GetDecimal(kol++);
            d.Aciklama = reader.GetString(kol++);
            return d;
        }
    }
}
