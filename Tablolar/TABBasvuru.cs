using System.Text.Json;
using Microsoft.Data.SqlClient;
using TarimDonusum.Araclar;
using TarimDonusum.Models;

namespace TarimDonusum.Tablolar
{
    public class TABBasvuru : TABTablo
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public TABBasvuru(SqlConnection connection, SqlTransaction? transaction = null)
            : base(connection, transaction)
        {
        }

        public async Task<int> KaydetAsync(Basvuru basvuru)
        {
            if (basvuru.Id <= 0)
            {
                await EkleAsync(basvuru);
                await DetaylariKaydetAsync(basvuru);
                return basvuru.Id;
            }

            await GuncelleAsync(basvuru);
            await DetaylariKaydetAsync(basvuru);
            return basvuru.Id;
        }

        public async Task<int> KaydetAsama1Async(Basvuru basvuru)
        {
            if (basvuru.Id <= 0)
                return await EkleAsama1Async(basvuru);

            await GuncelleAsama1Async(basvuru);
            return basvuru.Id;
        }

        public async Task<Basvuru?> OkuAsync(int id)
        {
            const string sql = @"
                SELECT
                    Id,
                    KullaniciId,
                    Durum,
                    AktifBolum,
                    KayitTarihi,
                    GuncellemeTarihi,
                    JsonText,
                    OzelSektorPayi,
                    BagliOrtakIsletmeVarMi,
                    BagliOrtakAciklama,
                    FirmaId,
                    DonemId,
                    IlId,
                    BasvuruKonusu,
                    YatirimAdi,
                    YatirimTuru,
                    ToplamYatirimTutari,
                    UygunHarcamaTutari,
                    TalepEdilenDestekTutari,
                    BasvuruSahibiKatkisi,
                    DestekOrani,
                    YatiriminAmaci
                FROM dbo.Basvuru
                WHERE Id = @Id;";

            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@Id", id);

            Basvuru basvuru;
            await using (SqlDataReader reader = await command.ExecuteReaderAsync())
            {
                if (!await reader.ReadAsync())
                    return null;

                basvuru = Oku(reader);
            }

            await DetaylariYukleAsync(basvuru);
            return basvuru;
        }

        public async Task<List<Basvuru>> KullaniciBasvurulariniListeleAsync(int kullaniciId)
        {
            const string sql = @"
                SELECT
                    Id,
                    KullaniciId,
                    Durum,
                    AktifBolum,
                    KayitTarihi,
                    GuncellemeTarihi,
                    JsonText,
                    OzelSektorPayi,
                    BagliOrtakIsletmeVarMi,
                    BagliOrtakAciklama,
                    FirmaId,
                    DonemId,
                    IlId,
                    BasvuruKonusu,
                    YatirimAdi,
                    YatirimTuru,
                    ToplamYatirimTutari,
                    UygunHarcamaTutari,
                    TalepEdilenDestekTutari,
                    BasvuruSahibiKatkisi,
                    DestekOrani,
                    YatiriminAmaci
                FROM dbo.Basvuru
                WHERE KullaniciId = @KullaniciId
                ORDER BY GuncellemeTarihi DESC, Id DESC;";

            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@KullaniciId", kullaniciId);

            return await ListeOkuAsync(command);
        }

        public async Task<List<Basvuru>> TumunuListeleAsync()
        {
            const string sql = @"
                SELECT
                    Id,
                    KullaniciId,
                    Durum,
                    AktifBolum,
                    KayitTarihi,
                    GuncellemeTarihi,
                    JsonText,
                    OzelSektorPayi,
                    BagliOrtakIsletmeVarMi,
                    BagliOrtakAciklama,
                    FirmaId,
                    DonemId,
                    IlId,
                    BasvuruKonusu,
                    YatirimAdi,
                    YatirimTuru,
                    ToplamYatirimTutari,
                    UygunHarcamaTutari,
                    TalepEdilenDestekTutari,
                    BasvuruSahibiKatkisi,
                    DestekOrani,
                    YatiriminAmaci
                FROM dbo.Basvuru
                ORDER BY GuncellemeTarihi DESC, Id DESC;";

            await using SqlCommand command = KomutOlustur(sql);
            return await ListeOkuAsync(command);
        }

        private async Task<int> EkleAsync(Basvuru basvuru)
        {
            const string sql = @"
                INSERT INTO dbo.Basvuru
                (
                    KullaniciId, FirmaId, DonemId, IlId, BasvuruKonusu, YatirimAdi, YatirimTuru,
                    ToplamYatirimTutari, UygunHarcamaTutari, TalepEdilenDestekTutari,
                    BasvuruSahibiKatkisi, DestekOrani, YatiriminAmaci,
                    OzelSektorPayi, BagliOrtakIsletmeVarMi, BagliOrtakAciklama,
                    Durum, AktifBolum, KayitTarihi, GuncellemeTarihi, JsonText
                )
                OUTPUT INSERTED.Id
                VALUES
                (
                    @KullaniciId, @FirmaId, @DonemId, @IlId, @BasvuruKonusu, @YatirimAdi, @YatirimTuru,
                    @ToplamYatirimTutari, @UygunHarcamaTutari, @TalepEdilenDestekTutari,
                    @BasvuruSahibiKatkisi, @DestekOrani, @YatiriminAmaci,
                    @OzelSektorPayi, @BagliOrtakIsletmeVarMi, @BagliOrtakAciklama,
                    @Durum, @AktifBolum, @KayitTarihi, @GuncellemeTarihi, @JsonText
                );";

            basvuru.KayitTarihi = DateTime.Now;
            basvuru.GuncellemeTarihi = DateTime.Now;

            await using SqlCommand command = KomutOlustur(sql);
            ParametreleriEkle(command, basvuru);

            int id = OrtakFonksiyonlar.Int32Yap(await command.ExecuteScalarAsync());
            basvuru.Id = id;
            return id;
        }

        private async Task<int> EkleAsama1Async(Basvuru basvuru)
        {
            const string sql = @"
                INSERT INTO dbo.Basvuru
                (
                    KullaniciId, FirmaId, DonemId, IlId, BasvuruKonusu,
                    YatirimAdi, YatirimTuru,
                    OzelSektorPayi, BagliOrtakIsletmeVarMi, BagliOrtakAciklama,
                    Durum, AktifBolum, KayitTarihi, GuncellemeTarihi, JsonText
                )
                OUTPUT INSERTED.Id
                VALUES
                (
                    @KullaniciId, @FirmaId, @DonemId, @IlId, @BasvuruKonusu,
                    N'', N'',
                    @OzelSektorPayi, @BagliOrtakIsletmeVarMi, @BagliOrtakAciklama,
                    @Durum, @AktifBolum, @KayitTarihi, @GuncellemeTarihi, @JsonText
                );";

            basvuru.KayitTarihi = DateTime.Now;
            basvuru.GuncellemeTarihi = DateTime.Now;

            await using SqlCommand command = KomutOlustur(sql);
            Asama1ParametreleriEkle(command, basvuru, kayitTarihiEkle: true);

            int id = OrtakFonksiyonlar.Int32Yap(await command.ExecuteScalarAsync());
            basvuru.Id = id;
            return id;
        }

        private async Task GuncelleAsync(Basvuru basvuru)
        {
            const string sql = @"
                UPDATE dbo.Basvuru
                SET
                    FirmaId = @FirmaId,
                    DonemId = @DonemId,
                    IlId = @IlId,
                    BasvuruKonusu = @BasvuruKonusu,
                    YatirimAdi = @YatirimAdi,
                    YatirimTuru = @YatirimTuru,
                    ToplamYatirimTutari = @ToplamYatirimTutari,
                    UygunHarcamaTutari = @UygunHarcamaTutari,
                    TalepEdilenDestekTutari = @TalepEdilenDestekTutari,
                    BasvuruSahibiKatkisi = @BasvuruSahibiKatkisi,
                    DestekOrani = @DestekOrani,
                    YatiriminAmaci = @YatiriminAmaci,
                    OzelSektorPayi = @OzelSektorPayi,
                    BagliOrtakIsletmeVarMi = @BagliOrtakIsletmeVarMi,
                    BagliOrtakAciklama = @BagliOrtakAciklama,
                    Durum = @Durum,
                    AktifBolum = @AktifBolum,
                    GuncellemeTarihi = @GuncellemeTarihi,
                    JsonText = @JsonText
                WHERE Id = @Id;";

            basvuru.GuncellemeTarihi = DateTime.Now;

            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@Id", basvuru.Id);
            ParametreleriEkle(command, basvuru);

            await command.ExecuteNonQueryAsync();
        }

        private async Task GuncelleAsama1Async(Basvuru basvuru)
        {
            const string sql = @"
                UPDATE dbo.Basvuru
                SET
                    FirmaId = @FirmaId,
                    DonemId = @DonemId,
                    IlId = @IlId,
                    BasvuruKonusu = @BasvuruKonusu,
                    OzelSektorPayi = @OzelSektorPayi,
                    BagliOrtakIsletmeVarMi = @BagliOrtakIsletmeVarMi,
                    BagliOrtakAciklama = @BagliOrtakAciklama,
                    Durum = @Durum,
                    AktifBolum = @AktifBolum,
                    GuncellemeTarihi = @GuncellemeTarihi,
                    JsonText = @JsonText
                WHERE Id = @Id;";

            basvuru.GuncellemeTarihi = DateTime.Now;

            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@Id", basvuru.Id);
            Asama1ParametreleriEkle(command, basvuru, kayitTarihiEkle: false);

            await command.ExecuteNonQueryAsync();
        }

        public async Task OrtaklikKaydetAsync(Basvuru basvuru)
        {
            const string sql = @"
                UPDATE dbo.Basvuru
                SET
                    OzelSektorPayi = @OzelSektorPayi,
                    BagliOrtakIsletmeVarMi = @BagliOrtakIsletmeVarMi,
                    BagliOrtakAciklama = @BagliOrtakAciklama,
                    AktifBolum = @AktifBolum,
                    GuncellemeTarihi = @GuncellemeTarihi,
                    JsonText = @JsonText
                WHERE Id = @Id;";

            basvuru.GuncellemeTarihi = DateTime.Now;

            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@Id", basvuru.Id);
            command.Parameters.AddWithValue("@OzelSektorPayi", (object?)basvuru.OzelSektorPayi ?? DBNull.Value);
            command.Parameters.AddWithValue("@BagliOrtakIsletmeVarMi", string.IsNullOrWhiteSpace(basvuru.BagliOrtakIsletmeVarMi) ? DBNull.Value : basvuru.BagliOrtakIsletmeVarMi);
            command.Parameters.AddWithValue("@BagliOrtakAciklama", string.IsNullOrWhiteSpace(basvuru.BagliOrtakAciklama) ? DBNull.Value : basvuru.BagliOrtakAciklama);
            command.Parameters.AddWithValue("@AktifBolum", basvuru.AktifBolum);
            command.Parameters.AddWithValue("@GuncellemeTarihi", basvuru.GuncellemeTarihi);
            command.Parameters.AddWithValue("@JsonText", JsonSerializer.Serialize(basvuru, JsonOptions));

            await command.ExecuteNonQueryAsync();
        }

        private static void ParametreleriEkle(SqlCommand command, Basvuru basvuru)
        {
            command.Parameters.AddWithValue("@KullaniciId", basvuru.KullaniciId);
            command.Parameters.AddWithValue("@FirmaId", (object?)basvuru.FirmaId ?? DBNull.Value);
            command.Parameters.AddWithValue("@DonemId", (object?)basvuru.DonemId ?? DBNull.Value);
            command.Parameters.AddWithValue("@IlId", (object?)basvuru.IlId ?? DBNull.Value);
            command.Parameters.AddWithValue("@BasvuruKonusu", basvuru.BasvuruKonusu ?? "");
            command.Parameters.AddWithValue("@YatirimAdi", basvuru.YatirimAdi ?? "");
            command.Parameters.AddWithValue("@YatirimTuru", basvuru.YatirimTuru ?? "");
            command.Parameters.AddWithValue("@ToplamYatirimTutari", (object?)basvuru.ToplamYatirimTutari ?? DBNull.Value);
            command.Parameters.AddWithValue("@UygunHarcamaTutari", (object?)basvuru.UygunHarcamaTutari ?? DBNull.Value);
            command.Parameters.AddWithValue("@TalepEdilenDestekTutari", (object?)basvuru.TalepEdilenDestekTutari ?? DBNull.Value);
            command.Parameters.AddWithValue("@BasvuruSahibiKatkisi", (object?)basvuru.BasvuruSahibiKatkisi ?? DBNull.Value);
            command.Parameters.AddWithValue("@DestekOrani", (object?)basvuru.DestekOrani ?? DBNull.Value);
            command.Parameters.AddWithValue("@YatiriminAmaci", basvuru.YatiriminAmaci ?? "");
            command.Parameters.AddWithValue("@OzelSektorPayi", (object?)basvuru.OzelSektorPayi ?? DBNull.Value);
            command.Parameters.AddWithValue("@BagliOrtakIsletmeVarMi", string.IsNullOrWhiteSpace(basvuru.BagliOrtakIsletmeVarMi) ? DBNull.Value : basvuru.BagliOrtakIsletmeVarMi);
            command.Parameters.AddWithValue("@BagliOrtakAciklama", string.IsNullOrWhiteSpace(basvuru.BagliOrtakAciklama) ? DBNull.Value : basvuru.BagliOrtakAciklama);
            command.Parameters.AddWithValue("@Durum", basvuru.Durum ?? "Ön Başvuru");
            command.Parameters.AddWithValue("@AktifBolum", basvuru.AktifBolum);
            command.Parameters.AddWithValue("@KayitTarihi", basvuru.KayitTarihi);
            command.Parameters.AddWithValue("@GuncellemeTarihi", basvuru.GuncellemeTarihi);
            command.Parameters.AddWithValue("@JsonText", JsonSerializer.Serialize(basvuru, JsonOptions));
        }

        private static void Asama1ParametreleriEkle(SqlCommand command, Basvuru basvuru, bool kayitTarihiEkle)
        {
            command.Parameters.AddWithValue("@KullaniciId", basvuru.KullaniciId);
            command.Parameters.AddWithValue("@FirmaId", (object?)basvuru.FirmaId ?? DBNull.Value);
            command.Parameters.AddWithValue("@DonemId", (object?)basvuru.DonemId ?? DBNull.Value);
            command.Parameters.AddWithValue("@IlId", (object?)basvuru.IlId ?? DBNull.Value);
            command.Parameters.AddWithValue("@BasvuruKonusu", basvuru.BasvuruKonusu ?? "");
            command.Parameters.AddWithValue("@OzelSektorPayi", (object?)basvuru.OzelSektorPayi ?? DBNull.Value);
            command.Parameters.AddWithValue("@BagliOrtakIsletmeVarMi", string.IsNullOrWhiteSpace(basvuru.BagliOrtakIsletmeVarMi) ? DBNull.Value : basvuru.BagliOrtakIsletmeVarMi);
            command.Parameters.AddWithValue("@BagliOrtakAciklama", string.IsNullOrWhiteSpace(basvuru.BagliOrtakAciklama) ? DBNull.Value : basvuru.BagliOrtakAciklama);
            command.Parameters.AddWithValue("@Durum", basvuru.Durum ?? "Ön Başvuru");
            command.Parameters.AddWithValue("@AktifBolum", basvuru.AktifBolum);
            command.Parameters.AddWithValue("@GuncellemeTarihi", basvuru.GuncellemeTarihi);
            command.Parameters.AddWithValue("@JsonText", JsonSerializer.Serialize(basvuru, JsonOptions));

            if (kayitTarihiEkle)
                command.Parameters.AddWithValue("@KayitTarihi", basvuru.KayitTarihi);
        }

        private async Task<List<Basvuru>> ListeOkuAsync(SqlCommand command)
        {
            await using SqlDataReader reader = await command.ExecuteReaderAsync();

            List<Basvuru> liste = new List<Basvuru>();
            while (await reader.ReadAsync())
            {
                liste.Add(Oku(reader));
            }

            return liste;
        }

        private static Basvuru Oku(SqlDataReader reader)
        {
            string jsonText = reader.GetString(6);
            Basvuru? basvuru = JsonSerializer.Deserialize<Basvuru>(jsonText, JsonOptions);
            basvuru ??= new Basvuru();

            basvuru.Id = reader.GetInt32(0);
            basvuru.KullaniciId = reader.GetInt32(1);
            basvuru.Durum = reader.GetString(2);
            basvuru.AktifBolum = reader.GetInt32(3);
            basvuru.KayitTarihi = reader.GetDateTime(4);
            basvuru.GuncellemeTarihi = reader.GetDateTime(5);
            basvuru.OzelSektorPayi = reader.IsDBNull(7) ? null : reader.GetDecimal(7);
            basvuru.BagliOrtakIsletmeVarMi = reader.IsDBNull(8) ? "" : reader.GetString(8);
            basvuru.BagliOrtakAciklama = reader.IsDBNull(9) ? "" : reader.GetString(9);
            basvuru.FirmaId = reader.IsDBNull(10) ? null : reader.GetInt32(10);
            basvuru.DonemId = reader.IsDBNull(11) ? null : reader.GetInt32(11);
            basvuru.IlId = reader.IsDBNull(12) ? null : reader.GetInt32(12);
            basvuru.BasvuruKonusu = reader.IsDBNull(13) ? basvuru.BasvuruKonusu : reader.GetString(13);
            basvuru.YatirimAdi = reader.IsDBNull(14) ? basvuru.YatirimAdi : reader.GetString(14);
            basvuru.YatirimTuru = reader.IsDBNull(15) ? basvuru.YatirimTuru : reader.GetString(15);
            basvuru.ToplamYatirimTutari = reader.IsDBNull(16) ? null : reader.GetDecimal(16);
            basvuru.UygunHarcamaTutari = reader.IsDBNull(17) ? null : reader.GetDecimal(17);
            basvuru.TalepEdilenDestekTutari = reader.IsDBNull(18) ? null : reader.GetDecimal(18);
            basvuru.BasvuruSahibiKatkisi = reader.IsDBNull(19) ? null : reader.GetDecimal(19);
            basvuru.DestekOrani = reader.IsDBNull(20) ? null : reader.GetDecimal(20);
            basvuru.YatiriminAmaci = reader.IsDBNull(21) ? "" : reader.GetString(21);

            return basvuru;
        }

        private async Task DetaylariKaydetAsync(Basvuru basvuru)
        {
            await BasvuruSecimDetaylariniSilAsync(basvuru.Id);
            await HarcamaTurleriEkleAsync(basvuru);
            await DegerZinciriAsamalariEkleAsync(basvuru);
        }

        private async Task BasvuruSecimDetaylariniSilAsync(int basvuruId)
        {
            const string sql = @"
                DELETE FROM dbo.BasvuruHarcamaTuru WHERE BasvuruId = @BasvuruId;
                DELETE FROM dbo.BasvuruDegerZinciriAsama WHERE BasvuruId = @BasvuruId;";

            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@BasvuruId", basvuruId);
            await command.ExecuteNonQueryAsync();
        }

        public async Task<BasvuruUygulamaAdresi?> UygulamaAdresiOkuAsync(int basvuruId, int adresId)
        {
            const string sql = @"
                SELECT
                    bua.Id,
                    bua.BasvuruId,
                    bua.SiraNo,
                    bua.IlceId,
                    il.Id AS IlId,
                    il.Kod AS IlKod,
                    il.Ad AS IlAdi,
                    ilce.Ad AS IlceAdi,
                    bua.TamAdres,
                    bua.YatirimYeriStatusu,
                    bua.KiraVeyaTahsisSuresi,
                    bua.KiraTahsisBitisTarihi,
                    bua.YapiRuhsatiDurumu
                FROM dbo.BasvuruUygulamaAdresleri bua
                LEFT JOIN dbo.Ilce ilce ON ilce.Id = bua.IlceId
                LEFT JOIN dbo.Il il ON il.Id = ilce.IlId
                WHERE bua.BasvuruId = @BasvuruId
                    AND bua.Id = @Id;";

            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@BasvuruId", basvuruId);
            command.Parameters.AddWithValue("@Id", adresId);

            await using SqlDataReader reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return null;

            return UygulamaAdresiOku(reader);
        }

        public async Task<int> UygulamaAdresiKaydetAsync(BasvuruUygulamaAdresi adres)
        {
            if (adres.Id <= 0)
                return await UygulamaAdresiEkleAsync(adres);

            await UygulamaAdresiGuncelleAsync(adres);
            return adres.Id;
        }

        public async Task UygulamaAdresiSilAsync(int basvuruId, int adresId)
        {
            const string sql = @"
                DELETE FROM dbo.BasvuruUygulamaAdresleri
                WHERE BasvuruId = @BasvuruId
                    AND Id = @Id;";

            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@BasvuruId", basvuruId);
            command.Parameters.AddWithValue("@Id", adresId);
            await command.ExecuteNonQueryAsync();
        }

        private async Task<int> UygulamaAdresiEkleAsync(BasvuruUygulamaAdresi adres)
        {
            const string sql = @"
                INSERT INTO dbo.BasvuruUygulamaAdresleri
                    (BasvuruId, SiraNo, IlceId, TamAdres, YatirimYeriStatusu, KiraVeyaTahsisSuresi, KiraTahsisBitisTarihi, YapiRuhsatiDurumu)
                OUTPUT INSERTED.Id
                VALUES
                    (@BasvuruId, @SiraNo, @IlceId, @TamAdres, @YatirimYeriStatusu, @KiraVeyaTahsisSuresi, @KiraTahsisBitisTarihi, @YapiRuhsatiDurumu);";

            await using SqlCommand command = KomutOlustur(sql);
            UygulamaAdresiParametreleriEkle(command, adres);

            int id = OrtakFonksiyonlar.Int32Yap(await command.ExecuteScalarAsync());
            adres.Id = id;
            return id;
        }

        private async Task UygulamaAdresiGuncelleAsync(BasvuruUygulamaAdresi adres)
        {
            const string sql = @"
                UPDATE dbo.BasvuruUygulamaAdresleri
                SET
                    SiraNo = @SiraNo,
                    IlceId = @IlceId,
                    TamAdres = @TamAdres,
                    YatirimYeriStatusu = @YatirimYeriStatusu,
                    KiraVeyaTahsisSuresi = @KiraVeyaTahsisSuresi,
                    KiraTahsisBitisTarihi = @KiraTahsisBitisTarihi,
                    YapiRuhsatiDurumu = @YapiRuhsatiDurumu
                WHERE Id = @Id
                    AND BasvuruId = @BasvuruId;";

            await using SqlCommand command = KomutOlustur(sql);
            UygulamaAdresiParametreleriEkle(command, adres);
            command.Parameters.AddWithValue("@Id", adres.Id);
            await command.ExecuteNonQueryAsync();
        }

        private static void UygulamaAdresiParametreleriEkle(SqlCommand command, BasvuruUygulamaAdresi adres)
        {
            command.Parameters.AddWithValue("@BasvuruId", adres.BasvuruId);
            command.Parameters.AddWithValue("@SiraNo", adres.SiraNo);
            command.Parameters.AddWithValue("@IlceId", adres.IlceId.HasValue && adres.IlceId.Value > 0 ? adres.IlceId.Value : (object)DBNull.Value);
            command.Parameters.AddWithValue("@TamAdres", adres.TamAdres?.Trim() ?? "");
            command.Parameters.AddWithValue("@YatirimYeriStatusu", adres.YatirimYeriStatusu.HasValue ? (int)adres.YatirimYeriStatusu.Value : (object)DBNull.Value);
            command.Parameters.AddWithValue("@KiraVeyaTahsisSuresi", adres.KiraVeyaTahsisSuresi.HasValue ? adres.KiraVeyaTahsisSuresi.Value : (object)DBNull.Value);
            command.Parameters.AddWithValue("@KiraTahsisBitisTarihi", adres.KiraTahsisBitisTarihi.HasValue ? adres.KiraTahsisBitisTarihi.Value.Date : (object)DBNull.Value);
            command.Parameters.AddWithValue("@YapiRuhsatiDurumu", adres.YapiRuhsatiDurumu.HasValue ? (int)adres.YapiRuhsatiDurumu.Value : (object)DBNull.Value);
        }

        private async Task HarcamaTurleriEkleAsync(Basvuru basvuru)
        {
            const string sql = @"
                INSERT INTO dbo.BasvuruHarcamaTuru(BasvuruId, HarcamaTuru)
                VALUES (@BasvuruId, @HarcamaTuru);";

            foreach (string harcamaTuru in basvuru.HarcamaTurleri.Select(x => x?.Trim() ?? "").Where(x => x.Length > 0).Distinct())
            {
                await using SqlCommand command = KomutOlustur(sql);
                command.Parameters.AddWithValue("@BasvuruId", basvuru.Id);
                command.Parameters.AddWithValue("@HarcamaTuru", harcamaTuru);
                await command.ExecuteNonQueryAsync();
            }
        }

        private async Task DegerZinciriAsamalariEkleAsync(Basvuru basvuru)
        {
            const string sql = @"
                INSERT INTO dbo.BasvuruDegerZinciriAsama(BasvuruId, DegerZinciriAsamaId, AsamaAdi)
                SELECT @BasvuruId, (
                    SELECT TOP 1 Id
                    FROM dbo.DegerZinciriAsama
                    WHERE Ad = @AsamaAdi
                        AND (@DegerZinciriId IS NULL OR DegerZinciriId = @DegerZinciriId)
                    ORDER BY Id
                ), @AsamaAdi;";

            foreach (string asamaAdi in basvuru.DegerZinciriAsamalari.Select(x => x?.Trim() ?? "").Where(x => x.Length > 0).Distinct())
            {
                await using SqlCommand command = KomutOlustur(sql);
                command.Parameters.AddWithValue("@BasvuruId", basvuru.Id);
                command.Parameters.AddWithValue("@DegerZinciriId", basvuru.DegerZinciriId.HasValue ? basvuru.DegerZinciriId.Value : DBNull.Value);
                command.Parameters.AddWithValue("@AsamaAdi", asamaAdi);
                await command.ExecuteNonQueryAsync();
            }
        }

        private async Task DetaylariYukleAsync(Basvuru basvuru)
        {
            const string sql = @"
                SELECT
                    bua.Id,
                    bua.BasvuruId,
                    bua.SiraNo,
                    bua.IlceId,
                    il.Id AS IlId,
                    il.Kod AS IlKod,
                    il.Ad AS IlAdi,
                    ilce.Ad AS IlceAdi,
                    bua.TamAdres,
                    bua.YatirimYeriStatusu,
                    bua.KiraVeyaTahsisSuresi,
                    bua.KiraTahsisBitisTarihi,
                    bua.YapiRuhsatiDurumu
                FROM dbo.BasvuruUygulamaAdresleri bua
                LEFT JOIN dbo.Ilce ilce ON ilce.Id = bua.IlceId
                LEFT JOIN dbo.Il il ON il.Id = ilce.IlId
                WHERE bua.BasvuruId = @BasvuruId
                ORDER BY bua.SiraNo;

                SELECT HarcamaTuru
                FROM dbo.BasvuruHarcamaTuru
                WHERE BasvuruId = @BasvuruId
                ORDER BY HarcamaTuru;

                SELECT bdza.AsamaAdi, dza.DegerZinciriId
                FROM dbo.BasvuruDegerZinciriAsama bdza
                LEFT JOIN dbo.DegerZinciriAsama dza ON dza.Id = bdza.DegerZinciriAsamaId
                WHERE bdza.BasvuruId = @BasvuruId
                ORDER BY bdza.Id;";

            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@BasvuruId", basvuru.Id);
            await using SqlDataReader reader = await command.ExecuteReaderAsync();

            List<BasvuruUygulamaAdresi> adresler = new List<BasvuruUygulamaAdresi>();
            while (await reader.ReadAsync())
            {
                adresler.Add(UygulamaAdresiOku(reader));
            }

            await reader.NextResultAsync();
            List<string> harcamaTurleri = new List<string>();
            while (await reader.ReadAsync())
            {
                harcamaTurleri.Add(reader.GetString(0));
            }

            await reader.NextResultAsync();
            List<string> asamalar = new List<string>();
            List<int> degerZinciriIdleri = new List<int>();
            while (await reader.ReadAsync())
            {
                asamalar.Add(reader.GetString(0));
                if (!reader.IsDBNull(1))
                    degerZinciriIdleri.Add(reader.GetInt32(1));
            }

            basvuru.YatirimAdresleri = adresler;
            basvuru.YatirimAdresSayisi = adresler.Count;

            if (harcamaTurleri.Count > 0)
                basvuru.HarcamaTurleri = harcamaTurleri;

            if (asamalar.Count > 0)
            {
                basvuru.DegerZinciriAsamalari = asamalar;
                basvuru.DegerZinciriId = degerZinciriIdleri.Distinct().Count() == 1
                    ? degerZinciriIdleri.First()
                    : null;
            }
        }

        private static BasvuruUygulamaAdresi UygulamaAdresiOku(SqlDataReader reader)
        {
            return new BasvuruUygulamaAdresi
            {
                Id = reader.GetInt32(0),
                BasvuruId = reader.GetInt32(1),
                SiraNo = reader.GetInt32(2),
                IlceId = reader.IsDBNull(3) ? null : reader.GetInt32(3),
                IlId = reader.IsDBNull(4) ? null : reader.GetInt32(4),
                IlKod = reader.IsDBNull(5) ? null : OrtakFonksiyonlar.Int32Yap(reader.GetValue(5)),
                IlAdi = reader.IsDBNull(6) ? "" : reader.GetString(6),
                IlceAdi = reader.IsDBNull(7) ? "" : reader.GetString(7),
                TamAdres = reader.GetString(8),
                YatirimYeriStatusu = reader.IsDBNull(9) ? null : (UygulamaAdresiYatirimYeriStatusu)reader.GetInt32(9),
                KiraVeyaTahsisSuresi = reader.IsDBNull(10) ? null : reader.GetInt32(10),
                KiraTahsisBitisTarihi = reader.IsDBNull(11) ? null : reader.GetDateTime(11),
                YapiRuhsatiDurumu = reader.IsDBNull(12) ? null : (UygulamaAdresiYapiRuhsatiDurumu)reader.GetInt32(12)
            };
        }
    }
}

