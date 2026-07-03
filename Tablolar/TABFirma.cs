using Microsoft.Data.SqlClient;
using System.Text.Json;
using TarimDonusum.Araclar;
using TarimDonusum.Models;

namespace TarimDonusum.Tablolar
{
    public class TABFirma : TABTablo
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public TABFirma(SqlConnection connection, SqlTransaction? transaction = null)
            : base(connection, transaction)
        {
        }

        public async Task<Firma?> VergiKimlikNoIleOkuAsync(string vergiKimlikNo)
        {
            const string sql = @"
                SELECT
                    f.Id,
                    f.KullaniciId,
                    f.VergiKimlikNo,
                    f.TicaretUnvani,
                    f.TicaretSicilNo,
                    f.KurulusTarihi,
                    f.MersisNo,
                    f.NaceKodu,
                    f.WebSitesi,
                    f.Telefon,
                    f.KepAdresi,
                    f.Eposta,
                    f.FaaliyetKonusu,
                    f.Adres,
                    ISNULL((
                        SELECT
                            fk.KullaniciId,
                            LTRIM(RTRIM(k.Ad + N' ' + k.Soyad)) AS AdSoyad,
                            k.Eposta,
                            k.Telefon,
                            CAST(fk.Aktif AS bit) AS Aktif,
                            fk.IliskiTarihi
                        FROM dbo.FirmaKullanici fk
                        INNER JOIN dbo.Kullanici k ON k.Id = fk.KullaniciId
                        WHERE fk.FirmaId = f.Id
                        FOR JSON PATH
                    ), N'[]') AS BasvuranlarJson
                FROM dbo.Firma f
                WHERE f.VergiKimlikNo = @VergiKimlikNo;";

            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@VergiKimlikNo", vergiKimlikNo?.Trim() ?? "");

            await using SqlDataReader reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return null;

            return Oku(reader);
        }

        public async Task<Firma?> OkuAsync(int id)
        {
            const string sql = @"
                SELECT
                    f.Id,
                    f.KullaniciId,
                    f.VergiKimlikNo,
                    f.TicaretUnvani,
                    f.TicaretSicilNo,
                    f.KurulusTarihi,
                    f.MersisNo,
                    f.NaceKodu,
                    f.WebSitesi,
                    f.Telefon,
                    f.KepAdresi,
                    f.Eposta,
                    f.FaaliyetKonusu,
                    f.Adres,
                    ISNULL((
                        SELECT
                            fk.KullaniciId,
                            LTRIM(RTRIM(k.Ad + N' ' + k.Soyad)) AS AdSoyad,
                            k.Eposta,
                            k.Telefon,
                            CAST(fk.Aktif AS bit) AS Aktif,
                            fk.IliskiTarihi
                        FROM dbo.FirmaKullanici fk
                        INNER JOIN dbo.Kullanici k ON k.Id = fk.KullaniciId
                        WHERE fk.FirmaId = f.Id
                        FOR JSON PATH
                    ), N'[]') AS BasvuranlarJson
                FROM dbo.Firma f
                WHERE f.Id = @Id;";

            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@Id", id);

            await using SqlDataReader reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return null;

            return Oku(reader);
        }

        public async Task<int> EkleAsync(Firma firma)
        {
            const string sql = @"
                INSERT INTO dbo.Firma
                (
                    KullaniciId, VergiKimlikNo, TicaretUnvani, TicaretSicilNo,
                    KurulusTarihi, MersisNo, NaceKodu, WebSitesi, Telefon, KepAdresi,
                    Eposta, FaaliyetKonusu, Adres
                )
                OUTPUT INSERTED.Id
                VALUES
                (
                    @KullaniciId, @VergiKimlikNo, @TicaretUnvani, @TicaretSicilNo,
                    @KurulusTarihi, @MersisNo, @NaceKodu, @WebSitesi, @Telefon, @KepAdresi,
                    @Eposta, @FaaliyetKonusu, @Adres
                );";

            await using SqlCommand command = KomutOlustur(sql);
            ParametreleriEkle(command, firma);

            int id = OrtakFonksiyonlar.Int32Yap(await command.ExecuteScalarAsync());
            firma.Id = id;
            return id;
        }

        public async Task GuncelleAsync(Firma firma)
        {
            const string sql = @"
                UPDATE dbo.Firma
                SET
                    VergiKimlikNo = @VergiKimlikNo,
                    TicaretUnvani = @TicaretUnvani,
                    TicaretSicilNo = @TicaretSicilNo,
                    KurulusTarihi = @KurulusTarihi,
                    MersisNo = @MersisNo,
                    NaceKodu = @NaceKodu,
                    WebSitesi = @WebSitesi,
                    Telefon = @Telefon,
                    KepAdresi = @KepAdresi,
                    Eposta = @Eposta,
                    FaaliyetKonusu = @FaaliyetKonusu,
                    Adres = @Adres
                WHERE Id = @Id;";

            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@Id", firma.Id);
            ParametreleriEkle(command, firma);

            await command.ExecuteNonQueryAsync();
        }

        private static void ParametreleriEkle(SqlCommand command, Firma firma)
        {
            command.Parameters.AddWithValue("@KullaniciId", firma.KullaniciId);
            command.Parameters.AddWithValue("@VergiKimlikNo", firma.VergiKimlikNo ?? "");
            command.Parameters.AddWithValue("@TicaretUnvani", firma.TicaretUnvani ?? "");
            command.Parameters.AddWithValue("@TicaretSicilNo", firma.TicaretSicilNo ?? "");
            command.Parameters.AddWithValue("@KurulusTarihi", (object?)firma.KurulusTarihi ?? DBNull.Value);
            command.Parameters.AddWithValue("@MersisNo", firma.MersisNo ?? "");
            command.Parameters.AddWithValue("@NaceKodu", firma.NaceKodu ?? "");
            command.Parameters.AddWithValue("@WebSitesi", firma.WebSitesi ?? "");
            command.Parameters.AddWithValue("@Telefon", firma.Telefon ?? "");
            command.Parameters.AddWithValue("@KepAdresi", firma.KepAdresi ?? "");
            command.Parameters.AddWithValue("@Eposta", firma.Eposta ?? "");
            command.Parameters.AddWithValue("@FaaliyetKonusu", firma.FaaliyetKonusu ?? "");
            command.Parameters.AddWithValue("@Adres", firma.Adres ?? "");
        }

        private static Firma Oku(SqlDataReader reader)
        {
            return new Firma
            {
                Id = reader.GetInt32(0),
                KullaniciId = reader.GetInt32(1),
                VergiKimlikNo = reader.GetString(2),
                TicaretUnvani = reader.GetString(3),
                TicaretSicilNo = reader.GetString(4),
                KurulusTarihi = reader.IsDBNull(5) ? null : reader.GetDateTime(5),
                MersisNo = reader.GetString(6),
                NaceKodu = reader.GetString(7),
                WebSitesi = reader.GetString(8),
                Telefon = reader.GetString(9),
                KepAdresi = reader.GetString(10),
                Eposta = reader.GetString(11),
                FaaliyetKonusu = reader.GetString(12),
                Adres = reader.GetString(13),
                Basvuranlar = BasvuranlariOku(reader.GetString(14))
            };
        }

        private static List<FirmaBasvuran> BasvuranlariOku(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new List<FirmaBasvuran>();

            return JsonSerializer.Deserialize<List<FirmaBasvuran>>(json, JsonOptions) ?? new List<FirmaBasvuran>();
        }
    }
}
