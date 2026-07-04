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
            command.Parameters.AddWithValue("@VergiKimlikNo", firma.vergiKimlikNo ?? "");
            command.Parameters.AddWithValue("@TicaretUnvani", firma.ticaretUnvani ?? "");
            command.Parameters.AddWithValue("@TicaretSicilNo", firma.ticaretSicilNo ?? "");
            command.Parameters.AddWithValue("@KurulusTarihi", (object?)firma.kurulusTarihi ?? DBNull.Value);
            command.Parameters.AddWithValue("@MersisNo", firma.mersisNo ?? "");
            command.Parameters.AddWithValue("@NaceKodu", firma.naceKodu ?? "");
            command.Parameters.AddWithValue("@WebSitesi", firma.webSitesi ?? "");
            command.Parameters.AddWithValue("@Telefon", firma.telefon ?? "");
            command.Parameters.AddWithValue("@KepAdresi", firma.kepAdresi ?? "");
            command.Parameters.AddWithValue("@Eposta", firma.eposta ?? "");
            command.Parameters.AddWithValue("@FaaliyetKonusu", firma.faaliyetKonusu ?? "");
            command.Parameters.AddWithValue("@Adres", firma.adres ?? "");
        }

        private static Firma Oku(SqlDataReader reader)
        {
            return new Firma
            {
                Id = reader.GetInt32(0),
                vergiKimlikNo = reader.GetString(2),
                ticaretUnvani = reader.GetString(3),
                ticaretSicilNo = reader.GetString(4),
                kurulusTarihi = reader.IsDBNull(5) ? null : reader.GetDateTime(5),
                mersisNo = reader.GetString(6),
                naceKodu = reader.GetString(7),
                webSitesi = reader.GetString(8),
                telefon = reader.GetString(9),
                kepAdresi = reader.GetString(10),
                eposta = reader.GetString(11),
                faaliyetKonusu = reader.GetString(12),
                adres = reader.GetString(13),
                basvuranlar = BasvuranlariOku(reader.GetString(14))
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
