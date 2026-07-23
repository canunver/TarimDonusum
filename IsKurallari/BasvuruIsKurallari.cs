using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Localization;
using System.Text.Json;
using TarimDonusum.Models;
using TarimDonusum.Tablolar;

namespace TarimDonusum.IsKurallari
{
    public class BasvuruIsKurallari
    {
        private const string BasvuruZorunluBelgelerFormAd = "ZorunluBelgeler";
        private const string BasvuruZorunluBelgeFormAd = "Basvuru_ZorunluBelge";
        private const string BasvuruBagliBelgeFormAd = "Basvuru_BagliBelge";
        private const string BasvuruAdliSicilFormAd = "Basvuru_AdliSicil";
        private const string BasvuruOrtakUboKycFormAd = "Basvuru_OrtakUboKyc";
        private const string BasvuruOrtakUboKycFormAdPrefix = "OBOKYC_";
        private static readonly IReadOnlyDictionary<int, string> ZorunluBelgeTurleri = new Dictionary<int, string>
        {
            [1] = "Basvuru.Documents.Required.1",
            [2] = "Basvuru.Documents.Required.2",
            [3] = "Basvuru.Documents.Required.3",
            [4] = "Basvuru.Documents.Required.4",
            [5] = "Basvuru.Documents.Required.5",
            [6] = "Basvuru.Documents.Required.6",
            [7] = "Basvuru.Documents.Required.7"
        };
        private static readonly IReadOnlyDictionary<int, string> BagliOrtakDosyaTurleri = new Dictionary<int, string>
        {
            [1] = "Basvuru.Documents.Required.1",
            [2] = "Basvuru.Documents.Required.2",
            [3] = "Basvuru.Documents.Required.3",
            [4] = "Basvuru.Documents.Required.4",
            [5] = "Basvuru.Documents.Required.5",
            [6] = "Basvuru.Documents.Required.6",
            [7] = "Basvuru.Documents.Required.7"
        };

        private readonly string _connectionString;
        private readonly ILogger<BasvuruIsKurallari> _logger;
        private readonly IStringLocalizer<SharedResource> _localizer;
        private readonly DosyaYonetimIsKurallari _dosyaYonetimIsKurallari;

        public BasvuruIsKurallari(IConfiguration configuration, ILogger<BasvuruIsKurallari> logger, IStringLocalizer<SharedResource> localizer, DosyaYonetimIsKurallari dosyaYonetimIsKurallari)
        {
            _logger = logger;
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
            _localizer = localizer;
            _dosyaYonetimIsKurallari = dosyaYonetimIsKurallari;
        }

        public async Task<Sonuc<List<Basvuru>>> KullaniciBasvurulariniListeleAsync(Kullanici kullanici)
        {
            Sonuc<List<Basvuru>> sonuc = new Sonuc<List<Basvuru>>();

            try
            {
                await using SqlConnection connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                TABBasvuru tabBasvuru = new TABBasvuru(connection, _localizer);
                sonuc = BasvuruKullanicisiMi(kullanici)
                    ? await tabBasvuru.KullaniciBasvurulariniListeleAsync(kullanici.Id)
                    : await tabBasvuru.TumunuListeleAsync();
            }
            catch (Exception ex)
            {
                BeklenmeyenHata(sonuc, ex, "Başvuru kayıtları listelenemedi. KullaniciId: {KullaniciId}", "Başvuru kayıtları listelenemedi.", kullanici.Id);
            }

            return sonuc;
        }

        public async Task<Sonuc<Kullanici>> KullaniciOkuAsync(int kullaniciId)
        {
            Sonuc<Kullanici> sonuc = new Sonuc<Kullanici>();

            try
            {
                if (kullaniciId <= 0)
                {
                    HataEkle(sonuc, "Business.Session.Expired");
                    return sonuc;
                }

                await using SqlConnection connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                TABKullanici tabKullanici = new TABKullanici(connection);
                Kullanici? kullanici = await tabKullanici.OkuAsync(kullaniciId);
                if (kullanici == null || !kullanici.Aktif)
                {
                    HataEkle(sonuc, "Business.User.NotFoundOrPassive");
                    return sonuc;
                }

                TABKullaniciYetki tabKullaniciYetki = new TABKullaniciYetki(connection, _localizer);
                kullanici.Yetkiler = await tabKullaniciYetki.KullaniciYetkileriniListeleAsync(kullanici.Id);
                sonuc.nesne = kullanici;
            }
            catch (Exception ex)
            {
                BeklenmeyenHata(sonuc, ex, "Kullanıcı okunamadı. KullaniciId: {KullaniciId}", "Kullanıcı bilgisi okunamadı.", kullaniciId);
            }

            return sonuc;
        }

        public async Task<Sonuc<List<Basvuru>>> TumunuListeleAsync()
        {
            Sonuc<List<Basvuru>> sonuc = new Sonuc<List<Basvuru>>();

            try
            {
                await using SqlConnection connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                TABBasvuru tabBasvuru = new TABBasvuru(connection, _localizer);
                sonuc = await tabBasvuru.TumunuListeleAsync();
            }
            catch (Exception ex)
            {
                BeklenmeyenHata(sonuc, ex, "Tüm başvurular listelenemedi.", "Başvuru kayıtları listelenemedi.");
            }

            return sonuc;
        }

        public async Task<Sonuc<List<Donem>>> DonemleriListeleAsync()
        {
            Sonuc<List<Donem>> sonuc = new Sonuc<List<Donem>>();

            try
            {
                await using SqlConnection connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                TABDonem tabDonem = new TABDonem(connection);
                sonuc.nesne = await tabDonem.ListeleAsync();
            }
            catch (Exception ex)
            {
                BeklenmeyenHata(sonuc, ex, "Başvuru dönemleri listelenemedi.", "Başvuru dönemleri listelenemedi.");
            }

            return sonuc;
        }

        public async Task<Sonuc<List<Il>>> IlleriListeleAsync()
        {
            Sonuc<List<Il>> sonuc = new Sonuc<List<Il>>();

            try
            {
                await using SqlConnection connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                TABIl tabIl = new TABIl(connection, _localizer);
                sonuc.nesne = await tabIl.ListeleAsync();
            }
            catch (Exception ex)
            {
                BeklenmeyenHata(sonuc, ex, "İl listesi okunamadı.", "İl listesi okunamadı.");
            }

            return sonuc;
        }

        public async Task<Sonuc<List<Ilce>>> IlceleriListeleAsync(int? ilId)
        {
            Sonuc<List<Ilce>> sonuc = new Sonuc<List<Ilce>>();

            try
            {
                await using SqlConnection connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                TABIlce tabIlce = new TABIlce(connection);
                sonuc.nesne = await tabIlce.ListeleAsync(ilId);
            }
            catch (Exception ex)
            {
                BeklenmeyenHata(sonuc, ex, "İlçe listesi okunamadı. IlId: {IlId}", "İlçe listesi okunamadı.", ilId.GetValueOrDefault());
            }

            return sonuc;
        }


        public async Task<Sonuc<List<DegerZinciri>>> DegerZincirleriListeleAsync(Kullanici? kullanici, int ilId, int basvuruId)
        {
            Sonuc<List<DegerZinciri>> sonuc = new Sonuc<List<DegerZinciri>>();

            try
            {
                await using SqlConnection connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                TABBasvuru tabBasvuru = new TABBasvuru(connection, _localizer);
                int seciliZincirId = await tabBasvuru.DegerZinciriBul(basvuruId);

                TABDegerZinciri tabDegerZinciri = new TABDegerZinciri(connection, _localizer);
                sonuc = await tabDegerZinciri.ListeleAsync(true, ilId, seciliZincirId);
            }
            catch (Exception ex)
            {
                BeklenmeyenHata(sonuc, ex, "Değer zincirleri listelenemedi. IlId: {IlId}", "Değer zincirleri listelenemedi.", ilId);
            }

            return sonuc;
        }

        public async Task<Sonuc<List<DegerZinciriAsama>>> DegerZinciriAsamalariListeleAsync(Kullanici? kullanici, int degerZinciriId, int basvuruId)
        {
            Sonuc<List<DegerZinciriAsama>> sonuc = new Sonuc<List<DegerZinciriAsama>>();
            //sonuc.nesne.Add(new DegerZinciriAsama() { id = 33, ad = "eee", aciklama = "zzzz1" });
            //sonuc.nesne.Add(new DegerZinciriAsama() { id = 34, ad = "fff", aciklama = "zzzz2", secili = true });
            //sonuc.nesne.Add(new DegerZinciriAsama() { id = 35, ad = "gggg", aciklama = "zzzz3" });

            //return sonuc;

            try
            {
                if (degerZinciriId <= 0)
                {
                    return sonuc;
                }

                await using SqlConnection connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                TABDegerZinciri tabDegerZinciri = new TABDegerZinciri(connection, _localizer);
                sonuc = await tabDegerZinciri.AsamalariOku(degerZinciriId, basvuruId);
            }
            catch (Exception ex)
            {
                BeklenmeyenHata(sonuc, ex, "Değer zinciri aşamaları listelenemedi. DegerZinciriId: {DegerZinciriId}", "Değer zinciri aşamaları listelenemedi.", degerZinciriId);
            }

            return sonuc;
        }

        public async Task<Sonuc<Basvuru>> OkuAsync(int basvuruId, Kullanici? kullanici = null)
        {
            Sonuc<Basvuru> sonuc = new Sonuc<Basvuru>();

            try
            {
                await using SqlConnection connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                TABBasvuru tabBasvuru = new TABBasvuru(connection, _localizer);
                Basvuru? basvuru = await tabBasvuru.OkuAsync(basvuruId);

                if (basvuru == null)
                {
                    HataEkle(sonuc, "Business.Application.NotFound");
                    return sonuc;
                }

                if (kullanici != null &&
                    BasvuruKullanicisiMi(kullanici) &&
                    basvuru.basvuruFirma.firmaId > 0)
                {
                    TABFirmaKullanici tabFirmaKullanici = new TABFirmaKullanici(connection, _localizer);
                    if (!await tabFirmaKullanici.IliskiVarMiAsync(basvuru.basvuruFirma.firmaId, kullanici.Id))
                    {
                        HataEkle(sonuc, "Business.Application.ViewUnauthorized");
                        return sonuc;
                    }
                }

                await BasvuruDosyaListeleriniYukleAsync(basvuru);
                sonuc.nesne = basvuru;
            }
            catch (Exception ex)
            {
                BeklenmeyenHata(sonuc, ex, "Başvuru okunamadı. BasvuruId: {BasvuruId}", "Başvuru kaydı okunamadı.", basvuruId);
            }

            return sonuc;
        }

        public async Task<Sonuc<Firma>> FirmaVergiNoIleOkuAsync(Kullanici? kullanici, int firmaId, string vergiKimlikNo)
        {
            Sonuc<Firma> sonuc = new Sonuc<Firma>();
            vergiKimlikNo = vergiKimlikNo?.Trim() ?? "";

            if (kullanici == null)
            {
                HataEkle(sonuc, "Business.User.InfoMissing");
                return sonuc;
            }

            if (string.IsNullOrWhiteSpace(vergiKimlikNo) && firmaId <= 0)
            {
                HataEkle(sonuc, "Business.Query.InfoRequired");
            }
            if (!sonuc.basarili)
                return sonuc;

            try
            {
                await using SqlConnection connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                TABFirma tabFirma = new TABFirma(connection, _localizer);
                Firma? firma = await tabFirma.VergiKimlikNoIleOkuAsync(firmaId, vergiKimlikNo);
                if (firma == null || firma.id <= 0)
                {
                    HataEkle(sonuc, "Business.Company.NotFound");
                    return sonuc;
                }
                if (true) //başvuru kullanıcısı ise
                {
                    TABFirmaKullanici tabFirmaKullanici = new TABFirmaKullanici(connection, _localizer);
                    if (!await tabFirmaKullanici.IliskiVarMiAsync(firma.id, kullanici.Id))
                    {
                        HataEkle(sonuc, "Business.Company.UserNotRelated");
                        return sonuc;
                    }
                }
                sonuc.nesne = firma;
            }
            catch (Exception ex)
            {
                BeklenmeyenHata(sonuc, ex, "Firma sorgulanamadı. KullaniciId: {KullaniciId}, VergiKimlikNo: {VergiKimlikNo}", "Firma sorgulanamadı.", kullanici.Id, vergiKimlikNo);
            }

            return sonuc;
        }

        public async Task<Sonuc<int>> FirmaEkleGuncelleAsync(Firma firma, Kullanici? kullanici)
        {
            Sonuc<int> sonuc = new Sonuc<int>();
            if (!BasvuruKullanicisiMi(kullanici))
            {
                BasvuruKullanicisiYetkiHatasiEkle(sonuc);
                return sonuc;
            }

            if (kullanici == null)
            {
                HataEkle(sonuc, "Business.User.InfoMissing");
                return sonuc;
            }
            int kullaniciId = kullanici.Id;

            try
            {
                firma.Dogrula(sonuc);

                if (!sonuc.basarili)
                    return sonuc;

                string vergiKimlikNo = firma.vergiKimlikNo!;

                await using SqlConnection connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                TABFirma tabFirma = new TABFirma(connection);
                Firma? mevcut = await tabFirma.VergiKimlikNoIleOkuAsync(0, vergiKimlikNo);
                if (mevcut != null)
                {
                    TABFirmaKullanici mevcutIliskiTablosu = new TABFirmaKullanici(connection);
                    if (await mevcutIliskiTablosu.IliskiVarMiAsync(mevcut.id, kullaniciId))
                        sonuc.nesne = mevcut.id;
                    else
                        HataEkle(sonuc, "Business.Company.ExistsButUserNotRelated");

                    return sonuc;
                }

                await using SqlTransaction transaction = (SqlTransaction)await connection.BeginTransactionAsync();
                try
                {
                    TABFirma txFirma = new TABFirma(connection, null, transaction);
                    sonuc.nesne = await txFirma.EkleAsync(firma);

                    TABFirmaKullanici tabFirmaKullanici = new TABFirmaKullanici(connection, null, transaction);
                    await tabFirmaKullanici.EkleYoksaAsync(new FirmaKullanici
                    {
                        FirmaId = firma.id,
                        KullaniciId = kullaniciId,
                        Aktif = true,
                        IliskiTarihi = DateTime.Now,
                        IliskiyiKuranKullaniciId = kullaniciId
                    });

                    TABFirmaLog tabFirmaLog = new TABFirmaLog(connection, null, transaction);
                    await tabFirmaLog.EkleAsync(firma, "YeniKayit");

                    await transaction.CommitAsync();
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Firma kaydedilemedi. KullaniciId: {KullaniciId}, VergiKimlikNo: {VergiKimlikNo}", kullaniciId, firma.vergiKimlikNo);
                    HataEkle(sonuc, "Business.Company.SaveFailed");
                }
            }
            catch (Exception ex)
            {
                BeklenmeyenHata(sonuc, ex, "Firma kaydetme işlemi tamamlanamadı. KullaniciId: {KullaniciId}", "Firma kaydedilemedi.", kullaniciId);
            }

            return sonuc;
        }

        //public async Task<Sonuc<Firma>> FirmaGuncelleAsync(Firma firma, Kullanici? kullanici)
        //{
        //    Sonuc<Firma> sonuc = new Sonuc<Firma>();
        //    if (kullanici == null)
        //    {
        //        sonuc.HataEkle("Kullanıcı bilgisi gelmedi!.");
        //        return sonuc;
        //    }
        //    int kullaniciId = kullanici.Id;
        //    try
        //    {
        //        //FirmaNormalizeEt(firma);

        //        if (firma.id <= 0)
        //            sonuc.HataEkle("Firma seçilmelidir.");

        //        if (string.IsNullOrWhiteSpace(firma.vergiKimlikNo))
        //            sonuc.HataEkle("Vergi kimlik no girilmelidir.");

        //        if (string.IsNullOrWhiteSpace(firma.ticaretUnvani))
        //            sonuc.HataEkle("Firma adı girilmelidir.");

        //        if (!sonuc.basarili)
        //            return sonuc;

        //        await using SqlConnection connection = new SqlConnection(_connectionString);
        //        await connection.OpenAsync();

        //        TABFirma tabFirma = new TABFirma(connection);
        //        Firma? mevcut = await tabFirma.OkuAsync(firma.id);
        //        if (mevcut == null)
        //        {
        //            sonuc.HataEkle("Firma bulunamadı.");
        //            return sonuc;
        //        }

        //        TABFirmaKullanici tabFirmaKullanici = new TABFirmaKullanici(connection);
        //        if (!await tabFirmaKullanici.IliskiVarMiAsync(firma.id, kullaniciId))
        //        {
        //            sonuc.HataEkle("Bu firma kullanıcı ile ilişkili değil.");
        //            return sonuc;
        //        }

        //        Firma? ayniVergiNo = await tabFirma.VergiKimlikNoIleOkuAsync(0, firma.vergiKimlikNo);
        //        if (ayniVergiNo != null && ayniVergiNo.id != firma.id)
        //        {
        //            sonuc.HataEkle("Bu vergi kimlik no başka bir firma kaydında kullanılıyor.");
        //            return sonuc;
        //        }

        //        await using SqlTransaction transaction = (SqlTransaction)await connection.BeginTransactionAsync();
        //        try
        //        {
        //            TABFirma txFirma = new TABFirma(connection, null, transaction);
        //            await txFirma.GuncelleAsync(firma);

        //            TABFirmaLog tabFirmaLog = new TABFirmaLog(connection, null, transaction);
        //            await tabFirmaLog.EkleAsync(firma, "Update");

        //            await transaction.CommitAsync();
        //            sonuc.nesne = firma;
        //        }
        //        catch (Exception ex)
        //        {
        //            await transaction.RollbackAsync();
        //            _logger.LogError(ex, "Firma güncellenemedi. FirmaId: {FirmaId}, KullaniciId: {KullaniciId}", firma.id, kullaniciId);
        //            sonuc.HataEkle("Firma güncellenemedi.");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        BeklenmeyenHata(sonuc, ex, "Firma güncelleme işlemi tamamlanamadı. FirmaId: {FirmaId}, KullaniciId: {KullaniciId}", "Firma güncellenemedi.", firma.id, kullaniciId);
        //    }

        //    return sonuc;
        //}

        public async Task<Sonuc<int>> KaydetFirmaBasvuru(BasvuruFirma firmaBasvuru, Kullanici kullanici)
        {
            Sonuc<int> sonuc = new Sonuc<int>();
            try
            {
                if (!BasvuruKullanicisiMi(kullanici))
                {
                    BasvuruKullanicisiYetkiHatasiEkle(sonuc);
                    return sonuc;
                }

                firmaBasvuru.Dogrula(sonuc);

                if (!sonuc.basarili)
                    return sonuc;

                await using SqlConnection connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                Basvuru? mevcut = null;
                if (firmaBasvuru.id > 0)
                {
                    mevcut = await BasvuruOnBasvuruYetkiKontrolAsync(connection, firmaBasvuru.id, kullanici, sonuc);
                    if (!sonuc.basarili || mevcut == null)
                        return sonuc;
                }

                await using SqlTransaction transaction = (SqlTransaction)await connection.BeginTransactionAsync();

                try
                {
                    TABBasvuru tabBasvuru = new TABBasvuru(connection, _localizer, transaction);
                    sonuc.nesne = await tabBasvuru.BasvuruFirmaKaydetAsync(firmaBasvuru);

                    TABBasvuruLog tabBasvuruLog = new TABBasvuruLog(connection, _localizer, transaction);
                    await tabBasvuruLog.EkleAsync(sonuc.nesne, kullanici, "KaydetFirmaBasvuru", firmaBasvuru);

                    await transaction.CommitAsync();
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Başvuru firma bilgisi kaydedilemedi. BasvuruId: {BasvuruId}", firmaBasvuru.id);
                    throw;
                }
            }
            catch (Exception ex)
            {
                BeklenmeyenHata(sonuc, ex, "Firma basvurusu  kaydedilemedi. BasvuruId: {BasvuruId}, KullaniciId: {KullaniciId}", "Başvuru kaydedilemedi.", firmaBasvuru.id, kullanici.Id);
            }

            return sonuc;
        }

        public async Task<Sonuc<int>> KaydetBasvuruSahibiAsync(Basvuru basvuru, Kullanici kullanici)
        {
            Sonuc<int> sonuc = new Sonuc<int>();
            try
            {
                if (basvuru == null)
                {
                    HataEkle(sonuc, "Business.Application.ApplicantInfoRequired");
                    return sonuc;
                }

                basvuru.basvuruFirma ??= new BasvuruFirma();
                basvuru.irtibat ??= new BasvuruIrtibat();

                basvuru.basvuruFirma.Dogrula(sonuc);
                basvuru.irtibat.Dogrula(sonuc, basvuru.Id > 0);

                if (!sonuc.basarili)
                    return sonuc;

                await using SqlConnection connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                Basvuru? mevcut = null;
                if (basvuru.Id > 0)
                {
                    mevcut = await BasvuruOnBasvuruYetkiKontrolAsync(connection, basvuru.Id, kullanici, sonuc);
                    if (!sonuc.basarili || mevcut == null)
                        return sonuc;
                }

                await using SqlTransaction transaction = (SqlTransaction)await connection.BeginTransactionAsync();

                try
                {
                    TABBasvuru tabBasvuru = new TABBasvuru(connection, _localizer, transaction);
                    sonuc.nesne = await tabBasvuru.BasvuruFirmaKaydetAsync(basvuru.basvuruFirma);

                    basvuru.Id = sonuc.nesne;
                    basvuru.irtibat.basvuruId = sonuc.nesne;
                    await tabBasvuru.BasvuruIletisimGuncelleAsync(basvuru.irtibat);

                    TABBasvuruLog tabBasvuruLog = new TABBasvuruLog(connection, _localizer, transaction);
                    await tabBasvuruLog.EkleAsync(sonuc.nesne, kullanici, "KaydetBasvuruSahibiAsync", basvuru);

                    await transaction.CommitAsync();
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Başvuru sahibi bilgisi kaydedilemedi. BasvuruId: {BasvuruId}", basvuru.Id);
                    throw;
                }
            }
            catch (Exception ex)
            {
                BeklenmeyenHata(sonuc, ex, "Başvuru sahibi kaydedilemedi. BasvuruId: {BasvuruId}, KullaniciId: {KullaniciId}", "Başvuru kaydedilemedi.", basvuru.Id, kullanici.Id);
            }

            return sonuc;
        }

        public async Task<Sonuc<int>> KaydetIrtibatAsync(BasvuruIrtibat irtibat, Kullanici kullanici)
        {
            Sonuc<int> sonuc = new Sonuc<int>();
            if (kullanici == null)
            {
                HataEkle(sonuc, "Business.User.InfoMissing");
                return sonuc;
            }
            try
            {
                irtibat.Dogrula(sonuc);

                if (!sonuc.basarili)
                    return sonuc;

                await using SqlConnection connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                Basvuru? mevcut = null;
                if (irtibat.basvuruId > 0)
                {
                    mevcut = await BasvuruOnBasvuruYetkiKontrolAsync(connection, irtibat.basvuruId, kullanici, sonuc);
                    if (!sonuc.basarili || mevcut == null)
                        return sonuc;
                }

                await using SqlTransaction transaction = (SqlTransaction)await connection.BeginTransactionAsync();

                try
                {
                    TABBasvuru tabBasvuru = new TABBasvuru(connection, null, transaction);
                    await tabBasvuru.BasvuruIletisimGuncelleAsync(irtibat);

                    TABBasvuruLog tabBasvuruLog = new TABBasvuruLog(connection, null, transaction);
                    await tabBasvuruLog.EkleAsync(irtibat.basvuruId, kullanici, "KaydetIrtibatAsync", irtibat);

                    await transaction.CommitAsync();
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                BeklenmeyenHata(sonuc, ex, "Firma basvurusu  kaydedilemedi. BasvuruId: {BasvuruId}, KullaniciId: {KullaniciId}", "Başvuru kaydedilemedi.", irtibat.basvuruId, kullanici.Id);
            }
            return sonuc;
        }

        public async Task<Sonuc<int>> KaydetYatirimBilgisiAsync(BasvuruYatirim yatirim, Kullanici? kullanici)
        {
            Sonuc<int> sonuc = new Sonuc<int>();
            if (kullanici == null)
            {
                HataEkle(sonuc, "Business.User.InfoMissing");
                return sonuc;
            }
            try
            {
                yatirim.Dogrula(sonuc);

                if (!sonuc.basarili)
                    return sonuc;

                await using SqlConnection connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                Basvuru? mevcut = null;
                if (yatirim.basvuruId > 0)
                {
                    mevcut = await BasvuruOnBasvuruYetkiKontrolAsync(connection, yatirim.basvuruId, kullanici, sonuc);
                    if (!sonuc.basarili || mevcut == null)
                        return sonuc;
                }

                await using SqlTransaction transaction = (SqlTransaction)await connection.BeginTransactionAsync();
                try
                {
                    TABBasvuru tabBasvuru = new TABBasvuru(connection, null, transaction);
                    int eklenenKayit = await tabBasvuru.YatirimBilgisiGuncelleAsync(yatirim);
                    await tabBasvuru.YatirimDetaylariKaydetAsync(yatirim);

                    TABBasvuruLog tabBasvuruLog = new TABBasvuruLog(connection, null, transaction);
                    await tabBasvuruLog.EkleAsync(yatirim.basvuruId, kullanici, "KaydetYatirimBilgisiAsync", yatirim);
                    await transaction.CommitAsync();
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                BeklenmeyenHata(sonuc, ex, "Firma basvurusu  kaydedilemedi. BasvuruId: {BasvuruId}, KullaniciId: {KullaniciId}", "Başvuru kaydedilemedi.", yatirim.basvuruId, kullanici.Id);
            }
            return sonuc;
        }

        public async Task<Sonuc<int>> KaydetYatirimBilgileriAsync(BasvuruYatirim yatirim, Kullanici? kullanici)
        {
            Sonuc<int> sonuc = new Sonuc<int>();
            if (kullanici == null)
            {
                HataEkle(sonuc, "Business.User.InfoMissing");
                return sonuc;
            }
            try
            {
                yatirim.YatirimBilgileriDogrula(sonuc);

                if (!sonuc.basarili)
                    return sonuc;

                await using SqlConnection connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                Basvuru? mevcut = await BasvuruOnBasvuruYetkiKontrolAsync(connection, yatirim.basvuruId, kullanici, sonuc);
                if (!sonuc.basarili || mevcut == null)
                    return sonuc;

                await using SqlTransaction transaction = (SqlTransaction)await connection.BeginTransactionAsync();
                try
                {
                    TABBasvuru tabBasvuru = new TABBasvuru(connection, null, transaction);
                    await tabBasvuru.YatirimBilgileriKaydetAsync(yatirim);

                    TABBasvuruLog tabBasvuruLog = new TABBasvuruLog(connection, null, transaction);
                    await tabBasvuruLog.EkleAsync(yatirim.basvuruId, kullanici, "KaydetYatirimBilgileriAsync", yatirim);
                    await transaction.CommitAsync();
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                BeklenmeyenHata(sonuc, ex, "Yatırım bilgileri kaydedilemedi. BasvuruId: {BasvuruId}, KullaniciId: {KullaniciId}", "Başvuru kaydedilemedi.", yatirim.basvuruId, kullanici.Id);
            }
            return sonuc;
        }

        public async Task<Sonuc<int>> KaydetDegerZinciriAsync(BasvuruYatirim yatirim, Kullanici? kullanici)
        {
            Sonuc<int> sonuc = new Sonuc<int>();
            if (kullanici == null)
            {
                HataEkle(sonuc, "Business.User.InfoMissing");
                return sonuc;
            }
            try
            {
                yatirim.DegerZinciriDogrula(sonuc);

                if (!sonuc.basarili)
                    return sonuc;

                await using SqlConnection connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                Basvuru? mevcut = await BasvuruOnBasvuruYetkiKontrolAsync(connection, yatirim.basvuruId, kullanici, sonuc);
                if (!sonuc.basarili || mevcut == null)
                    return sonuc;

                await using SqlTransaction transaction = (SqlTransaction)await connection.BeginTransactionAsync();
                try
                {
                    TABBasvuru tabBasvuru = new TABBasvuru(connection, null, transaction);
                    await tabBasvuru.DegerZinciriKaydetAsync(yatirim);

                    TABBasvuruLog tabBasvuruLog = new TABBasvuruLog(connection, null, transaction);
                    await tabBasvuruLog.EkleAsync(yatirim.basvuruId, kullanici, "KaydetDegerZinciriAsync", yatirim);
                    await transaction.CommitAsync();
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                BeklenmeyenHata(sonuc, ex, "Değer zinciri kaydedilemedi. BasvuruId: {BasvuruId}, KullaniciId: {KullaniciId}", "Başvuru kaydedilemedi.", yatirim.basvuruId, kullanici.Id);
            }
            return sonuc;
        }

        public async Task<Sonuc<int>> KaydetFinansAsync(BasvuruFinans finans, Kullanici kullanici)
        {
            Sonuc<int> sonuc = new Sonuc<int>();
            if (kullanici == null)
            {
                HataEkle(sonuc, "Business.User.InfoMissing");
                return sonuc;
            }
            try
            {
                finans.Dogrula(sonuc);

                if (!sonuc.basarili)
                    return sonuc;

                await using SqlConnection connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                Basvuru? mevcut = null;
                if (finans.basvuruId > 0)
                {
                    mevcut = await BasvuruOnBasvuruYetkiKontrolAsync(connection, finans.basvuruId, kullanici, sonuc);
                    if (!sonuc.basarili || mevcut == null)
                        return sonuc;
                }

                await using SqlTransaction transaction = (SqlTransaction)await connection.BeginTransactionAsync();

                try
                {
                    TABBasvuru tabBasvuru = new TABBasvuru(connection, null, transaction);
                    await tabBasvuru.BasvuruFinansGuncelleAsync(finans);

                    TABBasvuruLog tabBasvuruLog = new TABBasvuruLog(connection, null, transaction);
                    await tabBasvuruLog.EkleAsync(finans.basvuruId, kullanici, "KaydetFinansAsync", finans);

                    await transaction.CommitAsync();
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                BeklenmeyenHata(sonuc, ex, "Firma basvurusu  kaydedilemedi. BasvuruId: {BasvuruId}, KullaniciId: {KullaniciId}", "Başvuru kaydedilemedi.", finans.basvuruId, kullanici.Id);
            }
            return sonuc;
        }

        public async Task<Sonuc<int>> KaydetUygunHarcamaAsync(BasvuruUygunHarcama uygunHarcama, Kullanici kullanici)
        {
            Sonuc<int> sonuc = new Sonuc<int>();
            if (kullanici == null)
            {
                HataEkle(sonuc, "Business.User.InfoMissing");
                return sonuc;
            }
            try
            {
                uygunHarcama.Dogrula(sonuc);

                if (!sonuc.basarili)
                    return sonuc;

                await using SqlConnection connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                Basvuru? mevcut = await BasvuruOnBasvuruYetkiKontrolAsync(connection, uygunHarcama.basvuruId, kullanici, sonuc);
                if (!sonuc.basarili || mevcut == null)
                    return sonuc;

                if (mevcut.yatirim.harcamaTurleri.Contains((int)enumHarcamaTuru.MakineEkipman)
                    && !PikkDoluSatirVarMi(uygunHarcama.pikkListesiJson, "equipmentRows"))
                    HataEkle(sonuc, "Business.Pikk.EquipmentRowRequired");

                if (mevcut.yatirim.harcamaTurleri.Contains((int)enumHarcamaTuru.YapimIsleri)
                    && !PikkDoluSatirVarMi(uygunHarcama.pikkListesiJson, "constructionRows"))
                    HataEkle(sonuc, "Business.Pikk.ConstructionRowRequired");

                if (!sonuc.basarili)
                    return sonuc;

                await using SqlTransaction transaction = (SqlTransaction)await connection.BeginTransactionAsync();

                try
                {
                    TABBasvuru tabBasvuru = new TABBasvuru(connection, null, transaction);
                    await tabBasvuru.UygunHarcamaKaydetAsync(uygunHarcama);

                    TABBasvuruLog tabBasvuruLog = new TABBasvuruLog(connection, null, transaction);
                    await tabBasvuruLog.EkleAsync(uygunHarcama.basvuruId, kullanici, "KaydetUygunHarcamaAsync", uygunHarcama);

                    await transaction.CommitAsync();
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                BeklenmeyenHata(sonuc, ex, "Uygun harcama kaydedilemedi. BasvuruId: {BasvuruId}, KullaniciId: {KullaniciId}", "Başvuru kaydedilemedi.", uygunHarcama.basvuruId, kullanici.Id);
            }
            return sonuc;
        }

        private static bool PikkDoluSatirVarMi(string? pikkListesiJson, string listeAdi)
        {
            if (string.IsNullOrWhiteSpace(pikkListesiJson))
                return false;

            try
            {
                using JsonDocument document = JsonDocument.Parse(pikkListesiJson);
                if (!document.RootElement.TryGetProperty(listeAdi, out JsonElement rows) || rows.ValueKind != JsonValueKind.Array)
                    return false;

                foreach (JsonElement row in rows.EnumerateArray())
                {
                    string name = row.TryGetProperty("name", out JsonElement nameElement) ? nameElement.GetString() ?? "" : "";
                    string purpose = row.TryGetProperty("purpose", out JsonElement purposeElement) ? purposeElement.GetString() ?? "" : "";
                    if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(purpose))
                        return true;
                }
            }
            catch (JsonException)
            {
                return false;
            }

            return false;
        }

        public async Task<Sonuc<int>> KaydetYatirimOzetiAsync(BasvuruYatirimOzeti yatirimOzeti, Kullanici kullanici)
        {
            Sonuc<int> sonuc = new Sonuc<int>();
            if (kullanici == null)
            {
                HataEkle(sonuc, "Business.User.InfoMissing");
                return sonuc;
            }
            try
            {
                yatirimOzeti.Dogrula(sonuc);

                if (!sonuc.basarili)
                    return sonuc;

                await using SqlConnection connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                Basvuru? mevcut = await BasvuruOnBasvuruYetkiKontrolAsync(connection, yatirimOzeti.basvuruId, kullanici, sonuc);
                if (!sonuc.basarili || mevcut == null)
                    return sonuc;

                await using SqlTransaction transaction = (SqlTransaction)await connection.BeginTransactionAsync();

                try
                {
                    TABBasvuru tabBasvuru = new TABBasvuru(connection, null, transaction);
                    await tabBasvuru.YatirimOzetiKaydetAsync(yatirimOzeti);

                    TABBasvuruLog tabBasvuruLog = new TABBasvuruLog(connection, null, transaction);
                    await tabBasvuruLog.EkleAsync(yatirimOzeti.basvuruId, kullanici, "KaydetYatirimOzetiAsync", yatirimOzeti);

                    await transaction.CommitAsync();
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                BeklenmeyenHata(sonuc, ex, "Yatırım özeti kaydedilemedi. BasvuruId: {BasvuruId}, KullaniciId: {KullaniciId}", "Başvuru kaydedilemedi.", yatirimOzeti.basvuruId, kullanici.Id);
            }
            return sonuc;
        }

        public async Task<Sonuc<int>> KaydetCevreselSosyalAsync(BasvuruCevreselSosyal cevreselSosyal, Kullanici kullanici)
        {
            Sonuc<int> sonuc = new Sonuc<int>();
            if (kullanici == null)
            {
                HataEkle(sonuc, "Business.User.InfoMissing");
                return sonuc;
            }
            try
            {
                cevreselSosyal.Dogrula(sonuc);
                if (!string.IsNullOrWhiteSpace(cevreselSosyal.cevreselSosyalJson))
                {
                    try
                    {
                        using JsonDocument document = JsonDocument.Parse(cevreselSosyal.cevreselSosyalJson);
                        CevreselSosyalCevapAnahtarlariDogrula(document.RootElement, sonuc);
                    }
                    catch (JsonException)
                    {
                        HataEkle(sonuc, "Business.Esf.AnswersReadFailed");
                    }
                }

                if (!sonuc.basarili)
                    return sonuc;

                await using SqlConnection connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                Basvuru? mevcut = await BasvuruOnBasvuruYetkiKontrolAsync(connection, cevreselSosyal.basvuruId, kullanici, sonuc);
                if (!sonuc.basarili || mevcut == null)
                    return sonuc;

                await using SqlTransaction transaction = (SqlTransaction)await connection.BeginTransactionAsync();
                try
                {
                    TABBasvuru tabBasvuru = new TABBasvuru(connection, null, transaction);
                    await tabBasvuru.CevreselSosyalKaydetAsync(cevreselSosyal);

                    TABBasvuruLog tabBasvuruLog = new TABBasvuruLog(connection, null, transaction);
                    await tabBasvuruLog.EkleAsync(cevreselSosyal.basvuruId, kullanici, "KaydetCevreselSosyalAsync", cevreselSosyal);

                    await transaction.CommitAsync();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                BeklenmeyenHata(sonuc, ex, "Çevresel-sosyal anket kaydedilemedi. BasvuruId: {BasvuruId}, KullaniciId: {KullaniciId}", "Başvuru kaydedilemedi.", cevreselSosyal.basvuruId, kullanici.Id);
            }
            return sonuc;
        }

        private void CevreselSosyalCevapAnahtarlariDogrula(JsonElement root, Sonuc sonuc)
        {
            if (!root.TryGetProperty("answers", out JsonElement answers) || answers.ValueKind != JsonValueKind.Object)
                return;

            HashSet<string> tanimliSorular = CevreselSosyalAnketTanimlari.Tum
                .SelectMany(grup => grup.Questions)
                .Select(soru => soru.Id)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (JsonProperty cevap in answers.EnumerateObject())
            {
                if (!CevreselSosyalCevapAnahtariTanimliMi(cevap.Name, tanimliSorular))
                    HataEkle(sonuc, "Business.Esf.UnknownQuestion", cevap.Name);
            }
        }

        private static bool CevreselSosyalCevapAnahtariTanimliMi(string key, HashSet<string> tanimliSorular)
        {
            if (tanimliSorular.Contains(key))
                return true;

            foreach (string soruId in tanimliSorular)
            {
                string normalized = soruId.Replace(".", "_");
                if (key.StartsWith($"csf_{normalized}_", StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        public async Task<Sonuc<int>> KaydetMaliAsync(BasvuruMali mali, Kullanici kullanici)
        {
            Sonuc<int> sonuc = new Sonuc<int>();
            if (kullanici == null)
            {
                HataEkle(sonuc, "Business.User.InfoMissing");
                return sonuc;
            }
            try
            {
                mali.Dogrula(sonuc);

                if (!sonuc.basarili)
                    return sonuc;

                await using SqlConnection connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                Basvuru? mevcut = null;
                if (mali.basvuruId > 0)
                {
                    mevcut = await BasvuruOnBasvuruYetkiKontrolAsync(connection, mali.basvuruId, kullanici, sonuc);
                    if (!sonuc.basarili || mevcut == null)
                        return sonuc;
                }

                await using SqlTransaction transaction = (SqlTransaction)await connection.BeginTransactionAsync();

                try
                {
                    TABBasvuru tabBasvuru = new TABBasvuru(connection, null, transaction);
                    await tabBasvuru.BasvuruMaliGuncelleAsync(mali);

                    TABBasvuruLog tabBasvuruLog = new TABBasvuruLog(connection, null, transaction);
                    await tabBasvuruLog.EkleAsync(mali.basvuruId, kullanici, "KaydetMaliAsync", mali);

                    await transaction.CommitAsync();
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                BeklenmeyenHata(sonuc, ex, "Firma basvurusu  kaydedilemedi. BasvuruId: {BasvuruId}, KullaniciId: {KullaniciId}", "Başvuru kaydedilemedi.", mali.basvuruId, kullanici.Id);
            }
            return sonuc;
        }

        public async Task<Sonuc<int>> KaydetOrtaklikAsync(BasvuruOrtaklik ortaklik, Kullanici kullanici)
        {
            Sonuc<int> sonuc = new Sonuc<int>();
            if (kullanici == null)
            {
                HataEkle(sonuc, "Business.User.InfoMissing");
                return sonuc;
            }

            try
            {
                ortaklik ??= new BasvuruOrtaklik();
                ortaklik.ortaklar ??= new List<BasvuruOrtak>();
                ortaklik.ozelSektorPayi = ortaklik.ortaklar
                    .Where(x => string.Equals(x.ozelKamuNiteligi, "Özel", StringComparison.OrdinalIgnoreCase))
                    .Sum(x => x.payOrani.GetValueOrDefault());

                ortaklik.Dogrula(sonuc);
                if (!sonuc.basarili)
                    return sonuc;

                await using SqlConnection connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                Basvuru? mevcut = await BasvuruOnBasvuruYetkiKontrolAsync(connection, ortaklik.basvuruId, kullanici, sonuc);
                if (!sonuc.basarili || mevcut == null)
                    return sonuc;

                mevcut.ortaklik = ortaklik;

                await using SqlTransaction transaction = (SqlTransaction)await connection.BeginTransactionAsync();
                try
                {
                    TABBasvuru tabBasvuru = new TABBasvuru(connection, null, transaction);
                    await tabBasvuru.OrtaklikKaydetAsync(mevcut);

                    TABBasvuruLog tabBasvuruLog = new TABBasvuruLog(connection, null, transaction);
                    await tabBasvuruLog.EkleAsync(ortaklik.basvuruId, kullanici, "KaydetOrtaklikAsync", ortaklik);

                    await transaction.CommitAsync();
                    sonuc.nesne = ortaklik.basvuruId;
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                BeklenmeyenHata(sonuc, ex, "Ortaklık bilgileri kaydedilemedi. BasvuruId: {BasvuruId}, KullaniciId: {KullaniciId}", "Ortaklık bilgileri kaydedilemedi.", ortaklik?.basvuruId ?? 0, kullanici.Id);
            }

            return sonuc;
        }

        public async Task<Sonuc<int>> KaydetOrtaklarAsync(BasvuruOrtaklik ortaklik, Kullanici kullanici)
        {
            Sonuc<int> sonuc = new Sonuc<int>();
            if (kullanici == null)
            {
                HataEkle(sonuc, "Business.User.InfoMissing");
                return sonuc;
            }

            try
            {
                ortaklik ??= new BasvuruOrtaklik();
                ortaklik.ortaklar ??= new List<BasvuruOrtak>();
                if (ortaklik.basvuruId <= 0)
                    HataEkle(sonuc, "Business.Application.RecordRequired");
                ortaklik.OrtaklariDogrula(sonuc);
                if (!sonuc.basarili)
                    return sonuc;

                await using SqlConnection connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                Basvuru? mevcut = await BasvuruOnBasvuruYetkiKontrolAsync(connection, ortaklik.basvuruId, kullanici, sonuc);
                if (!sonuc.basarili || mevcut == null)
                    return sonuc;

                await using SqlTransaction transaction = (SqlTransaction)await connection.BeginTransactionAsync();
                try
                {
                    TABBasvuru tabBasvuru = new TABBasvuru(connection, null, transaction);
                    await tabBasvuru.BasvuruOrtaklariKaydetAsync(ortaklik.basvuruId, ortaklik.ortaklar);

                    TABBasvuruLog tabBasvuruLog = new TABBasvuruLog(connection, null, transaction);
                    await tabBasvuruLog.EkleAsync(ortaklik.basvuruId, kullanici, "KaydetOrtaklarAsync", new
                    {
                        ortaklik.basvuruId,
                        ortaklik.ortaklar
                    });

                    await transaction.CommitAsync();
                    sonuc.nesne = ortaklik.basvuruId;
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                BeklenmeyenHata(sonuc, ex, "Ortak/pay sahibi bilgileri kaydedilemedi. BasvuruId: {BasvuruId}, KullaniciId: {KullaniciId}", "Ortak/pay sahibi bilgileri kaydedilemedi.", ortaklik?.basvuruId ?? 0, kullanici.Id);
            }

            return sonuc;
        }

        public async Task<Sonuc<BasvuruDosyaYuklemeSonucu>> BasvuruDosyasiKaydetAsync(int basvuruId, string formAd, int dosyaNo, string dosyaAdi, byte[] icerik, Kullanici kullanici)
        {
            Sonuc<BasvuruDosyaYuklemeSonucu> sonuc = new Sonuc<BasvuruDosyaYuklemeSonucu>();
            formAd = formAd?.Trim() ?? "";
            string dosyaTuru = BasvuruDosyaTuruBul(formAd, dosyaNo);

            if (basvuruId <= 0)
                HataEkle(sonuc, "Business.Application.RecordRequired");
            if (!BasvuruFormAdGecerliMi(formAd))
                HataEkle(sonuc, "Business.Application.FileFormUndefined");
            if (dosyaNo <= 0)
                HataEkle(sonuc, "Business.Application.FileTypeRequired");
            if (string.IsNullOrWhiteSpace(dosyaTuru))
                HataEkle(sonuc, "Business.Application.FileTypeUndefined");
            if (string.IsNullOrWhiteSpace(dosyaAdi))
                HataEkle(sonuc, "Business.File.FileRequired");
            if (icerik == null || icerik.Length == 0)
                HataEkle(sonuc, "Business.File.FileRequired");
            if (!sonuc.basarili)
                return sonuc;

            try
            {
                await using SqlConnection connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                Basvuru? mevcut = await BasvuruOnBasvuruYetkiKontrolAsync(connection, basvuruId, kullanici, sonuc);
                if (!sonuc.basarili || mevcut == null)
                    return sonuc;

                if (UboKycFormAdMi(formAd))
                {
                    string tcknVkn = UboKycFormAdKimlikOku(formAd);
                    if (!await BasvuruOrtakKimlikVarMiAsync(connection, basvuruId, tcknVkn))
                    {
                        HataEkle(sonuc, "Business.Application.UboKycPartnerRequired");
                        return sonuc;
                    }
                }

                if (string.Equals(formAd, BasvuruAdliSicilFormAd, StringComparison.OrdinalIgnoreCase)
                    && !await BasvuruAdliSicilKisiVarMiAsync(connection, basvuruId, dosyaNo))
                {
                    HataEkle(sonuc, "Business.Application.CriminalPersonRequiredBeforeUpload");
                    return sonuc;
                }

                Sonuc<DosyaBilgisi> dosyaSonuc = await _dosyaYonetimIsKurallari.DosyaEkleVeyaGuncelleAsync(
                    BasvuruDosyaModeliOlustur(basvuruId, formAd, dosyaNo, dosyaAdi, icerik ?? [], dosyaTuru),
                    new BasvuruDosyaYetkiKontrol(basvuruId));

                if (!dosyaSonuc.basarili || dosyaSonuc.nesne == null)
                {
                    SonucHatalariniAktar(dosyaSonuc, sonuc);
                    return sonuc;
                }

                if (string.Equals(formAd, BasvuruOrtakUboKycFormAd, StringComparison.OrdinalIgnoreCase) || UboKycFormAdMi(formAd))
                {
                    int siraNo = dosyaNo;
                    string tcknVkn = UboKycFormAdKimlikOku(formAd);
                    await using SqlTransaction transaction = (SqlTransaction)await connection.BeginTransactionAsync();
                    try
                    {
                        TABBasvuru tabBasvuru = new TABBasvuru(connection, null, transaction);
                        if (!string.IsNullOrWhiteSpace(tcknVkn))
                            await tabBasvuru.BasvuruOrtakUboKycDosyasiGuncelleAsync(basvuruId, tcknVkn, dosyaSonuc.nesne.Id, dosyaSonuc.nesne.DosyaAdi);
                        else
                            await tabBasvuru.BasvuruOrtakUboKycDosyasiGuncelleAsync(basvuruId, siraNo, dosyaSonuc.nesne.Id, dosyaSonuc.nesne.DosyaAdi);

                        TABBasvuruLog tabBasvuruLog = new TABBasvuruLog(connection, null, transaction);
                        await tabBasvuruLog.EkleAsync(basvuruId, kullanici, "OrtakUboKycDosyasiKaydet", new
                        {
                            SiraNo = siraNo,
                            TcknVkn = tcknVkn,
                            dosyaSonuc.nesne.Id,
                            dosyaSonuc.nesne.DosyaAdi
                        });

                        await transaction.CommitAsync();
                    }
                    catch
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                }

                if (string.Equals(formAd, BasvuruAdliSicilFormAd, StringComparison.OrdinalIgnoreCase))
                {
                    await using SqlTransaction transaction = (SqlTransaction)await connection.BeginTransactionAsync();
                    try
                    {
                        TABBasvuru tabBasvuru = new TABBasvuru(connection, null, transaction);
                        await tabBasvuru.BasvuruAdliSicilDosyasiGuncelleAsync(basvuruId, dosyaNo, dosyaSonuc.nesne.Id, dosyaSonuc.nesne.DosyaAdi);

                        TABBasvuruLog tabBasvuruLog = new TABBasvuruLog(connection, null, transaction);
                        await tabBasvuruLog.EkleAsync(basvuruId, kullanici, "AdliSicilDosyasiKaydet", new
                        {
                            KisiId = dosyaNo,
                            dosyaSonuc.nesne.Id,
                            dosyaSonuc.nesne.DosyaAdi
                        });

                        await transaction.CommitAsync();
                    }
                    catch
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                }

                sonuc.nesne = new BasvuruDosyaYuklemeSonucu
                {
                    BasvuruId = basvuruId,
                    DosyaId = dosyaSonuc.nesne.Id,
                    DosyaAdi = dosyaSonuc.nesne.DosyaAdi,
                    Aciklama = dosyaTuru
                };
                sonuc.mesaj = Metin("Business.File.Uploaded");
            }
            catch (Exception ex)
            {
                BeklenmeyenHata(sonuc, ex, "Başvuru dosyası kaydedilemedi. BasvuruId: {BasvuruId}, FormAd: {FormAd}, DosyaNo: {DosyaNo}", "Başvuru dosyası kaydedilemedi.", basvuruId, formAd, dosyaNo);
            }

            return sonuc;
        }

        public Task<Sonuc<BasvuruDosyaYuklemeSonucu>> OrtaklikDosyasiKaydetAsync(int basvuruId, string formAd, int dosyaNo, string dosyaAdi, byte[] icerik, Kullanici kullanici)
        {
            return BasvuruDosyasiKaydetAsync(basvuruId, formAd, dosyaNo, dosyaAdi, icerik, kullanici);
        }

        public async Task<Sonuc<List<BasvuruAdliSicilKisi>>> KaydetAdliSicilKisileriAsync(int basvuruId, List<BasvuruAdliSicilKisi>? kisiler, Kullanici kullanici)
        {
            Sonuc<List<BasvuruAdliSicilKisi>> sonuc = new Sonuc<List<BasvuruAdliSicilKisi>>();
            kisiler ??= new List<BasvuruAdliSicilKisi>();

            if (basvuruId <= 0)
                HataEkle(sonuc, "Business.Application.RecordRequired");

            foreach (BasvuruAdliSicilKisi kisi in kisiler)
            {
                kisi.basvuruId = basvuruId;
                kisi.Dogrula(sonuc);
            }

            List<string> tekrarliTcknler = kisiler
                .Select(x => TcknVknNormalizeEt(x.tckn))
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .GroupBy(x => x, StringComparer.OrdinalIgnoreCase)
                .Where(x => x.Count() > 1)
                .Select(x => x.Key)
                .ToList();

            foreach (string tckn in tekrarliTcknler)
            {
                HataEkle(sonuc, "Business.Criminal.DuplicateTcknWithValue", tckn);
            }

            if (!sonuc.basarili)
                return sonuc;

            try
            {
                await using SqlConnection connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                Basvuru? mevcut = await BasvuruOnBasvuruYetkiKontrolAsync(connection, basvuruId, kullanici, sonuc);
                if (!sonuc.basarili || mevcut == null)
                    return sonuc;

                await using SqlTransaction transaction = (SqlTransaction)await connection.BeginTransactionAsync();
                try
                {
                    TABBasvuru tabBasvuru = new TABBasvuru(connection, null, transaction);
                    sonuc.nesne = await tabBasvuru.BasvuruAdliSicilKisileriKaydetAsync(basvuruId, kisiler);

                    TABBasvuruLog tabBasvuruLog = new TABBasvuruLog(connection, null, transaction);
                    await tabBasvuruLog.EkleAsync(basvuruId, kullanici, "AdliSicilKisileriKaydet", sonuc.nesne);

                    await transaction.CommitAsync();
                    sonuc.mesaj = Metin("Business.Criminal.PeopleSaved");
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                BeklenmeyenHata(sonuc, ex, "Adli sicil kişileri kaydedilemedi. BasvuruId: {BasvuruId}", "Adli sicil kişileri kaydedilemedi.", basvuruId);
            }

            return sonuc;
        }

        public async Task<Sonuc<BasvuruDosyaYuklemeSonucu>> BelgePaketiKaydetAsync(
            int basvuruId,
            string dosyaAdi,
            byte[] icerik,
            string aciklama,
            string belgeBeyani,
            List<string>? belgeGruplari,
            List<string>? seciliBelgeGruplari,
            Kullanici kullanici)
        {
            Sonuc<BasvuruDosyaYuklemeSonucu> sonuc = new Sonuc<BasvuruDosyaYuklemeSonucu>();
            string temizAciklama = aciklama?.Trim() ?? "";
            string temizBeyan = belgeBeyani?.Trim() ?? "";
            List<string> tumBelgeGruplari = TemizListe(belgeGruplari);
            List<string> seciliGruplar = TemizListe(seciliBelgeGruplari);

            if (basvuruId <= 0)
                HataEkle(sonuc, "Business.Application.RecordRequired");
            if (string.IsNullOrWhiteSpace(dosyaAdi))
                HataEkle(sonuc, "Basvuru.Documents.PackageFileRequired");
            if (icerik == null || icerik.Length == 0)
                HataEkle(sonuc, "Basvuru.Documents.PackageFileRequired");
            if (string.IsNullOrWhiteSpace(temizAciklama))
                HataEkle(sonuc, "Business.Application.DocumentDescriptionRequired");
            BelgePaketiBeyaniDogrula(temizBeyan, tumBelgeGruplari, seciliGruplar, sonuc);
            if (!sonuc.basarili)
                return sonuc;

            try
            {
                await using SqlConnection connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                Basvuru? mevcut = await BasvuruOnBasvuruYetkiKontrolAsync(connection, basvuruId, kullanici, sonuc);
                if (!sonuc.basarili || mevcut == null)
                    return sonuc;

                Sonuc<DosyaBilgisi> dosyaSonuc = await _dosyaYonetimIsKurallari.DosyaEkleVeyaGuncelleAsync(
                    BasvuruDosyaModeliOlustur(basvuruId, 1, dosyaAdi, icerik ?? [], temizAciklama),
                    new BasvuruDosyaYetkiKontrol(basvuruId));

                if (!dosyaSonuc.basarili || dosyaSonuc.nesne == null)
                {
                    SonucHatalariniAktar(dosyaSonuc, sonuc);
                    return sonuc;
                }

                mevcut.BelgePaketiDosyaAdi = dosyaSonuc.nesne.DosyaAdi;
                mevcut.BelgePaketiDosyaId = dosyaSonuc.nesne.Id;
                mevcut.BelgePaketiAciklama = temizAciklama;
                mevcut.BelgeBeyani = temizBeyan;

                await using SqlTransaction transaction = (SqlTransaction)await connection.BeginTransactionAsync();
                try
                {
                    TABBasvuru tabBasvuru = new TABBasvuru(connection, null, transaction);
                    await tabBasvuru.BasvuruBelgePaketiGuncelleAsync(mevcut);

                    TABBasvuruLog tabBasvuruLog = new TABBasvuruLog(connection, null, transaction);
                    await tabBasvuruLog.EkleAsync(mevcut.Id, kullanici, "BelgePaketiKaydet", new
                    {
                        mevcut.BelgePaketiDosyaAdi,
                        mevcut.BelgePaketiDosyaId,
                        mevcut.BelgePaketiAciklama,
                        mevcut.BelgeBeyani
                    });

                    await transaction.CommitAsync();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }

                sonuc.nesne = new BasvuruDosyaYuklemeSonucu
                {
                    BasvuruId = mevcut.Id,
                    DosyaId = dosyaSonuc.nesne.Id,
                    DosyaAdi = dosyaSonuc.nesne.DosyaAdi,
                    Aciklama = temizAciklama
                };
                sonuc.mesaj = Metin("Business.Application.DocumentPackageUploaded");
            }
            catch (Exception ex)
            {
                BeklenmeyenHata(sonuc, ex, "Doküman paketi kaydedilemedi. BasvuruId: {BasvuruId}, KullaniciId: {KullaniciId}", "Doküman paketi kaydedilemedi.", basvuruId, kullanici.Id);
            }

            return sonuc;
        }

        public async Task<Sonuc<BasvuruDosyaYuklemeSonucu>> TaahhutDosyasiKaydetAsync(int basvuruId, string dosyaAdi, byte[] icerik, string aciklama, Kullanici kullanici)
        {
            Sonuc<BasvuruDosyaYuklemeSonucu> sonuc = new Sonuc<BasvuruDosyaYuklemeSonucu>();
            string temizAciklama = aciklama?.Trim() ?? "";

            if (basvuruId <= 0)
                HataEkle(sonuc, "Business.Application.RecordRequired");
            if (string.IsNullOrWhiteSpace(dosyaAdi))
                HataEkle(sonuc, "Basvuru.Documents.CommitmentFileRequired");
            if (icerik == null || icerik.Length == 0)
                HataEkle(sonuc, "Basvuru.Documents.CommitmentFileRequired");
            if (!sonuc.basarili)
                return sonuc;

            try
            {
                await using SqlConnection connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                Basvuru? mevcut = await BasvuruOnBasvuruYetkiKontrolAsync(connection, basvuruId, kullanici, sonuc);
                if (!sonuc.basarili || mevcut == null)
                    return sonuc;

                Sonuc<DosyaBilgisi> dosyaSonuc = await _dosyaYonetimIsKurallari.DosyaEkleVeyaGuncelleAsync(
                    BasvuruDosyaModeliOlustur(basvuruId, 2, dosyaAdi, icerik ?? [], temizAciklama),
                    new BasvuruDosyaYetkiKontrol(basvuruId));

                if (!dosyaSonuc.basarili || dosyaSonuc.nesne == null)
                {
                    SonucHatalariniAktar(dosyaSonuc, sonuc);
                    return sonuc;
                }

                mevcut.TaahhutDosyaAdi = dosyaSonuc.nesne.DosyaAdi;
                mevcut.TaahhutDosyaId = dosyaSonuc.nesne.Id;
                mevcut.TaahhutAciklama = temizAciklama;

                await using SqlTransaction transaction = (SqlTransaction)await connection.BeginTransactionAsync();
                try
                {
                    TABBasvuru tabBasvuru = new TABBasvuru(connection, null, transaction);
                    await tabBasvuru.BasvuruTaahhutGuncelleAsync(mevcut);

                    TABBasvuruLog tabBasvuruLog = new TABBasvuruLog(connection, null, transaction);
                    await tabBasvuruLog.EkleAsync(mevcut.Id, kullanici, "TaahhutDosyasiKaydet", new
                    {
                        mevcut.TaahhutDosyaAdi,
                        mevcut.TaahhutDosyaId,
                        mevcut.TaahhutAciklama
                    });

                    await transaction.CommitAsync();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }

                sonuc.nesne = new BasvuruDosyaYuklemeSonucu
                {
                    BasvuruId = mevcut.Id,
                    DosyaId = dosyaSonuc.nesne.Id,
                    DosyaAdi = dosyaSonuc.nesne.DosyaAdi,
                    Aciklama = temizAciklama
                };
                sonuc.mesaj = Metin("Business.Application.CommitmentFileUploaded");
            }
            catch (Exception ex)
            {
                BeklenmeyenHata(sonuc, ex, "Taahhüt dosyası kaydedilemedi. BasvuruId: {BasvuruId}, KullaniciId: {KullaniciId}", "Taahhüt dosyası kaydedilemedi.", basvuruId, kullanici.Id);
            }

            return sonuc;
        }

        public async Task<Sonuc<BasvuruDosyaYuklemeSonucu>> DenetimDosyasiKaydetAsync(int basvuruId, string dosyaAdi, byte[] icerik, Kullanici kullanici)
        {
            Sonuc<BasvuruDosyaYuklemeSonucu> sonuc = new Sonuc<BasvuruDosyaYuklemeSonucu>();
            const string aciklama = "Bağımsız denetim dosyası";

            if (basvuruId <= 0)
                HataEkle(sonuc, "Business.Application.RecordRequired");
            if (string.IsNullOrWhiteSpace(dosyaAdi))
                HataEkle(sonuc, "Basvuru.Mali.AuditFileRequired");
            if (icerik == null || icerik.Length == 0)
                HataEkle(sonuc, "Basvuru.Mali.AuditFileRequired");
            if (!sonuc.basarili)
                return sonuc;

            try
            {
                await using SqlConnection connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                Basvuru? mevcut = await BasvuruOnBasvuruYetkiKontrolAsync(connection, basvuruId, kullanici, sonuc);
                if (!sonuc.basarili || mevcut == null)
                    return sonuc;

                Sonuc<DosyaBilgisi> dosyaSonuc = await _dosyaYonetimIsKurallari.DosyaEkleVeyaGuncelleAsync(
                    BasvuruDosyaModeliOlustur(basvuruId, 3, dosyaAdi, icerik ?? [], aciklama),
                    new BasvuruDosyaYetkiKontrol(basvuruId));

                if (!dosyaSonuc.basarili || dosyaSonuc.nesne == null)
                {
                    SonucHatalariniAktar(dosyaSonuc, sonuc);
                    return sonuc;
                }

                mevcut.mali.denetimDosyaAdi = dosyaSonuc.nesne.DosyaAdi;
                mevcut.mali.denetimDosyaId = dosyaSonuc.nesne.Id;

                await using SqlTransaction transaction = (SqlTransaction)await connection.BeginTransactionAsync();
                try
                {
                    TABBasvuru tabBasvuru = new TABBasvuru(connection, null, transaction);
                    await tabBasvuru.BasvuruDenetimDosyasiGuncelleAsync(mevcut.mali);

                    TABBasvuruLog tabBasvuruLog = new TABBasvuruLog(connection, null, transaction);
                    await tabBasvuruLog.EkleAsync(mevcut.Id, kullanici, "DenetimDosyasiKaydet", new
                    {
                        mevcut.mali.denetimDosyaAdi,
                        mevcut.mali.denetimDosyaId
                    });

                    await transaction.CommitAsync();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }

                sonuc.nesne = new BasvuruDosyaYuklemeSonucu
                {
                    BasvuruId = mevcut.Id,
                    DosyaId = dosyaSonuc.nesne.Id,
                    DosyaAdi = dosyaSonuc.nesne.DosyaAdi,
                    Aciklama = aciklama
                };
                sonuc.mesaj = Metin("Business.Application.AuditFileUploaded");
            }
            catch (Exception ex)
            {
                BeklenmeyenHata(sonuc, ex, "Bağımsız denetim dosyası kaydedilemedi. BasvuruId: {BasvuruId}, KullaniciId: {KullaniciId}", "Bağımsız denetim dosyası kaydedilemedi.", basvuruId, kullanici.Id);
            }

            return sonuc;
        }

        public async Task<Sonuc<Dosya>> DosyaIndirAsync(int dosyaId, Kullanici kullanici)
        {
            Sonuc<Dosya> sonuc = new Sonuc<Dosya>();

            if (dosyaId <= 0)
            {
                HataEkle(sonuc, "Business.File.FileRequired");
                return sonuc;
            }

            try
            {
                Sonuc<Dosya> dosyaSonuc = await _dosyaYonetimIsKurallari.DosyaGetirAsync(dosyaId, new BasvuruDosyaIndirmeYetkiKontrol());
                if (!dosyaSonuc.basarili || dosyaSonuc.nesne == null)
                {
                    SonucHatalariniAktar(dosyaSonuc, sonuc);
                    return sonuc;
                }

                Dosya dosyaBilgisi = dosyaSonuc.nesne!;
                int basvuruId = BasvuruIdDosyaFormAnahtarindanOku(dosyaBilgisi.FormAnahtar);
                int dosyaNo = dosyaBilgisi.DosyaNo;
                if (basvuruId <= 0 || dosyaNo <= 0)
                {
                    HataEkle(sonuc, "Business.File.NotFound");
                    return sonuc;
                }

                await using SqlConnection connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                Basvuru? mevcut = await BasvuruOnBasvuruYetkiKontrolAsync(connection, basvuruId, kullanici, sonuc);
                if (!sonuc.basarili || mevcut == null)
                    return sonuc;

                sonuc.nesne = dosyaBilgisi;
            }
            catch (Exception ex)
            {
                BeklenmeyenHata(sonuc, ex, "Başvuru dosyası indirilemedi. DosyaId: {DosyaId}, KullaniciId: {KullaniciId}", "Dosya indirilemedi.", dosyaId, kullanici.Id);
            }

            return sonuc;
        }


        public async Task<Sonuc<List<BasvuruUygulamaAdresi>>> UygulamaAdresiListeleAsync(int basvuruId, Kullanici? kullanici)
        {
            Sonuc<List<BasvuruUygulamaAdresi>> sonuc = new Sonuc<List<BasvuruUygulamaAdresi>>();
            if (kullanici == null)
            {
                HataEkle(sonuc, "Business.User.InfoMissing");
                return sonuc;
            }
            try
            {
                await using SqlConnection connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                Basvuru? mevcut = await BasvuruOnBasvuruYetkiKontrolAsync(connection, basvuruId, kullanici, sonuc);
                if (!sonuc.basarili || mevcut == null)
                    return sonuc;

                TABBasvuru tabBasvuru = new TABBasvuru(connection, _localizer);
                sonuc.nesne = await tabBasvuru.UygulamaAdresiOkuAsync(basvuruId, 0);

            }
            catch (Exception ex)
            {
                BeklenmeyenHata(sonuc, ex, "Uygulama adresi kaydedilemedi. BasvuruId: {BasvuruId}, KullaniciId: {KullaniciId}", "Uygulama adresi kaydedilemedi.", basvuruId, kullanici.Id);
            }

            return sonuc;
        }

        public async Task<Sonuc<BasvuruUygulamaAdresi>> UygulamaAdresiKaydetAsync(BasvuruUygulamaAdresi adres, Kullanici kullanici)
        {
            Sonuc<BasvuruUygulamaAdresi> sonuc = new Sonuc<BasvuruUygulamaAdresi>();
            if (kullanici == null)
            {
                HataEkle(sonuc, "Business.User.InfoMissing");
                return sonuc;
            }

            try
            {
                UygulamaAdresiNormalizeEt(adres);
                adres.UygulamaAdresiDogrula(sonuc);
                if (!sonuc.basarili)
                    return sonuc;

                await using SqlConnection connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                Basvuru? mevcut = await BasvuruOnBasvuruYetkiKontrolAsync(connection, adres.basvuruId, kullanici, sonuc);
                if (!sonuc.basarili || mevcut == null)
                    return sonuc;

                TABIlce tabIlce = new TABIlce(connection);
                Ilce? ilce = adres.ilceId.HasValue ? await tabIlce.OkuAsync(adres.ilceId.Value) : null;
                if (ilce == null || !ilce.Aktif || ilce.IlId != mevcut.basvuruFirma.il.id)
                {
                    HataEkle(sonuc, "Business.Address.DistrictNotInApplicationProvince");
                    return sonuc;
                }

                if (adres.id > 0)
                {
                    TABBasvuru kontrolTablosu = new TABBasvuru(connection);

                    List<BasvuruUygulamaAdresi> d = await kontrolTablosu.UygulamaAdresiOkuAsync(adres.basvuruId, adres.id);

                    BasvuruUygulamaAdresi? eskiAdres;

                    if (d != null && d.Count == 1)
                        eskiAdres = d[0];
                    else
                        eskiAdres = null;
                    if (eskiAdres == null)
                    {
                        HataEkle(sonuc, "Business.Address.NotFound");
                        return sonuc;
                    }
                }

                bool yeniKayit = adres.id <= 0;
                await using SqlTransaction transaction = (SqlTransaction)await connection.BeginTransactionAsync();
                try
                {
                    TABBasvuru tabBasvuru = new TABBasvuru(connection, null, transaction);
                    int adresId = await tabBasvuru.UygulamaAdresiKaydetAsync(adres);

                    TABBasvuruLog tabBasvuruLog = new TABBasvuruLog(connection, null, transaction);
                    await tabBasvuruLog.EkleAsync(
                        mevcut.Id,
                        kullanici,
                        yeniKayit ? "UygulamaAdresiYeniKayit" : "UygulamaAdresiUpdate",
                        adres);

                    await transaction.CommitAsync();
                    sonuc.nesne = adres;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Uygulama adresi kaydedilemedi. BasvuruId: {BasvuruId}, AdresId: {AdresId}, KullaniciId: {KullaniciId}", adres.basvuruId, adres.id, kullanici.Id);
                    HataEkle(sonuc, "Business.Address.SaveFailed");
                }
            }
            catch (Exception ex)
            {
                BeklenmeyenHata(sonuc, ex, "Uygulama adresi kaydedilemedi. BasvuruId: {BasvuruId}, KullaniciId: {KullaniciId}", "Uygulama adresi kaydedilemedi.", adres.basvuruId, kullanici.Id);
            }

            return sonuc;
        }

        public async Task<Sonuc> UygulamaAdresiSilAsync(int adresId, Kullanici? kullanici)
        {
            Sonuc sonuc = new Sonuc();
            if (kullanici == null)
            {
                HataEkle(sonuc, "Business.User.InfoMissing");
                return sonuc;
            }

            try
            {
                if (adresId <= 0)
                {
                    HataEkle(sonuc, "Business.Address.RecordRequired");
                    return sonuc;
                }

                await using SqlConnection connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();
                TABBasvuru kontrolTablosu = new TABBasvuru(connection);
                List<BasvuruUygulamaAdresi> d = await kontrolTablosu.UygulamaAdresiOkuAsync(0, adresId);

                BasvuruUygulamaAdresi? eskiAdres;

                if (d != null && d.Count == 1)
                    eskiAdres = d[0];
                else
                    eskiAdres = null;

                if (eskiAdres == null)
                {
                    HataEkle(sonuc, "Business.Address.NotFound");
                    return sonuc;
                }

                Basvuru? mevcut = await BasvuruOnBasvuruYetkiKontrolAsync(connection, eskiAdres.basvuruId, kullanici, sonuc);
                if (!sonuc.basarili || mevcut == null)
                    return sonuc;

                await using SqlTransaction transaction = (SqlTransaction)await connection.BeginTransactionAsync();
                try
                {
                    TABBasvuru tabBasvuru = new TABBasvuru(connection, null, transaction);
                    await tabBasvuru.UygulamaAdresiSilAsync(eskiAdres.basvuruId, adresId);

                    TABBasvuruLog tabBasvuruLog = new TABBasvuruLog(connection, null, transaction);
                    await tabBasvuruLog.EkleAsync(mevcut.Id, kullanici, "UygulamaAdresiSil", eskiAdres);

                    await transaction.CommitAsync();
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    HataEkle(sonuc, "Business.Address.DeleteFailed");
                    _logger.LogError(ex, sonuc.hatalar[0]);
                }
            }
            catch (Exception ex)
            {
                BeklenmeyenHata(sonuc, ex, $"Uygulama adresi silinemedi. AdresId: {adresId}, KullaniciId: {kullanici.Id}", "Uygulama adresi silinemedi.");
            }

            return sonuc;
        }

        //private async Task<Sonuc<int>> KaydetMevcutAsamaAsync(
        //    Basvuru gelen,
        //    Kullanici kullanici,
        //    int asama,
        //    Action<Basvuru, Basvuru> asamaKopyala,
        //    Action<Basvuru, Sonuc>? asamaDogrula,
        //    string logIslem)
        //{
        //    Sonuc<int> sonuc = new Sonuc<int>();

        //    try
        //    {
        //        if (gelen.Id <= 0)
        //        {
        //            sonuc.HataEkle("Önce birinci aşama kaydedilmelidir.");
        //            return sonuc;
        //        }


        //        await using SqlConnection connection = new SqlConnection(_connectionString);
        //        await connection.OpenAsync();

        //        Basvuru? mevcut = await BasvuruOnBasvuruYetkiKontrolAsync(connection, gelen.Id, kullanici, sonuc);
        //        if (!sonuc.Basarili || mevcut == null)
        //            return sonuc;

        //        asamaKopyala(mevcut, gelen);

        //        asamaDogrula?.Invoke(mevcut, sonuc);
        //        if (!sonuc.Basarili)
        //            return sonuc;

        //        sonuc.Nesne = await BasvuruKaydetVeLoglaAsync(connection, mevcut, kullanici, logIslem);
        //    }
        //    catch (Exception ex)
        //    {
        //        BeklenmeyenHata(sonuc, ex, "Başvuru aşaması kaydedilemedi. BasvuruId: {BasvuruId}, Asama: {Asama}, KullaniciId: {KullaniciId}", "Başvuru kaydedilemedi.", gelen.Id, asama, kullanici.Id);
        //    }

        //    return sonuc;
        //}

        //private async Task<int> BasvuruKaydetVeLoglaAsync(SqlConnection connection, Basvuru basvuru, Kullanici kullanici, string logIslem)
        //{
        //    await using SqlTransaction transaction = (SqlTransaction)await connection.BeginTransactionAsync();

        //    try
        //    {
        //        TABBasvuru tabBasvuru = new TABBasvuru(connection, transaction);
        //        int basvuruId = await tabBasvuru.KaydetAsync(basvuru);

        //        TABBasvuruLog tabBasvuruLog = new TABBasvuruLog(connection, transaction);
        //        await tabBasvuruLog.EkleAsync(basvuru.Id, kullanici, logIslem, BasvuruLogDetayiOlustur(basvuru, logIslem));

        //        await transaction.CommitAsync();
        //        return basvuruId;
        //    }
        //    catch (Exception ex)
        //    {
        //        await transaction.RollbackAsync();
        //        _logger.LogError(ex, "Başvuru kaydedilemedi. BasvuruId: {BasvuruId}", basvuru.Id);
        //        throw;
        //    }
        //}


        private async Task<Basvuru?> BasvuruOnBasvuruYetkiKontrolAsync(SqlConnection connection, int basvuruId, Kullanici kullanici, Sonuc sonuc)
        {
            if (!BasvuruKullanicisiMi(kullanici))
            {
                BasvuruKullanicisiYetkiHatasiEkle(sonuc);
                return null;
            }

            TABBasvuru tabBasvuru = new TABBasvuru(connection, _localizer);
            Basvuru? mevcut = await tabBasvuru.OkuAsync(basvuruId);
            if (mevcut == null)
            {
                HataEkle(sonuc, "Business.Application.NotFoundOrUnauthorized");
                return null;
            }

            if (mevcut.durum != enumBasvuruDurum.OnBasvuruDurumu)
            {
                sonuc.HataEkle("Bu kayıt ön başvuru aşamasında olmadığı için güncellenemez.");
                return null;
            }

            //if (!string.Equals(mevcut.Durum, Basvuru.OnBasvuruDurumu, StringComparison.OrdinalIgnoreCase))
            //{
            //    sonuc.HataEkle("Bu kayıt ön başvuru aşamasında olmadığı için başvuru kullanıcı ekranından güncellenemez.");
            //    return null;
            //}

            if (mevcut.basvuruFirma.firmaId > 0)
            {
                TABFirmaKullanici tabFirmaKullanici = new TABFirmaKullanici(connection);
                if (!await tabFirmaKullanici.IliskiVarMiAsync(mevcut.basvuruFirma.firmaId, kullanici.Id))
                {
                    HataEkle(sonuc, "Business.Application.CompanyUserNotRelated");
                    return null;
                }
            }

            return mevcut;
        }

        private static bool BasvuruKullanicisiMi(Kullanici? kullanici)
        {
            return kullanici?.Yetkiler.Any(y => y.Rol == KullaniciRol.BasvuruKullanicisi) == true;
        }

        private static void BasvuruKullanicisiYetkiHatasiEkle(Sonuc sonuc)
        {
            sonuc.HataEkle("Ön başvuru kayıt işlemleri yalnızca başvuru kullanıcıları tarafından yapılabilir.");
        }

        public async Task<Sonuc<int>> OrtaklikKaydetAsync(Basvuru basvuru, Kullanici kullanici)
        {
            Sonuc<int> sonuc = new Sonuc<int>();

            try
            {
                if (basvuru.Id <= 0)
                {
                    HataEkle(sonuc, "Business.Partnership.ApplicationRequiredBeforeSave");
                    return sonuc;
                }

                //basvuru.BagliOrtakIsletmeVarMi = basvuru.BagliOrtakIsletmeVarMi;
                //basvuru.BagliOrtakAciklama = basvuru.BagliOrtakAciklama?.Trim() ?? "";

                await using SqlConnection connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                Basvuru? mevcut = await BasvuruOnBasvuruYetkiKontrolAsync(connection, basvuru.Id, kullanici, sonuc);
                if (!sonuc.basarili || mevcut == null)
                    return sonuc;

                //mevcut.OzelSektorPayi = basvuru.OzelSektorPayi;
                //mevcut.BagliOrtakIsletmeVarMi = basvuru.BagliOrtakIsletmeVarMi;
                //mevcut.BagliOrtakAciklama = basvuru.BagliOrtakAciklama;

                await using SqlTransaction transaction = (SqlTransaction)await connection.BeginTransactionAsync();
                try
                {
                    TABBasvuru tabBasvuru = new TABBasvuru(connection, null, transaction);
                    await tabBasvuru.OrtaklikKaydetAsync(mevcut);

                    TABBasvuruLog tabBasvuruLog = new TABBasvuruLog(connection, null, transaction);
                    //await tabBasvuruLog.EkleAsync(mevcut.Id, kullanici, "OrtaklikKaydet", new
                    //{
                    //    mevcut.OzelSektorPayi,
                    //    mevcut.BagliOrtakIsletmeVarMi,
                    //    mevcut.BagliOrtakAciklama
                    //});

                    await transaction.CommitAsync();
                    sonuc.nesne = mevcut.Id;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Ortaklık bilgileri kaydedilemedi. BasvuruId: {BasvuruId}, KullaniciId: {KullaniciId}", basvuru.Id, kullanici.Id);
                    HataEkle(sonuc, "Business.Partnership.SaveFailed");
                }
            }
            catch (Exception ex)
            {
                BeklenmeyenHata(sonuc, ex, "Ortaklık bilgileri kaydedilemedi. BasvuruId: {BasvuruId}, KullaniciId: {KullaniciId}", "Ortaklık bilgileri kaydedilemedi.", basvuru.Id, kullanici.Id);
            }

            return sonuc;
        }

        //private static void BasvuruNormalizeEt(Basvuru basvuru)
        //{
        //    if (string.IsNullOrWhiteSpace(basvuru.Durum) ||
        //        string.Equals(basvuru.Durum, "Taslak", StringComparison.OrdinalIgnoreCase))
        //    {
        //        basvuru.Durum = Basvuru.OnBasvuruDurumu;
        //    }

        //    basvuru.TicaretUnvani = basvuru.TicaretUnvani?.Trim() ?? "";
        //    basvuru.VergiKimlikNo = basvuru.VergiKimlikNo?.Trim() ?? "";
        //    basvuru.BasvuruDonemi = basvuru.BasvuruDonemi?.Trim() ?? "";
        //    basvuru.DonemId = basvuru.DonemId.GetValueOrDefault() > 0 ? basvuru.DonemId : null;
        //    basvuru.IlId = basvuru.IlId.GetValueOrDefault() > 0 ? basvuru.IlId : null;
        //    basvuru.IlAdi = basvuru.IlAdi?.Trim() ?? "";
        //    basvuru.BasvuruKonusu = basvuru.BasvuruKonusu?.Trim() ?? "";
        //    basvuru.IrtibatTelefon = basvuru.IrtibatTelefon?.Trim() ?? "";
        //    basvuru.IrtibatEposta = basvuru.IrtibatEposta?.Trim() ?? "";
        //    basvuru.YatirimAdi = basvuru.YatirimAdi?.Trim() ?? "";
        //    basvuru.DegerZinciriId = basvuru.DegerZinciriId.GetValueOrDefault() > 0 ? basvuru.DegerZinciriId : null;
        //    basvuru.DegerZinciriAsamalari = basvuru.DegerZinciriAsamalari
        //        .Select(x => x?.Trim() ?? "")
        //        .Where(x => x.Length > 0)
        //        .Distinct()
        //        .ToList();
        //    basvuru.HarcamaTurleri = basvuru.HarcamaTurleri
        //        .Select(x => x?.Trim() ?? "")
        //        .Where(x => x.Length > 0)
        //        .Distinct()
        //        .ToList();

        //    basvuru.YatirimAdresleri = basvuru.YatirimAdresleri
        //        .Select((adres, index) => new BasvuruUygulamaAdresi
        //        {
        //            Id = adres.Id,
        //            BasvuruId = adres.BasvuruId,
        //            SiraNo = adres.SiraNo > 0 ? adres.SiraNo : index + 1,
        //            IlceId = adres.IlceId.GetValueOrDefault() > 0 ? adres.IlceId : null,
        //            IlId = adres.IlId.GetValueOrDefault() > 0 ? adres.IlId : null,
        //            IlKod = adres.IlKod.GetValueOrDefault() > 0 ? adres.IlKod : null,
        //            IlAdi = adres.IlAdi?.Trim() ?? "",
        //            IlceAdi = adres.IlceAdi?.Trim() ?? "",
        //            TamAdres = adres.TamAdres?.Trim() ?? "",
        //            YatirimYeriStatusu = adres.YatirimYeriStatusu,
        //            KiraVeyaTahsisSuresi = adres.KiraVeyaTahsisSuresi.GetValueOrDefault() > 0 ? adres.KiraVeyaTahsisSuresi : null,
        //            KiraTahsisBitisTarihi = adres.KiraTahsisBitisTarihi,
        //            YapiRuhsatiDurumu = adres.YapiRuhsatiDurumu
        //        })
        //        .Where(adres =>
        //            adres.IlceId.HasValue ||
        //            !string.IsNullOrWhiteSpace(adres.TamAdres) ||
        //            adres.YatirimYeriStatusu.HasValue ||
        //            adres.KiraVeyaTahsisSuresi.HasValue ||
        //            adres.KiraTahsisBitisTarihi.HasValue ||
        //            adres.YapiRuhsatiDurumu.HasValue)
        //        .ToList();

        //    basvuru.YatirimAdresSayisi = basvuru.YatirimAdresleri.Count;
        //}

        //private static void FirmaNormalizeEt(Firma firma)
        //{
        //    firma.VergiKimlikNo = firma.VergiKimlikNo?.Trim() ?? "";
        //    firma.TicaretUnvani = firma.TicaretUnvani?.Trim() ?? "";
        //    firma.TicaretSicilNo = firma.TicaretSicilNo?.Trim() ?? "";
        //    firma.MersisNo = firma.MersisNo?.Trim() ?? "";
        //    firma.NaceKodu = firma.NaceKodu?.Trim() ?? "";
        //    firma.WebSitesi = firma.WebSitesi?.Trim() ?? "";
        //    firma.Telefon = firma.Telefon?.Trim() ?? "";
        //    firma.KepAdresi = firma.KepAdresi?.Trim() ?? "";
        //    firma.Eposta = firma.Eposta?.Trim() ?? "";
        //}

        //private static void Asama1Kopyala(Basvuru hedef, Basvuru kaynak)
        //{
        //    hedef.FirmaId = kaynak.FirmaId;
        //    hedef.DonemId = kaynak.DonemId;
        //    hedef.IlId = kaynak.IlId;
        //    hedef.BasvuruKonusu = kaynak.BasvuruKonusu;
        //    hedef.BasvuruSahibiTuru = kaynak.BasvuruSahibiTuru;
        //    hedef.SonIkiYildirFaalMi = kaynak.SonIkiYildirFaalMi;
        //    hedef.OzelSektorPayi = kaynak.OzelSektorPayi;
        //    hedef.BagliOrtakIsletmeVarMi = kaynak.BagliOrtakIsletmeVarMi;
        //    hedef.BagliOrtakAciklama = kaynak.BagliOrtakAciklama;
        //}

        //private static void Asama2Kopyala(Basvuru hedef, Basvuru kaynak)
        //{
        //    hedef.Telefon = kaynak.Telefon;
        //    hedef.IrtibatKisisi = kaynak.IrtibatKisisi;
        //    hedef.IrtibatUnvani = kaynak.IrtibatUnvani;
        //    hedef.IrtibatTelefon = kaynak.IrtibatTelefon;
        //    hedef.IrtibatEposta = kaynak.IrtibatEposta;
        //    hedef.IletisimAdresi = kaynak.IletisimAdresi;
        //    hedef.YetkiliKisiler = kaynak.YetkiliKisiler;
        //}

        //private static void Asama3Kopyala(Basvuru hedef, Basvuru kaynak)
        //{
        //    hedef.YatirimAdi = kaynak.YatirimAdi;
        //    hedef.YatirimTuru = kaynak.YatirimTuru;
        //    hedef.DegerZinciriId = kaynak.DegerZinciriId;
        //    hedef.DegerZinciriAsamalari = kaynak.DegerZinciriAsamalari;
        //    hedef.HarcamaTurleri = kaynak.HarcamaTurleri;
        //}

        //private static void Asama4Kopyala(Basvuru hedef, Basvuru kaynak)
        //{
        //}

        //private static void Asama5Kopyala(Basvuru hedef, Basvuru kaynak)
        //{
        //    hedef.ToplamYatirimTutari = kaynak.ToplamYatirimTutari;
        //    hedef.UygunHarcamaTutari = kaynak.UygunHarcamaTutari;
        //    hedef.TalepEdilenDestekTutari = kaynak.TalepEdilenDestekTutari;
        //    hedef.BasvuruSahibiKatkisi = kaynak.BasvuruSahibiKatkisi;
        //    hedef.DestekOrani = kaynak.DestekOrani;
        //    hedef.YatiriminAmaci = kaynak.YatiriminAmaci;
        //}

        //private static void Asama6Kopyala(Basvuru hedef, Basvuru kaynak)
        //{
        //    hedef.OncekiYilNetSatis = kaynak.OncekiYilNetSatis;
        //    hedef.SonYilNetSatis = kaynak.SonYilNetSatis;
        //    hedef.OncekiYilAktifToplami = kaynak.OncekiYilAktifToplami;
        //    hedef.SonYilAktifToplami = kaynak.SonYilAktifToplami;
        //}

        //private static void Asama7Kopyala(Basvuru hedef, Basvuru kaynak)
        //{
        //    hedef.BelgePaketiDosyaAdi = kaynak.BelgePaketiDosyaAdi;
        //    hedef.TaahhutDosyaAdi = kaynak.TaahhutDosyaAdi;
        //    hedef.BelgeBeyani = kaynak.BelgeBeyani;
        //    hedef.BelgeGruplari = kaynak.BelgeGruplari;
        //}

        //private static object BasvuruLogDetayiOlustur(Basvuru basvuru, string logIslem)
        //{
        //    return logIslem switch
        //    {
        //        "YeniKayit" or "Asama1Update" => basvuru.BasvuruFirma,
        //        "Asama2Update" => new BasvuruIletisim
        //        {
        //            BasvuruId = basvuru.Id,
        //            Telefon = basvuru.Telefon,
        //            IrtibatKisisi = basvuru.IrtibatKisisi,
        //            IrtibatUnvani = basvuru.IrtibatUnvani,
        //            IrtibatTelefon = basvuru.IrtibatTelefon,
        //            IrtibatEposta = basvuru.IrtibatEposta,
        //            IletisimAdresi = basvuru.IletisimAdresi,
        //            YetkiliKisiler = basvuru.YetkiliKisiler
        //        },
        //        "Asama3Update" => new BasvuruYatirim
        //        {
        //            BasvuruId = basvuru.Id,
        //            YatirimAdi = basvuru.YatirimAdi,
        //            YatirimTuru = basvuru.YatirimTuru,
        //            DegerZinciriId = basvuru.DegerZinciriId,
        //            DegerZinciriAsamalari = basvuru.DegerZinciriAsamalari,
        //            HarcamaTurleri = basvuru.HarcamaTurleri
        //        },
        //        "Asama4Update" => new BasvuruYatirimAdresBilgisi
        //        {
        //            BasvuruId = basvuru.Id,
        //            YatirimAdresleri = basvuru.YatirimAdresleri
        //        },
        //        "Asama5Update" => new BasvuruFinans
        //        {
        //            BasvuruId = basvuru.Id,
        //            ToplamYatirimTutari = basvuru.ToplamYatirimTutari,
        //            UygunHarcamaTutari = basvuru.UygunHarcamaTutari,
        //            TalepEdilenDestekTutari = basvuru.TalepEdilenDestekTutari,
        //            BasvuruSahibiKatkisi = basvuru.BasvuruSahibiKatkisi,
        //            DestekOrani = basvuru.DestekOrani,
        //            YatiriminAmaci = basvuru.YatiriminAmaci
        //        },
        //        "Asama6Update" => new BasvuruMali
        //        {
        //            BasvuruId = basvuru.Id,
        //            OncekiYilNetSatis = basvuru.OncekiYilNetSatis,
        //            SonYilNetSatis = basvuru.SonYilNetSatis,
        //            OncekiYilAktifToplami = basvuru.OncekiYilAktifToplami,
        //            SonYilAktifToplami = basvuru.SonYilAktifToplami
        //        },
        //        "Asama7Update" => new BasvuruBelge
        //        {
        //            BasvuruId = basvuru.Id,
        //            BelgePaketiDosyaAdi = basvuru.BelgePaketiDosyaAdi,
        //            TaahhutDosyaAdi = basvuru.TaahhutDosyaAdi,
        //            BelgeBeyani = basvuru.BelgeBeyani,
        //            BelgeGruplari = basvuru.BelgeGruplari
        //        },
        //        _ => basvuru
        //    };
        //}

        private static void SonucHatalariniAktar(Sonuc kaynak, Sonuc hedef)
        {
            foreach (string hata in kaynak.hatalar)
            {
                hedef.HataEkle(hata);
            }
        }

        private void BelgePaketiBeyaniDogrula(string belgeBeyani, List<string> belgeGruplari, List<string> seciliBelgeGruplari, Sonuc sonuc)
        {
            if (!string.Equals(belgeBeyani, "Evet", StringComparison.OrdinalIgnoreCase))
                HataEkle(sonuc, "Business.Documents.PackageDeclarationRequired");

            List<string> zorunluGruplar = ResourceListesi("Basvuru.Options.RequiredDocumentGroups");
            if (zorunluGruplar.Count == 0)
            {
                HataEkle(sonuc, "Business.Documents.RequiredGroupsUndefined");
                return;
            }

            if (belgeGruplari.Count == 0)
            {
                HataEkle(sonuc, "Business.Documents.RequiredGroupsMustBeSent");
                return;
            }

            bool tumZorunluGruplarEkrandaVar = zorunluGruplar.All(zorunlu => belgeGruplari.Contains(zorunlu, StringComparer.OrdinalIgnoreCase));
            bool tumEkranGruplariSecili = belgeGruplari.All(grup => seciliBelgeGruplari.Contains(grup, StringComparer.OrdinalIgnoreCase));
            bool tumZorunluGruplarSecili = zorunluGruplar.All(zorunlu => seciliBelgeGruplari.Contains(zorunlu, StringComparer.OrdinalIgnoreCase));

            if (!tumZorunluGruplarEkrandaVar || !tumEkranGruplariSecili || !tumZorunluGruplarSecili)
                HataEkle(sonuc, "Business.Documents.RequiredGroupsMustBeChecked");
        }

        private List<string> ResourceListesi(string key)
        {
            string deger = _localizer[key].Value;
            if (string.IsNullOrWhiteSpace(deger) || string.Equals(deger, key, StringComparison.Ordinal))
                return new List<string>();

            return deger
                .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static List<string> TemizListe(IEnumerable<string>? liste)
        {
            return (liste ?? [])
                .Select(x => x?.Trim() ?? "")
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static DosyaKaydetModel BasvuruDosyaModeliOlustur(int basvuruId, int dosyaNo, string dosyaAdi, byte[] icerik, string aciklama)
        {
            return BasvuruDosyaModeliOlustur(basvuruId, BasvuruZorunluBelgelerFormAd, dosyaNo, dosyaAdi, icerik, aciklama);
        }

        private static DosyaKaydetModel BasvuruDosyaModeliOlustur(int basvuruId, string formAd, int dosyaNo, string dosyaAdi, byte[] icerik, string aciklama)
        {
            DosyaAnahtari anahtar = BasvuruDosyaAnahtariOlustur(basvuruId, dosyaNo);
            anahtar.FormAd = formAd;

            return new DosyaKaydetModel
            {
                ModulKod = anahtar.ModulKod,
                FormAd = anahtar.FormAd,
                FormAnahtar = anahtar.FormAnahtar,
                DosyaNo = anahtar.DosyaNo,
                DosyaAdi = dosyaAdi,
                Icerik = icerik,
                Aciklama = aciklama
            };
        }

        private async Task BasvuruDosyaListeleriniYukleAsync(Basvuru basvuru)
        {
            if (basvuru.Id <= 0)
                return;

            basvuru.ZorunluBelgeler = await BasvuruDosyaListesiOlusturAsync(basvuru.Id, BasvuruZorunluBelgeFormAd, ZorunluBelgeTurleri);
            basvuru.ortaklik.bagliOrtakDosyalari = await BasvuruDosyaListesiOlusturAsync(basvuru.Id, BasvuruBagliBelgeFormAd, BagliOrtakDosyaTurleri);
        }

        private async Task<List<BasvuruOrtaklikDosya>> BasvuruDosyaListesiOlusturAsync(int basvuruId, string formAd, IReadOnlyDictionary<int, string> dosyaTurleri)
        {
            Sonuc<List<DosyaBilgisi>> dosyaSonuc = await _dosyaYonetimIsKurallari.DosyaListeleAsync(
                "Basvuru",
                new BasvuruDosyaYetkiKontrol(basvuruId),
                formAd,
                basvuruId.ToString());

            List<DosyaBilgisi> dosyalar = dosyaSonuc.basarili && dosyaSonuc.nesne != null
                ? dosyaSonuc.nesne
                : new List<DosyaBilgisi>();

            return dosyaTurleri
                .Select(tur =>
                {
                    DosyaBilgisi? dosya = dosyalar.FirstOrDefault(x => x.DosyaNo == tur.Key);
                    return new BasvuruOrtaklikDosya
                    {
                        dosyaNo = tur.Key,
                        dosyaTuru = Metin(tur.Value),
                        dosyaId = dosya?.Id,
                        dosyaAdi = dosya?.DosyaAdi ?? ""
                    };
                })
                .ToList();
        }

        private string BasvuruDosyaTuruBul(string formAd, int dosyaNo)
        {
            if (string.Equals(formAd, BasvuruZorunluBelgeFormAd, StringComparison.OrdinalIgnoreCase)
                && ZorunluBelgeTurleri.TryGetValue(dosyaNo, out string? zorunluBelgeTuru))
                return Metin(zorunluBelgeTuru);

            if (string.Equals(formAd, BasvuruBagliBelgeFormAd, StringComparison.OrdinalIgnoreCase)
                && BagliOrtakDosyaTurleri.TryGetValue(dosyaNo, out string? dosyaTuru))
                return Metin(dosyaTuru);

            if (string.Equals(formAd, BasvuruAdliSicilFormAd, StringComparison.OrdinalIgnoreCase) && dosyaNo > 0)
                return Metin("Basvuru.Criminal.FileColumn");

            if (string.Equals(formAd, BasvuruOrtakUboKycFormAd, StringComparison.OrdinalIgnoreCase) && dosyaNo > 0)
                return Metin("Business.Application.UboKycDocument");

            if (UboKycFormAdMi(formAd) && dosyaNo == 1)
                return Metin("Business.Application.UboKycDocument");

            return "";
        }

        private static bool UboKycFormAdMi(string? formAd)
        {
            return !string.IsNullOrWhiteSpace(formAd)
                && formAd.StartsWith(BasvuruOrtakUboKycFormAdPrefix, StringComparison.OrdinalIgnoreCase)
                && formAd.Length > BasvuruOrtakUboKycFormAdPrefix.Length;
        }

        private static string UboKycFormAdKimlikOku(string formAd)
        {
            return UboKycFormAdMi(formAd)
                ? formAd[BasvuruOrtakUboKycFormAdPrefix.Length..].Trim()
                : "";
        }

        private static async Task<bool> BasvuruOrtakKimlikVarMiAsync(SqlConnection connection, int basvuruId, string tcknVkn)
        {
            const string sql = @"
                SELECT COUNT(1)
                FROM dbo.BasvuruOrtaklar
                WHERE BasvuruId = @BasvuruId
                    AND TcknVkn = @TcknVkn;";

            await using SqlCommand command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@BasvuruId", basvuruId);
            command.Parameters.AddWithValue("@TcknVkn", tcknVkn?.Trim() ?? "");
            object? sonuc = await command.ExecuteScalarAsync();
            return Convert.ToInt32(sonuc) > 0;
        }

        private static async Task<bool> BasvuruAdliSicilKisiVarMiAsync(SqlConnection connection, int basvuruId, int kisiId)
        {
            const string sql = @"
                SELECT COUNT(1)
                FROM dbo.BasvuruAdliSicilKisiler
                WHERE BasvuruId = @BasvuruId
                    AND Id = @Id;";

            await using SqlCommand command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@BasvuruId", basvuruId);
            command.Parameters.AddWithValue("@Id", kisiId);
            object? sonuc = await command.ExecuteScalarAsync();
            return Convert.ToInt32(sonuc) > 0;
        }

        private static string TcknVknNormalizeEt(string? tcknVkn)
        {
            return new string((tcknVkn ?? "")
                .Trim()
                .Where(char.IsLetterOrDigit)
                .Select(char.ToUpperInvariant)
                .ToArray());
        }

        private static int BasvuruIdDosyaFormAnahtarindanOku(string? formAnahtar)
        {
            return int.TryParse(formAnahtar, out int basvuruId) ? basvuruId : 0;
        }

        private static DosyaAnahtari BasvuruDosyaAnahtariOlustur(int basvuruId, int dosyaNo)
        {
            return new DosyaAnahtari
            {
                ModulKod = "Basvuru",
                FormAd = BasvuruZorunluBelgelerFormAd,
                FormAnahtar = basvuruId.ToString(),
                DosyaNo = dosyaNo
            };
        }

        private async Task<(int BasvuruId, int DosyaNo)> BasvuruDosyaAnahtariBulAsync(SqlConnection connection, int dosyaId)
        {
            const string sql = @"
                SELECT TOP 1
                    Id,
                    CASE
                        WHEN BelgePaketiDosyaId = @DosyaId THEN 1
                        WHEN TaahhutDosyaId = @DosyaId THEN 2
                        WHEN DenetimDosyaId = @DosyaId THEN 3
                        ELSE 0
                    END AS DosyaNo
                FROM dbo.Basvuru
                WHERE BelgePaketiDosyaId = @DosyaId
                   OR TaahhutDosyaId = @DosyaId
                   OR DenetimDosyaId = @DosyaId;";

            await using SqlCommand command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@DosyaId", dosyaId);

            await using SqlDataReader reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return (0, 0);

            return (reader.GetInt32(0), reader.GetInt32(1));
        }

        private sealed class BasvuruDosyaYetkiKontrol : IDosyaYetkiKontrol
        {
            private readonly string _formAnahtar;

            public BasvuruDosyaYetkiKontrol(int basvuruId)
            {
                _formAnahtar = basvuruId.ToString();
            }

            public Task<bool> GorebilirAsync(string modulKod, string? formAd, string? formAnahtar, int? dosyaNo)
            {
                return Task.FromResult(AnahtarUygunMu(modulKod, formAd, formAnahtar));
            }

            public Task<bool> EkleyebilirAsync(string modulKod, string formAd, string formAnahtar)
            {
                return Task.FromResult(AnahtarUygunMu(modulKod, formAd, formAnahtar));
            }

            public Task<bool> GuncelleyebilirAsync(string modulKod, string formAd, string formAnahtar, int dosyaNo)
            {
                return Task.FromResult(AnahtarUygunMu(modulKod, formAd, formAnahtar) && dosyaNo > 0);
            }

            public Task<bool> SilebilirAsync(string modulKod, string formAd, string formAnahtar, int dosyaNo)
            {
                return Task.FromResult(false);
            }

            private bool AnahtarUygunMu(string modulKod, string? formAd, string? formAnahtar)
            {
                return string.Equals(modulKod, "Basvuru", StringComparison.OrdinalIgnoreCase)
                    && BasvuruFormAdGecerliMi(formAd)
                    && string.Equals(formAnahtar, _formAnahtar, StringComparison.Ordinal);
            }
        }

        private sealed class BasvuruDosyaIndirmeYetkiKontrol : IDosyaYetkiKontrol
        {
            public Task<bool> GorebilirAsync(string modulKod, string? formAd, string? formAnahtar, int? dosyaNo)
            {
                return Task.FromResult(string.Equals(modulKod, "Basvuru", StringComparison.OrdinalIgnoreCase)
                    && BasvuruFormAdGecerliMi(formAd)
                    && int.TryParse(formAnahtar, out _)
                    && dosyaNo.GetValueOrDefault() > 0);
            }

            public Task<bool> EkleyebilirAsync(string modulKod, string formAd, string formAnahtar)
            {
                return Task.FromResult(false);
            }

            public Task<bool> GuncelleyebilirAsync(string modulKod, string formAd, string formAnahtar, int dosyaNo)
            {
                return Task.FromResult(false);
            }

            public Task<bool> SilebilirAsync(string modulKod, string formAd, string formAnahtar, int dosyaNo)
            {
                return Task.FromResult(false);
            }
        }

        private static bool BasvuruFormAdGecerliMi(string? formAd)
        {
            return string.Equals(formAd, BasvuruZorunluBelgelerFormAd, StringComparison.OrdinalIgnoreCase)
                || string.Equals(formAd, BasvuruZorunluBelgeFormAd, StringComparison.OrdinalIgnoreCase)
                || string.Equals(formAd, BasvuruBagliBelgeFormAd, StringComparison.OrdinalIgnoreCase)
                || string.Equals(formAd, BasvuruAdliSicilFormAd, StringComparison.OrdinalIgnoreCase)
                || string.Equals(formAd, BasvuruOrtakUboKycFormAd, StringComparison.OrdinalIgnoreCase)
                || UboKycFormAdMi(formAd);
        }

        private static void UygulamaAdresiNormalizeEt(BasvuruUygulamaAdresi adres)
        {
            adres.siraNo = adres.siraNo <= 0 ? 1 : adres.siraNo;
            adres.ilceId = adres.ilceId.GetValueOrDefault() > 0 ? adres.ilceId : null;
            adres.tamAdres = adres.tamAdres?.Trim() ?? "";
            adres.kiraVeyaTahsisSuresi = adres.kiraVeyaTahsisSuresi.GetValueOrDefault() > 0 ? adres.kiraVeyaTahsisSuresi : null;
        }


        private void BeklenmeyenHata(Sonuc sonuc, Exception ex, string logMesaji, string kullaniciMesaji, params object[] logParametreleri)
        {
            _logger.LogError(ex, logMesaji, logParametreleri);
            HataEkle(sonuc, kullaniciMesaji);
        }

        private void HataEkle(Sonuc sonuc, string key)
        {
            sonuc.HataEkle(Metin(key));
        }

        private void HataEkle(Sonuc sonuc, string key, params object[] args)
        {
            sonuc.HataEkle(string.Format(Metin(key), args));
        }

        private string Metin(string key)
        {
            string value = _localizer[key].Value;
            return string.IsNullOrWhiteSpace(value) || string.Equals(value, key, StringComparison.Ordinal) ? key : value;
        }
    }
}

