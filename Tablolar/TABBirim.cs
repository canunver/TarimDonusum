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
                SELECT B.Id, B.BirimAdi, B.BirimTuru, B.SiraNo, B.Aktif, B.UzmanBirimTuru
                FROM dbo.Birim B
                WHERE @SadeceAktif = 0 OR B.Aktif = 1
                ORDER BY B.SiraNo, B.BirimAdi;
                SELECT BI.BirimId, I.Id, I.Kod, I.Ad, I.Aktif
                FROM dbo.BirimIl BI
                INNER JOIN dbo.Il I ON I.Kod=BI.IlKod
                INNER JOIN dbo.Birim B ON B.Id=BI.BirimId
                WHERE @SadeceAktif=0 OR B.Aktif=1
                ORDER BY BI.BirimId, I.Ad;";

            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@SadeceAktif", sadeceAktif ? 1 : 0);

            await using SqlDataReader reader = await command.ExecuteReaderAsync();
            List<Birim> liste = new List<Birim>();
            while (await reader.ReadAsync())
                liste.Add(Oku(reader));

            await reader.NextResultAsync();
            while (await reader.ReadAsync())
            {
                Birim? birim = liste.FirstOrDefault(x => x.id == reader.GetInt32(0));
                if (birim == null) continue;
                Il il = new() { id=reader.GetInt32(1), kod=reader.GetInt32(2), ad=reader.GetString(3), aktif=OrtakFonksiyonlar.Int32Yap(reader.GetValue(4)) == 1 };
                birim.iller.Add(il);
                birim.ilKodlari.Add(il.kod);
            }

            return liste;
        }

        public async Task<Birim?> OkuAsync(int id)
        {
            const string sql = @"
                SELECT B.Id, B.BirimAdi, B.BirimTuru, B.SiraNo, B.Aktif, B.UzmanBirimTuru
                FROM dbo.Birim B
                WHERE B.Id = @Id;
                SELECT I.Id, I.Kod, I.Ad, I.Aktif
                FROM dbo.BirimIl BI INNER JOIN dbo.Il I ON I.Kod=BI.IlKod
                WHERE BI.BirimId=@Id ORDER BY I.Ad;";

            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@Id", id);

            await using SqlDataReader reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return null;

            Birim birim = Oku(reader);
            await reader.NextResultAsync();
            while (await reader.ReadAsync())
            {
                Il il = new() { id=reader.GetInt32(0), kod=reader.GetInt32(1), ad=reader.GetString(2), aktif=OrtakFonksiyonlar.Int32Yap(reader.GetValue(3)) == 1 };
                birim.iller.Add(il); birim.ilKodlari.Add(il.kod);
            }
            return birim;
        }

        public async Task<bool> IlKodlariGecerliMiAsync(IEnumerable<int> ilKodlari)
        {
            List<int> kodlar = ilKodlari.Distinct().ToList();
            if (kodlar.Count == 0) return true;
            const string sql = "SELECT COUNT(1) FROM dbo.Il WHERE Kod IN (SELECT value FROM OPENJSON(@IlKodlari));";

            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@IlKodlari", System.Text.Json.JsonSerializer.Serialize(kodlar));

            int sayi = Convert.ToInt32(await command.ExecuteScalarAsync());
            return sayi == kodlar.Count;
        }

        public async Task<int> EkleAsync(Birim birim)
        {
            const string sql = @"
                INSERT INTO dbo.Birim (BirimAdi, BirimTuru, SiraNo, Aktif, UzmanBirimTuru)
                OUTPUT INSERTED.Id
                VALUES (@BirimAdi, @BirimTuru, @SiraNo, @Aktif, @UzmanBirimTuru);";

            await using SqlCommand command = KomutOlustur(sql);
            ParametreleriEkle(command, birim);

            int id = Convert.ToInt32(await command.ExecuteScalarAsync());
            birim.id = id;
            await IlleriKaydetAsync(birim);
            return id;
        }

        public async Task<bool> GuncelleAsync(Birim birim)
        {
            const string sql = @"
                UPDATE dbo.Birim
                SET BirimAdi = @BirimAdi,
                    BirimTuru = @BirimTuru,
                    SiraNo = @SiraNo,
                    Aktif = @Aktif,
                    UzmanBirimTuru = @UzmanBirimTuru
                WHERE Id = @Id;";

            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@Id", birim.id);
            ParametreleriEkle(command, birim);

            bool guncellendi = await command.ExecuteNonQueryAsync() > 0;
            if (guncellendi) await IlleriKaydetAsync(birim);
            return guncellendi;
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
            command.Parameters.AddWithValue("@SiraNo", birim.siraNo);
            command.Parameters.AddWithValue("@Aktif", birim.aktif ? 1 : 0);
            command.Parameters.AddWithValue("@UzmanBirimTuru", string.IsNullOrWhiteSpace(birim.uzmanBirimTuru) ? DBNull.Value : birim.uzmanBirimTuru.Trim().ToUpperInvariant());
        }

        private static Birim Oku(SqlDataReader reader)
        {
            return new Birim
            {
                id = reader.GetInt32(0),
                birimAdi = reader.GetString(1),
                birimTuru = (enumBirimTuru)reader.GetInt32(2),
                siraNo = OrtakFonksiyonlar.Int32Yap(reader.GetValue(3)),
                aktif = OrtakFonksiyonlar.Int32Yap(reader.GetValue(4)) == 1,
                uzmanBirimTuru = reader.IsDBNull(5) ? "" : reader.GetString(5)
            };
        }

        private async Task IlleriKaydetAsync(Birim birim)
        {
            const string sql = @"
                DELETE FROM dbo.BirimIl WHERE BirimId=@BirimId;
                INSERT INTO dbo.BirimIl(BirimId, IlKod)
                SELECT @BirimId, CONVERT(INT, value) FROM OPENJSON(@IlKodlari);";
            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@BirimId", birim.id);
            command.Parameters.AddWithValue("@IlKodlari", System.Text.Json.JsonSerializer.Serialize(birim.ilKodlari.Distinct()));
            await command.ExecuteNonQueryAsync();
        }
    }
}
