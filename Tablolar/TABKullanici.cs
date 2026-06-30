using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Identity;
using TarimDonusum.Araclar;
using TarimDonusum.Models;

namespace TarimDonusum.Tablolar
{
    public class TABKullanici : TABTablo
    {
        public TABKullanici(SqlConnection connection, SqlTransaction? transaction = null)
            : base(connection, transaction)
        {
        }

        public async Task<int> EkleAsync(Kullanici kullanici)
        {
            const string sql = @"
                INSERT INTO dbo.Kullanici
                (
                    TCKN,
                    Ad,
                    Soyad,
                    DogumTarihi,
                    Cinsiyet,
                    Eposta,
                    Telefon,
                    ParolaHash,
                    KayitTarihi,
                    Aktif
                )
                OUTPUT INSERTED.Id
                VALUES
                (
                    @TCKN,
                    @Ad,
                    @Soyad,
                    @DogumTarihi,
                    @Cinsiyet,
                    @Eposta,
                    @Telefon,
                    @ParolaHash,
                    @KayitTarihi,
                    @Aktif
                );";

            await using SqlCommand command = KomutOlustur(sql);
            EkleParametreleriEkle(command, kullanici);
            command.Parameters.AddWithValue("@ParolaHash", ParolaHashOlustur(kullanici));

            object? sonuc = await command.ExecuteScalarAsync();
            int id = OrtakFonksiyonlar.Int32Yap(sonuc);
            kullanici.Id = id;

            return id;
        }

        public async Task<Kullanici?> GetirAsync(int id)
        {
            const string sql = @"
                SELECT
                    Id,
                    TCKN,
                    Ad,
                    Soyad,
                    DogumTarihi,
                    Cinsiyet,
                    Eposta,
                    Telefon,
                    KayitTarihi,
                    Aktif
                FROM dbo.Kullanici
                WHERE Id = @Id;";

            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@Id", id);

            await using SqlDataReader reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return null;

            return Oku(reader);
        }

        public async Task<List<Kullanici>> ListeleAsync()
        {
            const string sql = @"
                SELECT
                    Id,
                    TCKN,
                    Ad,
                    Soyad,
                    DogumTarihi,
                    Cinsiyet,
                    Eposta,
                    Telefon,
                    KayitTarihi,
                    Aktif
                FROM dbo.Kullanici
                ORDER BY Id;";

            await using SqlCommand command = KomutOlustur(sql);
            await using SqlDataReader reader = await command.ExecuteReaderAsync();

            List<Kullanici> liste = new List<Kullanici>();
            while (await reader.ReadAsync())
            {
                liste.Add(Oku(reader));
            }

            return liste;
        }

        public async Task<bool> GuncelleAsync(Kullanici kullanici)
        {
            const string sql = @"
                UPDATE dbo.Kullanici
                SET
                    Ad = @Ad,
                    Soyad = @Soyad,
                    DogumTarihi = @DogumTarihi,
                    Cinsiyet = @Cinsiyet,
                    KayitTarihi = @KayitTarihi,
                    Aktif = @Aktif
                WHERE Id = @Id;";

            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@Id", kullanici.Id);
            GuncelleParametreleriEkle(command, kullanici);

            return await command.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> SilAsync(int id)
        {
            const string sql = "DELETE FROM dbo.Kullanici WHERE Id = @Id;";

            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@Id", id);

            return await command.ExecuteNonQueryAsync() > 0;
        }

        private static void EkleParametreleriEkle(SqlCommand command, Kullanici kullanici)
        {
            command.Parameters.AddWithValue("@TCKN", kullanici.TCKN);
            command.Parameters.AddWithValue("@Ad", kullanici.Ad);
            command.Parameters.AddWithValue("@Soyad", kullanici.Soyad);
            command.Parameters.AddWithValue("@DogumTarihi", kullanici.DogumTarihi);
            command.Parameters.AddWithValue("@Cinsiyet", kullanici.Cinsiyet);
            command.Parameters.AddWithValue("@Eposta", kullanici.Eposta);
            command.Parameters.AddWithValue("@Telefon", kullanici.Telefon);
            command.Parameters.AddWithValue("@KayitTarihi", kullanici.KayitTarihi);
            command.Parameters.AddWithValue("@Aktif", kullanici.Aktif ? 1 : 0);
        }

        private static void GuncelleParametreleriEkle(SqlCommand command, Kullanici kullanici)
        {
            command.Parameters.AddWithValue("@Ad", kullanici.Ad);
            command.Parameters.AddWithValue("@Soyad", kullanici.Soyad);
            command.Parameters.AddWithValue("@DogumTarihi", kullanici.DogumTarihi);
            command.Parameters.AddWithValue("@Cinsiyet", kullanici.Cinsiyet);
            command.Parameters.AddWithValue("@KayitTarihi", kullanici.KayitTarihi);
            command.Parameters.AddWithValue("@Aktif", kullanici.Aktif ? 1 : 0);
        }

        public async Task<bool> TelefonDegistirAsync(int kullaniciId, string telefon)
        {
            const string sql = "UPDATE dbo.Kullanici SET Telefon = @Telefon WHERE Id = @Id;";

            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@Id", kullaniciId);
            command.Parameters.AddWithValue("@Telefon", telefon);

            return await command.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> EpostaDegistirAsync(int kullaniciId, string eposta)
        {
            const string sql = "UPDATE dbo.Kullanici SET Eposta = @Eposta WHERE Id = @Id;";

            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@Id", kullaniciId);
            command.Parameters.AddWithValue("@Eposta", eposta);

            return await command.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> ParolaDegistirAsync(Kullanici kullanici)
        {
            const string sql = "UPDATE dbo.Kullanici SET ParolaHash = @ParolaHash WHERE Id = @Id;";

            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@Id", kullanici.Id);
            command.Parameters.AddWithValue("@ParolaHash", ParolaHashOlustur(kullanici));

            return await command.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> BenzerKayitVarMiAsync(string tckn, string eposta, string telefon)
        {
            const string sql = @"
                SELECT COUNT(1)
                FROM dbo.Kullanici
                WHERE TCKN = @TCKN
                    OR Eposta = @Eposta
                    OR Telefon = @Telefon;";

            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@TCKN", tckn);
            command.Parameters.AddWithValue("@Eposta", eposta);
            command.Parameters.AddWithValue("@Telefon", telefon);

            object? sonuc = await command.ExecuteScalarAsync();
            return OrtakFonksiyonlar.Int32Yap(sonuc) > 0;
        }

        private static Kullanici Oku(SqlDataReader reader)
        {
            return new Kullanici
            {
                Id = reader.GetInt32(0),
                TCKN = reader.GetString(1),
                Ad = reader.GetString(2),
                Soyad = reader.GetString(3),
                DogumTarihi = reader.GetDateTime(4),
                Cinsiyet = reader.GetString(5),
                Eposta = reader.GetString(6),
                Telefon = reader.GetString(7),
                KayitTarihi = reader.GetDateTime(8),
                Aktif = reader.GetInt32(9) == 1
            };
        }

        private static string ParolaHashOlustur(Kullanici kullanici)
        {
            if (string.IsNullOrWhiteSpace(kullanici.Parola))
                throw new InvalidOperationException("Parola boş olamaz.");

            PasswordHasher<Kullanici> passwordHasher = new PasswordHasher<Kullanici>();

            return passwordHasher.HashPassword(kullanici, kullanici.Parola);
        }
    }
}
