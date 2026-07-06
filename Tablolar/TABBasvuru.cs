using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using TarimDonusum.Araclar;
using TarimDonusum.Models;

namespace TarimDonusum.Tablolar
{
    public class TABBasvuru : TABTablo
    {
        public TABBasvuru(SqlConnection connection, SqlTransaction? transaction = null)
            : base(connection, transaction)
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
            if (basvuru.Id <= 0)
                return await BasvuruFirmaEkleAsync(basvuru);

            await BasvuruFirmaGuncelleAsync(basvuru);
            return basvuru.Id;
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

            basvuru.Id = OrtakFonksiyonlar.Int32Yap(await command.ExecuteScalarAsync());
            return basvuru.Id;
        }

        private async Task BasvuruFirmaGuncelleAsync(BasvuruFirma basvuru)
        {
            const string sql = @"UPDATE dbo.Basvuru SET FirmaId = @FirmaId, DonemId = @DonemId, IlId = @IlId,
                    BasvuruKonusu = @BasvuruKonusu, BasvuruSahibiTuru = @BasvuruSahibiTuru,
                    SonIkiYildirFaalMi = @SonIkiYildirFaalMi, OzelSektorPayi = @OzelSektorPayi,
                    BagliOrtakIsletmeVarMi = @BagliOrtakIsletmeVarMi, BagliOrtakAciklama = @BagliOrtakAciklama
                WHERE Id = @Id;";

            await using SqlCommand command = KomutOlustur(sql);
            command.Parameters.AddWithValue("@Id", basvuru.Id);
            BasvuruFirmaParametreleriEkle(command, basvuru);

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

        public async Task BasvuruIletisimGuncelleAsync(BasvuruIletisim iletisim)
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
            command.Parameters.AddWithValue("@Id", iletisim.BasvuruId);

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

        private static void BasvuruFirmaParametreleriEkle(SqlCommand command, BasvuruFirma basvuru)
        {
            command.Parameters.AddWithValue("@FirmaId", (object?)basvuru.firma.Id ?? DBNull.Value);
            command.Parameters.AddWithValue("@DonemId", (object?)basvuru.donem.Id ?? DBNull.Value);
            command.Parameters.AddWithValue("@IlId", (object?)basvuru.il.Id ?? DBNull.Value);
            command.Parameters.AddWithValue("@BasvuruKonusu", basvuru.BasvuruKonusu ?? "");
            command.Parameters.AddWithValue("@BasvuruSahibiTuru", basvuru.BasvuruSahibiTuru.HasValue ? (int)basvuru.BasvuruSahibiTuru.Value : DBNull.Value);
            command.Parameters.AddWithValue("@SonIkiYildirFaalMi", basvuru.SonIkiYildirFaalMi ? 1 : 0);
            command.Parameters.AddWithValue("@OzelSektorPayi", (object?)basvuru.OzelSektorPayi ?? DBNull.Value);
            command.Parameters.AddWithValue("@BagliOrtakIsletmeVarMi", basvuru.BagliOrtakIsletmeVarMi ? 1 : 0);
            command.Parameters.AddWithValue("@BagliOrtakAciklama", string.IsNullOrWhiteSpace(basvuru.BagliOrtakAciklama) ? DBNull.Value : basvuru.BagliOrtakAciklama);
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
            basvuru.basvuruFirma.OzelSektorPayi = NullOkuDecimal(reader, kol++);
            basvuru.basvuruFirma.BagliOrtakIsletmeVarMi = BoolYap(NullOkuInt(reader, kol++));
            basvuru.basvuruFirma.BagliOrtakAciklama = NullOkuString(reader, kol++);
            basvuru.FirmaId = reader.GetInt32(kol++);
            basvuru.DonemId = reader.GetInt32(kol++);
            basvuru.IlId = reader.GetInt32(kol++);
            basvuru.basvuruFirma.BasvuruKonusu = reader.GetString(kol++);
            basvuru.basvuruFirma.BasvuruSahibiTuru = (enumBasvuruSahibiTuru)NullDuzeltInt(reader, kol++);
            basvuru.basvuruFirma.SonIkiYildirFaalMi = BoolYap(NullOkuInt(reader, kol++));
            basvuru.yatirim.yatirimAdi = NullOkuString(reader, kol++);
            basvuru.yatirim.yatirimTuru = (enumYatirimTuru)NullDuzeltInt(reader, kol++);
            basvuru.ToplamYatirimTutari = NullOkuDecimal(reader, kol++);
            basvuru.UygunHarcamaTutari = NullOkuDecimal(reader, kol++);
            basvuru.TalepEdilenDestekTutari = NullOkuDecimal(reader, kol++);
            basvuru.BasvuruSahibiKatkisi = NullOkuDecimal(reader, kol++);
            basvuru.DestekOrani = NullOkuDecimal(reader, kol++);
            basvuru.YatiriminAmaci = NullOkuString(reader, kol++);

            basvuru.irtibat.BasvuruId = basvuru.Id;
            basvuru.irtibat.kisi = NullOkuString(reader, kol++);
            basvuru.irtibat.unvan = NullOkuString(reader, kol++);
            basvuru.irtibat.telefon = NullOkuString(reader, kol++);
            basvuru.irtibat.ePosta = NullOkuString(reader, kol++);
            basvuru.irtibat.adres = NullOkuString(reader, kol++);
            basvuru.irtibat.yetkiliKisiler = NullOkuString(reader, kol++);

            basvuru.donem.Yil = NullDuzeltInt(reader, kol++);
            basvuru.donem.Ad = reader.GetString(kol++);
            basvuru.donem.BasvuruyaAcikMi = BoolYap(NullOkuInt(reader, kol++));
            basvuru.donem.BasvuruBaslangicTarihi = reader.GetDateTime(kol++);
            basvuru.donem.BasvuruBitisTarihi = reader.GetDateTime(kol++);
            basvuru.donem.OnBasvuruBitisTarihi = reader.GetDateTime(kol++);
            basvuru.donem.MinimumYatirimTutari = NullOkuDecimal(reader, kol++);
            basvuru.donem.MaksimumYatirimTutari = NullOkuDecimal(reader, kol++);
            basvuru.donem.MaksimumDestekTutari = NullOkuDecimal(reader, kol++);
            basvuru.donem.DestekOrani = NullOkuDecimal(reader, kol++);
            basvuru.donem.Aciklama = reader.GetString(kol++);
            basvuru.Il.Kod = NullDuzeltInt(reader, kol++);
            basvuru.Il.Ad = reader.GetString(kol++);
            basvuru.Il.Aktif = BoolYap(NullOkuInt(reader, kol++));

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
                adresler.Add(UygulamaAdresiOku(reader));
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
            command.Parameters.AddWithValue("@YatirimYeriStatusu", adres.yatirimYeriStatusu.HasValue ? (int)adres.yatirimYeriStatusu.Value : (object)DBNull.Value);
            command.Parameters.AddWithValue("@KiraVeyaTahsisSuresi", adres.kiraVeyaTahsisSuresi.HasValue ? adres.kiraVeyaTahsisSuresi.Value : (object)DBNull.Value);
            command.Parameters.AddWithValue("@KiraTahsisBitisTarihi", adres.kiraTahsisBitisTarihi.HasValue ? adres.kiraTahsisBitisTarihi.Value.Date : (object)DBNull.Value);
            command.Parameters.AddWithValue("@YapiRuhsatiDurumu", adres.yapiRuhsatiDurumu.HasValue ? (int)adres.yapiRuhsatiDurumu.Value : (object)DBNull.Value);
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
                adresler.Add(UygulamaAdresiOku(reader));
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

        private static BasvuruUygulamaAdresi UygulamaAdresiOku(SqlDataReader reader)
        {
            return new BasvuruUygulamaAdresi
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
                yatirimYeriStatusu = reader.IsDBNull(9) ? null : (enumUygulamaAdresiYatirimYeriStatusu)reader.GetInt32(9),
                kiraVeyaTahsisSuresi = reader.IsDBNull(10) ? null : reader.GetInt32(10),
                kiraTahsisBitisTarihi = reader.IsDBNull(11) ? null : reader.GetDateTime(11),
                yapiRuhsatiDurumu = reader.IsDBNull(12) ? null : (enumUygulamaAdresiYapiRuhsatiDurumu)reader.GetInt32(12)
            };
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



