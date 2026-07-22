using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Localization;
using TarimDonusum.Araclar;
using TarimDonusum.Models;

namespace TarimDonusum.Tablolar
{
    public class TABBirim : TABTablo
    {
        public TABBirim(SqlConnection connection, IStringLocalizer<SharedResource>? localizer = null, SqlTransaction? transaction = null)
            : base(connection, localizer, transaction)
        {
        }

        public async Task<List<Birim>> ListeleAsync(bool sadeceAktif = false)
        {
            const string sql = @"
                SELECT B.Id, B.BirimAdi, B.BirimTuru, B.IlKod, ISNULL(I.Ad, N'') AS IlAdi, B.SiraNo, B.Aktif
                FROM dbo.Birim B
                LEFT JOIN dbo.Il I ON I.Kod = B.IlKod
                WHERE @SadeceAktif = 0 OR B.Aktif = 1
                ORDER BY B.SiraNo, B.BirimAdi;";

            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@SadeceAktif", sadeceAktif ? 1 : 0);

            await using SqlDataReader reader = await command.ExecuteReaderAsync();
            List<Birim> liste = new List<Birim>();
            while (await reader.ReadAsync())
                liste.Add(Oku(reader));

            return liste;
        }

        public async Task<Birim?> OkuAsync(int id)
        {
            const string sql = @"
                SELECT B.Id, B.BirimAdi, B.BirimTuru, B.IlKod, ISNULL(I.Ad, N'') AS IlAdi, B.SiraNo, B.Aktif
                FROM dbo.Birim B
                LEFT JOIN dbo.Il I ON I.Kod = B.IlKod
                WHERE B.Id = @Id;";

            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@Id", id);

            await using SqlDataReader reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return null;

            return Oku(reader);
        }

        public async Task<bool> IlKoduVarMiAsync(int ilKod)
        {
            const string sql = "SELECT COUNT(1) FROM dbo.Il WHERE Kod = @IlKod;";

            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@IlKod", ilKod);

            int sayi = Convert.ToInt32(await command.ExecuteScalarAsync());
            return sayi > 0;
        }

        public async Task<int> EkleAsync(Birim birim)
        {
            const string sql = @"
                INSERT INTO dbo.Birim (BirimAdi, BirimTuru, IlKod, SiraNo, Aktif)
                OUTPUT INSERTED.Id
                VALUES (@BirimAdi, @BirimTuru, @IlKod, @SiraNo, @Aktif);";

            await using SqlCommand command = KomutOlustur(sql);
            ParametreleriEkle(command, birim);

            int id = Convert.ToInt32(await command.ExecuteScalarAsync());
            birim.id = id;
            return id;
        }

        public async Task<bool> GuncelleAsync(Birim birim)
        {
            const string sql = @"
                UPDATE dbo.Birim
                SET BirimAdi = @BirimAdi,
                    BirimTuru = @BirimTuru,
                    IlKod = @IlKod,
                    SiraNo = @SiraNo,
                    Aktif = @Aktif
                WHERE Id = @Id;";

            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@Id", birim.id);
            ParametreleriEkle(command, birim);

            return await command.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> PasifYapAsync(int id)
        {
            const string sql = "UPDATE dbo.Birim SET Aktif = 0 WHERE Id = @Id;";

            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@Id", id);

            return await command.ExecuteNonQueryAsync() > 0;
        }

        private static void ParametreleriEkle(SqlCommand command, Birim birim)
        {
            command.Parameters.AddWithValue("@BirimAdi", birim.birimAdi.Trim());
            command.Parameters.AddWithValue("@BirimTuru", (int)birim.birimTuru);
            command.Parameters.AddWithValue("@IlKod", birim.ilKod.HasValue ? birim.ilKod.Value : DBNull.Value);
            command.Parameters.AddWithValue("@SiraNo", birim.siraNo);
            command.Parameters.AddWithValue("@Aktif", birim.aktif ? 1 : 0);
        }

        private static Birim Oku(SqlDataReader reader)
        {
            return new Birim
            {
                id = reader.GetInt32(0),
                birimAdi = reader.GetString(1),
                birimTuru = (enumBirimTuru)reader.GetInt32(2),
                ilKod = NullOkuInt(reader, 3),
                ilAdi = reader.GetString(4),
                siraNo = OrtakFonksiyonlar.Int32Yap(reader.GetValue(5)),
                aktif = OrtakFonksiyonlar.Int32Yap(reader.GetValue(6)) == 1
            };
        }
    }
}
