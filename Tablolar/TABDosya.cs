using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Localization;
using TarimDonusum.Models;

namespace TarimDonusum.Tablolar
{
    public class TABDosya : TABTablo
    {
        public TABDosya(SqlConnection connection, IStringLocalizer<SharedResource>? localizer = null, SqlTransaction? transaction = null)
            : base(connection, localizer, transaction)
        {
        }

        public async Task<List<DosyaBilgisi>> ListeleAsync(string modulKod, string? formAd = null, string? formAnahtar = null)
        {
            const string sql = @"SELECT Id, ModulKod, FormAd, FormAnahtar, DosyaNo, DosyaAdi, Buyukluk, IlkYuklemeTarihi, STarihi, Aciklama
                FROM dbo.DosyaBilgisi
                WHERE ModulKod = @ModulKod
                  AND (@FormAd IS NULL OR FormAd = @FormAd)
                  AND (@FormAnahtar IS NULL OR FormAnahtar = @FormAnahtar)
                ORDER BY FormAd, FormAnahtar, DosyaNo;";

            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@ModulKod", modulKod);
            command.Parameters.AddWithValue("@FormAd", string.IsNullOrWhiteSpace(formAd) ? DBNull.Value : formAd);
            command.Parameters.AddWithValue("@FormAnahtar", string.IsNullOrWhiteSpace(formAnahtar) ? DBNull.Value : formAnahtar);

            List<DosyaBilgisi> liste = new List<DosyaBilgisi>();
            await using SqlDataReader reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                liste.Add(DosyaBilgisiOku(reader));
            }

            return liste;
        }

        public async Task<Dosya?> GetirAsync(DosyaAnahtari anahtar)
        {
            DosyaBilgisi? bilgi = await BilgiGetirAsync(anahtar);
            if (bilgi == null)
                return null;

            const string sql = @"
                SELECT PaketIcerik
                FROM dbo.DosyaIcerik
                WHERE DosyaId = @DosyaId
                ORDER BY PaketNo;";

            List<byte[]> paketler = new List<byte[]>();
            int toplam = 0;
            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@DosyaId", bilgi.Id);

            await using SqlDataReader reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                byte[] paket = (byte[])reader["PaketIcerik"];
                paketler.Add(paket);
                toplam += paket.Length;
            }

            byte[] icerik = new byte[toplam];
            int konum = 0;
            foreach (byte[] paket in paketler)
            {
                Buffer.BlockCopy(paket, 0, icerik, konum, paket.Length);
                konum += paket.Length;
            }

            return new Dosya
            {
                Id = bilgi.Id,
                ModulKod = bilgi.ModulKod,
                FormAd = bilgi.FormAd,
                FormAnahtar = bilgi.FormAnahtar,
                DosyaNo = bilgi.DosyaNo,
                DosyaAdi = bilgi.DosyaAdi,
                Buyukluk = bilgi.Buyukluk,
                IlkYuklemeTarihi = bilgi.IlkYuklemeTarihi,
                STarihi = bilgi.STarihi,
                Aciklama = bilgi.Aciklama,
                Icerik = icerik
            };
        }

        public async Task<DosyaBilgisi?> BilgiGetirAsync(DosyaAnahtari anahtar)
        {
            const string sql = @"
                SELECT
                    Id,
                    ModulKod,
                    FormAd,
                    FormAnahtar,
                    DosyaNo,
                    DosyaAdi,
                    Buyukluk,
                    IlkYuklemeTarihi,
                    STarihi,
                    Aciklama
                FROM dbo.DosyaBilgisi
                WHERE ModulKod = @ModulKod
                  AND FormAd = @FormAd
                  AND FormAnahtar = @FormAnahtar
                  AND DosyaNo = @DosyaNo;";

            await using SqlCommand command = KomutOlustur(sql);
            AnahtarParametreleriEkle(command, anahtar);

            await using SqlDataReader reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return null;

            return DosyaBilgisiOku(reader);
        }

        public async Task<int> SonrakiDosyaNoAsync(DosyaAnahtari anahtar)
        {
            const string sql = @"
                SELECT ISNULL(MAX(DosyaNo), 0) + 1
                FROM dbo.DosyaBilgisi WITH (UPDLOCK, HOLDLOCK)
                WHERE ModulKod = @ModulKod
                  AND FormAd = @FormAd
                  AND FormAnahtar = @FormAnahtar;";

            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@ModulKod", anahtar.ModulKod);
            command.Parameters.AddWithValue("@FormAd", anahtar.FormAd);
            command.Parameters.AddWithValue("@FormAnahtar", anahtar.FormAnahtar);

            object? sonuc = await command.ExecuteScalarAsync();
            return Convert.ToInt32(sonuc);
        }

        public async Task<int> EkleAsync(DosyaKaydetModel dosya, int dosyaNo)
        {
            const string sql = @"
                INSERT INTO dbo.DosyaBilgisi
                (
                    ModulKod,
                    FormAd,
                    FormAnahtar,
                    DosyaNo,
                    DosyaAdi,
                    Buyukluk,
                    IlkYuklemeTarihi,
                    STarihi,
                    Aciklama
                )
                OUTPUT INSERTED.Id
                VALUES
                (
                    @ModulKod,
                    @FormAd,
                    @FormAnahtar,
                    @DosyaNo,
                    @DosyaAdi,
                    @Buyukluk,
                    @Tarih,
                    @Tarih,
                    @Aciklama
                );";

            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@ModulKod", dosya.ModulKod);
            command.Parameters.AddWithValue("@FormAd", dosya.FormAd);
            command.Parameters.AddWithValue("@FormAnahtar", dosya.FormAnahtar);
            command.Parameters.AddWithValue("@DosyaNo", dosyaNo);
            BilgiParametreleriEkle(command, dosya);

            object? sonuc = await command.ExecuteScalarAsync();
            return Convert.ToInt32(sonuc);
        }

        public async Task GuncelleAsync(int dosyaId, DosyaKaydetModel dosya)
        {
            const string sql = @"
                UPDATE dbo.DosyaBilgisi
                SET DosyaAdi = @DosyaAdi,
                    Buyukluk = @Buyukluk,
                    STarihi = @Tarih,
                    Aciklama = @Aciklama
                WHERE Id = @Id;";

            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@Id", dosyaId);
            BilgiParametreleriEkle(command, dosya);

            await command.ExecuteNonQueryAsync();
        }

        public async Task IcerikSilAsync(int dosyaId)
        {
            const string sql = "DELETE FROM dbo.DosyaIcerik WHERE DosyaId = @DosyaId;";

            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@DosyaId", dosyaId);
            await command.ExecuteNonQueryAsync();
        }

        public async Task IcerikEkleAsync(int dosyaId, int paketNo, byte[] paket)
        {
            const string sql = @"
                INSERT INTO dbo.DosyaIcerik (DosyaId, PaketNo, PaketIcerik)
                VALUES (@DosyaId, @PaketNo, @PaketIcerik);";

            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@DosyaId", dosyaId);
            command.Parameters.AddWithValue("@PaketNo", paketNo);
            SqlParameter icerikParametresi = command.Parameters.Add("@PaketIcerik", SqlDbType.VarBinary, -1);
            icerikParametresi.Value = paket;
            await command.ExecuteNonQueryAsync();
        }

        public async Task SilAsync(int dosyaId)
        {
            await IcerikSilAsync(dosyaId);

            const string sql = "DELETE FROM dbo.DosyaBilgisi WHERE Id = @Id;";
            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@Id", dosyaId);
            await command.ExecuteNonQueryAsync();
        }

        private static DosyaBilgisi DosyaBilgisiOku(SqlDataReader reader)
        {
            return new DosyaBilgisi
            {
                Id = reader.GetInt32(0),
                ModulKod = reader.GetString(1),
                FormAd = reader.GetString(2),
                FormAnahtar = reader.GetString(3),
                DosyaNo = reader.GetInt32(4),
                DosyaAdi = reader.GetString(5),
                Buyukluk = reader.GetInt64(6),
                IlkYuklemeTarihi = reader.GetDateTime(7),
                STarihi = reader.GetDateTime(8),
                Aciklama = NullOkuString(reader, 9)
            };
        }

        private static void AnahtarParametreleriEkle(SqlCommand command, DosyaAnahtari anahtar)
        {
            command.Parameters.AddWithValue("@ModulKod", anahtar.ModulKod);
            command.Parameters.AddWithValue("@FormAd", anahtar.FormAd);
            command.Parameters.AddWithValue("@FormAnahtar", anahtar.FormAnahtar);
            command.Parameters.AddWithValue("@DosyaNo", anahtar.DosyaNo);
        }

        private static void BilgiParametreleriEkle(SqlCommand command, DosyaKaydetModel dosya)
        {
            command.Parameters.AddWithValue("@DosyaAdi", dosya.DosyaAdi);
            command.Parameters.AddWithValue("@Buyukluk", dosya.Icerik.LongLength);
            command.Parameters.AddWithValue("@Tarih", DateTime.Now);
            command.Parameters.AddWithValue("@Aciklama", string.IsNullOrWhiteSpace(dosya.Aciklama) ? DBNull.Value : dosya.Aciklama);
        }
    }
}
