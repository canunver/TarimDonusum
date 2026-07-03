using Microsoft.Data.SqlClient;
using TarimDonusum.Araclar;
using TarimDonusum.Models;

namespace TarimDonusum.Tablolar
{
    public class TABDonem : TABTablo
    {
        public TABDonem(SqlConnection connection, SqlTransaction? transaction = null)
            : base(connection, transaction)
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
                ORDER BY BasvuruyaAcikMi DESC, BasvuruBitisTarihi DESC, Id DESC;";

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
            return new Donem
            {
                Id = reader.GetInt32(0),
                Ad = reader.GetString(1),
                BasvuruyaAcikMi = OrtakFonksiyonlar.Int32Yap(reader.GetValue(2)) == 1,
                BasvuruBaslangicTarihi = reader.IsDBNull(3) ? null : reader.GetDateTime(3),
                BasvuruBitisTarihi = reader.IsDBNull(4) ? null : reader.GetDateTime(4),
                OnBasvuruBitisTarihi = reader.IsDBNull(5) ? null : reader.GetDateTime(5),
                MinimumYatirimTutari = reader.IsDBNull(6) ? null : reader.GetDecimal(6),
                MaksimumYatirimTutari = reader.IsDBNull(7) ? null : reader.GetDecimal(7),
                MaksimumDestekTutari = reader.IsDBNull(8) ? null : reader.GetDecimal(8),
                DestekOrani = reader.IsDBNull(9) ? null : reader.GetDecimal(9),
                Aciklama = reader.GetString(10),
                KayitTarihi = reader.GetDateTime(11),
                GuncellemeTarihi = reader.GetDateTime(12)
            };
        }
    }
}
