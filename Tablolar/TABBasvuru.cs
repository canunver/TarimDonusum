using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Localization;
using Microsoft.IdentityModel.Tokens;
using TarimDonusum.Araclar;
using TarimDonusum.Models;

namespace TarimDonusum.Tablolar
{
    public class TABBasvuru : TABTablo
    {
        public TABBasvuru(SqlConnection connection, IStringLocalizer<SharedResource>? localizer = null, SqlTransaction? transaction = null)
            : base(connection, localizer, transaction)
        {
        }

        //public async Task<int> KaydetAsync(Basvuru basvuru)
        //{
        //    if (basvuru.Id <= 0)
        //    {
        //        await EkleAsync(basvuru);
        //        await DetaylariKaydetAsync(basvuru);
        //        return basvuru.Id;
        //    }

        //    await GuncelleAsync(basvuru);
        //    await DetaylariKaydetAsync(basvuru);
        //    return basvuru.Id;
        //}

        private string BasvuruSelectSql()
        {
            return @"SELECT
                    B.Id,
                    B.Durum,
                    B.OzelSektorPayi,
                    B.BagliOrtakIsletmeVarMi,
                    B.BagliOrtakAciklama,
                    B.FirmaId,
                    B.DonemId,
                    B.IlId,
                    B.BasvuruKonusu,
                    B.BasvuruSahibiTuru,
                    B.SonIkiYildirFaalMi,
                    B.YatirimAdi,
                    B.YatirimTuru,
                    B.ToplamYatirimTutari,
                    B.UygunHarcamaTutari,
                    B.TalepEdilenDestekTutari,
                    B.BasvuruSahibiKatkisi,
                    B.DestekOrani,
                    B.YatiriminAmaci,
                    B.IrtibatKisi,
                    B.IrtibatUnvan,
                    B.IrtibatTelefon,
                    B.IrtibatePosta,
                    B.IrtibatAdres,
                    B.IrtibatYetkiliKisiler,

                    B.OncekiYilNetSatis,
                    B.SonYilNetSatis,
                    B.OncekiYilAktifToplami,
                    B.SonYilAktifToplami,
                    B.BelgePaketiDosyaAdi,
                    B.BelgePaketiDosyaId,
                    B.BelgePaketiAciklama,
                    B.BelgeBeyani,
                    B.TaahhutDosyaAdi,
                    B.TaahhutDosyaId,
                    B.TaahhutAciklama,

                    D.Yil,
                    D.Ad,
                    D.BasvuruyaAcikMi,
                    D.BasvuruBaslangicTarihi,
                    D.BasvuruBitisTarihi,
                    D.OnBasvuruBitisTarihi,
                    D.MinimumYatirimTutari,
                    D.MaksimumYatirimTutari,
                    D.MaksimumDestekTutari,
                    D.DestekOrani,
                    D.Aciklama,

                    I.Kod,
                    I.Ad,
                    I.Aktif,
                    F.VergiKimlikNo,
                    F.TicaretUnvani,
                    F.TicaretSicilNo,
                    F.KurulusTarihi,
                    F.MersisNo,
                    F.NaceKodu,
                    F.WebSitesi,
                    F.Telefon,
                    F.KepAdresi,
                    F.Eposta,
                    F.FaaliyetKonusu,
                    F.Adres 
                FROM dbo.Basvuru B 
                INNER JOIN dbo.Donem D On D.Id = B.DonemId
                INNER JOIN dbo.Il I On I.Id = B.IlId
                INNER JOIN dbo.Firma F On F.Id = B.FirmaId ";
        }

        public async Task<Basvuru?> OkuAsync(int id)
        {
            string sql = BasvuruSelectSql() + " WHERE B.Id = @Id;";

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

        public async Task<Sonuc<List<Basvuru>>> KullaniciBasvurulariniListeleAsync(int kullaniciId)
        {
            string sql = BasvuruSelectSql() + @" WHERE EXISTS (
                    SELECT 1
                    FROM dbo.FirmaKullanici fk
                    WHERE fk.FirmaId = B.FirmaId
                        AND fk.KullaniciId = @KullaniciId
                        AND fk.Aktif = 1
                )
                ORDER BY D.Yil DESC, Id DESC;";

            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@KullaniciId", kullaniciId);

            return await ListeOkuAsync(command);
        }

        public async Task<Sonuc<List<Basvuru>>> TumunuListeleAsync()
        {
            string sql = BasvuruSelectSql() + " ORDER BY D.Yil DESC, Id DESC;";

            await using SqlCommand command = KomutOlustur(sql);
            return await ListeOkuAsync(command);
        }

        public async Task<int> BasvuruFirmaKaydetAsync(BasvuruFirma basvuru)
        {
            if (basvuru.id <= 0)
                return await BasvuruFirmaEkleAsync(basvuru);

            await BasvuruFirmaGuncelleAsync(basvuru);
            return basvuru.id;
        }

        private async Task<int> BasvuruFirmaEkleAsync(BasvuruFirma basvuru)
        {
            const string sql = @"INSERT INTO dbo.Basvuru (
                    FirmaId, DonemId, IlId, BasvuruKonusu, BasvuruSahibiTuru, SonIkiYildirFaalMi,
                    OzelSektorPayi, BagliOrtakIsletmeVarMi, BagliOrtakAciklama, Durum)
                OUTPUT INSERTED.Id
                VALUES (
                    @FirmaId, @DonemId, @IlId, @BasvuruKonusu, @BasvuruSahibiTuru, @SonIkiYildirFaalMi,
                    @OzelSektorPayi, @BagliOrtakIsletmeVarMi, @BagliOrtakAciklama, @Durum);";

            await using SqlCommand command = KomutOlustur(sql);
            BasvuruFirmaParametreleriEkle(command, basvuru);

            basvuru.id = OrtakFonksiyonlar.Int32Yap(await command.ExecuteScalarAsync());
            return basvuru.id;
        }

        private async Task BasvuruFirmaGuncelleAsync(BasvuruFirma basvuru)
        {
            const string sql = @"UPDATE dbo.Basvuru SET FirmaId = @FirmaId, DonemId = @DonemId, IlId = @IlId,
                    BasvuruKonusu = @BasvuruKonusu, BasvuruSahibiTuru = @BasvuruSahibiTuru,
                    SonIkiYildirFaalMi = @SonIkiYildirFaalMi, OzelSektorPayi = @OzelSektorPayi,
                    BagliOrtakIsletmeVarMi = @BagliOrtakIsletmeVarMi, BagliOrtakAciklama = @BagliOrtakAciklama
                WHERE Id = @Id;";

            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@Id", basvuru.id);
            BasvuruFirmaParametreleriEkle(command, basvuru);

            await command.ExecuteNonQueryAsync();
        }

        public async Task BasvuruFinansGuncelleAsync(BasvuruFinans finans)
        {
            const string sql = @"
                UPDATE dbo.Basvuru
                SET
                    ToplamYatirimTutari = @ToplamYatirimTutari,
                    UygunHarcamaTutari = @UygunHarcamaTutari,
                    TalepEdilenDestekTutari = @TalepEdilenDestekTutari,
                    BasvuruSahibiKatkisi = @BasvuruSahibiKatkisi,
                    DestekOrani = @DestekOrani,
                    YatiriminAmaci = @YatiriminAmaci
                WHERE Id = @Id;";

            await using SqlCommand command = KomutOlustur(sql);

            command.Parameters.AddWithValue("@ToplamYatirimTutari", finans.toplamYatirimTutari);
            command.Parameters.AddWithValue("@UygunHarcamaTutari", finans.uygunHarcamaTutari);
            command.Parameters.AddWithValue("@TalepEdilenDestekTutari", finans.talepEdilenDestekTutari);
            command.Parameters.AddWithValue("@BasvuruSahibiKatkisi", finans.basvuruSahibiKatkisi);
            command.Parameters.AddWithValue("@DestekOrani", finans.destekOrani);
            command.Parameters.AddWithValue("@YatiriminAmaci", finans.yatiriminAmaci);
            command.Parameters.AddWithValue("@Id", finans.basvuruId);

            await command.ExecuteNonQueryAsync();
        }

        public async Task BasvuruMaliGuncelleAsync(BasvuruMali mali)
        {
            const string sql = @"
                UPDATE dbo.Basvuru
                SET
                    OncekiYilNetSatis = @OncekiYilNetSatis,
                    SonYilNetSatis = @SonYilNetSatis,
                    OncekiYilAktifToplami = @OncekiYilAktifToplami,
                    SonYilAktifToplami = @SonYilAktifToplami
                WHERE Id = @Id;";

            await using SqlCommand command = KomutOlustur(sql);

            command.Parameters.AddWithValue("@OncekiYilNetSatis", mali.oncekiYilNetSatis);
            command.Parameters.AddWithValue("@SonYilNetSatis", mali.sonYilNetSatis);
            command.Parameters.AddWithValue("@OncekiYilAktifToplami", mali.oncekiYilAktifToplami);
            command.Parameters.AddWithValue("@SonYilAktifToplami", mali.sonYilAktifToplami);
            command.Parameters.AddWithValue("@Id", mali.basvuruId);

            await command.ExecuteNonQueryAsync();
        }

        public async Task<int> YatirimBilgisiGuncelleAsync(BasvuruYatirim yatirim)
        {
            const string sql = @"UPDATE dbo.Basvuru SET YatirimAdi = @YatirimAdi, YatirimTuru = @YatirimTuru WHERE Id = @Id;";

            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@YatirimAdi", yatirim.yatirimAdi);
            command.Parameters.AddWithValue("@YatirimTuru", yatirim.yatirimTuru);
            command.Parameters.AddWithValue("@Id", yatirim.basvuruId);
            return await command.ExecuteNonQueryAsync();
        }

        public async Task BasvuruIletisimGuncelleAsync(BasvuruIrtibat iletisim)
        {
            const string sql = @"
                UPDATE dbo.Basvuru
                SET
                    IrtibatKisi = @IrtibatKisi,
                    IrtibatUnvan = @IrtibatUnvan,
                    IrtibatTelefon = @IrtibatTelefon,
                    IrtibatePosta =  @IrtibatePosta,
                    IrtibatAdres =   @IrtibatAdres,
                    IrtibatYetkiliKisiler = @IrtibatYetkiliKisiler
                WHERE Id = @Id;";

            await using SqlCommand command = KomutOlustur(sql);

            command.Parameters.AddWithValue("@IrtibatKisi", iletisim.kisi);
            command.Parameters.AddWithValue("@IrtibatUnvan", iletisim.unvan);
            command.Parameters.AddWithValue("@IrtibatTelefon", iletisim.telefon);
            command.Parameters.AddWithValue("@IrtibatePosta", iletisim.ePosta);
            command.Parameters.AddWithValue("@IrtibatAdres", iletisim.adres);
            command.Parameters.AddWithValue("@IrtibatYetkiliKisiler", iletisim.yetkiliKisiler);
            command.Parameters.AddWithValue("@Id", iletisim.basvuruId);

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
                    
                WHERE Id = @Id;";

            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@Id", basvuru.Id);
            //command.Parameters.AddWithValue("@OzelSektorPayi", (object?)basvuru.OzelSektorPayi ?? DBNull.Value);
            //command.Parameters.AddWithValue("@BagliOrtakIsletmeVarMi", basvuru.BagliOrtakIsletmeVarMi ? 1 : 0);
            //command.Parameters.AddWithValue("@BagliOrtakAciklama", string.IsNullOrWhiteSpace(basvuru.BagliOrtakAciklama) ? DBNull.Value : basvuru.BagliOrtakAciklama);

            await command.ExecuteNonQueryAsync();
        }

        public async Task BasvuruBelgePaketiGuncelleAsync(Basvuru basvuru)
        {
            const string sql = @"
                UPDATE dbo.Basvuru
                SET
                    BelgePaketiDosyaAdi = @BelgePaketiDosyaAdi,
                    BelgePaketiDosyaId = @BelgePaketiDosyaId,
                    BelgePaketiAciklama = @BelgePaketiAciklama,
                    BelgeBeyani = @BelgeBeyani
                WHERE Id = @Id;";

            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@Id", basvuru.Id);
            command.Parameters.AddWithValue("@BelgePaketiDosyaAdi", basvuru.BelgePaketiDosyaAdi ?? "");
            command.Parameters.AddWithValue("@BelgePaketiDosyaId", basvuru.BelgePaketiDosyaId.HasValue ? basvuru.BelgePaketiDosyaId.Value : (object)DBNull.Value);
            command.Parameters.AddWithValue("@BelgePaketiAciklama", basvuru.BelgePaketiAciklama ?? "");
            command.Parameters.AddWithValue("@BelgeBeyani", basvuru.BelgeBeyani ?? "");

            await command.ExecuteNonQueryAsync();
        }

        public async Task BasvuruTaahhutGuncelleAsync(Basvuru basvuru)
        {
            const string sql = @"
                UPDATE dbo.Basvuru
                SET
                    TaahhutDosyaAdi = @TaahhutDosyaAdi,
                    TaahhutDosyaId = @TaahhutDosyaId,
                    TaahhutAciklama = @TaahhutAciklama
                WHERE Id = @Id;";

            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@Id", basvuru.Id);
            command.Parameters.AddWithValue("@TaahhutDosyaAdi", basvuru.TaahhutDosyaAdi ?? "");
            command.Parameters.AddWithValue("@TaahhutDosyaId", basvuru.TaahhutDosyaId.HasValue ? basvuru.TaahhutDosyaId.Value : (object)DBNull.Value);
            command.Parameters.AddWithValue("@TaahhutAciklama", basvuru.TaahhutAciklama ?? "");

            await command.ExecuteNonQueryAsync();
        }

        private static void BasvuruFirmaParametreleriEkle(SqlCommand command, BasvuruFirma basvuru)
        {
            command.Parameters.AddWithValue("@FirmaId", (object?)basvuru.firma.id ?? DBNull.Value);
            command.Parameters.AddWithValue("@DonemId", (object?)basvuru.donem.id ?? DBNull.Value);
            command.Parameters.AddWithValue("@IlId", (object?)basvuru.il.id ?? DBNull.Value);
            command.Parameters.AddWithValue("@BasvuruKonusu", basvuru.basvuruKonusu ?? "");
            command.Parameters.AddWithValue("@BasvuruSahibiTuru", basvuru.basvuruSahibiTuru.HasValue ? (int)basvuru.basvuruSahibiTuru.Value : DBNull.Value);
            command.Parameters.AddWithValue("@SonIkiYildirFaalMi", basvuru.sonIkiYildirFaalMi ? 1 : 0);
            command.Parameters.AddWithValue("@OzelSektorPayi", (object?)basvuru.ozelSektorPayi ?? DBNull.Value);
            command.Parameters.AddWithValue("@BagliOrtakIsletmeVarMi", basvuru.bagliOrtakIsletmeVarMi ? 1 : 0);
            command.Parameters.AddWithValue("@BagliOrtakAciklama", string.IsNullOrWhiteSpace(basvuru.bagliOrtakAciklama) ? DBNull.Value : basvuru.bagliOrtakAciklama);
            command.Parameters.AddWithValue("@Durum", 1);
        }

        private async Task<Sonuc<List<Basvuru>>> ListeOkuAsync(SqlCommand command)
        {
            Sonuc<List<Basvuru>> liste = new Sonuc<List<Basvuru>>();
            await using SqlDataReader reader = await command.ExecuteReaderAsync();
            if (reader != null)
            {
                while (await reader.ReadAsync())
                {
                    liste.nesne.Add(Oku(reader));
                }
                reader.Close();
            }
            else
                liste.HataEkle("Veri tabanından okuma yapılamadı!");
            return liste;
        }

        private static Basvuru Oku(SqlDataReader reader)
        {
            Basvuru basvuru = new Basvuru();
            int kol = 0;
            basvuru.Id = reader.GetInt32(kol++);
            basvuru.durum = (enumBasvuruDurum)reader.GetInt32(kol++);
            basvuru.basvuruFirma.ozelSektorPayi = NullOkuDecimal(reader, kol++);
            basvuru.basvuruFirma.bagliOrtakIsletmeVarMi = BoolYap(NullOkuInt(reader, kol++));
            basvuru.basvuruFirma.bagliOrtakAciklama = NullOkuString(reader, kol++);
            basvuru.basvuruFirma.firmaId = reader.GetInt32(kol++);
            basvuru.basvuruFirma.donem.id = reader.GetInt32(kol++);
            basvuru.basvuruFirma.il.id = reader.GetInt32(kol++);
            basvuru.basvuruFirma.basvuruKonusu = reader.GetString(kol++);
            basvuru.basvuruFirma.basvuruSahibiTuru = (enumBasvuruSahibiTuru)NullDuzeltInt(reader, kol++);
            basvuru.basvuruFirma.sonIkiYildirFaalMi = BoolYap(NullOkuInt(reader, kol++));
            basvuru.yatirim.yatirimAdi = NullOkuString(reader, kol++);
            basvuru.yatirim.yatirimTuru = (enumYatirimTuru)NullDuzeltInt(reader, kol++);
            basvuru.finans.toplamYatirimTutari = NullOkuDecimal(reader, kol++);
            basvuru.finans.uygunHarcamaTutari = NullOkuDecimal(reader, kol++);
            basvuru.finans.talepEdilenDestekTutari = NullOkuDecimal(reader, kol++);
            basvuru.finans.basvuruSahibiKatkisi = NullOkuDecimal(reader, kol++);
            basvuru.finans.destekOrani = NullOkuDecimal(reader, kol++);
            basvuru.finans.yatiriminAmaci = NullOkuString(reader, kol++);

            basvuru.irtibat.basvuruId = basvuru.Id;
            basvuru.irtibat.kisi = NullOkuString(reader, kol++);
            basvuru.irtibat.unvan = NullOkuString(reader, kol++);
            basvuru.irtibat.telefon = NullOkuString(reader, kol++);
            basvuru.irtibat.ePosta = NullOkuString(reader, kol++);
            basvuru.irtibat.adres = NullOkuString(reader, kol++);
            basvuru.irtibat.yetkiliKisiler = NullOkuString(reader, kol++);

            basvuru.mali.basvuruId = basvuru.Id;
            basvuru.mali.oncekiYilNetSatis = NullOkuDecimal(reader, kol++);
            basvuru.mali.sonYilNetSatis = NullOkuDecimal(reader, kol++);
            basvuru.mali.oncekiYilAktifToplami = NullOkuDecimal(reader, kol++);
            basvuru.mali.sonYilAktifToplami = NullOkuDecimal(reader, kol++);
            basvuru.BelgePaketiDosyaAdi = NullOkuString(reader, kol++) ?? "";
            basvuru.BelgePaketiDosyaId = NullOkuInt(reader, kol++);
            basvuru.BelgePaketiAciklama = NullOkuString(reader, kol++) ?? "";
            basvuru.BelgeBeyani = NullOkuString(reader, kol++) ?? "";
            basvuru.TaahhutDosyaAdi = NullOkuString(reader, kol++) ?? "";
            basvuru.TaahhutDosyaId = NullOkuInt(reader, kol++);
            basvuru.TaahhutAciklama = NullOkuString(reader, kol++) ?? "";

            basvuru.basvuruFirma.donem.yil = NullDuzeltInt(reader, kol++);
            basvuru.basvuruFirma.donem.ad = reader.GetString(kol++);
            basvuru.basvuruFirma.donem.basvuruyaAcikMi = BoolYap(NullOkuInt(reader, kol++));
            basvuru.basvuruFirma.donem.basvuruBaslangicTarihi = reader.GetDateTime(kol++);
            basvuru.basvuruFirma.donem.basvuruBitisTarihi = reader.GetDateTime(kol++);
            basvuru.basvuruFirma.donem.onBasvuruBitisTarihi = reader.GetDateTime(kol++);
            basvuru.basvuruFirma.donem.minimumYatirimTutari = NullOkuDecimal(reader, kol++);
            basvuru.basvuruFirma.donem.maksimumYatirimTutari = NullOkuDecimal(reader, kol++);
            basvuru.basvuruFirma.donem.maksimumDestekTutari = NullOkuDecimal(reader, kol++);
            basvuru.basvuruFirma.donem.destekOrani = NullOkuDecimal(reader, kol++);
            basvuru.basvuruFirma.donem.aciklama = reader.GetString(kol++);
            basvuru.basvuruFirma.il.kod = NullDuzeltInt(reader, kol++);
            basvuru.basvuruFirma.il.ad = reader.GetString(kol++);
            basvuru.basvuruFirma.il.aktif = BoolYap(NullOkuInt(reader, kol++));

            basvuru.basvuruFirma.firma.vergiKimlikNo = reader.GetString(kol++);
            basvuru.basvuruFirma.firma.ticaretUnvani = reader.GetString(kol++);
            basvuru.basvuruFirma.firma.ticaretSicilNo = reader.GetString(kol++);
            basvuru.basvuruFirma.firma.kurulusTarihi = reader.GetDateTime(kol++);
            basvuru.basvuruFirma.firma.mersisNo = reader.GetString(kol++);
            basvuru.basvuruFirma.firma.naceKodu = reader.GetString(kol++);
            basvuru.basvuruFirma.firma.webSitesi = reader.GetString(kol++);
            basvuru.basvuruFirma.firma.telefon = reader.GetString(kol++);
            basvuru.basvuruFirma.firma.kepAdresi = reader.GetString(kol++);
            basvuru.basvuruFirma.firma.eposta = reader.GetString(kol++);
            basvuru.basvuruFirma.firma.faaliyetKonusu = reader.GetString(kol++);
            basvuru.basvuruFirma.firma.adres = reader.GetString(kol++);

            return basvuru;
        }

        public async Task YatirimDetaylariKaydetAsync(BasvuruYatirim yatirim)
        {
            await BasvuruSecimDetaylariniSilAsync(yatirim.basvuruId);
            await HarcamaTurleriEkleAsync(yatirim);
            await DegerZinciriAsamalariEkleAsync(yatirim);
        }

        private async Task HarcamaTurleriEkleAsync(BasvuruYatirim yatirim)
        {
            const string sql = @"INSERT INTO dbo.BasvuruHarcamaTuru(BasvuruId, HarcamaTuru) VALUES (@BasvuruId, @HarcamaTuru);";

            foreach (int harcamaTuru in yatirim.harcamaTurleri)
            {
                await using SqlCommand command = KomutOlustur(sql);
                command.Parameters.AddWithValue("@BasvuruId", yatirim.basvuruId);
                command.Parameters.AddWithValue("@HarcamaTuru", harcamaTuru);
                await command.ExecuteNonQueryAsync();
            }
        }

        private async Task DegerZinciriAsamalariEkleAsync(BasvuruYatirim yatirim)
        {
            const string sql = @"INSERT INTO dbo.BasvuruDegerZinciriAsama(BasvuruId, DegerZinciriAsamaId) Values(@BasvuruId, @DegerZinciriAsamaId);";

            foreach (DegerZinciriAsama dza in yatirim.degerZinciriAsamalari)
            {
                await using SqlCommand command = KomutOlustur(sql);
                command.Parameters.AddWithValue("@BasvuruId", yatirim.basvuruId);
                command.Parameters.AddWithValue("@DegerZinciriAsamaId", dza.id);
                await command.ExecuteNonQueryAsync();
            }
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

        private string AdresSorguOl()
        {
            return @"SELECT bua.Id, bua.BasvuruId, bua.SiraNo, bua.IlceId,
                    il.Id AS IlId, il.Kod AS IlKod, il.Ad AS IlAdi, ilce.Ad AS IlceAdi,
                    bua.TamAdres, bua.YatirimYeriStatusu, bua.KiraVeyaTahsisSuresi,
                    bua.KiraTahsisBitisTarihi, bua.YapiRuhsatiDurumu
                FROM dbo.BasvuruUygulamaAdresleri bua
                LEFT JOIN dbo.Ilce ilce ON ilce.Id = bua.IlceId
                LEFT JOIN dbo.Il il ON il.Id = ilce.IlId ";
        }

        public async Task<List<BasvuruUygulamaAdresi>> UygulamaAdresiOkuAsync(int basvuruId, int adresId)
        {
            string sql = AdresSorguOl() + " WHERE ";

            if (adresId > 0)
                sql += "bua.Id = @Id";
            else
                sql += "bua.BasvuruId = @BasvuruId";
            await using SqlCommand command = KomutOlustur(sql);
            if (adresId > 0)
                command.Parameters.AddWithValue("@Id", adresId);
            else
                command.Parameters.AddWithValue("@BasvuruId", basvuruId);
            List<BasvuruUygulamaAdresi> adresler = new List<BasvuruUygulamaAdresi>();
            await using SqlDataReader reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                adresler.Add(UygulamaAdresiOku(reader, L));
            }
            return adresler;
        }

        public async Task<int> UygulamaAdresiKaydetAsync(BasvuruUygulamaAdresi adres)
        {
            if (adres.id <= 0)
                return await UygulamaAdresiEkleAsync(adres);

            await UygulamaAdresiGuncelleAsync(adres);
            return adres.id;
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
            adres.id = id;
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
            command.Parameters.AddWithValue("@Id", adres.id);
            await command.ExecuteNonQueryAsync();
        }

        private static void UygulamaAdresiParametreleriEkle(SqlCommand command, BasvuruUygulamaAdresi adres)
        {
            command.Parameters.AddWithValue("@BasvuruId", adres.basvuruId);
            command.Parameters.AddWithValue("@SiraNo", adres.siraNo);
            command.Parameters.AddWithValue("@IlceId", adres.ilceId.HasValue && adres.ilceId.Value > 0 ? adres.ilceId.Value : (object)DBNull.Value);
            command.Parameters.AddWithValue("@TamAdres", adres.tamAdres?.Trim() ?? "");
            command.Parameters.AddWithValue("@YatirimYeriStatusu", (int)adres.yatirimYeriStatusu);
            command.Parameters.AddWithValue("@KiraVeyaTahsisSuresi", adres.kiraVeyaTahsisSuresi.HasValue ? adres.kiraVeyaTahsisSuresi.Value : (object)DBNull.Value);
            command.Parameters.AddWithValue("@KiraTahsisBitisTarihi", adres.kiraTahsisBitisTarihi.HasValue ? adres.kiraTahsisBitisTarihi.Value.Date : (object)DBNull.Value);
            command.Parameters.AddWithValue("@YapiRuhsatiDurumu", (int)adres.yapiRuhsatiDurumu);
        }

        private async Task DetaylariYukleAsync(Basvuru basvuru)
        {
            string sql = AdresSorguOl() + @" WHERE bua.BasvuruId = @BasvuruId ORDER BY bua.SiraNo;

                SELECT HarcamaTuru
                FROM dbo.BasvuruHarcamaTuru
                WHERE BasvuruId = @BasvuruId
                ORDER BY HarcamaTuru;

                SELECT dz.Id, dz.Ad, dz.Aciklama, dz.Aktif, dza.Id, dza.SiraNo, dza.Ad, dza.Aciklama, dza.Aktif  
                FROM dbo.BasvuruDegerZinciriAsama bdza
                LEFT JOIN dbo.DegerZinciriAsama dza ON dza.Id = bdza.DegerZinciriAsamaId
                LEFT JOIN dbo.DegerZinciri dz ON dz.Id = dza.DegerZinciriId
                WHERE bdza.BasvuruId = @BasvuruId
                ORDER BY dza.SiraNo;";

            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@BasvuruId", basvuru.Id);
            await using SqlDataReader reader = await command.ExecuteReaderAsync();

            List<BasvuruUygulamaAdresi> adresler = new List<BasvuruUygulamaAdresi>();
            while (await reader.ReadAsync())
            {
                adresler.Add(UygulamaAdresiOku(reader, L));
            }

            await reader.NextResultAsync();
            List<int> harcamaTurleri = new List<int>();
            while (await reader.ReadAsync())
            {
                harcamaTurleri.Add(NullDuzeltInt(reader, 0));
            }

            await reader.NextResultAsync();
            List<DegerZinciriAsama> asamalar = new List<DegerZinciriAsama>();
            while (await reader.ReadAsync())
            {
                int kolNo = 0;
                DegerZinciriAsama dza = new DegerZinciriAsama();
                dza.dz.id = NullDuzeltInt(reader, kolNo++);
                dza.dz.ad = reader.GetString(kolNo++);
                dza.dz.aciklama = reader.GetString(kolNo++);
                dza.dz.aktif = BoolYap(NullDuzeltInt(reader, kolNo++));

                dza.id = NullDuzeltInt(reader, kolNo++);
                dza.siraNo = NullDuzeltInt(reader, kolNo++);
                dza.ad = reader.GetString(kolNo++);
                dza.aciklama = reader.GetString(kolNo++);
                dza.aktif = BoolYap(NullDuzeltInt(reader, kolNo++));

                asamalar.Add(dza);
            }

            basvuru.YatirimAdresleri = adresler;

            basvuru.yatirim.harcamaTurleri = harcamaTurleri;

            basvuru.yatirim.degerZinciriAsamalari = asamalar;
            basvuru.yatirim.degerZinciriId = asamalar.Count >= 1
                    ? asamalar.First().dz.id
                    : null;
        }

        private static BasvuruUygulamaAdresi UygulamaAdresiOku(SqlDataReader reader, IStringLocalizer<SharedResource>? l)
        {
            BasvuruUygulamaAdresi bu = new BasvuruUygulamaAdresi
            {
                id = reader.GetInt32(0),
                basvuruId = reader.GetInt32(1),
                siraNo = reader.GetInt32(2),
                ilceId = reader.IsDBNull(3) ? null : reader.GetInt32(3),
                ilId = reader.IsDBNull(4) ? null : reader.GetInt32(4),
                ilKod = reader.IsDBNull(5) ? null : OrtakFonksiyonlar.Int32Yap(reader.GetValue(5)),
                ilAdi = reader.IsDBNull(6) ? "" : reader.GetString(6),
                ilceAdi = reader.IsDBNull(7) ? "" : reader.GetString(7),
                tamAdres = reader.GetString(8),
                yatirimYeriStatusu = reader.IsDBNull(9) ? enumUygulamaAdresiYatirimYeriStatusu.Tanimsiz : (enumUygulamaAdresiYatirimYeriStatusu)reader.GetInt32(9),
                kiraVeyaTahsisSuresi = reader.IsDBNull(10) ? null : reader.GetInt32(10),
                kiraTahsisBitisTarihi = reader.IsDBNull(11) ? null : reader.GetDateTime(11),
                yapiRuhsatiDurumu = reader.IsDBNull(12) ? enumUygulamaAdresiYapiRuhsatiDurumu.Tanimsiz : (enumUygulamaAdresiYapiRuhsatiDurumu)reader.GetInt32(12)
            };
            if (l != null)
            {
                bu.yapiRuhsatiDurumuAd = IsimBul.EnumAdi<enumUygulamaAdresiYapiRuhsatiDurumu>(bu.yapiRuhsatiDurumu, l);
                bu.yatirimYeriStatusuAd = IsimBul.EnumAdi<enumUygulamaAdresiYatirimYeriStatusu>(bu.yatirimYeriStatusu, l);
            }
            return bu;
        }

        internal async Task<int> DegerZinciriBul(int basvuruId)
        {
            string sql = @" SELECT MIN(dza.DegerZinciriId)
        FROM dbo.BasvuruDegerZinciriAsama bdza
        INNER JOIN DegerZinciriAsama dza ON dza.Id = bdza.DegerZinciriAsamaId
        WHERE bdza.BasvuruId = @BasvuruId";

            using (var command = new SqlCommand(sql, this.Connection))
            {
                command.Parameters.AddWithValue("@BasvuruId", basvuruId);

                object? result = await command.ExecuteScalarAsync();

                if (result != null && result != DBNull.Value)
                {
                    return Convert.ToInt32(result); // Değer varsa int'e çevirip dönüyoruz
                }
                else
                    return -1;
            }
        }
    }
}



