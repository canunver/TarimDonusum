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
            d.id = reader.GetInt32(kol++);
            d.yil = NullDuzeltInt(reader, kol++);
            d.ad = reader.GetString(kol++);
            d.basvuruyaAcikMi = BoolYap(NullDuzeltInt(reader, kol++));
            d.basvuruBaslangicTarihi = reader.GetDateTime(kol++);
            d.basvuruBitisTarihi = reader.GetDateTime(kol++);
            d.onBasvuruBitisTarihi = reader.GetDateTime(kol++);
            d.minimumYatirimTutari = reader.GetDecimal(kol++);
            d.maksimumYatirimTutari = reader.GetDecimal(kol++);
            d.maksimumDestekTutari = reader.GetDecimal(kol++);
            d.destekOrani = reader.GetDecimal(kol++);
            d.aciklama = reader.GetString(kol++);
            return d;
        }
    }
}
