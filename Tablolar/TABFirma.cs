using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Localization;
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

        public TABFirma(SqlConnection connection, IStringLocalizer<SharedResource>? localizer = null, SqlTransaction? transaction = null)
            : base(connection, localizer, transaction)
        {
        }

        public async Task<Firma?> VergiKimlikNoIleOkuAsync(int firmaId, string vergiKimlikNo)
        {
            string sql = @"SELECT f.Id, f.VergiKimlikNo, f.TicaretUnvani,
                    f.TicaretSicilNo, f.KurulusTarihi, f.MersisNo, f.NaceKodu, f.WebSitesi,
                    f.Telefon, f.KepAdresi, f.Eposta, f.FaaliyetKonusu, f.Adres,
                    ISNULL((SELECT
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
                    ), N'[]') AS BasvuranlarJson,
                    ISNULL((SELECT n.Kod,n.Ad,CAST(n.Aktif AS bit) Aktif FROM dbo.FirmaNace fn INNER JOIN dbo.Nace n ON n.Kod=fn.NaceKodu WHERE fn.FirmaId=f.Id ORDER BY fn.SiraNo FOR JSON PATH),N'[]') AS NaceKodlariJson,
                    ISNULL((SELECT N.Ad FROM dbo.Nace N WHERE N.Kod=f.NaceKodu),N'') AS NaceAdi
                FROM dbo.Firma f WHERE ";

            if (firmaId > 0)
                sql += "f.Id = @Id;";
            else
                sql += "f.VergiKimlikNo = @VergiKimlikNo;";

            await using SqlCommand command = KomutOlustur(sql);

            if (firmaId > 0)
                command.Parameters.AddWithValue("@Id", firmaId);
            else
                command.Parameters.AddWithValue("@VergiKimlikNo", vergiKimlikNo?.Trim() ?? "");

            await using SqlDataReader reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return null;

            return OkuFirma(reader);
        }

        public async Task<Firma?> OkuAsync(int id)
        {
            const string sql = @"
                SELECT
                    f.Id,
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
                    ), N'[]') AS BasvuranlarJson,
                    ISNULL((SELECT n.Kod,n.Ad,CAST(n.Aktif AS bit) Aktif FROM dbo.FirmaNace fn INNER JOIN dbo.Nace n ON n.Kod=fn.NaceKodu WHERE fn.FirmaId=f.Id ORDER BY fn.SiraNo FOR JSON PATH),N'[]') AS NaceKodlariJson,
                    ISNULL((SELECT N.Ad FROM dbo.Nace N WHERE N.Kod=f.NaceKodu),N'') AS NaceAdi
                FROM dbo.Firma f
                WHERE f.Id = @Id;";

            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@Id", id);

            await using SqlDataReader reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return null;

            return OkuFirma(reader);
        }

        public async Task<List<Firma>> AraAsync(string aramaMetni, int? kullaniciId)
        {
            string sql = @"
                SELECT f.Id, f.VergiKimlikNo, f.TicaretUnvani, f.TicaretSicilNo,
                    f.KurulusTarihi, f.MersisNo, f.NaceKodu, f.WebSitesi,
                    f.Telefon, f.KepAdresi, f.Eposta, f.FaaliyetKonusu, f.Adres,
                    N'[]' AS BasvuranlarJson,
                    ISNULL((SELECT n.Kod,n.Ad,CAST(n.Aktif AS bit) Aktif FROM dbo.FirmaNace fn INNER JOIN dbo.Nace n ON n.Kod=fn.NaceKodu WHERE fn.FirmaId=f.Id ORDER BY fn.SiraNo FOR JSON PATH),N'[]') AS NaceKodlariJson,
                    ISNULL((SELECT N.Ad FROM dbo.Nace N WHERE N.Kod=f.NaceKodu),N'') AS NaceAdi
                FROM dbo.Firma f ";
            if (kullaniciId.HasValue)
                sql += @"INNER JOIN dbo.FirmaKullanici fk ON fk.FirmaId=f.Id
                         AND fk.KullaniciId=@KullaniciId AND fk.Aktif=1 ";
            sql += @"WHERE (f.TicaretUnvani LIKE N'%' + @AramaMetni + N'%'
                       OR f.VergiKimlikNo LIKE N'%' + @AramaMetni + N'%'
                       OR f.MersisNo LIKE N'%' + @AramaMetni + N'%')
                ORDER BY f.TicaretUnvani;";

            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@AramaMetni", aramaMetni.Trim());
            if (kullaniciId.HasValue) command.Parameters.AddWithValue("@KullaniciId", kullaniciId.Value);
            await using SqlDataReader reader = await command.ExecuteReaderAsync();
            List<Firma> liste = new();
            while (await reader.ReadAsync())
                liste.Add(OkuFirma(reader));
            return liste;
        }

        public async Task<int> EkleAsync(Firma firma)
        {
            const string sql = @"
                INSERT INTO dbo.Firma
                (
                    VergiKimlikNo, TicaretUnvani, TicaretSicilNo,
                    KurulusTarihi, MersisNo, NaceKodu, WebSitesi, Telefon, KepAdresi,
                    Eposta, FaaliyetKonusu, Adres
                )
                OUTPUT INSERTED.Id
                VALUES
                (
                    @VergiKimlikNo, @TicaretUnvani, @TicaretSicilNo,
                    @KurulusTarihi, @MersisNo, @NaceKodu, @WebSitesi, @Telefon, @KepAdresi,
                    @Eposta, @FaaliyetKonusu, @Adres
                );";

            await using SqlCommand command = KomutOlustur(sql);
            ParametreleriEkle(command, firma);

            int id = OrtakFonksiyonlar.Int32Yap(await command.ExecuteScalarAsync());
            firma.id = id;
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
            command.Parameters.AddWithValue("@Id", firma.id);
            ParametreleriEkle(command, firma);

            await command.ExecuteNonQueryAsync();
        }

        public async Task NaceKodlariniKaydetAsync(Firma firma)
        {
            await using SqlCommand sil = KomutOlustur("DELETE FROM dbo.FirmaNace WHERE FirmaId=@FirmaId;");
            sil.Parameters.AddWithValue("@FirmaId", firma.id);
            await sil.ExecuteNonQueryAsync();
            int siraNo = 1;
            foreach (Nace nace in firma.naceKodlari)
            {
                await using SqlCommand ekle = KomutOlustur("INSERT dbo.FirmaNace(FirmaId,NaceKodu,SiraNo) VALUES(@FirmaId,@NaceKodu,@SiraNo);");
                ekle.Parameters.AddWithValue("@FirmaId", firma.id);
                ekle.Parameters.AddWithValue("@NaceKodu", nace.kod);
                ekle.Parameters.AddWithValue("@SiraNo", siraNo++);
                await ekle.ExecuteNonQueryAsync();
            }
        }

        private static void ParametreleriEkle(SqlCommand command, Firma firma)
        {
            command.Parameters.AddWithValue("@VergiKimlikNo", firma.vergiKimlikNo ?? "");
            command.Parameters.AddWithValue("@TicaretUnvani", firma.ticaretUnvani ?? "");
            command.Parameters.AddWithValue("@TicaretSicilNo", firma.ticaretSicilNo ?? "");
            command.Parameters.AddWithValue("@KurulusTarihi", (object?)firma.kurulusTarihi ?? DBNull.Value);
            command.Parameters.AddWithValue("@MersisNo", firma.mersisNo ?? "");
            command.Parameters.AddWithValue("@NaceKodu", string.IsNullOrWhiteSpace(firma.naceKodu) ? DBNull.Value : firma.naceKodu);
            command.Parameters.AddWithValue("@WebSitesi", firma.webSitesi ?? "");
            command.Parameters.AddWithValue("@Telefon", firma.telefon ?? "");
            command.Parameters.AddWithValue("@KepAdresi", firma.kepAdresi ?? "");
            command.Parameters.AddWithValue("@Eposta", firma.eposta ?? "");
            command.Parameters.AddWithValue("@FaaliyetKonusu", firma.faaliyetKonusu ?? "");
            command.Parameters.AddWithValue("@Adres", firma.adres ?? "");
        }

        private static Firma OkuFirma(SqlDataReader reader)
        {
            return new Firma
            {
                id = reader.GetInt32(0),
                vergiKimlikNo = reader.GetString(1),
                ticaretUnvani = reader.GetString(2),
                ticaretSicilNo = reader.GetString(3),
                kurulusTarihi = reader.IsDBNull(4) ? null : reader.GetDateTime(4),
                mersisNo = reader.GetString(5),
                naceKodu = reader.IsDBNull(6) ? "" : reader.GetString(6),
                webSitesi = reader.GetString(7),
                telefon = reader.GetString(8),
                kepAdresi = reader.GetString(9),
                eposta = reader.GetString(10),
                faaliyetKonusu = reader.GetString(11),
                adres = reader.GetString(12),
                basvuranlar = BasvuranlariOku(reader.GetString(13)),
                naceKodlari = JsonSerializer.Deserialize<List<Nace>>(reader.GetString(14), JsonOptions) ?? new List<Nace>(),
                naceAdi = reader.GetString(15)
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
