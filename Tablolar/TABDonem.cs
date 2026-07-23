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
                    Aciklama,
                    Aciklama
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
                ORDER BY Yil DESC, BasvuruBaslangicTarihi DESC, Id DESC;";

            await using SqlCommand command = KomutOlustur(sql);
            await using SqlDataReader reader = await command.ExecuteReaderAsync();

            List<Donem> liste = new List<Donem>();
            while (await reader.ReadAsync())
            {
                liste.Add(Oku(reader));
            }

            return liste;
        }

        public async Task<int> EkleAsync(Donem donem)
        {
            const string sql = @"
                INSERT INTO dbo.Donem
                    (Yil, Ad, BasvuruyaAcikMi, BasvuruBaslangicTarihi, BasvuruBitisTarihi,
                     OnBasvuruBitisTarihi, MinimumYatirimTutari, MaksimumYatirimTutari,
                     MaksimumDestekTutari, DestekOrani, Aciklama)
                OUTPUT INSERTED.Id
                VALUES
                    (@Yil, @Ad, @BasvuruyaAcikMi, @BasvuruBaslangicTarihi, @BasvuruBitisTarihi,
                     @OnBasvuruBitisTarihi, @MinimumYatirimTutari, @MaksimumYatirimTutari,
                     @MaksimumDestekTutari, @DestekOrani, @Aciklama);";

            await using SqlCommand command = KomutOlustur(sql);
            ParametreleriEkle(command, donem);
            return Convert.ToInt32(await command.ExecuteScalarAsync());
        }

        public async Task<bool> GuncelleAsync(Donem donem)
        {
            const string sql = @"
                UPDATE dbo.Donem
                SET Yil = @Yil, Ad = @Ad, BasvuruyaAcikMi = @BasvuruyaAcikMi,
                    BasvuruBaslangicTarihi = @BasvuruBaslangicTarihi,
                    BasvuruBitisTarihi = @BasvuruBitisTarihi,
                    OnBasvuruBitisTarihi = @OnBasvuruBitisTarihi,
                    MinimumYatirimTutari = @MinimumYatirimTutari,
                    MaksimumYatirimTutari = @MaksimumYatirimTutari,
                    MaksimumDestekTutari = @MaksimumDestekTutari,
                    DestekOrani = @DestekOrani, Aciklama = @Aciklama
                WHERE Id = @Id;";

            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@Id", donem.id);
            ParametreleriEkle(command, donem);
            return await command.ExecuteNonQueryAsync() > 0;
        }

        private static void ParametreleriEkle(SqlCommand command, Donem donem)
        {
            command.Parameters.AddWithValue("@Yil", donem.yil);
            command.Parameters.AddWithValue("@Ad", donem.ad.Trim());
            command.Parameters.AddWithValue("@BasvuruyaAcikMi", donem.basvuruyaAcikMi ? 1 : 0);
            command.Parameters.AddWithValue("@BasvuruBaslangicTarihi", (object?)donem.basvuruBaslangicTarihi ?? DBNull.Value);
            command.Parameters.AddWithValue("@BasvuruBitisTarihi", (object?)donem.basvuruBitisTarihi ?? DBNull.Value);
            command.Parameters.AddWithValue("@OnBasvuruBitisTarihi", (object?)donem.onBasvuruBitisTarihi ?? DBNull.Value);
            command.Parameters.AddWithValue("@MinimumYatirimTutari", (object?)donem.minimumYatirimTutari ?? DBNull.Value);
            command.Parameters.AddWithValue("@MaksimumYatirimTutari", (object?)donem.maksimumYatirimTutari ?? DBNull.Value);
            command.Parameters.AddWithValue("@MaksimumDestekTutari", (object?)donem.maksimumDestekTutari ?? DBNull.Value);
            command.Parameters.AddWithValue("@DestekOrani", (object?)donem.destekOrani ?? DBNull.Value);
            command.Parameters.AddWithValue("@Aciklama", donem.aciklama?.Trim() ?? "");
        }

        private static Donem Oku(SqlDataReader reader)
        {
            Donem d = new Donem();
            int kol = 0;
            d.id = reader.GetInt32(kol++);
            d.yil = NullDuzeltInt(reader, kol++);
            d.ad = reader.GetString(kol++);
            d.basvuruyaAcikMi = BoolYap(NullDuzeltInt(reader, kol++));
            d.basvuruBaslangicTarihi = reader.IsDBNull(kol) ? null : reader.GetDateTime(kol); kol++;
            d.basvuruBitisTarihi = reader.IsDBNull(kol) ? null : reader.GetDateTime(kol); kol++;
            d.onBasvuruBitisTarihi = reader.IsDBNull(kol) ? null : reader.GetDateTime(kol); kol++;
            d.minimumYatirimTutari = reader.IsDBNull(kol) ? null : reader.GetDecimal(kol); kol++;
            d.maksimumYatirimTutari = reader.IsDBNull(kol) ? null : reader.GetDecimal(kol); kol++;
            d.maksimumDestekTutari = reader.IsDBNull(kol) ? null : reader.GetDecimal(kol); kol++;
            d.destekOrani = reader.IsDBNull(kol) ? null : reader.GetDecimal(kol); kol++;
            d.aciklama = reader.GetString(kol++);
            return d;
        }
    }
}
