using Microsoft.Data.SqlClient;
using TarimDonusum.Araclar;
using TarimDonusum.Models;

namespace TarimDonusum.Tablolar
{
    public class TABKullaniciYetki : TABTablo
    {
        public TABKullaniciYetki(SqlConnection connection, SqlTransaction? transaction = null)
            : base(connection, transaction)
        {
        }

        public async Task<int> EkleAsync(KullaniciYetki kullaniciYetki)
        {
            const string sql = @"
                INSERT INTO dbo.KullaniciYetki
                (
                    KullaniciId,
                    Rol,
                    YetkiKodu,
                    Birim
                )
                OUTPUT INSERTED.Id
                VALUES
                (
                    @KullaniciId,
                    @Rol,
                    @YetkiKodu,
                    @Birim
                );";

            await using SqlCommand command = KomutOlustur(sql);
            ParametreleriEkle(command, kullaniciYetki);

            object? sonuc = await command.ExecuteScalarAsync();
            int id = OrtakFonksiyonlar.Int32Yap(sonuc);
            kullaniciYetki.Id = id;

            return id;
        }

        public async Task<KullaniciYetki?> GetirAsync(int id)
        {
            const string sql = @"
                SELECT
                    Id,
                    KullaniciId,
                    Rol,
                    YetkiKodu,
                    Birim
                FROM dbo.KullaniciYetki
                WHERE Id = @Id;";

            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@Id", id);

            await using SqlDataReader reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return null;

            return Oku(reader);
        }

        public async Task<List<KullaniciYetki>> KullaniciYetkileriniListeleAsync(int kullaniciId)
        {
            const string sql = @"
                SELECT
                    Id,
                    KullaniciId,
                    Rol,
                    YetkiKodu,
                    Birim
                FROM dbo.KullaniciYetki
                WHERE KullaniciId = @KullaniciId
                ORDER BY Birim, Rol;";

            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@KullaniciId", kullaniciId);

            await using SqlDataReader reader = await command.ExecuteReaderAsync();

            List<KullaniciYetki> liste = new List<KullaniciYetki>();
            while (await reader.ReadAsync())
            {
                liste.Add(Oku(reader));
            }

            return liste;
        }

        public async Task<List<KullaniciYetki>> ListeleAsync()
        {
            const string sql = @"
                SELECT
                    Id,
                    KullaniciId,
                    Rol,
                    YetkiKodu,
                    Birim
                FROM dbo.KullaniciYetki
                ORDER BY KullaniciId, Birim, Rol;";

            await using SqlCommand command = KomutOlustur(sql);
            await using SqlDataReader reader = await command.ExecuteReaderAsync();

            List<KullaniciYetki> liste = new List<KullaniciYetki>();
            while (await reader.ReadAsync())
            {
                liste.Add(Oku(reader));
            }

            return liste;
        }

        public async Task<bool> GuncelleAsync(KullaniciYetki kullaniciYetki)
        {
            const string sql = @"
                UPDATE dbo.KullaniciYetki
                SET
                    KullaniciId = @KullaniciId,
                    Rol = @Rol,
                    YetkiKodu = @YetkiKodu,
                    Birim = @Birim
                WHERE Id = @Id;";

            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@Id", kullaniciYetki.Id);
            ParametreleriEkle(command, kullaniciYetki);

            return await command.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> SilAsync(int id)
        {
            const string sql = "DELETE FROM dbo.KullaniciYetki WHERE Id = @Id;";

            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@Id", id);

            return await command.ExecuteNonQueryAsync() > 0;
        }

        private static void ParametreleriEkle(SqlCommand command, KullaniciYetki kullaniciYetki)
        {
            command.Parameters.AddWithValue("@KullaniciId", kullaniciYetki.KullaniciId);
            command.Parameters.AddWithValue("@Rol", (int)kullaniciYetki.Rol);
            command.Parameters.AddWithValue("@YetkiKodu", kullaniciYetki.YetkiKodu);
            object birim = kullaniciYetki.Birim.HasValue ? (object)kullaniciYetki.Birim.Value : DBNull.Value;
            command.Parameters.AddWithValue("@Birim", birim);
        }

        private static KullaniciYetki Oku(SqlDataReader reader)
        {
            return new KullaniciYetki
            {
                Id = reader.GetInt32(0),
                KullaniciId = reader.GetInt32(1),
                Rol = (KullaniciRol)reader.GetInt32(2),
                YetkiKodu = reader.GetInt32(3),
                Birim = reader.IsDBNull(4) ? null : reader.GetInt32(4)
            };
        }
    }
}
