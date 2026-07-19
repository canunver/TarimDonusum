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
                    B.BasvuruAnaId,
                    B.RevizyonNo,
                    B.SiraNo,
                    BA.Durum,
                    B.OzelSektorPayi,
                    B.BagliOrtakIsletmeVarMi,
                    B.BagliOrtakAciklama,
                    B.BagliOrtakUnvani,
                    B.BagliOrtakKimlikNo,
                    B.BagliOrtakOncekiYilNetSatis,
                    B.BagliOrtakSonYilNetSatis,
                    B.BagliOrtakOncekiYilAktifToplami,
                    B.BagliOrtakSonYilAktifToplami,
                    BA.FirmaId,
                    BA.DonemId,
                    BA.IlId,
                    B.BasvuruKonusu,
                    B.BasvuruSahibiTuru,
                    B.HukukiTurSirketTuru,
                    B.YonetimKuruluUyeleriAdliSicilKisiler,
                    B.SonIkiYildirFaalMi,
                    B.YatirimAdi,
                    B.YatirimTuru,
                    B.ToplamYatirimTutari,
                    B.UygunHarcamaTutari,
                    B.TalepEdilenDestekTutari,
                    B.TalepEdilenFinansmanOrani,
                    B.OnBasvuruSahibiKatkisi,
                    B.BasvuruSahibiKatkisi,
                    B.TalepEdilenVadeSuresiYil,
                    B.DestekOrani,
                    B.DigerFinansmanKaynaklariAciklama,
                    B.YatiriminAmaci,
                    B.PikkListesiJson,
                    B.YatirimOzetiJson,
                    B.CevreselSosyalJson,
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
                    B.BagimsizDenetimeTabiMi,
                    B.DenetimDosyaAdi,
                    B.DenetimDosyaId,
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
                INNER JOIN dbo.BasvuruAna BA ON BA.Id = B.BasvuruAnaId
                INNER JOIN dbo.Donem D On D.Id = BA.DonemId
                INNER JOIN dbo.Il I On I.Id = BA.IlId
                INNER JOIN dbo.Firma F On F.Id = BA.FirmaId ";
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
                    WHERE fk.FirmaId = BA.FirmaId
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
            basvuru.basvuruAnaId = await BasvuruAnaEkleAsync(basvuru);

            const string sql = @"INSERT INTO dbo.Basvuru (
                    BasvuruAnaId, RevizyonNo, SiraNo, BasvuruSahibiTuru, HukukiTurSirketTuru, YonetimKuruluUyeleriAdliSicilKisiler, SonIkiYildirFaalMi)
                OUTPUT INSERTED.Id
                VALUES (
                    @BasvuruAnaId, @RevizyonNo, @SiraNo, @BasvuruSahibiTuru, @HukukiTurSirketTuru, @YonetimKuruluUyeleriAdliSicilKisiler, @SonIkiYildirFaalMi);";

            await using SqlCommand command = KomutOlustur(sql);
            BasvuruIlkSayfaParametreleriEkle(command, basvuru);

            basvuru.id = OrtakFonksiyonlar.Int32Yap(await command.ExecuteScalarAsync());
            return basvuru.id;
        }

        private async Task BasvuruFirmaGuncelleAsync(BasvuruFirma basvuru)
        {
            if (basvuru.basvuruAnaId <= 0)
                basvuru.basvuruAnaId = await BasvuruAnaIdOkuAsync(basvuru.id);

            await BasvuruAnaGuncelleAsync(basvuru);

            const string sql = @"UPDATE dbo.Basvuru SET
                    BasvuruSahibiTuru = @BasvuruSahibiTuru,
                    HukukiTurSirketTuru = @HukukiTurSirketTuru,
                    YonetimKuruluUyeleriAdliSicilKisiler = @YonetimKuruluUyeleriAdliSicilKisiler,
                    SonIkiYildirFaalMi = @SonIkiYildirFaalMi
                WHERE Id = @Id;";

            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@Id", basvuru.id);
            command.Parameters.AddWithValue("@BasvuruSahibiTuru", basvuru.basvuruSahibiTuru.HasValue ? (int)basvuru.basvuruSahibiTuru.Value : DBNull.Value);
            command.Parameters.AddWithValue("@HukukiTurSirketTuru", basvuru.hukukiTurSirketTuru.HasValue ? (int)basvuru.hukukiTurSirketTuru.Value : DBNull.Value);
            command.Parameters.AddWithValue("@YonetimKuruluUyeleriAdliSicilKisiler", DbNull(basvuru.yonetimKuruluUyeleriAdliSicilKisiler));
            command.Parameters.AddWithValue("@SonIkiYildirFaalMi", basvuru.sonIkiYildirFaalMi.HasValue ? (basvuru.sonIkiYildirFaalMi.Value ? 1 : 0) : DBNull.Value);

            await command.ExecuteNonQueryAsync();
        }

        private async Task<int> BasvuruAnaEkleAsync(BasvuruFirma basvuru)
        {
            const string sql = @"INSERT INTO dbo.BasvuruAna (FirmaId, DonemId, IlId, Durum)
                OUTPUT INSERTED.Id
                VALUES (@FirmaId, @DonemId, @IlId, @Durum);";

            await using SqlCommand command = KomutOlustur(sql);
            BasvuruAnaParametreleriEkle(command, basvuru);
            return OrtakFonksiyonlar.Int32Yap(await command.ExecuteScalarAsync());
        }

        private async Task BasvuruAnaGuncelleAsync(BasvuruFirma basvuru)
        {
            const string sql = @"UPDATE dbo.BasvuruAna
                SET FirmaId = @FirmaId, DonemId = @DonemId, IlId = @IlId
                WHERE Id = @BasvuruAnaId;";

            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@BasvuruAnaId", basvuru.basvuruAnaId);
            BasvuruAnaParametreleriEkle(command, basvuru);
            await command.ExecuteNonQueryAsync();
        }

        private async Task<int> BasvuruAnaIdOkuAsync(int basvuruId)
        {
            const string sql = "SELECT BasvuruAnaId FROM dbo.Basvuru WHERE Id = @Id;";
            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@Id", basvuruId);
            return OrtakFonksiyonlar.Int32Yap(await command.ExecuteScalarAsync());
        }

        public async Task BasvuruFinansGuncelleAsync(BasvuruFinans finans)
        {
            const string sql = @"
                UPDATE dbo.Basvuru
                SET
                    ToplamYatirimTutari = @ToplamYatirimTutari,
                    UygunHarcamaTutari = @UygunHarcamaTutari,
                    TalepEdilenDestekTutari = @TalepEdilenDestekTutari,
                    TalepEdilenFinansmanOrani = @TalepEdilenFinansmanOrani,
                    OnBasvuruSahibiKatkisi = @OnBasvuruSahibiKatkisi,
                    BasvuruSahibiKatkisi = @BasvuruSahibiKatkisi,
                    TalepEdilenVadeSuresiYil = @TalepEdilenVadeSuresiYil,
                    DestekOrani = @DestekOrani,
                    DigerFinansmanKaynaklariAciklama = @DigerFinansmanKaynaklariAciklama
                WHERE Id = @Id;";

            await using SqlCommand command = KomutOlustur(sql);

            command.Parameters.AddWithValue("@ToplamYatirimTutari", DbNull(finans.toplamYatirimTutari));
            command.Parameters.AddWithValue("@UygunHarcamaTutari", DbNull(finans.uygunHarcamaTutari));
            command.Parameters.AddWithValue("@TalepEdilenDestekTutari", DbNull(finans.talepEdilenDestekTutari));
            command.Parameters.AddWithValue("@TalepEdilenFinansmanOrani", DbNull(finans.talepEdilenFinansmanOrani));
            command.Parameters.AddWithValue("@OnBasvuruSahibiKatkisi", DbNull(finans.onBasvuruSahibiKatkisi));
            command.Parameters.AddWithValue("@BasvuruSahibiKatkisi", DbNull(finans.basvuruSahibiKatkisi));
            command.Parameters.AddWithValue("@TalepEdilenVadeSuresiYil", DbNull(finans.talepEdilenVadeSuresiYil));
            command.Parameters.AddWithValue("@DestekOrani", DbNull(finans.destekOrani));
            command.Parameters.AddWithValue("@DigerFinansmanKaynaklariAciklama", DbNull(finans.digerFinansmanKaynaklariAciklama));
            command.Parameters.AddWithValue("@Id", finans.basvuruId);

            await command.ExecuteNonQueryAsync();
        }

        public async Task UygunHarcamaKaydetAsync(BasvuruUygunHarcama uygunHarcama)
        {
            const string sql = @"
                UPDATE dbo.Basvuru
                SET PikkListesiJson = @PikkListesiJson
                WHERE Id = @Id;";

            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@PikkListesiJson", DbNull(uygunHarcama.pikkListesiJson));
            command.Parameters.AddWithValue("@Id", uygunHarcama.basvuruId);
            await command.ExecuteNonQueryAsync();
        }

        public async Task YatirimOzetiKaydetAsync(BasvuruYatirimOzeti yatirimOzeti)
        {
            const string sql = @"
                UPDATE dbo.Basvuru
                SET YatirimOzetiJson = @YatirimOzetiJson
                WHERE Id = @Id;";

            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@YatirimOzetiJson", DbNull(yatirimOzeti.yatirimOzetiJson));
            command.Parameters.AddWithValue("@Id", yatirimOzeti.basvuruId);
            await command.ExecuteNonQueryAsync();
        }

        public async Task CevreselSosyalKaydetAsync(BasvuruCevreselSosyal cevreselSosyal)
        {
            const string sql = @"
                UPDATE dbo.Basvuru
                SET CevreselSosyalJson = @CevreselSosyalJson
                WHERE Id = @Id;";

            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@CevreselSosyalJson", DbNull(cevreselSosyal.cevreselSosyalJson));
            command.Parameters.AddWithValue("@Id", cevreselSosyal.basvuruId);
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
                    SonYilAktifToplami = @SonYilAktifToplami,
                    BagimsizDenetimeTabiMi = @BagimsizDenetimeTabiMi
                WHERE Id = @Id;";

            await using SqlCommand command = KomutOlustur(sql);

            command.Parameters.AddWithValue("@OncekiYilNetSatis", DbNull(mali.oncekiYilNetSatis));
            command.Parameters.AddWithValue("@SonYilNetSatis", DbNull(mali.sonYilNetSatis));
            command.Parameters.AddWithValue("@OncekiYilAktifToplami", DbNull(mali.oncekiYilAktifToplami));
            command.Parameters.AddWithValue("@SonYilAktifToplami", DbNull(mali.sonYilAktifToplami));
            command.Parameters.AddWithValue("@BagimsizDenetimeTabiMi", mali.bagimsizDenetimeTabiMi.HasValue ? (mali.bagimsizDenetimeTabiMi.Value ? 1 : 0) : DBNull.Value);
            command.Parameters.AddWithValue("@Id", mali.basvuruId);

            await command.ExecuteNonQueryAsync();
        }

        public async Task BasvuruDenetimDosyasiGuncelleAsync(BasvuruMali mali)
        {
            const string sql = @"
                UPDATE dbo.Basvuru
                SET
                    DenetimDosyaAdi = @DenetimDosyaAdi,
                    DenetimDosyaId = @DenetimDosyaId
                WHERE Id = @Id;";

            await using SqlCommand command = KomutOlustur(sql);

            command.Parameters.AddWithValue("@DenetimDosyaAdi", DbNull(mali.denetimDosyaAdi));
            command.Parameters.AddWithValue("@DenetimDosyaId", DbNull(mali.denetimDosyaId));
            command.Parameters.AddWithValue("@Id", mali.basvuruId);

            await command.ExecuteNonQueryAsync();
        }

        public async Task<int> YatirimBilgisiGuncelleAsync(BasvuruYatirim yatirim)
        {
            const string sql = @"UPDATE dbo.Basvuru SET YatirimAdi = @YatirimAdi, YatirimTuru = @YatirimTuru WHERE Id = @Id;";

            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@YatirimAdi", DbNull(yatirim.yatirimAdi));
            command.Parameters.AddWithValue("@YatirimTuru", yatirim.yatirimTuru == enumYatirimTuru.Tanimsiz ? DBNull.Value : (object)(int)yatirim.yatirimTuru);
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

            command.Parameters.AddWithValue("@IrtibatKisi", DbNull(iletisim.kisi));
            command.Parameters.AddWithValue("@IrtibatUnvan", DbNull(iletisim.unvan));
            command.Parameters.AddWithValue("@IrtibatTelefon", DbNull(iletisim.telefon));
            command.Parameters.AddWithValue("@IrtibatePosta", DbNull(iletisim.ePosta));
            command.Parameters.AddWithValue("@IrtibatAdres", DbNull(iletisim.adres));
            command.Parameters.AddWithValue("@IrtibatYetkiliKisiler", DbNull(iletisim.yetkiliKisiler));
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
                    BagliOrtakUnvani = @BagliOrtakUnvani,
                    BagliOrtakKimlikNo = @BagliOrtakKimlikNo,
                    BagliOrtakOncekiYilNetSatis = @BagliOrtakOncekiYilNetSatis,
                    BagliOrtakSonYilNetSatis = @BagliOrtakSonYilNetSatis,
                    BagliOrtakOncekiYilAktifToplami = @BagliOrtakOncekiYilAktifToplami,
                    BagliOrtakSonYilAktifToplami = @BagliOrtakSonYilAktifToplami
                WHERE Id = @Id;";

            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@Id", basvuru.Id);
            command.Parameters.AddWithValue("@OzelSektorPayi", DbNull(basvuru.ortaklik.ozelSektorPayi));
            command.Parameters.AddWithValue("@BagliOrtakIsletmeVarMi", basvuru.ortaklik.bagliOrtakIsletmeVarMi.HasValue ? (basvuru.ortaklik.bagliOrtakIsletmeVarMi.Value ? 1 : 0) : DBNull.Value);
            command.Parameters.AddWithValue("@BagliOrtakAciklama", DbNull(basvuru.ortaklik.bagliOrtakUnvani));
            command.Parameters.AddWithValue("@BagliOrtakUnvani", DbNull(basvuru.ortaklik.bagliOrtakUnvani));
            command.Parameters.AddWithValue("@BagliOrtakKimlikNo", DbNull(basvuru.ortaklik.bagliOrtakKimlikNo));
            command.Parameters.AddWithValue("@BagliOrtakOncekiYilNetSatis", DbNull(basvuru.ortaklik.bagliOrtakOncekiYilNetSatis));
            command.Parameters.AddWithValue("@BagliOrtakSonYilNetSatis", DbNull(basvuru.ortaklik.bagliOrtakSonYilNetSatis));
            command.Parameters.AddWithValue("@BagliOrtakOncekiYilAktifToplami", DbNull(basvuru.ortaklik.bagliOrtakOncekiYilAktifToplami));
            command.Parameters.AddWithValue("@BagliOrtakSonYilAktifToplami", DbNull(basvuru.ortaklik.bagliOrtakSonYilAktifToplami));

            await command.ExecuteNonQueryAsync();

            await BasvuruOrtaklariYenileAsync(basvuru.Id, basvuru.ortaklik.ortaklar);
        }

        public async Task BasvuruOrtaklariKaydetAsync(int basvuruId, List<BasvuruOrtak>? ortaklar)
        {
            await BasvuruOrtaklariYenileAsync(basvuruId, ortaklar);
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
            command.Parameters.AddWithValue("@BelgePaketiDosyaAdi", DbNull(basvuru.BelgePaketiDosyaAdi));
            command.Parameters.AddWithValue("@BelgePaketiDosyaId", DbNull(basvuru.BelgePaketiDosyaId));
            command.Parameters.AddWithValue("@BelgePaketiAciklama", DbNull(basvuru.BelgePaketiAciklama));
            command.Parameters.AddWithValue("@BelgeBeyani", DbNull(basvuru.BelgeBeyani));

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
            command.Parameters.AddWithValue("@TaahhutDosyaAdi", DbNull(basvuru.TaahhutDosyaAdi));
            command.Parameters.AddWithValue("@TaahhutDosyaId", DbNull(basvuru.TaahhutDosyaId));
            command.Parameters.AddWithValue("@TaahhutAciklama", DbNull(basvuru.TaahhutAciklama));

            await command.ExecuteNonQueryAsync();
        }

        private static void BasvuruAnaParametreleriEkle(SqlCommand command, BasvuruFirma basvuru)
        {
            command.Parameters.AddWithValue("@FirmaId", DbNullId(basvuru.firma.id));
            command.Parameters.AddWithValue("@DonemId", DbNullId(basvuru.donem.id));
            command.Parameters.AddWithValue("@IlId", DbNullId(basvuru.il.id));
            command.Parameters.AddWithValue("@Durum", (int)enumBasvuruDurum.OnBasvuruDurumu);
        }

        private static void BasvuruIlkSayfaParametreleriEkle(SqlCommand command, BasvuruFirma basvuru)
        {
            command.Parameters.AddWithValue("@BasvuruAnaId", basvuru.basvuruAnaId);
            command.Parameters.AddWithValue("@RevizyonNo", basvuru.revizyonNo);
            command.Parameters.AddWithValue("@SiraNo", basvuru.siraNo <= 0 ? 1 : basvuru.siraNo);
            command.Parameters.AddWithValue("@BasvuruSahibiTuru", basvuru.basvuruSahibiTuru.HasValue ? (int)basvuru.basvuruSahibiTuru.Value : DBNull.Value);
            command.Parameters.AddWithValue("@HukukiTurSirketTuru", basvuru.hukukiTurSirketTuru.HasValue ? (int)basvuru.hukukiTurSirketTuru.Value : DBNull.Value);
            command.Parameters.AddWithValue("@YonetimKuruluUyeleriAdliSicilKisiler", DbNull(basvuru.yonetimKuruluUyeleriAdliSicilKisiler));
            command.Parameters.AddWithValue("@SonIkiYildirFaalMi", basvuru.sonIkiYildirFaalMi.HasValue ? (basvuru.sonIkiYildirFaalMi.Value ? 1 : 0) : DBNull.Value);
        }

        private static object DbNull(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? DBNull.Value : value.Trim();
        }

        private static object DbNull(decimal? value)
        {
            return value.HasValue ? value.Value : DBNull.Value;
        }

        private static object DbNull(int? value)
        {
            return value.HasValue ? value.Value : DBNull.Value;
        }

        private static object DbNullId(int value)
        {
            return value > 0 ? value : DBNull.Value;
        }

        private static bool? NullOkuBool(SqlDataReader reader, int kolNo)
        {
            int? deger = NullOkuInt(reader, kolNo);
            return deger.HasValue ? deger.Value != 0 : null;
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
            basvuru.basvuruFirma.basvuruAnaId = reader.GetInt32(kol++);
            basvuru.basvuruFirma.revizyonNo = reader.GetInt32(kol++);
            basvuru.basvuruFirma.siraNo = reader.GetInt32(kol++);
            basvuru.durum = (enumBasvuruDurum)reader.GetInt32(kol++);
            basvuru.basvuruFirma.ozelSektorPayi = NullOkuDecimal(reader, kol++);
            basvuru.basvuruFirma.bagliOrtakIsletmeVarMi = NullOkuBool(reader, kol++);
            basvuru.basvuruFirma.bagliOrtakAciklama = NullOkuString(reader, kol++);
            string? bagliOrtakUnvani = NullOkuString(reader, kol++);
            string? bagliOrtakKimlikNo = NullOkuString(reader, kol++);
            decimal? bagliOrtakOncekiYilNetSatis = NullOkuDecimal(reader, kol++);
            decimal? bagliOrtakSonYilNetSatis = NullOkuDecimal(reader, kol++);
            decimal? bagliOrtakOncekiYilAktifToplami = NullOkuDecimal(reader, kol++);
            decimal? bagliOrtakSonYilAktifToplami = NullOkuDecimal(reader, kol++);
            basvuru.basvuruFirma.firmaId = reader.GetInt32(kol++);
            basvuru.basvuruFirma.donem.id = reader.GetInt32(kol++);
            basvuru.basvuruFirma.il.id = reader.GetInt32(kol++);
            basvuru.basvuruFirma.basvuruKonusu = NullOkuString(reader, kol++);
            basvuru.basvuruFirma.basvuruSahibiTuru = (enumBasvuruSahibiTuru)NullDuzeltInt(reader, kol++);
            basvuru.basvuruFirma.hukukiTurSirketTuru = (enumHukukiTurSirketTuru)NullDuzeltInt(reader, kol++);
            basvuru.basvuruFirma.yonetimKuruluUyeleriAdliSicilKisiler = NullOkuString(reader, kol++);
            basvuru.basvuruFirma.sonIkiYildirFaalMi = NullOkuBool(reader, kol++);
            basvuru.yatirim.yatirimAdi = NullOkuString(reader, kol++);
            basvuru.yatirim.yatirimTuru = (enumYatirimTuru)NullDuzeltInt(reader, kol++);
            basvuru.finans.toplamYatirimTutari = NullOkuDecimal(reader, kol++);
            basvuru.finans.uygunHarcamaTutari = NullOkuDecimal(reader, kol++);
            basvuru.finans.talepEdilenDestekTutari = NullOkuDecimal(reader, kol++);
            basvuru.finans.talepEdilenFinansmanOrani = NullOkuDecimal(reader, kol++);
            basvuru.finans.onBasvuruSahibiKatkisi = NullOkuDecimal(reader, kol++);
            basvuru.finans.basvuruSahibiKatkisi = NullOkuDecimal(reader, kol++);
            basvuru.finans.talepEdilenVadeSuresiYil = NullOkuInt(reader, kol++);
            basvuru.finans.destekOrani = NullOkuDecimal(reader, kol++);
            basvuru.finans.digerFinansmanKaynaklariAciklama = NullOkuString(reader, kol++);
            basvuru.finans.yatiriminAmaci = NullOkuString(reader, kol++);
            basvuru.yatirim.yatiriminAmaci = basvuru.finans.yatiriminAmaci;
            basvuru.uygunHarcama.basvuruId = basvuru.Id;
            basvuru.uygunHarcama.pikkListesiJson = NullOkuString(reader, kol++);
            basvuru.yatirimOzeti.basvuruId = basvuru.Id;
            basvuru.yatirimOzeti.yatirimOzetiJson = NullOkuString(reader, kol++);
            basvuru.cevreselSosyal.basvuruId = basvuru.Id;
            basvuru.cevreselSosyal.cevreselSosyalJson = NullOkuString(reader, kol++);

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
            basvuru.mali.bagimsizDenetimeTabiMi = NullOkuBool(reader, kol++);
            basvuru.mali.denetimDosyaAdi = NullOkuString(reader, kol++) ?? "";
            basvuru.mali.denetimDosyaId = NullOkuInt(reader, kol++);
            basvuru.BelgePaketiDosyaAdi = NullOkuString(reader, kol++) ?? "";
            basvuru.BelgePaketiDosyaId = NullOkuInt(reader, kol++);
            basvuru.BelgePaketiAciklama = NullOkuString(reader, kol++) ?? "";
            basvuru.BelgeBeyani = NullOkuString(reader, kol++) ?? "";
            basvuru.TaahhutDosyaAdi = NullOkuString(reader, kol++) ?? "";
            basvuru.TaahhutDosyaId = NullOkuInt(reader, kol++);
            basvuru.TaahhutAciklama = NullOkuString(reader, kol++) ?? "";
            basvuru.ortaklik.basvuruId = basvuru.Id;
            basvuru.ortaklik.ozelSektorPayi = basvuru.basvuruFirma.ozelSektorPayi;
            basvuru.ortaklik.bagliOrtakIsletmeVarMi = basvuru.basvuruFirma.bagliOrtakIsletmeVarMi;
            basvuru.ortaklik.bagliOrtakUnvani = bagliOrtakUnvani ?? basvuru.basvuruFirma.bagliOrtakAciklama;
            basvuru.ortaklik.bagliOrtakKimlikNo = bagliOrtakKimlikNo;
            basvuru.ortaklik.bagliOrtakOncekiYilNetSatis = bagliOrtakOncekiYilNetSatis;
            basvuru.ortaklik.bagliOrtakSonYilNetSatis = bagliOrtakSonYilNetSatis;
            basvuru.ortaklik.bagliOrtakOncekiYilAktifToplami = bagliOrtakOncekiYilAktifToplami;
            basvuru.ortaklik.bagliOrtakSonYilAktifToplami = bagliOrtakSonYilAktifToplami;

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

        public async Task YatirimBilgileriKaydetAsync(BasvuruYatirim yatirim)
        {
            await YatirimBilgileriGuncelleAsync(yatirim);
            await HarcamaTurleriniSilAsync(yatirim.basvuruId);
            await HarcamaTurleriEkleAsync(yatirim);
        }

        public async Task<int> YatirimBilgileriGuncelleAsync(BasvuruYatirim yatirim)
        {
            const string sql = @"UPDATE dbo.Basvuru SET YatirimAdi = @YatirimAdi, YatirimTuru = @YatirimTuru, YatiriminAmaci = @YatiriminAmaci WHERE Id = @Id;";

            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@YatirimAdi", DbNull(yatirim.yatirimAdi));
            command.Parameters.AddWithValue("@YatirimTuru", yatirim.yatirimTuru == enumYatirimTuru.Tanimsiz ? DBNull.Value : (object)(int)yatirim.yatirimTuru);
            command.Parameters.AddWithValue("@YatiriminAmaci", DbNull(yatirim.yatiriminAmaci));
            command.Parameters.AddWithValue("@Id", yatirim.basvuruId);
            return await command.ExecuteNonQueryAsync();
        }

        public async Task DegerZinciriKaydetAsync(BasvuruYatirim yatirim)
        {
            await DegerZinciriAsamalariniSilAsync(yatirim.basvuruId);
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
            const string sql = @"INSERT INTO dbo.BasvuruDegerZinciriAsama(BasvuruId, DegerZinciriAsamaId, YapilacakFaaliyetler) Values(@BasvuruId, @DegerZinciriAsamaId, @YapilacakFaaliyetler);";

            foreach (DegerZinciriAsama dza in yatirim.degerZinciriAsamalari)
            {
                await using SqlCommand command = KomutOlustur(sql);
                command.Parameters.AddWithValue("@BasvuruId", yatirim.basvuruId);
                command.Parameters.AddWithValue("@DegerZinciriAsamaId", dza.id);
                command.Parameters.AddWithValue("@YapilacakFaaliyetler", DbNull(dza.yapilacakFaaliyetler));
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

        private async Task HarcamaTurleriniSilAsync(int basvuruId)
        {
            const string sql = @"DELETE FROM dbo.BasvuruHarcamaTuru WHERE BasvuruId = @BasvuruId;";

            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@BasvuruId", basvuruId);
            await command.ExecuteNonQueryAsync();
        }

        private async Task DegerZinciriAsamalariniSilAsync(int basvuruId)
        {
            const string sql = @"DELETE FROM dbo.BasvuruDegerZinciriAsama WHERE BasvuruId = @BasvuruId;";

            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@BasvuruId", basvuruId);
            await command.ExecuteNonQueryAsync();
        }

        private async Task BasvuruOrtaklariYenileAsync(int basvuruId, List<BasvuruOrtak>? ortaklar)
        {
            await BasvuruOrtaklariniSilAsync(basvuruId);

            if (ortaklar == null)
                return;

            const string sql = @"
                INSERT INTO dbo.BasvuruOrtaklar
                    (BasvuruId, SiraNo, AdUnvan, TcknVkn, KisiTuru, PayOrani, HesabaDahilOran, OzelKamuNiteligi, NihaiFaydalaniciBilgisi, UboKycBelgeAdi, UboKycDosyaId,
                     OncekiYilNetSatis, SonYilNetSatis, OncekiYilAktifToplami, SonYilAktifToplami)
                VALUES
                    (@BasvuruId, @SiraNo, @AdUnvan, @TcknVkn, @KisiTuru, @PayOrani, @HesabaDahilOran, @OzelKamuNiteligi, @NihaiFaydalaniciBilgisi, @UboKycBelgeAdi, @UboKycDosyaId,
                     @OncekiYilNetSatis, @SonYilNetSatis, @OncekiYilAktifToplami, @SonYilAktifToplami);";

            int siraNo = 1;
            foreach (BasvuruOrtak ortak in ortaklar)
            {
                ortak.siraNo = siraNo++;
                await using SqlCommand command = KomutOlustur(sql);
                BasvuruOrtakParametreleriEkle(command, basvuruId, ortak);
                await command.ExecuteNonQueryAsync();
            }
        }

        private async Task BasvuruOrtaklariniSilAsync(int basvuruId)
        {
            const string sql = @"DELETE FROM dbo.BasvuruOrtaklar WHERE BasvuruId = @BasvuruId;";

            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@BasvuruId", basvuruId);
            await command.ExecuteNonQueryAsync();
        }

        public async Task BasvuruOrtakUboKycDosyasiGuncelleAsync(int basvuruId, int siraNo, int dosyaId, string dosyaAdi)
        {
            const string sql = @"
                UPDATE dbo.BasvuruOrtaklar
                SET UboKycDosyaId = @UboKycDosyaId,
                    UboKycBelgeAdi = @UboKycBelgeAdi
                WHERE BasvuruId = @BasvuruId
                    AND SiraNo = @SiraNo;";

            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@BasvuruId", basvuruId);
            command.Parameters.AddWithValue("@SiraNo", siraNo);
            command.Parameters.AddWithValue("@UboKycDosyaId", dosyaId);
            command.Parameters.AddWithValue("@UboKycBelgeAdi", dosyaAdi?.Trim() ?? "");
            await command.ExecuteNonQueryAsync();
        }

        public async Task BasvuruOrtakUboKycDosyasiGuncelleAsync(int basvuruId, string tcknVkn, int dosyaId, string dosyaAdi)
        {
            const string sql = @"
                UPDATE dbo.BasvuruOrtaklar
                SET UboKycDosyaId = @UboKycDosyaId,
                    UboKycBelgeAdi = @UboKycBelgeAdi
                WHERE BasvuruId = @BasvuruId
                    AND TcknVkn = @TcknVkn;";

            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@BasvuruId", basvuruId);
            command.Parameters.AddWithValue("@TcknVkn", tcknVkn?.Trim() ?? "");
            command.Parameters.AddWithValue("@UboKycDosyaId", dosyaId);
            command.Parameters.AddWithValue("@UboKycBelgeAdi", dosyaAdi?.Trim() ?? "");
            await command.ExecuteNonQueryAsync();
        }

        private static void BasvuruOrtakParametreleriEkle(SqlCommand command, int basvuruId, BasvuruOrtak ortak)
        {
            command.Parameters.AddWithValue("@BasvuruId", basvuruId);
            command.Parameters.AddWithValue("@SiraNo", ortak.siraNo);
            command.Parameters.AddWithValue("@AdUnvan", ortak.adUnvan?.Trim() ?? "");
            command.Parameters.AddWithValue("@TcknVkn", TcknVknNormalizeEt(ortak.tcknVkn));
            command.Parameters.AddWithValue("@KisiTuru", ortak.kisiTuru?.Trim() ?? "");
            command.Parameters.AddWithValue("@PayOrani", DbNull(ortak.payOrani));
            command.Parameters.AddWithValue("@HesabaDahilOran", DbNull(ortak.hesabaDahilOran));
            command.Parameters.AddWithValue("@OzelKamuNiteligi", ortak.ozelKamuNiteligi?.Trim() ?? "");
            command.Parameters.AddWithValue("@NihaiFaydalaniciBilgisi", ortak.nihaiFaydalaniciBilgisi?.Trim() ?? "");
            command.Parameters.AddWithValue("@UboKycBelgeAdi", ortak.uboKycBelgeAdi?.Trim() ?? "");
            command.Parameters.AddWithValue("@UboKycDosyaId", DbNull(ortak.uboKycDosyaId));
            command.Parameters.AddWithValue("@OncekiYilNetSatis", DbNull(ortak.oncekiYilNetSatis));
            command.Parameters.AddWithValue("@SonYilNetSatis", DbNull(ortak.sonYilNetSatis));
            command.Parameters.AddWithValue("@OncekiYilAktifToplami", DbNull(ortak.oncekiYilAktifToplami));
            command.Parameters.AddWithValue("@SonYilAktifToplami", DbNull(ortak.sonYilAktifToplami));
        }

        private static string TcknVknNormalizeEt(string? tcknVkn)
        {
            return new string((tcknVkn ?? "")
                .Trim()
                .Where(char.IsLetterOrDigit)
                .Select(char.ToUpperInvariant)
                .ToArray());
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

                SELECT dz.Id, dz.Ad, dz.Aciklama, dz.Aktif, dza.Id, dza.SiraNo, dza.Ad, dza.Aciklama, dza.Aktif, bdza.YapilacakFaaliyetler
                FROM dbo.BasvuruDegerZinciriAsama bdza
                LEFT JOIN dbo.DegerZinciriAsama dza ON dza.Id = bdza.DegerZinciriAsamaId
                LEFT JOIN dbo.DegerZinciri dz ON dz.Id = dza.DegerZinciriId
                WHERE bdza.BasvuruId = @BasvuruId
                ORDER BY dza.SiraNo;

                SELECT Id, BasvuruId, SiraNo, AdUnvan, TcknVkn, KisiTuru, PayOrani, HesabaDahilOran, OzelKamuNiteligi, NihaiFaydalaniciBilgisi, UboKycBelgeAdi, UboKycDosyaId,
                    OncekiYilNetSatis, SonYilNetSatis, OncekiYilAktifToplami, SonYilAktifToplami
                FROM dbo.BasvuruOrtaklar
                WHERE BasvuruId = @BasvuruId
                ORDER BY SiraNo, Id;

                SELECT Id, BasvuruId, SiraNo, Tckn, Ad, Soyad, Gorev, DosyaAdi, DosyaId
                FROM dbo.BasvuruAdliSicilKisiler
                WHERE BasvuruId = @BasvuruId
                ORDER BY SiraNo, Id;";

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
                dza.yapilacakFaaliyetler = NullOkuString(reader, kolNo++);

                asamalar.Add(dza);
            }

            await reader.NextResultAsync();
            List<BasvuruOrtak> ortaklar = new List<BasvuruOrtak>();
            while (await reader.ReadAsync())
            {
                ortaklar.Add(BasvuruOrtakOku(reader));
            }

            await reader.NextResultAsync();
            List<BasvuruAdliSicilKisi> adliSicilKisileri = new List<BasvuruAdliSicilKisi>();
            while (await reader.ReadAsync())
            {
                adliSicilKisileri.Add(BasvuruAdliSicilKisiOku(reader));
            }

            basvuru.YatirimAdresleri = adresler;

            basvuru.yatirim.harcamaTurleri = harcamaTurleri;

            basvuru.yatirim.degerZinciriAsamalari = asamalar;
            basvuru.yatirim.degerZinciriId = asamalar.Count >= 1
                    ? asamalar.First().dz.id
                    : null;

            basvuru.ortaklik.ortaklar = ortaklar;
            basvuru.AdliSicilKisileri = adliSicilKisileri;
        }

        public async Task<List<BasvuruAdliSicilKisi>> BasvuruAdliSicilKisileriKaydetAsync(int basvuruId, List<BasvuruAdliSicilKisi>? kisiler)
        {
            kisiler ??= new List<BasvuruAdliSicilKisi>();
            List<int> gelenIdler = kisiler.Where(x => x.id > 0).Select(x => x.id).Distinct().ToList();

            string silSql = gelenIdler.Count == 0
                ? @"DELETE FROM dbo.BasvuruAdliSicilKisiler WHERE BasvuruId = @BasvuruId;"
                : $@"DELETE FROM dbo.BasvuruAdliSicilKisiler
                    WHERE BasvuruId = @BasvuruId
                      AND Id NOT IN ({string.Join(",", gelenIdler)});";

            await using (SqlCommand silCommand = KomutOlustur(silSql))
            {
                silCommand.Parameters.AddWithValue("@BasvuruId", basvuruId);
                await silCommand.ExecuteNonQueryAsync();
            }

            int siraNo = 1;
            foreach (BasvuruAdliSicilKisi kisi in kisiler)
            {
                kisi.basvuruId = basvuruId;
                kisi.siraNo = siraNo++;
                if (kisi.id > 0)
                    await BasvuruAdliSicilKisiGuncelleAsync(kisi);
                else
                    kisi.id = await BasvuruAdliSicilKisiEkleAsync(kisi);
            }

            return await BasvuruAdliSicilKisileriOkuAsync(basvuruId);
        }

        public async Task<List<BasvuruAdliSicilKisi>> BasvuruAdliSicilKisileriOkuAsync(int basvuruId)
        {
            const string sql = @"
                SELECT Id, BasvuruId, SiraNo, Tckn, Ad, Soyad, Gorev, DosyaAdi, DosyaId
                FROM dbo.BasvuruAdliSicilKisiler
                WHERE BasvuruId = @BasvuruId
                ORDER BY SiraNo, Id;";

            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@BasvuruId", basvuruId);
            List<BasvuruAdliSicilKisi> kisiler = new List<BasvuruAdliSicilKisi>();
            await using SqlDataReader reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                kisiler.Add(BasvuruAdliSicilKisiOku(reader));
            }
            return kisiler;
        }

        public async Task BasvuruAdliSicilDosyasiGuncelleAsync(int basvuruId, int kisiId, int dosyaId, string dosyaAdi)
        {
            const string sql = @"
                UPDATE dbo.BasvuruAdliSicilKisiler
                SET DosyaId = @DosyaId,
                    DosyaAdi = @DosyaAdi
                WHERE BasvuruId = @BasvuruId
                  AND Id = @Id;";

            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@BasvuruId", basvuruId);
            command.Parameters.AddWithValue("@Id", kisiId);
            command.Parameters.AddWithValue("@DosyaId", dosyaId);
            command.Parameters.AddWithValue("@DosyaAdi", dosyaAdi?.Trim() ?? "");
            await command.ExecuteNonQueryAsync();
        }

        private async Task<int> BasvuruAdliSicilKisiEkleAsync(BasvuruAdliSicilKisi kisi)
        {
            const string sql = @"
                INSERT INTO dbo.BasvuruAdliSicilKisiler
                    (BasvuruId, SiraNo, Tckn, Ad, Soyad, Gorev, DosyaAdi, DosyaId)
                OUTPUT INSERTED.Id
                VALUES
                    (@BasvuruId, @SiraNo, @Tckn, @Ad, @Soyad, @Gorev, @DosyaAdi, @DosyaId);";

            await using SqlCommand command = KomutOlustur(sql);
            BasvuruAdliSicilKisiParametreleriEkle(command, kisi);
            return OrtakFonksiyonlar.Int32Yap(await command.ExecuteScalarAsync());
        }

        private async Task BasvuruAdliSicilKisiGuncelleAsync(BasvuruAdliSicilKisi kisi)
        {
            const string sql = @"
                UPDATE dbo.BasvuruAdliSicilKisiler
                SET SiraNo = @SiraNo,
                    Tckn = @Tckn,
                    Ad = @Ad,
                    Soyad = @Soyad,
                    Gorev = @Gorev,
                    DosyaAdi = @DosyaAdi,
                    DosyaId = @DosyaId
                WHERE Id = @Id
                  AND BasvuruId = @BasvuruId;";

            await using SqlCommand command = KomutOlustur(sql);
            BasvuruAdliSicilKisiParametreleriEkle(command, kisi);
            command.Parameters.AddWithValue("@Id", kisi.id);
            await command.ExecuteNonQueryAsync();
        }

        private static void BasvuruAdliSicilKisiParametreleriEkle(SqlCommand command, BasvuruAdliSicilKisi kisi)
        {
            command.Parameters.AddWithValue("@BasvuruId", kisi.basvuruId);
            command.Parameters.AddWithValue("@SiraNo", kisi.siraNo);
            command.Parameters.AddWithValue("@Tckn", TcknVknNormalizeEt(kisi.tckn));
            command.Parameters.AddWithValue("@Ad", kisi.ad?.Trim() ?? "");
            command.Parameters.AddWithValue("@Soyad", kisi.soyad?.Trim() ?? "");
            command.Parameters.AddWithValue("@Gorev", kisi.gorev?.Trim() ?? "");
            command.Parameters.AddWithValue("@DosyaAdi", kisi.dosyaAdi?.Trim() ?? "");
            command.Parameters.AddWithValue("@DosyaId", DbNull(kisi.dosyaId));
        }

        private static BasvuruAdliSicilKisi BasvuruAdliSicilKisiOku(SqlDataReader reader)
        {
            return new BasvuruAdliSicilKisi
            {
                id = reader.GetInt32(0),
                basvuruId = reader.GetInt32(1),
                siraNo = reader.GetInt32(2),
                tckn = NullOkuString(reader, 3) ?? "",
                ad = NullOkuString(reader, 4) ?? "",
                soyad = NullOkuString(reader, 5) ?? "",
                gorev = NullOkuString(reader, 6) ?? "",
                dosyaAdi = NullOkuString(reader, 7) ?? "",
                dosyaId = NullOkuInt(reader, 8)
            };
        }

        private static BasvuruOrtak BasvuruOrtakOku(SqlDataReader reader)
        {
            return new BasvuruOrtak
            {
                id = reader.GetInt32(0),
                basvuruId = reader.GetInt32(1),
                siraNo = reader.GetInt32(2),
                adUnvan = NullOkuString(reader, 3) ?? "",
                tcknVkn = NullOkuString(reader, 4) ?? "",
                kisiTuru = NullOkuString(reader, 5) ?? "",
                payOrani = NullOkuDecimal(reader, 6),
                hesabaDahilOran = NullOkuDecimal(reader, 7),
                ozelKamuNiteligi = NullOkuString(reader, 8) ?? "",
                nihaiFaydalaniciBilgisi = NullOkuString(reader, 9) ?? "",
                uboKycBelgeAdi = NullOkuString(reader, 10) ?? "",
                uboKycDosyaId = NullOkuInt(reader, 11),
                oncekiYilNetSatis = NullOkuDecimal(reader, 12),
                sonYilNetSatis = NullOkuDecimal(reader, 13),
                oncekiYilAktifToplami = NullOkuDecimal(reader, 14),
                sonYilAktifToplami = NullOkuDecimal(reader, 15)
            };
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



