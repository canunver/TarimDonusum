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
                    B.KayitTuru,
                    B.OnBasvuruSonrasiDegisiklikVarMi,
                    B.OnBasvuruSonrasiDegisiklikSebebi,
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
                    B.BasvuruKonusuTesis,
                    B.OrganizeAlanTuru,
                    B.PlanlananBaslangicTarihi,
                    B.PlanlananTamamlanmaTarihi,
                    B.ToplamYatirimTutari,
                    B.UygunHarcamaTutari,
                    B.TalepEdilenDestekTutari,
                    B.TalepEdilenFinansmanOrani,
                    B.OnBasvuruSahibiKatkisi,
                    B.BasvuruSahibiKatkisi,
                    B.TalepEdilenVadeSuresiAy,
                    B.YatirimSuresiAy,
                    B.OdemeSuresiAy,
                    B.DestekOrani,
                    B.DigerFinansmanKaynaklariAciklama,
                    B.FinansmanParaBirimi,
                    B.DigerFinansmanKaynaklari,
                    B.OncekiRffOnayliTutar,
                    B.OncekiRffSozlesmesiKapaliMi,
                    B.BankaTeminatMektubuSaglanabilirMi,
                    B.YatiriminAmaci,
                    B.YatirimFaaliyetleri,
                    B.YatirimGirdileri,
                    B.YatirimCiktilari,
                    B.PikkListesiJson,
                    B.YatirimOzetiJson,
                    B.DbCtpTeknikProjeJson,
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
                    B.OncekiYilIhracatSatis,
                    B.SonYilIhracatSatis,
                    B.OncekiYilCalisanSayisi,
                    B.SonYilCalisanSayisi,
                    B.MaliAciklama,
                    B.MaliBelgeReferanslariJson,
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
                    B.TaahhutBeyanlarJson,
                    B.DenetimAnketi,
                    B.SistemDenetimAnketi,
                    B.DenetimGerekcesi,
                    B.DenetimSonucu,

                    D.Yil,
                    D.Ad,
                    D.BasvuruyaAcikMi,
                    D.BasvuruBaslangicTarihi,
                    D.BasvuruBitisTarihi,
                    D.OnBasvuruBaslangicTarihi,
                    D.OnBasvuruBitisTarihi,
                    D.OnBasvuruCevrimKuru,
                    D.BasvuruCevrimKuru,
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
                AND B.SiraNo = 0
                ORDER BY D.Yil DESC, Id DESC;";

            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@KullaniciId", kullaniciId);

            return await ListeOkuAsync(command);
        }

        public async Task<Sonuc<List<Basvuru>>> KullaniciBasvuruVersiyonlariniListeleAsync(int kullaniciId)
        {
            string sql = BasvuruSelectSql() + @" WHERE EXISTS (
                    SELECT 1
                    FROM dbo.FirmaKullanici fk
                    WHERE fk.FirmaId = BA.FirmaId
                        AND fk.KullaniciId = @KullaniciId
                        AND fk.Aktif = 1
                )
                ORDER BY D.Yil DESC, B.BasvuruAnaId DESC, B.RevizyonNo DESC, B.Id DESC;";

            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@KullaniciId", kullaniciId);

            return await ListeOkuAsync(command);
        }

        public async Task<Sonuc<List<Basvuru>>> TumunuListeleAsync()
        {
            string sql = BasvuruSelectSql() + " WHERE B.SiraNo = 0 ORDER BY D.Yil DESC, Id DESC;";

            await using SqlCommand command = KomutOlustur(sql);
            return await ListeOkuAsync(command);
        }

        public async Task<Sonuc<List<Basvuru>>> TumVersiyonlariListeleAsync()
        {
            string sql = BasvuruSelectSql() + " ORDER BY D.Yil DESC, B.BasvuruAnaId DESC, B.RevizyonNo DESC, B.Id DESC;";

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
                    BasvuruAnaId, RevizyonNo, SiraNo, KayitTuru, BasvuruSahibiTuru, HukukiTurSirketTuru, YonetimKuruluUyeleriAdliSicilKisiler, SonIkiYildirFaalMi)
                OUTPUT INSERTED.Id
                VALUES (
                    @BasvuruAnaId, @RevizyonNo, @SiraNo, @KayitTuru, @BasvuruSahibiTuru, @HukukiTurSirketTuru, @YonetimKuruluUyeleriAdliSicilKisiler, @SonIkiYildirFaalMi);";

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
            command.Parameters.AddWithValue("@OnBasvuruSonrasiDegisiklikVarMi", basvuru.onBasvuruSonrasiDegisiklikVarMi.HasValue ? (basvuru.onBasvuruSonrasiDegisiklikVarMi.Value ? 1 : 0) : DBNull.Value);
            command.Parameters.AddWithValue("@OnBasvuruSonrasiDegisiklikSebebi", DbNull(basvuru.onBasvuruSonrasiDegisiklikSebebi));

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
                    TalepEdilenVadeSuresiAy = @TalepEdilenVadeSuresiAy,
                    YatirimSuresiAy = @YatirimSuresiAy,
                    OdemeSuresiAy = @OdemeSuresiAy,
                    DestekOrani = @DestekOrani,
                    DigerFinansmanKaynaklariAciklama = @DigerFinansmanKaynaklariAciklama,
                    FinansmanParaBirimi = @FinansmanParaBirimi,
                    DigerFinansmanKaynaklari = @DigerFinansmanKaynaklari,
                    OncekiRffOnayliTutar = @OncekiRffOnayliTutar,
                    OncekiRffSozlesmesiKapaliMi = @OncekiRffSozlesmesiKapaliMi,
                    BankaTeminatMektubuSaglanabilirMi = @BankaTeminatMektubuSaglanabilirMi
                WHERE Id = @Id;";

            await using SqlCommand command = KomutOlustur(sql);

            command.Parameters.AddWithValue("@ToplamYatirimTutari", DbNull(finans.toplamYatirimTutari));
            command.Parameters.AddWithValue("@UygunHarcamaTutari", DbNull(finans.uygunHarcamaTutari));
            command.Parameters.AddWithValue("@TalepEdilenDestekTutari", DbNull(finans.talepEdilenDestekTutari));
            command.Parameters.AddWithValue("@TalepEdilenFinansmanOrani", DbNull(finans.talepEdilenFinansmanOrani));
            command.Parameters.AddWithValue("@OnBasvuruSahibiKatkisi", DbNull(finans.onBasvuruSahibiKatkisi));
            command.Parameters.AddWithValue("@BasvuruSahibiKatkisi", DbNull(finans.basvuruSahibiKatkisi));
            command.Parameters.AddWithValue("@TalepEdilenVadeSuresiAy", DbNull(finans.talepEdilenVadeSuresiAy));
            command.Parameters.AddWithValue("@YatirimSuresiAy", DbNull(finans.yatirimSuresiAy));
            command.Parameters.AddWithValue("@OdemeSuresiAy", DbNull(finans.odemeSuresiAy));
            command.Parameters.AddWithValue("@DestekOrani", DbNull(finans.destekOrani));
            command.Parameters.AddWithValue("@DigerFinansmanKaynaklariAciklama", DbNull(finans.digerFinansmanKaynaklariAciklama));
            command.Parameters.AddWithValue("@FinansmanParaBirimi", DbNull(finans.finansmanParaBirimi));
            command.Parameters.AddWithValue("@DigerFinansmanKaynaklari", DbNull(finans.digerFinansmanKaynaklari));
            command.Parameters.AddWithValue("@OncekiRffOnayliTutar", DbNull(finans.oncekiRffOnayliTutar));
            command.Parameters.AddWithValue("@OncekiRffSozlesmesiKapaliMi", DbNull(finans.oncekiRffSozlesmesiKapaliMi));
            command.Parameters.AddWithValue("@BankaTeminatMektubuSaglanabilirMi", DbNull(finans.bankaTeminatMektubuSaglanabilirMi));
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

        public async Task DbCtpTeknikProjeKaydetAsync(BasvuruDbCtpTeknikProje teknikProje)
        {
            const string sql = @"
                UPDATE dbo.Basvuru
                SET DbCtpTeknikProjeJson = @DbCtpTeknikProjeJson
                WHERE Id = @Id;";

            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@DbCtpTeknikProjeJson", DbNull(teknikProje.dbCtpTeknikProjeJson));
            command.Parameters.AddWithValue("@Id", teknikProje.basvuruId);
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
                    OncekiYilIhracatSatis = @OncekiYilIhracatSatis,
                    SonYilIhracatSatis = @SonYilIhracatSatis,
                    OncekiYilCalisanSayisi = @OncekiYilCalisanSayisi,
                    SonYilCalisanSayisi = @SonYilCalisanSayisi,
                    MaliAciklama = @MaliAciklama,
                    MaliBelgeReferanslariJson = @MaliBelgeReferanslariJson,
                    BagimsizDenetimeTabiMi = @BagimsizDenetimeTabiMi
                WHERE Id = @Id;";

            await using SqlCommand command = KomutOlustur(sql);

            command.Parameters.AddWithValue("@OncekiYilNetSatis", DbNull(mali.oncekiYilNetSatis));
            command.Parameters.AddWithValue("@SonYilNetSatis", DbNull(mali.sonYilNetSatis));
            command.Parameters.AddWithValue("@OncekiYilAktifToplami", DbNull(mali.oncekiYilAktifToplami));
            command.Parameters.AddWithValue("@SonYilAktifToplami", DbNull(mali.sonYilAktifToplami));
            command.Parameters.AddWithValue("@OncekiYilIhracatSatis", DbNull(mali.oncekiYilIhracatSatis));
            command.Parameters.AddWithValue("@SonYilIhracatSatis", DbNull(mali.sonYilIhracatSatis));
            command.Parameters.AddWithValue("@OncekiYilCalisanSayisi", DbNull(mali.oncekiYilCalisanSayisi));
            command.Parameters.AddWithValue("@SonYilCalisanSayisi", DbNull(mali.sonYilCalisanSayisi));
            command.Parameters.AddWithValue("@MaliAciklama", DbNull(mali.aciklama));
            command.Parameters.AddWithValue("@MaliBelgeReferanslariJson", DbNull(mali.belgeReferanslariJson));
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
            yatirim.yatirimTurleri = yatirim.yatirimTurleri.Distinct().Where(x => x > 0).ToList();
            yatirim.yatirimTuru = yatirim.yatirimTurleri.Count > 0
                ? (enumYatirimTuru)yatirim.yatirimTurleri[0]
                : enumYatirimTuru.Tanimsiz;
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

        public async Task OrtaklikKaydetAsync(Basvuru basvuru, bool ortaklariKaydet = true)
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

            if (ortaklariKaydet)
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

        public async Task TaahhutBeyanlariKaydetAsync(BasvuruTaahhutBeyanlar beyanlar)
        {
            const string sql = @"
                UPDATE dbo.Basvuru SET
                    TaahhutBeyanlarJson = @TaahhutBeyanlarJson
                WHERE Id = @Id;";
            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@Id", beyanlar.basvuruId);
            command.Parameters.AddWithValue("@TaahhutBeyanlarJson", DbNull(beyanlar.taahhutBeyanlarJson));
            await command.ExecuteNonQueryAsync();
        }

        public async Task<bool> IncelemeyeGonderAsync(int basvuruId)
        {
            const string sql = @"
                UPDATE BA
                SET BA.Durum = @YeniDurum
                FROM dbo.BasvuruAna BA
                INNER JOIN dbo.Basvuru B ON B.BasvuruAnaId = BA.Id
                WHERE B.Id = @BasvuruId
                  AND BA.Durum IN (@OnBasvuruDurumu, @DuzeltmeDurumu);";

            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@BasvuruId", basvuruId);
            command.Parameters.AddWithValue("@OnBasvuruDurumu", (int)enumBasvuruDurum.OnBasvuruDurumu);
            command.Parameters.AddWithValue("@DuzeltmeDurumu", (int)enumBasvuruDurum.OnBasvuruDuzeltmeDurumu);
            command.Parameters.AddWithValue("@YeniDurum", (int)enumBasvuruDurum.OnBasvuruIncelemeDurumu);
            return await command.ExecuteNonQueryAsync() == 1;
        }

        public async Task<bool> OnBasvuruDenetimiKaydetAsync(Basvuru basvuru, enumBasvuruDurum yeniDurum)
        {
            const string sql = @"
                UPDATE B
                SET B.DenetimGerekcesi = @DenetimGerekcesi,
                    B.DenetimSonucu = @DenetimSonucu
                FROM dbo.Basvuru B
                INNER JOIN dbo.BasvuruAna BA ON BA.Id = B.BasvuruAnaId
                WHERE B.Id = @BasvuruId
                  AND BA.Durum = @MevcutDurum;

                IF @@ROWCOUNT = 1
                BEGIN
                    UPDATE BA
                    SET BA.Durum = @YeniDurum
                    FROM dbo.BasvuruAna BA
                    INNER JOIN dbo.Basvuru B ON B.BasvuruAnaId = BA.Id
                    WHERE B.Id = @BasvuruId
                      AND BA.Durum = @MevcutDurum;
                    SELECT @@ROWCOUNT;
                END
                ELSE
                    SELECT 0;";

            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@BasvuruId", basvuru.Id);
            command.Parameters.AddWithValue("@DenetimGerekcesi", DbNull(basvuru.DenetimGerekcesi));
            command.Parameters.AddWithValue("@DenetimSonucu", (int)basvuru.DenetimSonucu!.Value);
            command.Parameters.AddWithValue("@MevcutDurum", (int)enumBasvuruDurum.OnBasvuruIncelemeDurumu);
            command.Parameters.AddWithValue("@YeniDurum", (int)yeniDurum);
            return Convert.ToInt32(await command.ExecuteScalarAsync()) == 1;
        }

        public async Task<bool> OnBasvuruDenetimTaslagiKaydetAsync(Basvuru basvuru)
        {
            const string sql = @"
                UPDATE B
                SET B.DenetimGerekcesi = @DenetimGerekcesi,
                    B.DenetimSonucu = @DenetimSonucu
                FROM dbo.Basvuru B
                INNER JOIN dbo.BasvuruAna BA ON BA.Id = B.BasvuruAnaId
                WHERE B.Id = @BasvuruId
                  AND BA.Durum = @MevcutDurum;";

            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@BasvuruId", basvuru.Id);
            command.Parameters.AddWithValue("@DenetimGerekcesi", DbNull(basvuru.DenetimGerekcesi));
            command.Parameters.AddWithValue("@DenetimSonucu",
                basvuru.DenetimSonucu.HasValue && basvuru.DenetimSonucu != enumOnBasvuruDenetimSonucu.Tanimsiz
                    ? (int)basvuru.DenetimSonucu.Value
                    : DBNull.Value);
            command.Parameters.AddWithValue("@MevcutDurum", (int)enumBasvuruDurum.OnBasvuruIncelemeDurumu);
            return await command.ExecuteNonQueryAsync() == 1;
        }

        public async Task<bool> DenetimListesiKaydetAsync(int basvuruId, string json, bool sistemListesi)
        {
            string kolon = sistemListesi ? "SistemDenetimAnketi" : "DenetimAnketi";
            string sql = $@"
                UPDATE B SET B.{kolon} = @Json
                FROM dbo.Basvuru B
                INNER JOIN dbo.BasvuruAna BA ON BA.Id = B.BasvuruAnaId
                WHERE B.Id = @BasvuruId
                  AND BA.Durum = @Durum
                  AND B.SiraNo = 0;";
            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@BasvuruId", basvuruId);
            command.Parameters.AddWithValue("@Json", json);
            command.Parameters.AddWithValue("@Durum", (int)enumBasvuruDurum.OnBasvuruIncelemeDurumu);
            return await command.ExecuteNonQueryAsync() == 1;
        }

        public async Task<int> YeniRevizyonOlusturAsync(int kaynakBasvuruId, enumBasvuruKayitTuru kayitTuru = enumBasvuruKayitTuru.OnBasvuru)
        {
            const string sql = @"
                DECLARE @BasvuruAnaId INT;
                DECLARE @YeniRevizyonNo INT;
                DECLARE @YeniBasvuruId INT;
                DECLARE @Kolonlar NVARCHAR(MAX);
                DECLARE @KopyalamaSql NVARCHAR(MAX);

                SELECT @BasvuruAnaId = BasvuruAnaId
                FROM dbo.Basvuru WITH (UPDLOCK, HOLDLOCK)
                WHERE Id = @KaynakBasvuruId;

                IF @BasvuruAnaId IS NULL
                    THROW 50001, N'Kaynak başvuru bulunamadı.', 1;

                SELECT @YeniRevizyonNo = ISNULL(MAX(RevizyonNo), -1) + 1
                FROM dbo.Basvuru WITH (UPDLOCK, HOLDLOCK)
                WHERE BasvuruAnaId = @BasvuruAnaId;

                UPDATE dbo.Basvuru
                SET SiraNo = SiraNo + 100000
                WHERE BasvuruAnaId = @BasvuruAnaId;

                SELECT @Kolonlar = STRING_AGG(QUOTENAME(name), N',')
                FROM sys.columns
                WHERE object_id = OBJECT_ID(N'dbo.Basvuru')
                  AND name NOT IN (N'Id', N'RevizyonNo', N'SiraNo',
                                   N'DenetimAnketi', N'SistemDenetimAnketi', N'DenetimGerekcesi', N'DenetimSonucu',
                                   N'KayitTuru', N'OnBasvuruSonrasiDegisiklikVarMi', N'OnBasvuruSonrasiDegisiklikSebebi');

                SET @KopyalamaSql = N'
                    INSERT INTO dbo.Basvuru (' + @Kolonlar + N', RevizyonNo, SiraNo, KayitTuru,
                                             OnBasvuruSonrasiDegisiklikVarMi, OnBasvuruSonrasiDegisiklikSebebi,
                                             DenetimAnketi, SistemDenetimAnketi, DenetimGerekcesi, DenetimSonucu)
                    SELECT ' + @Kolonlar + N', @RevizyonNo, 0, @KayitTuru, NULL, NULL, NULL, NULL, NULL, NULL
                    FROM dbo.Basvuru
                    WHERE Id = @KaynakId;
                    SET @YeniId = CONVERT(INT, SCOPE_IDENTITY());';

                EXEC sp_executesql @KopyalamaSql,
                    N'@KaynakId INT, @RevizyonNo INT, @KayitTuru INT, @YeniId INT OUTPUT',
                    @KaynakId = @KaynakBasvuruId,
                    @RevizyonNo = @YeniRevizyonNo,
                    @KayitTuru = @KayitTuru,
                    @YeniId = @YeniBasvuruId OUTPUT;

                INSERT INTO dbo.BasvuruHarcamaTuru (BasvuruId, HarcamaTuru)
                    SELECT @YeniBasvuruId, HarcamaTuru
                    FROM dbo.BasvuruHarcamaTuru WHERE BasvuruId = @KaynakBasvuruId;

                INSERT INTO dbo.BasvuruYatirimTuru (BasvuruId, YatirimTuru)
                    SELECT @YeniBasvuruId, YatirimTuru
                    FROM dbo.BasvuruYatirimTuru WHERE BasvuruId = @KaynakBasvuruId;

                INSERT INTO dbo.BasvuruDegerZinciriAsama (BasvuruId, DegerZinciriAsamaId, YapilacakFaaliyetler)
                    SELECT @YeniBasvuruId, DegerZinciriAsamaId, YapilacakFaaliyetler
                    FROM dbo.BasvuruDegerZinciriAsama WHERE BasvuruId = @KaynakBasvuruId;

                DECLARE @UrunEsleme TABLE(EskiId INT NOT NULL,YeniId INT NOT NULL);
                MERGE dbo.BasvuruYatirimOnBilgi AS hedef
                USING(SELECT Id,Tur,SiraNo,Ad,Miktar,Birim,TekPanelGucu,TekPanelGucuBirim,ToplamGuc,ToplamGucBirim FROM dbo.BasvuruYatirimOnBilgi WHERE BasvuruId=@KaynakBasvuruId) AS kaynak ON 1=0
                WHEN NOT MATCHED THEN INSERT(BasvuruId,Tur,SiraNo,Ad,Miktar,Birim,TekPanelGucu,TekPanelGucuBirim,ToplamGuc,ToplamGucBirim) VALUES(@YeniBasvuruId,kaynak.Tur,kaynak.SiraNo,kaynak.Ad,kaynak.Miktar,kaynak.Birim,kaynak.TekPanelGucu,kaynak.TekPanelGucuBirim,kaynak.ToplamGuc,kaynak.ToplamGucBirim)
                OUTPUT kaynak.Id,inserted.Id INTO @UrunEsleme(EskiId,YeniId);

                DECLARE @MakineEsleme TABLE(EskiId INT NOT NULL, YeniId INT NOT NULL);
                MERGE dbo.BasvuruMakine AS hedef
                USING (SELECT Id,SiraNo,Ad,Birim,Miktar,Aciklama,Marka,Model,KapasiteOzellikleri,YerlesimPlaniSiraNo,KullanimAmaci,Durum,KapasiteSecimGerekcesi FROM dbo.BasvuruMakine WHERE BasvuruId=@KaynakBasvuruId) AS kaynak
                ON 1=0
                WHEN NOT MATCHED THEN INSERT(BasvuruId,SiraNo,Ad,Birim,Miktar,Aciklama,Marka,Model,KapasiteOzellikleri,YerlesimPlaniSiraNo,KullanimAmaci,Durum,KapasiteSecimGerekcesi)
                    VALUES(@YeniBasvuruId,kaynak.SiraNo,kaynak.Ad,kaynak.Birim,kaynak.Miktar,kaynak.Aciklama,kaynak.Marka,kaynak.Model,kaynak.KapasiteOzellikleri,kaynak.YerlesimPlaniSiraNo,kaynak.KullanimAmaci,kaynak.Durum,kaynak.KapasiteSecimGerekcesi)
                OUTPUT kaynak.Id,inserted.Id INTO @MakineEsleme(EskiId,YeniId);
                INSERT dbo.BasvuruMakineOzellik(MakineId,SiraNo,Baslik,AciklamaAsgariGereklilik,ZorunluMu)
                    SELECT e.YeniId,o.SiraNo,o.Baslik,o.AciklamaAsgariGereklilik,o.ZorunluMu FROM dbo.BasvuruMakineOzellik o INNER JOIN @MakineEsleme e ON e.EskiId=o.MakineId;
                INSERT dbo.BasvuruMakineTeklif(MakineId,SiraNo,BasvuruyaEsas,Tedarikci,Marka,Model,ParaBirimi,Kur,BirimFiyat,TeklifTarihi,GecerlilikTarihi,TeklifBelgesiDosyaId,TeklifBelgesiDosyaAdi,Aciklama)
                    SELECT e.YeniId,t.SiraNo,t.BasvuruyaEsas,t.Tedarikci,t.Marka,t.Model,t.ParaBirimi,t.Kur,t.BirimFiyat,t.TeklifTarihi,t.GecerlilikTarihi,t.TeklifBelgesiDosyaId,t.TeklifBelgesiDosyaAdi,t.Aciklama FROM dbo.BasvuruMakineTeklif t INNER JOIN @MakineEsleme e ON e.EskiId=t.MakineId;
                DECLARE @SurecEsleme TABLE(EskiId INT NOT NULL,YeniId INT NOT NULL);
                MERGE dbo.BasvuruUrunSurec AS hedef USING(SELECT s.Id,u.YeniId UrunId,s.SiraNo,s.SurecAdi FROM dbo.BasvuruUrunSurec s INNER JOIN @UrunEsleme u ON u.EskiId=s.UrunId WHERE s.BasvuruId=@KaynakBasvuruId) AS kaynak ON 1=0
                WHEN NOT MATCHED THEN INSERT(BasvuruId,UrunId,SiraNo,SurecAdi) VALUES(@YeniBasvuruId,kaynak.UrunId,kaynak.SiraNo,kaynak.SurecAdi)
                OUTPUT kaynak.Id,inserted.Id INTO @SurecEsleme(EskiId,YeniId);
                INSERT dbo.BasvuruUrunSurecMakine(SurecId,MakineId,SiraNo,Adet,YerlesimPlaniNo,GirdilerMiktarlar,CiktilarMiktarlar,IslemeKapasitesi,GunlukCalismaSuresi,GunlukCalismaSuresiBirimi,Aciklama)
                    SELECT se.YeniId,me.YeniId,sm.SiraNo,sm.Adet,sm.YerlesimPlaniNo,sm.GirdilerMiktarlar,sm.CiktilarMiktarlar,sm.IslemeKapasitesi,sm.GunlukCalismaSuresi,sm.GunlukCalismaSuresiBirimi,sm.Aciklama FROM dbo.BasvuruUrunSurecMakine sm INNER JOIN @SurecEsleme se ON se.EskiId=sm.SurecId INNER JOIN @MakineEsleme me ON me.EskiId=sm.MakineId;
                DECLARE @BinaEsleme TABLE(EskiId INT NOT NULL,YeniId INT NOT NULL);
                MERGE dbo.BasvuruBina AS hedef USING(SELECT Id,SiraNo,Ad,MevcutYeni,YatirimSekli,DestekTalebi,VaziyetPlaniNo FROM dbo.BasvuruBina WHERE BasvuruId=@KaynakBasvuruId) AS kaynak ON 1=0
                WHEN NOT MATCHED THEN INSERT(BasvuruId,SiraNo,Ad,MevcutYeni,YatirimSekli,DestekTalebi,VaziyetPlaniNo) VALUES(@YeniBasvuruId,kaynak.SiraNo,kaynak.Ad,kaynak.MevcutYeni,kaynak.YatirimSekli,kaynak.DestekTalebi,kaynak.VaziyetPlaniNo)
                OUTPUT kaynak.Id,inserted.Id INTO @BinaEsleme(EskiId,YeniId);
                INSERT dbo.BasvuruBinaMahal(BinaId,SiraNo,MahalAdi,AlanM2) SELECT e.YeniId,m.SiraNo,m.MahalAdi,m.AlanM2 FROM dbo.BasvuruBinaMahal m INNER JOIN @BinaEsleme e ON e.EskiId=m.BinaId;

                INSERT INTO dbo.BasvuruUygulamaAdresleri
                    (BasvuruId, SiraNo, IlceId, TamAdres, YatirimYeriStatusu,
                     KiraVeyaTahsisSuresi, KiraTahsisBitisTarihi, YapiRuhsatiDurumu, Koordinat, AdaParsel, SegeKademesi,
                     KullanimHakkiBaslangicTarihi, DonemleriKapsiyorMu, IzinTakvimAciklama, AdresBelgeDosyaId, AdresBelgeDosyaAdi, KullanimHakkiDosyaId, KullanimHakkiDosyaAdi, KanitDosyaId, KanitDosyaAdi)
                    SELECT @YeniBasvuruId, SiraNo, IlceId, TamAdres, YatirimYeriStatusu,
                           KiraVeyaTahsisSuresi, KiraTahsisBitisTarihi, YapiRuhsatiDurumu, Koordinat, AdaParsel, SegeKademesi,
                           KullanimHakkiBaslangicTarihi, DonemleriKapsiyorMu, IzinTakvimAciklama, AdresBelgeDosyaId, AdresBelgeDosyaAdi, KullanimHakkiDosyaId, KullanimHakkiDosyaAdi, KanitDosyaId, KanitDosyaAdi
                    FROM dbo.BasvuruUygulamaAdresleri WHERE BasvuruId = @KaynakBasvuruId;

                INSERT INTO dbo.BasvuruOrtaklar
                    (BasvuruId, SiraNo, AdUnvan, TcknVkn, KisiTuru, PayOrani, HesabaDahilOran,
                     OzelKamuNiteligi, DogumTarihi, Cinsiyet, SahiplikNiteligi, NihaiFaydalaniciBilgisi, UboKycBelgeAdi, UboKycDosyaId,
                     OncekiYilNetSatis, SonYilNetSatis, OncekiYilAktifToplami, SonYilAktifToplami, IliskiTuru, BelgeReferansi)
                    SELECT @YeniBasvuruId, SiraNo, AdUnvan, TcknVkn, KisiTuru, PayOrani, HesabaDahilOran,
                           OzelKamuNiteligi, DogumTarihi, Cinsiyet, SahiplikNiteligi, NihaiFaydalaniciBilgisi, UboKycBelgeAdi, UboKycDosyaId,
                           OncekiYilNetSatis, SonYilNetSatis, OncekiYilAktifToplami, SonYilAktifToplami, IliskiTuru, BelgeReferansi
                    FROM dbo.BasvuruOrtaklar WHERE BasvuruId = @KaynakBasvuruId;

                INSERT INTO dbo.BasvuruAdliSicilKisiler
                    (BasvuruId, SiraNo, Tckn, Ad, Soyad, Gorev, YetkiKapsami, Aciklama, ImzaYetkiDosyaAdi, ImzaYetkiDosyaId, DosyaAdi, DosyaId)
                    SELECT @YeniBasvuruId, SiraNo, Tckn, Ad, Soyad, Gorev, YetkiKapsami, Aciklama, ImzaYetkiDosyaAdi, ImzaYetkiDosyaId, DosyaAdi, DosyaId
                    FROM dbo.BasvuruAdliSicilKisiler WHERE BasvuruId = @KaynakBasvuruId;

                ;WITH Sirali AS
                (
                    SELECT Id, ROW_NUMBER() OVER (ORDER BY RevizyonNo DESC, Id DESC) - 1 AS YeniSiraNo
                    FROM dbo.Basvuru
                    WHERE BasvuruAnaId = @BasvuruAnaId
                )
                UPDATE B SET SiraNo = S.YeniSiraNo
                FROM dbo.Basvuru B
                INNER JOIN Sirali S ON S.Id = B.Id;

                SELECT @YeniBasvuruId;";

            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@KaynakBasvuruId", kaynakBasvuruId);
            command.Parameters.AddWithValue("@KayitTuru", (int)kayitTuru);
            return Convert.ToInt32(await command.ExecuteScalarAsync());
        }

        public async Task<Basvuru?> OnBasvuruDenetimBilgisiOkuAsync(int basvuruId)
        {
            const string sql = @"
                SELECT B.DenetimAnketi, B.SistemDenetimAnketi, B.DenetimGerekcesi, B.DenetimSonucu
                FROM dbo.Basvuru B
                INNER JOIN dbo.BasvuruAna BA ON BA.Id = B.BasvuruAnaId
                WHERE B.Id = @BasvuruId
                  AND BA.Durum = @Durum
                  AND B.SiraNo = 0;";

            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@BasvuruId", basvuruId);
            command.Parameters.AddWithValue("@Durum", (int)enumBasvuruDurum.OnBasvuruIncelemeDurumu);
            await using SqlDataReader reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return null;

            int? sonuc = NullOkuInt(reader, 3);
            return new Basvuru
            {
                Id = basvuruId,
                DenetimAnketi = NullOkuString(reader, 0) ?? "",
                SistemDenetimAnketi = NullOkuString(reader, 1) ?? "",
                DenetimGerekcesi = NullOkuString(reader, 2) ?? "",
                DenetimSonucu = sonuc.HasValue ? (enumOnBasvuruDenetimSonucu)sonuc.Value : null
            };
        }

        public async Task<bool> TamBasvuruOlusturulmusMuAsync(int onBasvuruId)
        {
            const string sql = @"
                SELECT CASE WHEN EXISTS
                (
                    SELECT 1
                    FROM dbo.Basvuru Kaynak
                    INNER JOIN dbo.Basvuru Yeni
                        ON Yeni.BasvuruAnaId = Kaynak.BasvuruAnaId
                       AND Yeni.SiraNo = 0
                       AND Yeni.KayitTuru = @BasvuruKayitTuru
                    INNER JOIN dbo.BasvuruAna BA ON BA.Id = Kaynak.BasvuruAnaId
                    WHERE Kaynak.Id = @OnBasvuruId
                      AND Kaynak.KayitTuru = @OnBasvuruKayitTuru
                      AND BA.Durum = @BasvuruDurumu
                ) THEN 1 ELSE 0 END;";

            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@OnBasvuruId", onBasvuruId);
            command.Parameters.AddWithValue("@OnBasvuruKayitTuru", (int)enumBasvuruKayitTuru.OnBasvuru);
            command.Parameters.AddWithValue("@BasvuruKayitTuru", (int)enumBasvuruKayitTuru.Basvuru);
            command.Parameters.AddWithValue("@BasvuruDurumu", (int)enumBasvuruDurum.BasvuruDurumu);
            return Convert.ToInt32(await command.ExecuteScalarAsync()) == 1;
        }
        public async Task DosyaReferanslariniGuncelleAsync(int basvuruId, IReadOnlyDictionary<int, int> dosyaIdEslemeleri)
        {
            const string sql = @"
                UPDATE dbo.Basvuru
                SET BelgePaketiDosyaId = CASE WHEN BelgePaketiDosyaId = @EskiId THEN @YeniId ELSE BelgePaketiDosyaId END,
                    TaahhutDosyaId = CASE WHEN TaahhutDosyaId = @EskiId THEN @YeniId ELSE TaahhutDosyaId END,
                    DenetimDosyaId = CASE WHEN DenetimDosyaId = @EskiId THEN @YeniId ELSE DenetimDosyaId END
                WHERE Id = @BasvuruId;

                UPDATE dbo.BasvuruOrtaklar
                SET UboKycDosyaId = @YeniId
                WHERE BasvuruId = @BasvuruId AND UboKycDosyaId = @EskiId;

                UPDATE dbo.BasvuruAdliSicilKisiler
                SET DosyaId = @YeniId
                WHERE BasvuruId = @BasvuruId AND DosyaId = @EskiId;

                UPDATE t SET TeklifBelgesiDosyaId=@YeniId
                FROM dbo.BasvuruMakineTeklif t INNER JOIN dbo.BasvuruMakine m ON m.Id=t.MakineId
                WHERE m.BasvuruId=@BasvuruId AND t.TeklifBelgesiDosyaId=@EskiId;";

            foreach ((int eskiId, int yeniId) in dosyaIdEslemeleri)
            {
                await using SqlCommand command = KomutOlustur(sql);
                command.Parameters.AddWithValue("@BasvuruId", basvuruId);
                command.Parameters.AddWithValue("@EskiId", eskiId);
                command.Parameters.AddWithValue("@YeniId", yeniId);
                await command.ExecuteNonQueryAsync();
            }
        }

        public async Task<bool> BasvuruAnaDurumGuncelleAsync(int basvuruId, enumBasvuruDurum mevcutDurum, enumBasvuruDurum yeniDurum)
        {
            const string sql = @"
                UPDATE BA SET Durum = @YeniDurum
                FROM dbo.BasvuruAna BA
                INNER JOIN dbo.Basvuru B ON B.BasvuruAnaId = BA.Id
                WHERE B.Id = @BasvuruId AND BA.Durum = @MevcutDurum;";
            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@BasvuruId", basvuruId);
            command.Parameters.AddWithValue("@MevcutDurum", (int)mevcutDurum);
            command.Parameters.AddWithValue("@YeniDurum", (int)yeniDurum);
            return await command.ExecuteNonQueryAsync() == 1;
        }

#if false
        public async Task<bool> IncelemeyeGonderAsync(int basvuruId)
        {
            const string sql = @"
                UPDATE BA
                SET BA.Durum = @YeniDurum
                FROM dbo.BasvuruAna BA
                INNER JOIN dbo.Basvuru B ON B.BasvuruAnaId = BA.Id
                WHERE B.Id = @BasvuruId
                  AND BA.Durum IN (@OnBasvuruDurumu, @DuzeltmeDurumu);";

            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@BasvuruId", basvuruId);
            command.Parameters.AddWithValue("@OnBasvuruDurumu", (int)enumBasvuruDurum.OnBasvuruDurumu);
            command.Parameters.AddWithValue("@DuzeltmeDurumu", (int)enumBasvuruDurum.OnBasvuruDuzeltmeDurumu);
            command.Parameters.AddWithValue("@YeniDurum", (int)enumBasvuruDurum.OnBasvuruIncelemeDurumu);
            return await command.ExecuteNonQueryAsync() == 1;
        }
#endif

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
            command.Parameters.AddWithValue("@KayitTuru", (int)enumBasvuruKayitTuru.OnBasvuru);
            command.Parameters.AddWithValue("@SiraNo", Math.Max(0, basvuru.siraNo));
            command.Parameters.AddWithValue("@BasvuruSahibiTuru", basvuru.basvuruSahibiTuru.HasValue ? (int)basvuru.basvuruSahibiTuru.Value : DBNull.Value);
            command.Parameters.AddWithValue("@HukukiTurSirketTuru", basvuru.hukukiTurSirketTuru.HasValue ? (int)basvuru.hukukiTurSirketTuru.Value : DBNull.Value);
            command.Parameters.AddWithValue("@YonetimKuruluUyeleriAdliSicilKisiler", DbNull(basvuru.yonetimKuruluUyeleriAdliSicilKisiler));
            command.Parameters.AddWithValue("@SonIkiYildirFaalMi", basvuru.sonIkiYildirFaalMi.HasValue ? (basvuru.sonIkiYildirFaalMi.Value ? 1 : 0) : DBNull.Value);
            command.Parameters.AddWithValue("@OnBasvuruSonrasiDegisiklikVarMi", basvuru.onBasvuruSonrasiDegisiklikVarMi.HasValue ? (basvuru.onBasvuruSonrasiDegisiklikVarMi.Value ? 1 : 0) : DBNull.Value);
            command.Parameters.AddWithValue("@OnBasvuruSonrasiDegisiklikSebebi", DbNull(basvuru.onBasvuruSonrasiDegisiklikSebebi));
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
            basvuru.kayitTuru = (enumBasvuruKayitTuru)reader.GetInt32(kol++);
            basvuru.basvuruFirma.onBasvuruSonrasiDegisiklikVarMi = NullOkuBool(reader, kol++);
            basvuru.basvuruFirma.onBasvuruSonrasiDegisiklikSebebi = NullOkuString(reader, kol++);
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
            basvuru.yatirim.basvuruKonusuTesis = NullOkuString(reader, kol++);
            basvuru.yatirim.organizeAlanTuru = NullOkuString(reader, kol++);
            basvuru.yatirim.planlananBaslangicTarihi = reader.IsDBNull(kol) ? null : reader.GetDateTime(kol); kol++;
            basvuru.yatirim.planlananTamamlanmaTarihi = reader.IsDBNull(kol) ? null : reader.GetDateTime(kol); kol++;
            basvuru.finans.toplamYatirimTutari = NullOkuDecimal(reader, kol++);
            basvuru.finans.uygunHarcamaTutari = NullOkuDecimal(reader, kol++);
            basvuru.finans.talepEdilenDestekTutari = NullOkuDecimal(reader, kol++);
            basvuru.finans.talepEdilenFinansmanOrani = NullOkuDecimal(reader, kol++);
            basvuru.finans.onBasvuruSahibiKatkisi = NullOkuDecimal(reader, kol++);
            basvuru.finans.basvuruSahibiKatkisi = NullOkuDecimal(reader, kol++);
            basvuru.finans.talepEdilenVadeSuresiAy = NullOkuInt(reader, kol++);
            basvuru.finans.yatirimSuresiAy = NullOkuInt(reader, kol++);
            basvuru.finans.odemeSuresiAy = NullOkuInt(reader, kol++);
            basvuru.finans.destekOrani = NullOkuDecimal(reader, kol++);
            basvuru.finans.digerFinansmanKaynaklariAciklama = NullOkuString(reader, kol++);
            basvuru.finans.finansmanParaBirimi = NullOkuString(reader, kol++);
            basvuru.finans.digerFinansmanKaynaklari = NullOkuString(reader, kol++);
            basvuru.finans.oncekiRffOnayliTutar = NullOkuDecimal(reader, kol++);
            basvuru.finans.oncekiRffSozlesmesiKapaliMi = NullOkuString(reader, kol++);
            basvuru.finans.bankaTeminatMektubuSaglanabilirMi = NullOkuString(reader, kol++);
            basvuru.finans.yatiriminAmaci = NullOkuString(reader, kol++);
            basvuru.yatirim.yatiriminAmaci = basvuru.finans.yatiriminAmaci;
            basvuru.yatirim.yatirimFaaliyetleri = NullOkuString(reader, kol++);
            basvuru.yatirim.yatirimGirdileri = NullOkuString(reader, kol++);
            basvuru.yatirim.yatirimCiktilari = NullOkuString(reader, kol++);
            basvuru.uygunHarcama.basvuruId = basvuru.Id;
            basvuru.uygunHarcama.pikkListesiJson = NullOkuString(reader, kol++);
            basvuru.yatirimOzeti.basvuruId = basvuru.Id;
            basvuru.yatirimOzeti.yatirimOzetiJson = NullOkuString(reader, kol++);
            basvuru.dbCtpTeknikProje.basvuruId = basvuru.Id;
            basvuru.dbCtpTeknikProje.dbCtpTeknikProjeJson = NullOkuString(reader, kol++);
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
            basvuru.mali.oncekiYilIhracatSatis = NullOkuDecimal(reader, kol++);
            basvuru.mali.sonYilIhracatSatis = NullOkuDecimal(reader, kol++);
            basvuru.mali.oncekiYilCalisanSayisi = NullOkuInt(reader, kol++);
            basvuru.mali.sonYilCalisanSayisi = NullOkuInt(reader, kol++);
            basvuru.mali.aciklama = NullOkuString(reader, kol++);
            basvuru.mali.belgeReferanslariJson = NullOkuString(reader, kol++);
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
            basvuru.TaahhutBeyanlarJson = NullOkuString(reader, kol++) ?? "";
            basvuru.DenetimAnketi = NullOkuString(reader, kol++) ?? "";
            basvuru.SistemDenetimAnketi = NullOkuString(reader, kol++) ?? "";
            basvuru.DenetimGerekcesi = NullOkuString(reader, kol++) ?? "";
            int? denetimSonucu = NullOkuInt(reader, kol++);
            basvuru.DenetimSonucu = denetimSonucu.HasValue
                ? (enumOnBasvuruDenetimSonucu)denetimSonucu.Value
                : null;
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
            basvuru.basvuruFirma.donem.basvuruBaslangicTarihi = reader.IsDBNull(kol) ? null : reader.GetDateTime(kol); kol++;
            basvuru.basvuruFirma.donem.basvuruBitisTarihi = reader.IsDBNull(kol) ? null : reader.GetDateTime(kol); kol++;
            basvuru.basvuruFirma.donem.onBasvuruBaslangicTarihi = reader.IsDBNull(kol) ? null : reader.GetDateTime(kol); kol++;
            basvuru.basvuruFirma.donem.onBasvuruBitisTarihi = reader.IsDBNull(kol) ? null : reader.GetDateTime(kol); kol++;
            basvuru.basvuruFirma.donem.onBasvuruCevrimKuru = reader.IsDBNull(kol) ? null : reader.GetDecimal(kol); kol++;
            basvuru.basvuruFirma.donem.basvuruCevrimKuru = reader.IsDBNull(kol) ? null : reader.GetDecimal(kol); kol++;
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
            basvuru.basvuruFirma.firma.kurulusTarihi = reader.IsDBNull(kol) ? null : reader.GetDateTime(kol); kol++;
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
            await YatirimTurleriEkleAsync(yatirim);
            await HarcamaTurleriEkleAsync(yatirim);
            await DegerZinciriAsamalariEkleAsync(yatirim);
        }

        public async Task YatirimBilgileriKaydetAsync(BasvuruYatirim yatirim)
        {
            yatirim.yatirimTurleri = yatirim.yatirimTurleri.Distinct().Where(x => x > 0).ToList();
            yatirim.yatirimTuru = yatirim.yatirimTurleri.Count > 0
                ? (enumYatirimTuru)yatirim.yatirimTurleri[0]
                : enumYatirimTuru.Tanimsiz;
            await YatirimBilgileriGuncelleAsync(yatirim);
            await YatirimTurleriniSilAsync(yatirim.basvuruId);
            await YatirimTurleriEkleAsync(yatirim);
            await HarcamaTurleriniSilAsync(yatirim.basvuruId);
            await HarcamaTurleriEkleAsync(yatirim);
        }

        public async Task<int> YatirimBilgileriGuncelleAsync(BasvuruYatirim yatirim)
        {
            const string sql = @"UPDATE dbo.Basvuru SET YatirimAdi = @YatirimAdi, YatirimTuru = @YatirimTuru, YatiriminAmaci = @YatiriminAmaci,
                YatirimFaaliyetleri = @YatirimFaaliyetleri, YatirimGirdileri = @YatirimGirdileri, YatirimCiktilari = @YatirimCiktilari, BasvuruKonusuTesis = @BasvuruKonusuTesis, OrganizeAlanTuru = @OrganizeAlanTuru, PlanlananBaslangicTarihi = @PlanlananBaslangicTarihi, PlanlananTamamlanmaTarihi = @PlanlananTamamlanmaTarihi WHERE Id = @Id;";

            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@YatirimAdi", DbNull(yatirim.yatirimAdi));
            command.Parameters.AddWithValue("@YatirimTuru", yatirim.yatirimTuru == enumYatirimTuru.Tanimsiz ? DBNull.Value : (object)(int)yatirim.yatirimTuru);
            command.Parameters.AddWithValue("@YatiriminAmaci", DbNull(yatirim.yatiriminAmaci));
            command.Parameters.AddWithValue("@YatirimFaaliyetleri", DbNull(yatirim.yatirimFaaliyetleri));
            command.Parameters.AddWithValue("@YatirimGirdileri", DbNull(yatirim.yatirimGirdileri));
            command.Parameters.AddWithValue("@YatirimCiktilari", DbNull(yatirim.yatirimCiktilari));
            command.Parameters.AddWithValue("@BasvuruKonusuTesis", DbNull(yatirim.basvuruKonusuTesis));
            command.Parameters.AddWithValue("@OrganizeAlanTuru", DbNull(yatirim.organizeAlanTuru));
            command.Parameters.AddWithValue("@PlanlananBaslangicTarihi", yatirim.planlananBaslangicTarihi.HasValue ? yatirim.planlananBaslangicTarihi.Value.Date : (object)DBNull.Value);
            command.Parameters.AddWithValue("@PlanlananTamamlanmaTarihi", yatirim.planlananTamamlanmaTarihi.HasValue ? yatirim.planlananTamamlanmaTarihi.Value.Date : (object)DBNull.Value);
            command.Parameters.AddWithValue("@Id", yatirim.basvuruId);
            return await command.ExecuteNonQueryAsync();
        }

        public async Task DegerZinciriKaydetAsync(BasvuruYatirim yatirim)
        {
            const string sql = @"UPDATE dbo.Basvuru SET
                IlDegerZinciriEslesmesi=@IlDegerZinciriEslesmesi, TarimGidaBaglantiTuru=@TarimGidaBaglantiTuru,
                TarimGidaBaglantiAciklamasi=@TarimGidaBaglantiAciklamasi, YatirimAlaniTipolojisi=@YatirimAlaniTipolojisi,
                DegerZinciriUygunlukAciklamasi=@DegerZinciriUygunlukAciklamasi, OncelikliYatirimUyumu=@OncelikliYatirimUyumu,
                OncelikliYatirimKonuKodu=@OncelikliYatirimKonuKodu, IthalatBagimliligiUyumu=@IthalatBagimliligiUyumu,
                IthalatBagimliligiUrunKodu=@IthalatBagimliligiUrunKodu, HedefUrunlerPazarCiktisi=@HedefUrunlerPazarCiktisi,
                RekabetcilikAciklamasi=@RekabetcilikAciklamasi WHERE Id=@BasvuruId;";
            await using (SqlCommand command = KomutOlustur(sql))
            {
                command.Parameters.AddWithValue("@BasvuruId", yatirim.basvuruId);
                command.Parameters.AddWithValue("@IlDegerZinciriEslesmesi", DbNull(yatirim.ilDegerZinciriEslesmesi));
                command.Parameters.AddWithValue("@TarimGidaBaglantiTuru", DbNull(yatirim.tarimGidaBaglantiTuru));
                command.Parameters.AddWithValue("@TarimGidaBaglantiAciklamasi", DbNull(yatirim.tarimGidaBaglantiAciklamasi));
                command.Parameters.AddWithValue("@YatirimAlaniTipolojisi", DbNull(yatirim.yatirimAlaniTipolojisi));
                command.Parameters.AddWithValue("@DegerZinciriUygunlukAciklamasi", DbNull(yatirim.degerZinciriUygunlukAciklamasi));
                command.Parameters.AddWithValue("@OncelikliYatirimUyumu", DbNull(yatirim.oncelikliYatirimUyumu));
                command.Parameters.AddWithValue("@OncelikliYatirimKonuKodu", DbNull(yatirim.oncelikliYatirimKonuKodu));
                command.Parameters.AddWithValue("@IthalatBagimliligiUyumu", DbNull(yatirim.ithalatBagimliligiUyumu));
                command.Parameters.AddWithValue("@IthalatBagimliligiUrunKodu", DbNull(yatirim.ithalatBagimliligiUrunKodu));
                command.Parameters.AddWithValue("@HedefUrunlerPazarCiktisi", DbNull(yatirim.hedefUrunlerPazarCiktisi));
                command.Parameters.AddWithValue("@RekabetcilikAciklamasi", DbNull(yatirim.rekabetcilikAciklamasi));
                await command.ExecuteNonQueryAsync();
            }
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

        private async Task YatirimTurleriEkleAsync(BasvuruYatirim yatirim)
        {
            const string sql = @"INSERT INTO dbo.BasvuruYatirimTuru(BasvuruId, YatirimTuru) VALUES (@BasvuruId, @YatirimTuru);";
            foreach (int yatirimTuru in yatirim.yatirimTurleri)
            {
                await using SqlCommand command = KomutOlustur(sql);
                command.Parameters.AddWithValue("@BasvuruId", yatirim.basvuruId);
                command.Parameters.AddWithValue("@YatirimTuru", yatirimTuru);
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
                DELETE FROM dbo.BasvuruYatirimTuru WHERE BasvuruId = @BasvuruId;
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

        private async Task YatirimTurleriniSilAsync(int basvuruId)
        {
            const string sql = @"DELETE FROM dbo.BasvuruYatirimTuru WHERE BasvuruId = @BasvuruId;";
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

        public async Task<int> BasvuruOrtakiKaydetAsync(int basvuruId, BasvuruOrtak ortak)
        {
            const string updateSql = @"
                UPDATE dbo.BasvuruOrtaklar
                SET SiraNo=@SiraNo, AdUnvan=@AdUnvan, TcknVkn=@TcknVkn, KisiTuru=@KisiTuru,
                    PayOrani=@PayOrani, HesabaDahilOran=@HesabaDahilOran, OzelKamuNiteligi=@OzelKamuNiteligi,
                    DogumTarihi=@DogumTarihi, Cinsiyet=@Cinsiyet, SahiplikNiteligi=@SahiplikNiteligi,
                    NihaiFaydalaniciBilgisi=@NihaiFaydalaniciBilgisi, UboKycBelgeAdi=@UboKycBelgeAdi,
                    UboKycDosyaId=@UboKycDosyaId, OncekiYilNetSatis=@OncekiYilNetSatis,
                    SonYilNetSatis=@SonYilNetSatis, OncekiYilAktifToplami=@OncekiYilAktifToplami,
                    SonYilAktifToplami=@SonYilAktifToplami, IliskiTuru=@IliskiTuru, BelgeReferansi=@BelgeReferansi
                WHERE Id=@Id AND BasvuruId=@BasvuruId;";
            if (ortak.id > 0)
            {
                await using SqlCommand update = KomutOlustur(updateSql);
                BasvuruOrtakParametreleriEkle(update, basvuruId, ortak);
                update.Parameters.AddWithValue("@Id", ortak.id);
                if (await update.ExecuteNonQueryAsync() == 1) return ortak.id;
                return 0;
            }

            const string insertSql = @"
                INSERT INTO dbo.BasvuruOrtaklar
                    (BasvuruId,SiraNo,AdUnvan,TcknVkn,KisiTuru,PayOrani,HesabaDahilOran,OzelKamuNiteligi,DogumTarihi,Cinsiyet,SahiplikNiteligi,NihaiFaydalaniciBilgisi,UboKycBelgeAdi,UboKycDosyaId,OncekiYilNetSatis,SonYilNetSatis,OncekiYilAktifToplami,SonYilAktifToplami,IliskiTuru,BelgeReferansi)
                OUTPUT INSERTED.Id
                VALUES (@BasvuruId,@SiraNo,@AdUnvan,@TcknVkn,@KisiTuru,@PayOrani,@HesabaDahilOran,@OzelKamuNiteligi,@DogumTarihi,@Cinsiyet,@SahiplikNiteligi,@NihaiFaydalaniciBilgisi,@UboKycBelgeAdi,@UboKycDosyaId,@OncekiYilNetSatis,@SonYilNetSatis,@OncekiYilAktifToplami,@SonYilAktifToplami,@IliskiTuru,@BelgeReferansi);";
            await using SqlCommand insert = KomutOlustur(insertSql);
            BasvuruOrtakParametreleriEkle(insert, basvuruId, ortak);
            return Convert.ToInt32(await insert.ExecuteScalarAsync());
        }

        public async Task<bool> BasvuruOrtakiSilAsync(int basvuruId, int ortakId)
        {
            const string sql = "DELETE FROM dbo.BasvuruOrtaklar WHERE Id=@Id AND BasvuruId=@BasvuruId;";
            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@Id", ortakId);
            command.Parameters.AddWithValue("@BasvuruId", basvuruId);
            return await command.ExecuteNonQueryAsync() == 1;
        }
        private async Task BasvuruOrtaklariYenileAsync(int basvuruId, List<BasvuruOrtak>? ortaklar)
        {
            await BasvuruOrtaklariniSilAsync(basvuruId);

            if (ortaklar == null)
                return;

            const string sql = @"
                INSERT INTO dbo.BasvuruOrtaklar
                    (BasvuruId, SiraNo, AdUnvan, TcknVkn, KisiTuru, PayOrani, HesabaDahilOran, OzelKamuNiteligi, DogumTarihi, Cinsiyet, SahiplikNiteligi, NihaiFaydalaniciBilgisi, UboKycBelgeAdi, UboKycDosyaId,
                     OncekiYilNetSatis, SonYilNetSatis, OncekiYilAktifToplami, SonYilAktifToplami, IliskiTuru, BelgeReferansi)
                VALUES
                    (@BasvuruId, @SiraNo, @AdUnvan, @TcknVkn, @KisiTuru, @PayOrani, @HesabaDahilOran, @OzelKamuNiteligi, @DogumTarihi, @Cinsiyet, @SahiplikNiteligi, @NihaiFaydalaniciBilgisi, @UboKycBelgeAdi, @UboKycDosyaId,
                     @OncekiYilNetSatis, @SonYilNetSatis, @OncekiYilAktifToplami, @SonYilAktifToplami, @IliskiTuru, @BelgeReferansi);";

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
            command.Parameters.AddWithValue("@DogumTarihi", ortak.dogumTarihi.HasValue ? (object)ortak.dogumTarihi.Value.Date : DBNull.Value);
            command.Parameters.AddWithValue("@Cinsiyet", ortak.cinsiyet?.Trim() ?? "");
            command.Parameters.AddWithValue("@SahiplikNiteligi", ortak.sahiplikNiteligi?.Trim() ?? "Uygulanamaz");
            command.Parameters.AddWithValue("@NihaiFaydalaniciBilgisi", ortak.nihaiFaydalaniciBilgisi?.Trim() ?? "");
            command.Parameters.AddWithValue("@UboKycBelgeAdi", ortak.uboKycBelgeAdi?.Trim() ?? "");
            command.Parameters.AddWithValue("@UboKycDosyaId", DbNull(ortak.uboKycDosyaId));
            command.Parameters.AddWithValue("@OncekiYilNetSatis", DbNull(ortak.oncekiYilNetSatis));
            command.Parameters.AddWithValue("@SonYilNetSatis", DbNull(ortak.sonYilNetSatis));
            command.Parameters.AddWithValue("@OncekiYilAktifToplami", DbNull(ortak.oncekiYilAktifToplami));
            command.Parameters.AddWithValue("@SonYilAktifToplami", DbNull(ortak.sonYilAktifToplami));
            command.Parameters.AddWithValue("@IliskiTuru", DbNull(ortak.iliskiTuru));
            command.Parameters.AddWithValue("@BelgeReferansi", DbNull(ortak.belgeReferansi));
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
                    bua.KiraTahsisBitisTarihi, bua.YapiRuhsatiDurumu,
                    bua.Koordinat, bua.AdaParsel, bua.SegeKademesi, bua.KullanimHakkiBaslangicTarihi, bua.DonemleriKapsiyorMu, bua.IzinTakvimAciklama,
                    bua.AdresBelgeDosyaId, bua.AdresBelgeDosyaAdi, bua.KullanimHakkiDosyaId, bua.KullanimHakkiDosyaAdi, bua.KanitDosyaId, bua.KanitDosyaAdi
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
                    (BasvuruId, SiraNo, IlceId, TamAdres, YatirimYeriStatusu, KiraVeyaTahsisSuresi, KiraTahsisBitisTarihi, YapiRuhsatiDurumu, Koordinat, AdaParsel, SegeKademesi, KullanimHakkiBaslangicTarihi, DonemleriKapsiyorMu, IzinTakvimAciklama)
                OUTPUT INSERTED.Id
                VALUES
                    (@BasvuruId, @SiraNo, @IlceId, @TamAdres, @YatirimYeriStatusu, @KiraVeyaTahsisSuresi, @KiraTahsisBitisTarihi, @YapiRuhsatiDurumu, @Koordinat, @AdaParsel, @SegeKademesi, @KullanimHakkiBaslangicTarihi, @DonemleriKapsiyorMu, @IzinTakvimAciklama);";

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
                    YapiRuhsatiDurumu = @YapiRuhsatiDurumu,
                    Koordinat = @Koordinat, AdaParsel = @AdaParsel, SegeKademesi = @SegeKademesi,
                    KullanimHakkiBaslangicTarihi = @KullanimHakkiBaslangicTarihi, DonemleriKapsiyorMu = @DonemleriKapsiyorMu, IzinTakvimAciklama = @IzinTakvimAciklama
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
            command.Parameters.AddWithValue("@Koordinat", DbNull(adres.koordinat));
            command.Parameters.AddWithValue("@AdaParsel", DbNull(adres.adaParsel));
            command.Parameters.AddWithValue("@SegeKademesi", DbNull(adres.segeKademesi));
            command.Parameters.AddWithValue("@KullanimHakkiBaslangicTarihi", adres.kullanimHakkiBaslangicTarihi.HasValue ? adres.kullanimHakkiBaslangicTarihi.Value.Date : (object)DBNull.Value);
            command.Parameters.AddWithValue("@DonemleriKapsiyorMu", adres.donemleriKapsiyorMu.HasValue ? adres.donemleriKapsiyorMu.Value : (object)DBNull.Value);
            command.Parameters.AddWithValue("@IzinTakvimAciklama", DbNull(adres.izinTakvimAciklama));
        }

        public async Task YatirimYeriDosyasiGuncelleAsync(int basvuruId, int adresId, int dosyaNo, int dosyaId, string dosyaAdi)
        {
            string alan = dosyaNo switch { 1 => "AdresBelge", 2 => "KullanimHakki", 3 => "Kanit", _ => throw new ArgumentOutOfRangeException(nameof(dosyaNo)) };
            string sql = $"UPDATE dbo.BasvuruUygulamaAdresleri SET {alan}DosyaId=@DosyaId, {alan}DosyaAdi=@DosyaAdi WHERE Id=@AdresId AND BasvuruId=@BasvuruId;";
            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@DosyaId", dosyaId);
            command.Parameters.AddWithValue("@DosyaAdi", dosyaAdi);
            command.Parameters.AddWithValue("@AdresId", adresId);
            command.Parameters.AddWithValue("@BasvuruId", basvuruId);
            await command.ExecuteNonQueryAsync();
        }
        private async Task DetaylariYukleAsync(Basvuru basvuru)
        {
            string sql = AdresSorguOl() + @" WHERE bua.BasvuruId = @BasvuruId ORDER BY bua.SiraNo;

                SELECT HarcamaTuru
                FROM dbo.BasvuruHarcamaTuru
                WHERE BasvuruId = @BasvuruId
                ORDER BY HarcamaTuru;

                SELECT YatirimTuru
                FROM dbo.BasvuruYatirimTuru
                WHERE BasvuruId = @BasvuruId
                ORDER BY YatirimTuru;

                SELECT dz.Id, dz.Ad, dz.Aciklama, dz.Aktif, dza.Id, dza.SiraNo, dza.Ad, dza.Aciklama, dza.Aktif, bdza.YapilacakFaaliyetler
                FROM dbo.BasvuruDegerZinciriAsama bdza
                LEFT JOIN dbo.DegerZinciriAsama dza ON dza.Id = bdza.DegerZinciriAsamaId
                LEFT JOIN dbo.DegerZinciri dz ON dz.Id = dza.DegerZinciriId
                WHERE bdza.BasvuruId = @BasvuruId
                ORDER BY dza.SiraNo;

                SELECT Id, BasvuruId, SiraNo, AdUnvan, TcknVkn, KisiTuru, PayOrani, HesabaDahilOran, OzelKamuNiteligi, DogumTarihi, Cinsiyet, SahiplikNiteligi, NihaiFaydalaniciBilgisi, UboKycBelgeAdi, UboKycDosyaId,
                    OncekiYilNetSatis, SonYilNetSatis, OncekiYilAktifToplami, SonYilAktifToplami, IliskiTuru, BelgeReferansi
                FROM dbo.BasvuruOrtaklar
                WHERE BasvuruId = @BasvuruId
                ORDER BY SiraNo, Id;

                SELECT Id, BasvuruId, SiraNo, Tckn, Ad, Soyad, Gorev, YetkiKapsami, Aciklama, ImzaYetkiDosyaAdi, ImzaYetkiDosyaId, DosyaAdi, DosyaId
                FROM dbo.BasvuruAdliSicilKisiler
                WHERE BasvuruId = @BasvuruId
                ORDER BY SiraNo, Id;

                SELECT IlDegerZinciriEslesmesi, TarimGidaBaglantiTuru, TarimGidaBaglantiAciklamasi,
                    YatirimAlaniTipolojisi, DegerZinciriUygunlukAciklamasi, OncelikliYatirimUyumu,
                    OncelikliYatirimKonuKodu, IthalatBagimliligiUyumu, IthalatBagimliligiUrunKodu,
                    HedefUrunlerPazarCiktisi, RekabetcilikAciklamasi
                FROM dbo.Basvuru WHERE Id=@BasvuruId;

                SELECT Id,BasvuruId,Tur,SiraNo,Ad,Miktar,Birim,TekPanelGucu,TekPanelGucuBirim,ToplamGuc,ToplamGucBirim
                FROM dbo.BasvuruYatirimOnBilgi WHERE BasvuruId=@BasvuruId ORDER BY Tur,SiraNo,Id;

                SELECT Id,BasvuruId,SiraNo,Ad,Birim,Miktar,Aciklama,Marka,Model,KapasiteOzellikleri,YerlesimPlaniSiraNo,KullanimAmaci,Durum,KapasiteSecimGerekcesi,UzmanParaBirimi,UzmanKur,UzmanMinimumFiyat,
                    UzmanMaksimumFiyat,UzmanSecilenTeklifId,UzmanOnerilenFiyatTl,UzmanKontrolSonucu,UzmanAciklama
                FROM dbo.BasvuruMakine WHERE BasvuruId=@BasvuruId ORDER BY SiraNo,Id;
                SELECT o.Id,o.MakineId,o.SiraNo,o.Baslik,o.AciklamaAsgariGereklilik,o.ZorunluMu
                FROM dbo.BasvuruMakineOzellik o INNER JOIN dbo.BasvuruMakine m ON m.Id=o.MakineId
                WHERE m.BasvuruId=@BasvuruId ORDER BY o.MakineId,o.SiraNo,o.Id;
                SELECT t.Id,t.MakineId,t.SiraNo,t.BasvuruyaEsas,t.Tedarikci,t.Marka,t.Model,t.ParaBirimi,t.Kur,t.BirimFiyat,
                    t.TeklifTarihi,t.GecerlilikTarihi,t.TeklifBelgesiDosyaId,t.TeklifBelgesiDosyaAdi,t.Aciklama
                FROM dbo.BasvuruMakineTeklif t INNER JOIN dbo.BasvuruMakine m ON m.Id=t.MakineId
                WHERE m.BasvuruId=@BasvuruId ORDER BY t.MakineId,t.SiraNo,t.Id;
                SELECT d.Id,d.MakineId,d.SiraNo,d.DokumanAdi,d.DokumanTuru,d.KaynakTedarikci,d.BelgeTarihi,d.Aciklama,d.DosyaId,d.DosyaAdi
                FROM dbo.BasvuruMakineUzmanDokuman d INNER JOIN dbo.BasvuruMakine m ON m.Id=d.MakineId
                WHERE m.BasvuruId=@BasvuruId ORDER BY d.MakineId,d.SiraNo,d.Id;
                SELECT Id,BasvuruId,UrunId,SiraNo,SurecAdi FROM dbo.BasvuruUrunSurec WHERE BasvuruId=@BasvuruId ORDER BY UrunId,SiraNo,Id;
                SELECT sm.Id,sm.SurecId,sm.MakineId,sm.SiraNo,sm.Adet,sm.YerlesimPlaniNo,sm.GirdilerMiktarlar,sm.CiktilarMiktarlar,sm.IslemeKapasitesi,sm.GunlukCalismaSuresi,sm.GunlukCalismaSuresiBirimi,sm.Aciklama
                FROM dbo.BasvuruUrunSurecMakine sm INNER JOIN dbo.BasvuruUrunSurec s ON s.Id=sm.SurecId WHERE s.BasvuruId=@BasvuruId ORDER BY sm.SurecId,sm.SiraNo,sm.Id;
                SELECT Id,BasvuruId,SiraNo,Ad,MevcutYeni,YatirimSekli,DestekTalebi,VaziyetPlaniNo FROM dbo.BasvuruBina WHERE BasvuruId=@BasvuruId ORDER BY SiraNo,Id;
                SELECT m.Id,m.BinaId,m.SiraNo,m.MahalAdi,m.AlanM2 FROM dbo.BasvuruBinaMahal m INNER JOIN dbo.BasvuruBina b ON b.Id=m.BinaId WHERE b.BasvuruId=@BasvuruId ORDER BY m.BinaId,m.SiraNo,m.Id;

                SELECT ISNULL(SUM(OncekiBasvuru.TalepEdilenDestekTutari), 0)
                FROM dbo.Basvuru MevcutBasvuru
                INNER JOIN dbo.BasvuruAna MevcutAna ON MevcutAna.Id = MevcutBasvuru.BasvuruAnaId
                INNER JOIN dbo.Donem MevcutDonem ON MevcutDonem.Id = MevcutAna.DonemId
                INNER JOIN dbo.BasvuruAna OncekiAna ON OncekiAna.FirmaId = MevcutAna.FirmaId
                    AND OncekiAna.Durum = @KabulEdildiDurumu
                INNER JOIN dbo.Donem OncekiDonem ON OncekiDonem.Id = OncekiAna.DonemId
                    AND (OncekiDonem.Yil < MevcutDonem.Yil
                        OR (OncekiDonem.Yil = MevcutDonem.Yil AND OncekiDonem.Id < MevcutDonem.Id))
                CROSS APPLY
                (
                    SELECT TOP (1) B.TalepEdilenDestekTutari
                    FROM dbo.Basvuru B
                    WHERE B.BasvuruAnaId = OncekiAna.Id
                        AND B.KayitTuru = @BasvuruKayitTuru
                    ORDER BY B.RevizyonNo DESC, B.Id DESC
                ) OncekiBasvuru
                WHERE MevcutBasvuru.Id = @BasvuruId;

                SELECT IstihdamJson, IstihdamSgkDosyaId, IstihdamSgkDosyaAdi
                FROM dbo.Basvuru WHERE Id=@BasvuruId;
                SELECT Id,BasvuruId,SiraNo,BirimUnite,GorevUretimHatti,Cinsiyet,YasDurumu,MevcutCalisan,NetCalisanArtisi,BazAylikBrutUcret,HedefAylikBrutUcret FROM dbo.BasvuruIstihdamSatir WHERE BasvuruId=@BasvuruId ORDER BY SiraNo,Id;
                SELECT t.Id,t.BasvuruId,t.UrunId,t.TarimsalUrun,t.IlId,t.IlceId,il.Ad,ilce.Ad,ilce.SegeKademesi,t.Birim,t.MevcutYillikMiktar,t.HedefYillikMiktar,t.MevcutKayitliCiftci,t.EklenecekKayitliCiftci,t.TedarikSekli,t.DayanakBelgeDosyaId,t.DayanakBelgeDosyaAdi,t.KisaAciklama
                FROM dbo.BasvuruTedarikciEntegrasyonu t INNER JOIN dbo.Il il ON il.Id=t.IlId INNER JOIN dbo.Ilce ilce ON ilce.Id=t.IlceId
                WHERE t.BasvuruId=@BasvuruId ORDER BY t.UrunId,t.Id;
                SELECT TedarikciEntegrasyonuAciklama FROM dbo.Basvuru WHERE Id=@BasvuruId;
                SELECT BasvuruOzetiKurumJson FROM dbo.Basvuru WHERE Id=@BasvuruId;
                SELECT IzlemeBaslangicTarihi,IzlemeHedefTarihi,IzlemeVeriSorumlusu FROM dbo.Basvuru WHERE Id=@BasvuruId;
                SELECT Id,BasvuruId,GostergeKodu,BaslangicDegeri,HedefDeger,KadinKirilimi,GencKirilimi,Aciklama FROM dbo.BasvuruIzlemeGostergesi WHERE BasvuruId=@BasvuruId ORDER BY Id;";

            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@BasvuruId", basvuru.Id);
            command.Parameters.AddWithValue("@KabulEdildiDurumu", (int)enumBasvuruDurum.KabulEdildiDurumu);
            command.Parameters.AddWithValue("@BasvuruKayitTuru", (int)enumBasvuruKayitTuru.Basvuru);
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
            List<int> yatirimTurleri = new List<int>();
            while (await reader.ReadAsync())
            {
                yatirimTurleri.Add(NullDuzeltInt(reader, 0));
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

            await reader.NextResultAsync();
            if (await reader.ReadAsync())
            {
                int kolNo = 0;
                basvuru.yatirim.ilDegerZinciriEslesmesi = NullOkuString(reader, kolNo++);
                basvuru.yatirim.tarimGidaBaglantiTuru = NullOkuString(reader, kolNo++);
                basvuru.yatirim.tarimGidaBaglantiAciklamasi = NullOkuString(reader, kolNo++);
                basvuru.yatirim.yatirimAlaniTipolojisi = NullOkuString(reader, kolNo++);
                basvuru.yatirim.degerZinciriUygunlukAciklamasi = NullOkuString(reader, kolNo++);
                basvuru.yatirim.oncelikliYatirimUyumu = NullOkuString(reader, kolNo++);
                basvuru.yatirim.oncelikliYatirimKonuKodu = NullOkuString(reader, kolNo++);
                basvuru.yatirim.ithalatBagimliligiUyumu = NullOkuString(reader, kolNo++);
                basvuru.yatirim.ithalatBagimliligiUrunKodu = NullOkuString(reader, kolNo++);
                basvuru.yatirim.hedefUrunlerPazarCiktisi = NullOkuString(reader, kolNo++);
                basvuru.yatirim.rekabetcilikAciklamasi = NullOkuString(reader, kolNo++);
            }

            await reader.NextResultAsync();
            List<BasvuruYatirimOnBilgi> yatirimOnBilgileri = new();
            while (await reader.ReadAsync())
            {
                int k=0;
                yatirimOnBilgileri.Add(new BasvuruYatirimOnBilgi { id=NullDuzeltInt(reader,k++), basvuruId=NullDuzeltInt(reader,k++), tur=(enumYatirimOnBilgiTuru)NullDuzeltInt(reader,k++), siraNo=NullDuzeltInt(reader,k++), ad=NullOkuString(reader,k++), miktar=NullOkuDecimal(reader,k++), birim=NullOkuString(reader,k++), tekPanelGucu=NullOkuDecimal(reader,k++), tekPanelGucuBirim=NullOkuString(reader,k++), toplamGuc=NullOkuDecimal(reader,k++), toplamGucBirim=NullOkuString(reader,k++) });
            }

            await reader.NextResultAsync();
            List<BasvuruMakine> makineler = new();
            while (await reader.ReadAsync())
            {
                int k=0;
                makineler.Add(new BasvuruMakine { id=NullDuzeltInt(reader,k++), basvuruId=NullDuzeltInt(reader,k++), siraNo=NullDuzeltInt(reader,k++), ad=NullOkuString(reader,k++), birim=NullOkuString(reader,k++), miktar=NullOkuDecimal(reader,k++).GetValueOrDefault(), aciklama=NullOkuString(reader,k++), marka=NullOkuString(reader,k++), model=NullOkuString(reader,k++), kapasiteOzellikleri=NullOkuString(reader,k++), yerlesimPlaniSiraNo=NullOkuInt(reader,k++), kullanimAmaci=NullOkuString(reader,k++), durum=NullOkuString(reader,k++), kapasiteSecimGerekcesi=NullOkuString(reader,k++), uzmanParaBirimi=NullOkuString(reader,k++), uzmanKur=NullOkuDecimal(reader,k++), uzmanMinimumFiyat=NullOkuDecimal(reader,k++), uzmanMaksimumFiyat=NullOkuDecimal(reader,k++), uzmanSecilenTeklifId=NullOkuInt(reader,k++), uzmanOnerilenFiyatTl=NullOkuDecimal(reader,k++), uzmanKontrolSonucu=NullOkuString(reader,k++), uzmanAciklama=NullOkuString(reader,k++) });
            }
            await reader.NextResultAsync();
            while (await reader.ReadAsync())
            {
                int k=0; BasvuruMakineOzellik o=new() { id=NullDuzeltInt(reader,k++), makineId=NullDuzeltInt(reader,k++), siraNo=NullDuzeltInt(reader,k++), baslik=NullOkuString(reader,k++), aciklamaAsgariGereklilik=NullOkuString(reader,k++), zorunluMu=reader.GetBoolean(k++) };
                makineler.FirstOrDefault(x=>x.id==o.makineId)?.teknikOzellikler.Add(o);
            }
            await reader.NextResultAsync();
            while (await reader.ReadAsync())
            {
                int k=0;
                int id=NullDuzeltInt(reader,k++), makineId=NullDuzeltInt(reader,k++), siraNo=NullDuzeltInt(reader,k++); bool esas=reader.GetBoolean(k++);
                string tedarikci=NullOkuString(reader,k++), marka=NullOkuString(reader,k++), model=NullOkuString(reader,k++), para=NullOkuString(reader,k++);
                decimal? kur=NullOkuDecimal(reader,k++), fiyat=NullOkuDecimal(reader,k++); DateTime? tarih=reader.IsDBNull(k)?null:reader.GetDateTime(k); k++; DateTime? gecerlilik=reader.IsDBNull(k)?null:reader.GetDateTime(k); k++;
                BasvuruMakineTeklif t=new() { id=id,makineId=makineId,siraNo=siraNo,basvuruyaEsas=esas,tedarikci=tedarikci,marka=marka,model=model,paraBirimi=para,kur=kur,birimFiyat=fiyat,teklifTarihi=tarih,gecerlilikTarihi=gecerlilik,teklifBelgesiDosyaId=NullOkuInt(reader,k++),teklifBelgesiDosyaAdi=NullOkuString(reader,k++),aciklama=NullOkuString(reader,k++) };
                makineler.FirstOrDefault(x=>x.id==t.makineId)?.teklifler.Add(t);
            }
            await reader.NextResultAsync();
            while (await reader.ReadAsync())
            {
                int k=0;
                BasvuruMakineUzmanDokuman d=new() { id=NullDuzeltInt(reader,k++), makineId=NullDuzeltInt(reader,k++), siraNo=NullDuzeltInt(reader,k++), dokumanAdi=NullOkuString(reader,k++), dokumanTuru=NullOkuString(reader,k++), kaynakTedarikci=NullOkuString(reader,k++) };
                d.belgeTarihi=reader.IsDBNull(k)?null:reader.GetDateTime(k);k++;d.aciklama=NullOkuString(reader,k++);d.dosyaId=NullOkuInt(reader,k++);d.dosyaAdi=NullOkuString(reader,k++);d.basvuruId=basvuru.Id;
                makineler.FirstOrDefault(x=>x.id==d.makineId)?.uzmanDokumanlari.Add(d);
            }
            await reader.NextResultAsync();List<BasvuruUrunSurec> urunSurecleri=[];
            while(await reader.ReadAsync()){int k=0;urunSurecleri.Add(new BasvuruUrunSurec{id=NullDuzeltInt(reader,k++),basvuruId=NullDuzeltInt(reader,k++),urunId=NullDuzeltInt(reader,k++),siraNo=NullDuzeltInt(reader,k++),surecAdi=NullOkuString(reader,k++)});}
            await reader.NextResultAsync();
            while(await reader.ReadAsync()){int k=0;BasvuruUrunSurecMakine sm=new(){id=NullDuzeltInt(reader,k++),surecId=NullDuzeltInt(reader,k++),makineId=NullDuzeltInt(reader,k++),siraNo=NullDuzeltInt(reader,k++),adet=NullOkuDecimal(reader,k++).GetValueOrDefault(),yerlesimPlaniNo=NullOkuString(reader,k++),girdilerMiktarlar=NullOkuString(reader,k++),ciktilarMiktarlar=NullOkuString(reader,k++),islemeKapasitesi=NullOkuString(reader,k++),gunlukCalismaSuresi=NullOkuDecimal(reader,k++),gunlukCalismaSuresiBirimi=NullOkuString(reader,k++),aciklama=NullOkuString(reader,k++)};urunSurecleri.FirstOrDefault(x=>x.id==sm.surecId)?.makineler.Add(sm);}
            await reader.NextResultAsync();List<BasvuruBina> binalar=[];
            while(await reader.ReadAsync()){int k=0;binalar.Add(new BasvuruBina{id=NullDuzeltInt(reader,k++),basvuruId=NullDuzeltInt(reader,k++),siraNo=NullDuzeltInt(reader,k++),ad=NullOkuString(reader,k++),mevcutYeni=NullOkuString(reader,k++),yatirimSekli=NullOkuString(reader,k++),destekTalebi=NullOkuString(reader,k++),vaziyetPlaniNo=NullOkuString(reader,k++)});}
            await reader.NextResultAsync();
            while(await reader.ReadAsync()){int k=0;BasvuruBinaMahal m=new(){id=NullDuzeltInt(reader,k++),binaId=NullDuzeltInt(reader,k++),siraNo=NullDuzeltInt(reader,k++),mahalAdi=NullOkuString(reader,k++),alanM2=NullOkuDecimal(reader,k++).GetValueOrDefault(),basvuruId=basvuru.Id};binalar.FirstOrDefault(x=>x.id==m.binaId)?.mahaller.Add(m);}
            await reader.NextResultAsync();
            if (await reader.ReadAsync())
                basvuru.finans.oncekiRffOnayliTutar = reader.GetDecimal(0);
            await reader.NextResultAsync();
            if (await reader.ReadAsync())
            {
                string json = NullOkuString(reader, 0);
                if (!string.IsNullOrWhiteSpace(json))
                {
                    try { basvuru.istihdam = System.Text.Json.JsonSerializer.Deserialize<BasvuruIstihdam>(json) ?? new(); }
                    catch { basvuru.istihdam = new(); }
                }
                basvuru.istihdam.basvuruId = basvuru.Id;
                basvuru.istihdam.sgkDosyaId = NullOkuInt(reader, 1);
                basvuru.istihdam.sgkDosyaAdi = NullOkuString(reader, 2);
            }
            await reader.NextResultAsync();List<BasvuruIstihdamSatir> istihdamSatirlari=[];
            while(await reader.ReadAsync()){int k=0;istihdamSatirlari.Add(new BasvuruIstihdamSatir{id=NullDuzeltInt(reader,k++),basvuruId=NullDuzeltInt(reader,k++),siraNo=NullDuzeltInt(reader,k++),birimUnite=NullOkuString(reader,k++),gorevUretimHatti=NullOkuString(reader,k++),cinsiyet=NullOkuString(reader,k++),yasDurumu=NullOkuString(reader,k++),mevcutCalisan=NullOkuDecimal(reader,k++).GetValueOrDefault(),netCalisanArtisi=NullOkuDecimal(reader,k++).GetValueOrDefault(),bazAylikBrutUcret=NullOkuDecimal(reader,k++).GetValueOrDefault(),hedefAylikBrutUcret=NullOkuDecimal(reader,k++).GetValueOrDefault()});}
            if(istihdamSatirlari.Count>0||basvuru.istihdam.satirlar.Count==0)basvuru.istihdam.satirlar=istihdamSatirlari;
            await reader.NextResultAsync();List<BasvuruTedarikciEntegrasyonu> tedarikler=[];
            while(await reader.ReadAsync()){int k=0;tedarikler.Add(new BasvuruTedarikciEntegrasyonu{id=NullDuzeltInt(reader,k++),basvuruId=NullDuzeltInt(reader,k++),urunId=NullDuzeltInt(reader,k++),tarimsalUrun=NullOkuString(reader,k++),ilId=NullDuzeltInt(reader,k++),ilceId=NullDuzeltInt(reader,k++),ilAdi=NullOkuString(reader,k++),ilceAdi=NullOkuString(reader,k++),segeKademesi=NullOkuInt(reader,k++),birim=NullOkuString(reader,k++),mevcutYillikMiktar=NullOkuDecimal(reader,k++).GetValueOrDefault(),hedefYillikMiktar=NullOkuDecimal(reader,k++).GetValueOrDefault(),mevcutKayitliCiftci=NullDuzeltInt(reader,k++),eklenecekKayitliCiftci=NullDuzeltInt(reader,k++),tedarikSekli=NullDuzeltInt(reader,k++),dayanakBelgeDosyaId=NullOkuInt(reader,k++),dayanakBelgeDosyaAdi=NullOkuString(reader,k++),kisaAciklama=NullOkuString(reader,k++)});}
            await reader.NextResultAsync();if(await reader.ReadAsync())basvuru.tedarikciEntegrasyonuAciklama=NullOkuString(reader,0);
            await reader.NextResultAsync();if(await reader.ReadAsync()){basvuru.basvuruOzetiKurum.basvuruId=basvuru.Id;basvuru.basvuruOzetiKurum.kurumJson=NullOkuString(reader,0);}
            await reader.NextResultAsync();if(await reader.ReadAsync()){basvuru.izlemeUstBilgi.basvuruId=basvuru.Id;basvuru.izlemeUstBilgi.baslangicTarihi=reader.IsDBNull(0)?null:reader.GetDateTime(0);basvuru.izlemeUstBilgi.hedefTarihi=reader.IsDBNull(1)?null:reader.GetDateTime(1);basvuru.izlemeUstBilgi.veriSorumlusu=NullOkuString(reader,2);}
            await reader.NextResultAsync();List<BasvuruIzlemeGostergesi> izleme=[];while(await reader.ReadAsync()){int k=0;izleme.Add(new(){id=NullDuzeltInt(reader,k++),basvuruId=NullDuzeltInt(reader,k++),gostergeKodu=NullOkuString(reader,k++),baslangicDegeri=NullOkuString(reader,k++),hedefDeger=NullOkuString(reader,k++),kadinKirilimi=NullOkuString(reader,k++),gencKirilimi=NullOkuString(reader,k++),aciklama=NullOkuString(reader,k++)});}

            basvuru.YatirimAdresleri = adresler;

            basvuru.yatirim.harcamaTurleri = harcamaTurleri;
            basvuru.yatirim.yatirimTurleri = yatirimTurleri.Count > 0
                ? yatirimTurleri
                : basvuru.yatirim.yatirimTuru == enumYatirimTuru.Tanimsiz
                    ? new List<int>()
                    : new List<int> { (int)basvuru.yatirim.yatirimTuru };

            basvuru.yatirim.degerZinciriAsamalari = asamalar;
            basvuru.yatirim.degerZinciriId = asamalar.Count >= 1
                    ? asamalar.First().dz.id
                    : null;

            basvuru.ortaklik.ortaklar = ortaklar;
            basvuru.AdliSicilKisileri = adliSicilKisileri;
            basvuru.YatirimOnBilgileri = yatirimOnBilgileri;
            basvuru.Makineler = makineler;
            basvuru.UrunSurecleri = urunSurecleri;
            basvuru.Binalar = binalar;
            basvuru.TedarikciEntegrasyonlari = tedarikler;
            basvuru.IzlemeGostergeleri = izleme;
        }

        public async Task BasvuruYatirimOnBilgileriKaydetAsync(int basvuruId, List<BasvuruYatirimOnBilgi> kayitlar)
        {
            await using (SqlCommand sil=KomutOlustur("DELETE FROM dbo.BasvuruYatirimOnBilgi WHERE BasvuruId=@BasvuruId;"))
            {
                sil.Parameters.AddWithValue("@BasvuruId",basvuruId);
                await sil.ExecuteNonQueryAsync();
            }
            const string sql=@"INSERT dbo.BasvuruYatirimOnBilgi(BasvuruId,Tur,SiraNo,Ad,Miktar,Birim,TekPanelGucu,TekPanelGucuBirim,ToplamGuc,ToplamGucBirim)
                OUTPUT INSERTED.Id VALUES(@BasvuruId,@Tur,@SiraNo,@Ad,@Miktar,@Birim,@TekPanelGucu,@TekPanelGucuBirim,@ToplamGuc,@ToplamGucBirim);";
            foreach(BasvuruYatirimOnBilgi x in kayitlar)
            {
                await using SqlCommand c=KomutOlustur(sql);
                c.Parameters.AddWithValue("@BasvuruId",basvuruId);c.Parameters.AddWithValue("@Tur",(int)x.tur);c.Parameters.AddWithValue("@SiraNo",x.siraNo);c.Parameters.AddWithValue("@Ad",x.ad);
                c.Parameters.AddWithValue("@Miktar",DbNull(x.miktar));c.Parameters.AddWithValue("@Birim",DbNull(x.birim));c.Parameters.AddWithValue("@TekPanelGucu",DbNull(x.tekPanelGucu));c.Parameters.AddWithValue("@TekPanelGucuBirim",DbNull(x.tekPanelGucuBirim));c.Parameters.AddWithValue("@ToplamGuc",DbNull(x.toplamGuc));c.Parameters.AddWithValue("@ToplamGucBirim",DbNull(x.toplamGucBirim));
                x.id=Convert.ToInt32(await c.ExecuteScalarAsync());x.basvuruId=basvuruId;
            }
        }

        public async Task BasvuruYatirimOnBilgisiKaydetAsync(BasvuruYatirimOnBilgi x)
        {
            const string guncelle=@"UPDATE dbo.BasvuruYatirimOnBilgi SET Tur=@Tur,SiraNo=@SiraNo,Ad=@Ad,Miktar=@Miktar,Birim=@Birim,TekPanelGucu=@TekPanelGucu,TekPanelGucuBirim=@TekPanelGucuBirim,ToplamGuc=@ToplamGuc,ToplamGucBirim=@ToplamGucBirim WHERE Id=@Id AND BasvuruId=@BasvuruId;";
            const string ekle=@"INSERT dbo.BasvuruYatirimOnBilgi(BasvuruId,Tur,SiraNo,Ad,Miktar,Birim,TekPanelGucu,TekPanelGucuBirim,ToplamGuc,ToplamGucBirim) OUTPUT INSERTED.Id VALUES(@BasvuruId,@Tur,@SiraNo,@Ad,@Miktar,@Birim,@TekPanelGucu,@TekPanelGucuBirim,@ToplamGuc,@ToplamGucBirim);";
            await using SqlCommand c=KomutOlustur(x.id>0?guncelle:ekle);
            c.Parameters.AddWithValue("@BasvuruId",x.basvuruId);c.Parameters.AddWithValue("@Tur",(int)x.tur);c.Parameters.AddWithValue("@SiraNo",x.siraNo);c.Parameters.AddWithValue("@Ad",x.ad);c.Parameters.AddWithValue("@Miktar",DbNull(x.miktar));c.Parameters.AddWithValue("@Birim",DbNull(x.birim));c.Parameters.AddWithValue("@TekPanelGucu",DbNull(x.tekPanelGucu));c.Parameters.AddWithValue("@TekPanelGucuBirim",DbNull(x.tekPanelGucuBirim));c.Parameters.AddWithValue("@ToplamGuc",DbNull(x.toplamGuc));c.Parameters.AddWithValue("@ToplamGucBirim",DbNull(x.toplamGucBirim));
            if(x.id>0){c.Parameters.AddWithValue("@Id",x.id);if(await c.ExecuteNonQueryAsync()==0)throw new InvalidOperationException("Yatırım ön bilgisi başvuruya ait değil.");}else x.id=Convert.ToInt32(await c.ExecuteScalarAsync());
        }

        public async Task<bool> BasvuruYatirimOnBilgisiSilAsync(int basvuruId,int id)
        {
            await using SqlCommand c=KomutOlustur("DELETE FROM dbo.BasvuruYatirimOnBilgi WHERE Id=@Id AND BasvuruId=@BasvuruId;");
            c.Parameters.AddWithValue("@Id",id);c.Parameters.AddWithValue("@BasvuruId",basvuruId);
            return await c.ExecuteNonQueryAsync()>0;
        }
        public async Task BasvuruMakineTeklifDosyasiGuncelleAsync(int basvuruId,int teklifId,int dosyaId,string dosyaAdi){const string sql=@"UPDATE t SET TeklifBelgesiDosyaId=@DosyaId,TeklifBelgesiDosyaAdi=@DosyaAdi FROM dbo.BasvuruMakineTeklif t INNER JOIN dbo.BasvuruMakine m ON m.Id=t.MakineId WHERE t.Id=@TeklifId AND m.BasvuruId=@BasvuruId;";await using SqlCommand c=KomutOlustur(sql);c.Parameters.AddWithValue("@DosyaId",dosyaId);c.Parameters.AddWithValue("@DosyaAdi",dosyaAdi);c.Parameters.AddWithValue("@TeklifId",teklifId);c.Parameters.AddWithValue("@BasvuruId",basvuruId);await c.ExecuteNonQueryAsync();}

        public async Task BasvuruMakinesiKaydetAsync(BasvuruMakine m)
        {
            if(m.id>0){await using SqlCommand c=KomutOlustur(@"UPDATE dbo.BasvuruMakine SET SiraNo=@SiraNo,Ad=@Ad,Birim=@Birim,Miktar=@Miktar,Aciklama=@Aciklama,Marka=@Marka,Model=@Model,KapasiteOzellikleri=@KapasiteOzellikleri,YerlesimPlaniSiraNo=@YerlesimPlaniSiraNo,KullanimAmaci=@KullanimAmaci,Durum=@Durum,KapasiteSecimGerekcesi=@KapasiteSecimGerekcesi WHERE Id=@Id AND BasvuruId=@BasvuruId;");MakineParametreleri(c,m);c.Parameters.AddWithValue("@Id",m.id);if(await c.ExecuteNonQueryAsync()==0)throw new InvalidOperationException("Makine kaydı başvuruya ait değil.");}
            else{await using SqlCommand c=KomutOlustur(@"INSERT dbo.BasvuruMakine(BasvuruId,SiraNo,Ad,Birim,Miktar,Aciklama,Marka,Model,KapasiteOzellikleri,YerlesimPlaniSiraNo,KullanimAmaci,Durum,KapasiteSecimGerekcesi) OUTPUT INSERTED.Id VALUES(@BasvuruId,@SiraNo,@Ad,@Birim,@Miktar,@Aciklama,@Marka,@Model,@KapasiteOzellikleri,@YerlesimPlaniSiraNo,@KullanimAmaci,@Durum,@KapasiteSecimGerekcesi);");MakineParametreleri(c,m);m.id=Convert.ToInt32(await c.ExecuteScalarAsync());}
            foreach(BasvuruMakineOzellik o in m.teknikOzellikler){o.makineId=m.id;if(o.id>0){await using SqlCommand c=KomutOlustur(@"UPDATE dbo.BasvuruMakineOzellik SET SiraNo=@SiraNo,Baslik=@Baslik,AciklamaAsgariGereklilik=@Aciklama,ZorunluMu=@Zorunlu WHERE Id=@Id AND MakineId=@MakineId;");OzellikParametreleri(c,o);c.Parameters.AddWithValue("@Id",o.id);if(await c.ExecuteNonQueryAsync()==0)throw new InvalidOperationException("Teknik özellik kaydı makineye ait değil.");}else{await using SqlCommand c=KomutOlustur(@"INSERT dbo.BasvuruMakineOzellik(MakineId,SiraNo,Baslik,AciklamaAsgariGereklilik,ZorunluMu) OUTPUT INSERTED.Id VALUES(@MakineId,@SiraNo,@Baslik,@Aciklama,@Zorunlu);");OzellikParametreleri(c,o);o.id=Convert.ToInt32(await c.ExecuteScalarAsync());}}
            foreach(BasvuruMakineTeklif t in m.teklifler){t.makineId=m.id;if(t.id>0){await using SqlCommand c=KomutOlustur(@"UPDATE dbo.BasvuruMakineTeklif SET SiraNo=@SiraNo,BasvuruyaEsas=@Esas,Tedarikci=@Tedarikci,Marka=@Marka,Model=@Model,ParaBirimi=@ParaBirimi,Kur=@Kur,BirimFiyat=@BirimFiyat,TeklifTarihi=@TeklifTarihi,GecerlilikTarihi=@GecerlilikTarihi,Aciklama=@Aciklama WHERE Id=@Id AND MakineId=@MakineId;");TeklifParametreleri(c,t);c.Parameters.AddWithValue("@Id",t.id);if(await c.ExecuteNonQueryAsync()==0)throw new InvalidOperationException("Teklif kaydı makineye ait değil.");}else{await using SqlCommand c=KomutOlustur(@"INSERT dbo.BasvuruMakineTeklif(MakineId,SiraNo,BasvuruyaEsas,Tedarikci,Marka,Model,ParaBirimi,Kur,BirimFiyat,TeklifTarihi,GecerlilikTarihi,Aciklama) OUTPUT INSERTED.Id VALUES(@MakineId,@SiraNo,@Esas,@Tedarikci,@Marka,@Model,@ParaBirimi,@Kur,@BirimFiyat,@TeklifTarihi,@GecerlilikTarihi,@Aciklama);");TeklifParametreleri(c,t);t.id=Convert.ToInt32(await c.ExecuteScalarAsync());}}
        }
        private static void MakineParametreleri(SqlCommand c,BasvuruMakine m){c.Parameters.AddWithValue("@BasvuruId",m.basvuruId);c.Parameters.AddWithValue("@SiraNo",m.siraNo);c.Parameters.AddWithValue("@Ad",m.ad);c.Parameters.AddWithValue("@Birim",m.birim);c.Parameters.AddWithValue("@Miktar",m.miktar);c.Parameters.AddWithValue("@Aciklama",DbNull(m.aciklama));c.Parameters.AddWithValue("@Marka",DbNull(m.marka));c.Parameters.AddWithValue("@Model",DbNull(m.model));c.Parameters.AddWithValue("@KapasiteOzellikleri",DbNull(m.kapasiteOzellikleri));c.Parameters.AddWithValue("@YerlesimPlaniSiraNo",DbNull(m.yerlesimPlaniSiraNo));c.Parameters.AddWithValue("@KullanimAmaci",DbNull(m.kullanimAmaci));c.Parameters.AddWithValue("@Durum",DbNull(m.durum));c.Parameters.AddWithValue("@KapasiteSecimGerekcesi",DbNull(m.kapasiteSecimGerekcesi));}
        private static void OzellikParametreleri(SqlCommand c,BasvuruMakineOzellik o){c.Parameters.AddWithValue("@MakineId",o.makineId);c.Parameters.AddWithValue("@SiraNo",o.siraNo);c.Parameters.AddWithValue("@Baslik",o.baslik);c.Parameters.AddWithValue("@Aciklama",o.aciklamaAsgariGereklilik);c.Parameters.AddWithValue("@Zorunlu",o.zorunluMu);}
        private static void TeklifParametreleri(SqlCommand c,BasvuruMakineTeklif t){c.Parameters.AddWithValue("@MakineId",t.makineId);c.Parameters.AddWithValue("@SiraNo",t.siraNo);c.Parameters.AddWithValue("@Esas",t.basvuruyaEsas);c.Parameters.AddWithValue("@Tedarikci",t.tedarikci);c.Parameters.AddWithValue("@Marka",DbNull(t.marka));c.Parameters.AddWithValue("@Model",DbNull(t.model));c.Parameters.AddWithValue("@ParaBirimi",t.paraBirimi);c.Parameters.AddWithValue("@Kur",DbNull(t.kur));c.Parameters.AddWithValue("@BirimFiyat",DbNull(t.birimFiyat));c.Parameters.AddWithValue("@TeklifTarihi",t.teklifTarihi.HasValue?t.teklifTarihi.Value.Date:DBNull.Value);c.Parameters.AddWithValue("@GecerlilikTarihi",t.gecerlilikTarihi.HasValue?t.gecerlilikTarihi.Value.Date:DBNull.Value);c.Parameters.AddWithValue("@Aciklama",DbNull(t.aciklama));}
        public async Task BasvuruMakineUzmanGuncelleAsync(BasvuruMakineUzmanKayitModel m){const string sql=@"UPDATE dbo.BasvuruMakine SET UzmanParaBirimi=@ParaBirimi,UzmanKur=@Kur,UzmanMinimumFiyat=@Min,UzmanMaksimumFiyat=@Maks,UzmanSecilenTeklifId=@TeklifId,UzmanOnerilenFiyatTl=@Oneri,UzmanKontrolSonucu=@Sonuc,UzmanAciklama=@Aciklama WHERE Id=@Id AND BasvuruId=@BasvuruId AND (@TeklifId IS NULL OR EXISTS(SELECT 1 FROM dbo.BasvuruMakineTeklif WHERE Id=@TeklifId AND MakineId=@Id));";await using SqlCommand c=KomutOlustur(sql);c.Parameters.AddWithValue("@ParaBirimi",DbNull(m.uzmanParaBirimi));c.Parameters.AddWithValue("@Kur",DbNull(m.uzmanKur));c.Parameters.AddWithValue("@Min",DbNull(m.uzmanMinimumFiyat));c.Parameters.AddWithValue("@Maks",DbNull(m.uzmanMaksimumFiyat));c.Parameters.AddWithValue("@TeklifId",DbNull(m.uzmanSecilenTeklifId));c.Parameters.AddWithValue("@Oneri",DbNull(m.uzmanOnerilenFiyatTl));c.Parameters.AddWithValue("@Sonuc",DbNull(m.uzmanKontrolSonucu));c.Parameters.AddWithValue("@Aciklama",DbNull(m.uzmanAciklama));c.Parameters.AddWithValue("@Id",m.makineId);c.Parameters.AddWithValue("@BasvuruId",m.basvuruId);if(await c.ExecuteNonQueryAsync()==0)throw new InvalidOperationException("Makine veya seçilen teklif bulunamadı.");}
        public async Task BasvuruMakineUzmanDokumaniKaydetAsync(BasvuruMakineUzmanDokuman d)
        {
            if(d.id<=0){await using SqlCommand s=KomutOlustur("SELECT ISNULL(MAX(d.SiraNo),0)+1 FROM dbo.BasvuruMakineUzmanDokuman d INNER JOIN dbo.BasvuruMakine m ON m.Id=d.MakineId WHERE d.MakineId=@MakineId AND m.BasvuruId=@BasvuruId;");s.Parameters.AddWithValue("@MakineId",d.makineId);s.Parameters.AddWithValue("@BasvuruId",d.basvuruId);d.siraNo=Convert.ToInt32(await s.ExecuteScalarAsync());}
            const string ekle=@"INSERT dbo.BasvuruMakineUzmanDokuman(MakineId,SiraNo,DokumanAdi,DokumanTuru,KaynakTedarikci,BelgeTarihi,Aciklama) SELECT @MakineId,@SiraNo,@DokumanAdi,@DokumanTuru,@Kaynak,@BelgeTarihi,@Aciklama WHERE EXISTS(SELECT 1 FROM dbo.BasvuruMakine WHERE Id=@MakineId AND BasvuruId=@BasvuruId); SELECT CAST(SCOPE_IDENTITY() AS INT);";
            const string guncelle=@"UPDATE d SET DokumanAdi=@DokumanAdi,DokumanTuru=@DokumanTuru,KaynakTedarikci=@Kaynak,BelgeTarihi=@BelgeTarihi,Aciklama=@Aciklama FROM dbo.BasvuruMakineUzmanDokuman d INNER JOIN dbo.BasvuruMakine m ON m.Id=d.MakineId WHERE d.Id=@Id AND d.MakineId=@MakineId AND m.BasvuruId=@BasvuruId;";
            await using SqlCommand c=KomutOlustur(d.id>0?guncelle:ekle);c.Parameters.AddWithValue("@MakineId",d.makineId);c.Parameters.AddWithValue("@BasvuruId",d.basvuruId);c.Parameters.AddWithValue("@SiraNo",d.siraNo);c.Parameters.AddWithValue("@DokumanAdi",d.dokumanAdi);c.Parameters.AddWithValue("@DokumanTuru",d.dokumanTuru);c.Parameters.AddWithValue("@Kaynak",d.kaynakTedarikci);c.Parameters.AddWithValue("@BelgeTarihi",d.belgeTarihi.HasValue?d.belgeTarihi.Value.Date:DBNull.Value);c.Parameters.AddWithValue("@Aciklama",DbNull(d.aciklama));
            if(d.id>0){c.Parameters.AddWithValue("@Id",d.id);if(await c.ExecuteNonQueryAsync()==0)throw new InvalidOperationException("Araştırma dokümanı makineye ait değil.");}else d.id=Convert.ToInt32(await c.ExecuteScalarAsync()??throw new InvalidOperationException("Makine bulunamadı."));
        }
        public async Task BasvuruMakineUzmanDokumaniDosyaGuncelleAsync(int basvuruId,int dokumanId,int dosyaId,string dosyaAdi){const string sql=@"UPDATE d SET DosyaId=@DosyaId,DosyaAdi=@DosyaAdi FROM dbo.BasvuruMakineUzmanDokuman d INNER JOIN dbo.BasvuruMakine m ON m.Id=d.MakineId WHERE d.Id=@DokumanId AND m.BasvuruId=@BasvuruId;";await using SqlCommand c=KomutOlustur(sql);c.Parameters.AddWithValue("@DosyaId",dosyaId);c.Parameters.AddWithValue("@DosyaAdi",dosyaAdi);c.Parameters.AddWithValue("@DokumanId",dokumanId);c.Parameters.AddWithValue("@BasvuruId",basvuruId);if(await c.ExecuteNonQueryAsync()==0)throw new InvalidOperationException("Araştırma dokümanı bulunamadı.");}
        public async Task BasvuruUrunSureciKaydetAsync(BasvuruUrunSurec s)
        {
            if(s.id>0){await using SqlCommand c=KomutOlustur("UPDATE dbo.BasvuruUrunSurec SET UrunId=@UrunId,SiraNo=@SiraNo,SurecAdi=@SurecAdi WHERE Id=@Id AND BasvuruId=@BasvuruId;");SurecParametreleri(c,s);c.Parameters.AddWithValue("@Id",s.id);if(await c.ExecuteNonQueryAsync()==0)throw new InvalidOperationException("Ürün süreci bulunamadı.");}
            else{await using SqlCommand c=KomutOlustur("INSERT dbo.BasvuruUrunSurec(BasvuruId,UrunId,SiraNo,SurecAdi) OUTPUT INSERTED.Id VALUES(@BasvuruId,@UrunId,@SiraNo,@SurecAdi);");SurecParametreleri(c,s);s.id=Convert.ToInt32(await c.ExecuteScalarAsync());}
        }
        public async Task BasvuruUrunSurecMakinesiKaydetAsync(BasvuruUrunSurecMakine m)
        {
            const string ekle=@"INSERT dbo.BasvuruUrunSurecMakine(SurecId,MakineId,SiraNo,Adet,YerlesimPlaniNo,GirdilerMiktarlar,CiktilarMiktarlar,IslemeKapasitesi,GunlukCalismaSuresi,GunlukCalismaSuresiBirimi,Aciklama)
                SELECT @SurecId,@MakineId,@SiraNo,@Adet,@Plan,@Girdiler,@Ciktilar,@Kapasite,@Sure,@SureBirimi,@Aciklama
                WHERE EXISTS(SELECT 1 FROM dbo.BasvuruUrunSurec WHERE Id=@SurecId AND BasvuruId=@BasvuruId)
                  AND EXISTS(SELECT 1 FROM dbo.BasvuruMakine WHERE Id=@MakineId AND BasvuruId=@BasvuruId);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";
            const string guncelle=@"UPDATE sm SET MakineId=@MakineId,SiraNo=@SiraNo,Adet=@Adet,YerlesimPlaniNo=@Plan,GirdilerMiktarlar=@Girdiler,CiktilarMiktarlar=@Ciktilar,IslemeKapasitesi=@Kapasite,GunlukCalismaSuresi=@Sure,GunlukCalismaSuresiBirimi=@SureBirimi,Aciklama=@Aciklama
                FROM dbo.BasvuruUrunSurecMakine sm INNER JOIN dbo.BasvuruUrunSurec s ON s.Id=sm.SurecId
                WHERE sm.Id=@Id AND sm.SurecId=@SurecId AND s.BasvuruId=@BasvuruId AND EXISTS(SELECT 1 FROM dbo.BasvuruMakine WHERE Id=@MakineId AND BasvuruId=@BasvuruId);";
            await using SqlCommand c=KomutOlustur(m.id>0?guncelle:ekle);c.Parameters.AddWithValue("@BasvuruId",m.basvuruId);c.Parameters.AddWithValue("@SurecId",m.surecId);c.Parameters.AddWithValue("@MakineId",m.makineId);c.Parameters.AddWithValue("@SiraNo",m.siraNo);c.Parameters.AddWithValue("@Adet",m.adet);c.Parameters.AddWithValue("@Plan",DbNull(m.yerlesimPlaniNo));c.Parameters.AddWithValue("@Girdiler",DbNull(m.girdilerMiktarlar));c.Parameters.AddWithValue("@Ciktilar",DbNull(m.ciktilarMiktarlar));c.Parameters.AddWithValue("@Kapasite",DbNull(m.islemeKapasitesi));c.Parameters.AddWithValue("@Sure",DbNull(m.gunlukCalismaSuresi));c.Parameters.AddWithValue("@SureBirimi",m.gunlukCalismaSuresiBirimi);c.Parameters.AddWithValue("@Aciklama",DbNull(m.aciklama));
            if(m.id>0){c.Parameters.AddWithValue("@Id",m.id);if(await c.ExecuteNonQueryAsync()==0)throw new InvalidOperationException("Süreç makine kaydı bulunamadı.");}else m.id=Convert.ToInt32(await c.ExecuteScalarAsync()??throw new InvalidOperationException("Süreç veya makine bulunamadı."));
        }
        public async Task<bool> BasvuruUrunSurecMakinesiSilAsync(int basvuruId,int id){await using SqlCommand c=KomutOlustur("DELETE sm FROM dbo.BasvuruUrunSurecMakine sm INNER JOIN dbo.BasvuruUrunSurec s ON s.Id=sm.SurecId WHERE sm.Id=@Id AND s.BasvuruId=@BasvuruId;");c.Parameters.AddWithValue("@Id",id);c.Parameters.AddWithValue("@BasvuruId",basvuruId);return await c.ExecuteNonQueryAsync()>0;}
        public async Task BasvuruBinasiKaydetAsync(BasvuruBina b)
        {
            const string ekle="INSERT dbo.BasvuruBina(BasvuruId,SiraNo,Ad,MevcutYeni,YatirimSekli,DestekTalebi,VaziyetPlaniNo) OUTPUT INSERTED.Id VALUES(@BasvuruId,@SiraNo,@Ad,@MevcutYeni,@YatirimSekli,@DestekTalebi,@VaziyetPlaniNo);";
            const string guncelle="UPDATE dbo.BasvuruBina SET SiraNo=@SiraNo,Ad=@Ad,MevcutYeni=@MevcutYeni,YatirimSekli=@YatirimSekli,DestekTalebi=@DestekTalebi,VaziyetPlaniNo=@VaziyetPlaniNo WHERE Id=@Id AND BasvuruId=@BasvuruId;";
            await using SqlCommand c=KomutOlustur(b.id>0?guncelle:ekle);c.Parameters.AddWithValue("@BasvuruId",b.basvuruId);c.Parameters.AddWithValue("@SiraNo",b.siraNo);c.Parameters.AddWithValue("@Ad",b.ad);c.Parameters.AddWithValue("@MevcutYeni",DbNull(b.mevcutYeni));c.Parameters.AddWithValue("@YatirimSekli",DbNull(b.yatirimSekli));c.Parameters.AddWithValue("@DestekTalebi",DbNull(b.destekTalebi));c.Parameters.AddWithValue("@VaziyetPlaniNo",DbNull(b.vaziyetPlaniNo));if(b.id>0){c.Parameters.AddWithValue("@Id",b.id);if(await c.ExecuteNonQueryAsync()==0)throw new InvalidOperationException("Bina bulunamadı.");}else b.id=Convert.ToInt32(await c.ExecuteScalarAsync());
        }
        public async Task BasvuruBinaMahaliKaydetAsync(BasvuruBinaMahal m)
        {
            const string ekle="INSERT dbo.BasvuruBinaMahal(BinaId,SiraNo,MahalAdi,AlanM2) SELECT @BinaId,@SiraNo,@MahalAdi,@AlanM2 WHERE EXISTS(SELECT 1 FROM dbo.BasvuruBina WHERE Id=@BinaId AND BasvuruId=@BasvuruId); SELECT CAST(SCOPE_IDENTITY() AS INT);";
            const string guncelle="UPDATE m SET SiraNo=@SiraNo,MahalAdi=@MahalAdi,AlanM2=@AlanM2 FROM dbo.BasvuruBinaMahal m INNER JOIN dbo.BasvuruBina b ON b.Id=m.BinaId WHERE m.Id=@Id AND m.BinaId=@BinaId AND b.BasvuruId=@BasvuruId;";
            await using SqlCommand c=KomutOlustur(m.id>0?guncelle:ekle);c.Parameters.AddWithValue("@BasvuruId",m.basvuruId);c.Parameters.AddWithValue("@BinaId",m.binaId);c.Parameters.AddWithValue("@SiraNo",m.siraNo);c.Parameters.AddWithValue("@MahalAdi",m.mahalAdi);c.Parameters.AddWithValue("@AlanM2",m.alanM2);if(m.id>0){c.Parameters.AddWithValue("@Id",m.id);if(await c.ExecuteNonQueryAsync()==0)throw new InvalidOperationException("Bölüm/mahal bulunamadı.");}else m.id=Convert.ToInt32(await c.ExecuteScalarAsync()??throw new InvalidOperationException("Bina bulunamadı."));
        }
        public async Task<bool> BasvuruBinaMahaliSilAsync(int basvuruId,int id){await using SqlCommand c=KomutOlustur("DELETE m FROM dbo.BasvuruBinaMahal m INNER JOIN dbo.BasvuruBina b ON b.Id=m.BinaId WHERE m.Id=@Id AND b.BasvuruId=@BasvuruId;");c.Parameters.AddWithValue("@Id",id);c.Parameters.AddWithValue("@BasvuruId",basvuruId);return await c.ExecuteNonQueryAsync()>0;}
        public async Task<bool> BasvuruBinasiSilAsync(int basvuruId,int id){await using SqlCommand c=KomutOlustur("DELETE FROM dbo.BasvuruBina WHERE Id=@Id AND BasvuruId=@BasvuruId;");c.Parameters.AddWithValue("@Id",id);c.Parameters.AddWithValue("@BasvuruId",basvuruId);return await c.ExecuteNonQueryAsync()>0;}
        public async Task IstihdamKaydetAsync(BasvuruIstihdam model){const string sql="UPDATE dbo.Basvuru SET IstihdamJson=@Json WHERE Id=@BasvuruId;";await using SqlCommand c=KomutOlustur(sql);c.Parameters.AddWithValue("@BasvuruId",model.basvuruId);c.Parameters.AddWithValue("@Json",System.Text.Json.JsonSerializer.Serialize(model));if(await c.ExecuteNonQueryAsync()!=1)throw new InvalidOperationException("İstihdam kaydı bulunamadı.");}
        public async Task IstihdamSgkDosyasiGuncelleAsync(int basvuruId,int dosyaId,string dosyaAdi){const string sql="UPDATE dbo.Basvuru SET IstihdamSgkDosyaId=@DosyaId,IstihdamSgkDosyaAdi=@DosyaAdi WHERE Id=@BasvuruId;";await using SqlCommand c=KomutOlustur(sql);c.Parameters.AddWithValue("@BasvuruId",basvuruId);c.Parameters.AddWithValue("@DosyaId",dosyaId);c.Parameters.AddWithValue("@DosyaAdi",dosyaAdi);if(await c.ExecuteNonQueryAsync()!=1)throw new InvalidOperationException("İstihdam kaydı bulunamadı.");}
        public async Task IstihdamSatiriKaydetAsync(BasvuruIstihdamSatir x){const string ekle="INSERT dbo.BasvuruIstihdamSatir(BasvuruId,SiraNo,BirimUnite,GorevUretimHatti,Cinsiyet,YasDurumu,MevcutCalisan,NetCalisanArtisi,BazAylikBrutUcret,HedefAylikBrutUcret) OUTPUT INSERTED.Id VALUES(@BasvuruId,@SiraNo,@Birim,@Gorev,@Cinsiyet,@Yas,@Mevcut,@Artis,@Baz,@Hedef);";const string guncelle="UPDATE dbo.BasvuruIstihdamSatir SET SiraNo=@SiraNo,BirimUnite=@Birim,GorevUretimHatti=@Gorev,Cinsiyet=@Cinsiyet,YasDurumu=@Yas,MevcutCalisan=@Mevcut,NetCalisanArtisi=@Artis,BazAylikBrutUcret=@Baz,HedefAylikBrutUcret=@Hedef WHERE Id=@Id AND BasvuruId=@BasvuruId;";await using SqlCommand c=KomutOlustur(x.id>0?guncelle:ekle);c.Parameters.AddWithValue("@BasvuruId",x.basvuruId);c.Parameters.AddWithValue("@SiraNo",x.siraNo);c.Parameters.AddWithValue("@Birim",x.birimUnite);c.Parameters.AddWithValue("@Gorev",x.gorevUretimHatti);c.Parameters.AddWithValue("@Cinsiyet",x.cinsiyet);c.Parameters.AddWithValue("@Yas",x.yasDurumu);c.Parameters.AddWithValue("@Mevcut",x.mevcutCalisan);c.Parameters.AddWithValue("@Artis",x.netCalisanArtisi);c.Parameters.AddWithValue("@Baz",x.bazAylikBrutUcret);c.Parameters.AddWithValue("@Hedef",x.hedefAylikBrutUcret);if(x.id>0){c.Parameters.AddWithValue("@Id",x.id);if(await c.ExecuteNonQueryAsync()!=1)throw new InvalidOperationException("İstihdam planı satırı bulunamadı.");}else x.id=Convert.ToInt32(await c.ExecuteScalarAsync());}
        public async Task<bool> IstihdamSatiriSilAsync(int basvuruId,int id){await using SqlCommand c=KomutOlustur("DELETE FROM dbo.BasvuruIstihdamSatir WHERE Id=@Id AND BasvuruId=@BasvuruId;");c.Parameters.AddWithValue("@Id",id);c.Parameters.AddWithValue("@BasvuruId",basvuruId);return await c.ExecuteNonQueryAsync()==1;}
        public async Task TedarikciEntegrasyonuKaydetAsync(BasvuruTedarikciEntegrasyonu x){const string ekle=@"INSERT dbo.BasvuruTedarikciEntegrasyonu(BasvuruId,UrunId,TarimsalUrun,IlId,IlceId,Birim,MevcutYillikMiktar,HedefYillikMiktar,MevcutKayitliCiftci,EklenecekKayitliCiftci,TedarikSekli,KisaAciklama) OUTPUT INSERTED.Id VALUES(@BasvuruId,@UrunId,@TarimsalUrun,@IlId,@IlceId,@Birim,@MevcutMiktar,@HedefMiktar,@MevcutCiftci,@EklenecekCiftci,@TedarikSekli,@Aciklama);";const string guncelle=@"UPDATE dbo.BasvuruTedarikciEntegrasyonu SET UrunId=@UrunId,TarimsalUrun=@TarimsalUrun,IlId=@IlId,IlceId=@IlceId,Birim=@Birim,MevcutYillikMiktar=@MevcutMiktar,HedefYillikMiktar=@HedefMiktar,MevcutKayitliCiftci=@MevcutCiftci,EklenecekKayitliCiftci=@EklenecekCiftci,TedarikSekli=@TedarikSekli,KisaAciklama=@Aciklama WHERE Id=@Id AND BasvuruId=@BasvuruId;";await using SqlCommand c=KomutOlustur(x.id>0?guncelle:ekle);c.Parameters.AddWithValue("@BasvuruId",x.basvuruId);c.Parameters.AddWithValue("@UrunId",x.urunId);c.Parameters.AddWithValue("@TarimsalUrun",x.tarimsalUrun);c.Parameters.AddWithValue("@IlId",x.ilId);c.Parameters.AddWithValue("@IlceId",x.ilceId);c.Parameters.AddWithValue("@Birim",x.birim);c.Parameters.AddWithValue("@MevcutMiktar",x.mevcutYillikMiktar);c.Parameters.AddWithValue("@HedefMiktar",x.hedefYillikMiktar);c.Parameters.AddWithValue("@MevcutCiftci",x.mevcutKayitliCiftci);c.Parameters.AddWithValue("@EklenecekCiftci",x.eklenecekKayitliCiftci);c.Parameters.AddWithValue("@TedarikSekli",x.tedarikSekli);c.Parameters.AddWithValue("@Aciklama",DbNull(x.kisaAciklama));if(x.id>0){c.Parameters.AddWithValue("@Id",x.id);if(await c.ExecuteNonQueryAsync()!=1)throw new InvalidOperationException("Tedarik kaydı bulunamadı.");}else x.id=Convert.ToInt32(await c.ExecuteScalarAsync());}
        public async Task<bool> TedarikciEntegrasyonuSilAsync(int basvuruId,int id){await using SqlCommand c=KomutOlustur("DELETE FROM dbo.BasvuruTedarikciEntegrasyonu WHERE Id=@Id AND BasvuruId=@BasvuruId;");c.Parameters.AddWithValue("@Id",id);c.Parameters.AddWithValue("@BasvuruId",basvuruId);return await c.ExecuteNonQueryAsync()==1;}
        public async Task TedarikciEntegrasyonuDosyasiGuncelleAsync(int basvuruId,int id,int dosyaId,string dosyaAdi){await using SqlCommand c=KomutOlustur("UPDATE dbo.BasvuruTedarikciEntegrasyonu SET DayanakBelgeDosyaId=@DosyaId,DayanakBelgeDosyaAdi=@DosyaAdi WHERE Id=@Id AND BasvuruId=@BasvuruId;");c.Parameters.AddWithValue("@Id",id);c.Parameters.AddWithValue("@BasvuruId",basvuruId);c.Parameters.AddWithValue("@DosyaId",dosyaId);c.Parameters.AddWithValue("@DosyaAdi",dosyaAdi);if(await c.ExecuteNonQueryAsync()!=1)throw new InvalidOperationException("Tedarik kaydı bulunamadı.");}
        public async Task TedarikciEntegrasyonuAciklamaKaydetAsync(int basvuruId,string aciklama){await using SqlCommand c=KomutOlustur("UPDATE dbo.Basvuru SET TedarikciEntegrasyonuAciklama=@Aciklama WHERE Id=@BasvuruId;");c.Parameters.AddWithValue("@BasvuruId",basvuruId);c.Parameters.AddWithValue("@Aciklama",DbNull(aciklama));if(await c.ExecuteNonQueryAsync()!=1)throw new InvalidOperationException("Başvuru bulunamadı.");}
        public async Task BasvuruOzetiKurumKaydetAsync(BasvuruOzetiKurum model){await using SqlCommand c=KomutOlustur("UPDATE dbo.Basvuru SET BasvuruOzetiKurumJson=@Json WHERE Id=@BasvuruId;");c.Parameters.AddWithValue("@BasvuruId",model.basvuruId);c.Parameters.AddWithValue("@Json",DbNull(model.kurumJson));if(await c.ExecuteNonQueryAsync()!=1)throw new InvalidOperationException("Başvuru bulunamadı.");}
        public async Task BasvuruIzlemeGostergesiKaydetAsync(BasvuruIzlemeGostergesi x){const string ekle="INSERT dbo.BasvuruIzlemeGostergesi(BasvuruId,GostergeKodu,BaslangicDegeri,HedefDeger,KadinKirilimi,GencKirilimi,Aciklama) OUTPUT INSERTED.Id VALUES(@BasvuruId,@Kod,@Baslangic,@Hedef,@Kadin,@Genc,@Aciklama);";const string guncelle="UPDATE dbo.BasvuruIzlemeGostergesi SET GostergeKodu=@Kod,BaslangicDegeri=@Baslangic,HedefDeger=@Hedef,KadinKirilimi=@Kadin,GencKirilimi=@Genc,Aciklama=@Aciklama WHERE Id=@Id AND BasvuruId=@BasvuruId;";await using SqlCommand c=KomutOlustur(x.id>0?guncelle:ekle);c.Parameters.AddWithValue("@BasvuruId",x.basvuruId);c.Parameters.AddWithValue("@Kod",x.gostergeKodu);c.Parameters.AddWithValue("@Baslangic",DbNull(x.baslangicDegeri));c.Parameters.AddWithValue("@Hedef",DbNull(x.hedefDeger));c.Parameters.AddWithValue("@Kadin",DbNull(x.kadinKirilimi));c.Parameters.AddWithValue("@Genc",DbNull(x.gencKirilimi));c.Parameters.AddWithValue("@Aciklama",DbNull(x.aciklama));if(x.id>0){c.Parameters.AddWithValue("@Id",x.id);if(await c.ExecuteNonQueryAsync()!=1)throw new InvalidOperationException("İzleme göstergesi bulunamadı.");}else x.id=Convert.ToInt32(await c.ExecuteScalarAsync());}
        public async Task BasvuruIzlemeUstBilgiKaydetAsync(BasvuruIzlemeUstBilgi x){await using SqlCommand c=KomutOlustur("UPDATE dbo.Basvuru SET IzlemeBaslangicTarihi=@Baslangic,IzlemeHedefTarihi=@Hedef,IzlemeVeriSorumlusu=@Sorumlu WHERE Id=@BasvuruId;");c.Parameters.AddWithValue("@BasvuruId",x.basvuruId);c.Parameters.AddWithValue("@Baslangic",x.baslangicTarihi.HasValue?x.baslangicTarihi.Value.Date:DBNull.Value);c.Parameters.AddWithValue("@Hedef",x.hedefTarihi.HasValue?x.hedefTarihi.Value.Date:DBNull.Value);c.Parameters.AddWithValue("@Sorumlu",DbNull(x.veriSorumlusu));if(await c.ExecuteNonQueryAsync()!=1)throw new InvalidOperationException("Başvuru bulunamadı.");}
        public async Task<bool> BasvuruIzlemeGostergesiSilAsync(int basvuruId,int id){await using SqlCommand c=KomutOlustur("DELETE dbo.BasvuruIzlemeGostergesi WHERE Id=@Id AND BasvuruId=@BasvuruId;");c.Parameters.AddWithValue("@Id",id);c.Parameters.AddWithValue("@BasvuruId",basvuruId);return await c.ExecuteNonQueryAsync()==1;}
        private static void SurecParametreleri(SqlCommand c,BasvuruUrunSurec s){c.Parameters.AddWithValue("@BasvuruId",s.basvuruId);c.Parameters.AddWithValue("@UrunId",s.urunId);c.Parameters.AddWithValue("@SiraNo",s.siraNo);c.Parameters.AddWithValue("@SurecAdi",s.surecAdi);}
        public async Task<bool> BasvuruUrunSureciSilAsync(int basvuruId,int id){await using SqlCommand c=KomutOlustur("DELETE FROM dbo.BasvuruUrunSurec WHERE Id=@Id AND BasvuruId=@BasvuruId;");c.Parameters.AddWithValue("@Id",id);c.Parameters.AddWithValue("@BasvuruId",basvuruId);return await c.ExecuteNonQueryAsync()>0;}

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
                SELECT Id, BasvuruId, SiraNo, Tckn, Ad, Soyad, Gorev, YetkiKapsami, Aciklama, ImzaYetkiDosyaAdi, ImzaYetkiDosyaId, DosyaAdi, DosyaId
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

        public async Task BasvuruImzaYetkiDosyasiGuncelleAsync(int basvuruId, int kisiId, int dosyaId, string dosyaAdi)
        {
            const string sql = @"UPDATE dbo.BasvuruAdliSicilKisiler SET ImzaYetkiDosyaId=@DosyaId, ImzaYetkiDosyaAdi=@DosyaAdi WHERE BasvuruId=@BasvuruId AND Id=@Id;";
            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@BasvuruId", basvuruId); command.Parameters.AddWithValue("@Id", kisiId); command.Parameters.AddWithValue("@DosyaId", dosyaId); command.Parameters.AddWithValue("@DosyaAdi", dosyaAdi?.Trim() ?? "");
            await command.ExecuteNonQueryAsync();
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
                    (BasvuruId, SiraNo, Tckn, Ad, Soyad, Gorev, YetkiKapsami, Aciklama, ImzaYetkiDosyaAdi, ImzaYetkiDosyaId, DosyaAdi, DosyaId)
                OUTPUT INSERTED.Id
                VALUES
                    (@BasvuruId, @SiraNo, @Tckn, @Ad, @Soyad, @Gorev, @YetkiKapsami, @Aciklama, @ImzaYetkiDosyaAdi, @ImzaYetkiDosyaId, @DosyaAdi, @DosyaId);";

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
                    YetkiKapsami = @YetkiKapsami,
                    Aciklama = @Aciklama,
                    ImzaYetkiDosyaAdi = @ImzaYetkiDosyaAdi,
                    ImzaYetkiDosyaId = @ImzaYetkiDosyaId,
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
            command.Parameters.AddWithValue("@YetkiKapsami", kisi.yetkiKapsami?.Trim() ?? "");
            command.Parameters.AddWithValue("@Aciklama", kisi.aciklama?.Trim() ?? "");
            command.Parameters.AddWithValue("@ImzaYetkiDosyaAdi", kisi.imzaYetkiDosyaAdi?.Trim() ?? "");
            command.Parameters.AddWithValue("@ImzaYetkiDosyaId", DbNull(kisi.imzaYetkiDosyaId));
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
                yetkiKapsami = NullOkuString(reader, 7) ?? "",
                aciklama = NullOkuString(reader, 8) ?? "",
                imzaYetkiDosyaAdi = NullOkuString(reader, 9) ?? "",
                imzaYetkiDosyaId = NullOkuInt(reader, 10),
                dosyaAdi = NullOkuString(reader, 11) ?? "",
                dosyaId = NullOkuInt(reader, 12)
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
                dogumTarihi = reader.IsDBNull(9) ? null : reader.GetDateTime(9),
                cinsiyet = NullOkuString(reader, 10) ?? "",
                sahiplikNiteligi = NullOkuString(reader, 11) ?? "Uygulanamaz",
                nihaiFaydalaniciBilgisi = NullOkuString(reader, 12) ?? "",
                uboKycBelgeAdi = NullOkuString(reader, 13) ?? "",
                uboKycDosyaId = NullOkuInt(reader, 14),
                oncekiYilNetSatis = NullOkuDecimal(reader, 15),
                sonYilNetSatis = NullOkuDecimal(reader, 16),
                oncekiYilAktifToplami = NullOkuDecimal(reader, 17),
                sonYilAktifToplami = NullOkuDecimal(reader, 18),
                iliskiTuru = NullOkuString(reader, 19) ?? "",
                belgeReferansi = NullOkuString(reader, 20) ?? ""
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
                yapiRuhsatiDurumu = reader.IsDBNull(12) ? enumUygulamaAdresiYapiRuhsatiDurumu.Tanimsiz : (enumUygulamaAdresiYapiRuhsatiDurumu)reader.GetInt32(12),
                koordinat = NullOkuString(reader, 13), adaParsel = NullOkuString(reader, 14), segeKademesi = NullOkuString(reader, 15),
                kullanimHakkiBaslangicTarihi = reader.IsDBNull(16) ? null : reader.GetDateTime(16), donemleriKapsiyorMu = reader.IsDBNull(17) ? null : reader.GetBoolean(17), izinTakvimAciklama = NullOkuString(reader, 18),
                adresBelgeDosyaId = NullOkuInt(reader, 19), adresBelgeDosyaAdi = NullOkuString(reader, 20), kullanimHakkiDosyaId = NullOkuInt(reader, 21), kullanimHakkiDosyaAdi = NullOkuString(reader, 22), kanitDosyaId = NullOkuInt(reader, 23), kanitDosyaAdi = NullOkuString(reader, 24)
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



