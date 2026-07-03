using Microsoft.Data.SqlClient;
using TarimDonusum.Models;
using TarimDonusum.Tablolar;

namespace TarimDonusum.IsKurallari
{
    public class BasvuruIsKurallari
    {
        private readonly string _connectionString;
        private readonly ILogger<BasvuruIsKurallari> _logger;

        public BasvuruIsKurallari(IConfiguration configuration, ILogger<BasvuruIsKurallari> logger)
        {
            _logger = logger;
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        }

        public async Task<Sonuc<List<Basvuru>>> KullaniciBasvurulariniListeleAsync(int kullaniciId)
        {
            Sonuc<List<Basvuru>> sonuc = new Sonuc<List<Basvuru>>();

            try
            {
                await using SqlConnection connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                TABBasvuru tabBasvuru = new TABBasvuru(connection);
                sonuc.Nesne = await tabBasvuru.KullaniciBasvurulariniListeleAsync(kullaniciId);
            }
            catch (Exception ex)
            {
                BeklenmeyenHata(sonuc, ex, "Başvuru kayıtları listelenemedi. KullaniciId: {KullaniciId}", "Başvuru kayıtları listelenemedi.", kullaniciId);
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

                TABBasvuru tabBasvuru = new TABBasvuru(connection);
                sonuc.Nesne = await tabBasvuru.TumunuListeleAsync();
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
                sonuc.Nesne = await tabDonem.ListeleAsync();
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

                TABIl tabIl = new TABIl(connection);
                sonuc.Nesne = await tabIl.ListeleAsync();
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
                sonuc.Nesne = await tabIlce.ListeleAsync(ilId);
            }
            catch (Exception ex)
            {
                BeklenmeyenHata(sonuc, ex, "İlçe listesi okunamadı. IlId: {IlId}", "İlçe listesi okunamadı.", ilId.GetValueOrDefault());
            }

            return sonuc;
        }

        public async Task<Sonuc<List<DegerZinciri>>> DegerZincirleriListeleAsync(int? ilId, bool asamalariYukle = true)
        {
            Sonuc<List<DegerZinciri>> sonuc = new Sonuc<List<DegerZinciri>>();

            try
            {
                await using SqlConnection connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                int? ilKod = null;
                if (ilId.HasValue && ilId.Value > 0)
                {
                    TABIl tabIl = new TABIl(connection);
                    Il? il = await tabIl.OkuAsync(ilId.Value);
                    ilKod = il?.Kod;
                }

                TABDegerZinciri tabDegerZinciri = new TABDegerZinciri(connection);
                sonuc.Nesne = await tabDegerZinciri.ListeleAsync(true, ilKod, asamalariYukle);
            }
            catch (Exception ex)
            {
                BeklenmeyenHata(sonuc, ex, "Değer zincirleri listelenemedi. IlId: {IlId}", "Değer zincirleri listelenemedi.", ilId.GetValueOrDefault());
            }

            return sonuc;
        }

        public async Task<Sonuc<List<DegerZinciriAsama>>> DegerZinciriAsamalariListeleAsync(int degerZinciriId)
        {
            Sonuc<List<DegerZinciriAsama>> sonuc = new Sonuc<List<DegerZinciriAsama>>();

            try
            {
                if (degerZinciriId <= 0)
                {
                    sonuc.Nesne = new List<DegerZinciriAsama>();
                    return sonuc;
                }

                await using SqlConnection connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                TABDegerZinciri tabDegerZinciri = new TABDegerZinciri(connection);
                DegerZinciri? degerZinciri = await tabDegerZinciri.OkuAsync(degerZinciriId);
                sonuc.Nesne = degerZinciri?.Asamalar ?? new List<DegerZinciriAsama>();
            }
            catch (Exception ex)
            {
                BeklenmeyenHata(sonuc, ex, "Değer zinciri aşamaları listelenemedi. DegerZinciriId: {DegerZinciriId}", "Değer zinciri aşamaları listelenemedi.", degerZinciriId);
            }

            return sonuc;
        }

        public async Task<Sonuc<Basvuru>> OkuAsync(int basvuruId, int? kullaniciId = null)
        {
            Sonuc<Basvuru> sonuc = new Sonuc<Basvuru>();

            try
            {
                await using SqlConnection connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                TABBasvuru tabBasvuru = new TABBasvuru(connection);
                Basvuru? basvuru = await tabBasvuru.OkuAsync(basvuruId);

                if (basvuru == null)
                {
                    sonuc.HataEkle("Başvuru bulunamadı.");
                    return sonuc;
                }

                if (kullaniciId.HasValue && basvuru.KullaniciId != kullaniciId.Value)
                {
                    sonuc.HataEkle("Bu başvuruyu görüntüleme yetkiniz yok.");
                    return sonuc;
                }

                sonuc.Nesne = basvuru;
                if (basvuru.FirmaId.HasValue)
                {
                    TABFirma tabFirma = new TABFirma(connection);
                    basvuru.Firma = await tabFirma.OkuAsync(basvuru.FirmaId.Value);
                }

                if (basvuru.DonemId.HasValue)
                {
                    TABDonem tabDonem = new TABDonem(connection);
                    basvuru.Donem = await tabDonem.OkuAsync(basvuru.DonemId.Value);
                }

                if (basvuru.IlId.HasValue)
                {
                    TABIl tabIl = new TABIl(connection);
                    basvuru.Il = await tabIl.OkuAsync(basvuru.IlId.Value);
                }
            }
            catch (Exception ex)
            {
                BeklenmeyenHata(sonuc, ex, "Başvuru okunamadı. BasvuruId: {BasvuruId}", "Başvuru kaydı okunamadı.", basvuruId);
            }

            return sonuc;
        }

        public async Task<Sonuc<Firma>> FirmaVergiNoIleOkuAsync(string vergiKimlikNo, int kullaniciId)
        {
            Sonuc<Firma> sonuc = new Sonuc<Firma>();
            vergiKimlikNo = vergiKimlikNo?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(vergiKimlikNo))
            {
                sonuc.HataEkle("Vergi kimlik no girilmelidir.");
                return sonuc;
            }

            try
            {
                await using SqlConnection connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                TABFirma tabFirma = new TABFirma(connection);
                Firma? firma = await tabFirma.VergiKimlikNoIleOkuAsync(vergiKimlikNo);
                if (firma == null)
                    return sonuc;

                TABFirmaKullanici tabFirmaKullanici = new TABFirmaKullanici(connection);
                if (!await tabFirmaKullanici.IliskiVarMiAsync(firma.Id, kullaniciId))
                {
                    sonuc.HataEkle("Bu firma kullanıcı ile ilişkili değil.");
                    return sonuc;
                }

                sonuc.Nesne = firma;
            }
            catch (Exception ex)
            {
                BeklenmeyenHata(sonuc, ex, "Firma sorgulanamadı. KullaniciId: {KullaniciId}, VergiKimlikNo: {VergiKimlikNo}", "Firma sorgulanamadı.", kullaniciId, vergiKimlikNo);
            }

            return sonuc;
        }

        public async Task<Sonuc<int>> FirmaEkleAsync(Firma firma, int kullaniciId)
        {
            Sonuc<int> sonuc = new Sonuc<int>();

            try
            {
                FirmaNormalizeEt(firma);
                firma.KullaniciId = kullaniciId;

                if (string.IsNullOrWhiteSpace(firma.VergiKimlikNo))
                    sonuc.HataEkle("Vergi kimlik no girilmelidir.");

                if (string.IsNullOrWhiteSpace(firma.TicaretUnvani))
                    sonuc.HataEkle("Firma adı girilmelidir.");

                if (!sonuc.Basarili)
                    return sonuc;

                await using SqlConnection connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                TABFirma tabFirma = new TABFirma(connection);
                Firma? mevcut = await tabFirma.VergiKimlikNoIleOkuAsync(firma.VergiKimlikNo);
                if (mevcut != null)
                {
                    TABFirmaKullanici mevcutIliskiTablosu = new TABFirmaKullanici(connection);
                    if (await mevcutIliskiTablosu.IliskiVarMiAsync(mevcut.Id, kullaniciId))
                        sonuc.Nesne = mevcut.Id;
                    else
                        sonuc.HataEkle("Firma sistemde var fakat kullanıcı ile ilişkili değil.");

                    return sonuc;
                }

                await using SqlTransaction transaction = (SqlTransaction)await connection.BeginTransactionAsync();
                try
                {
                    TABFirma txFirma = new TABFirma(connection, transaction);
                    sonuc.Nesne = await txFirma.EkleAsync(firma);

                    TABFirmaKullanici tabFirmaKullanici = new TABFirmaKullanici(connection, transaction);
                    await tabFirmaKullanici.EkleYoksaAsync(new FirmaKullanici
                    {
                        FirmaId = firma.Id,
                        KullaniciId = kullaniciId,
                        Aktif = true,
                        IliskiTarihi = DateTime.Now,
                        IliskiyiKuranKullaniciId = kullaniciId
                    });

                    TABFirmaLog tabFirmaLog = new TABFirmaLog(connection, transaction);
                    await tabFirmaLog.EkleAsync(firma, "YeniKayit");

                    await transaction.CommitAsync();
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Firma kaydedilemedi. KullaniciId: {KullaniciId}, VergiKimlikNo: {VergiKimlikNo}", kullaniciId, firma.VergiKimlikNo);
                    sonuc.HataEkle("Firma kaydedilemedi.");
                }
            }
            catch (Exception ex)
            {
                BeklenmeyenHata(sonuc, ex, "Firma kaydetme işlemi tamamlanamadı. KullaniciId: {KullaniciId}", "Firma kaydedilemedi.", kullaniciId);
            }

            return sonuc;
        }

        public async Task<Sonuc<Firma>> FirmaGuncelleAsync(Firma firma, int kullaniciId)
        {
            Sonuc<Firma> sonuc = new Sonuc<Firma>();
            try
            {
            FirmaNormalizeEt(firma);

            if (firma.Id <= 0)
                sonuc.HataEkle("Firma seçilmelidir.");

            if (string.IsNullOrWhiteSpace(firma.VergiKimlikNo))
                sonuc.HataEkle("Vergi kimlik no girilmelidir.");

            if (string.IsNullOrWhiteSpace(firma.TicaretUnvani))
                sonuc.HataEkle("Firma adı girilmelidir.");

            if (!sonuc.Basarili)
                return sonuc;

            await using SqlConnection connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            TABFirma tabFirma = new TABFirma(connection);
            Firma? mevcut = await tabFirma.OkuAsync(firma.Id);
            if (mevcut == null)
            {
                sonuc.HataEkle("Firma bulunamadı.");
                return sonuc;
            }

            TABFirmaKullanici tabFirmaKullanici = new TABFirmaKullanici(connection);
            if (!await tabFirmaKullanici.IliskiVarMiAsync(firma.Id, kullaniciId))
            {
                sonuc.HataEkle("Bu firma kullanıcı ile ilişkili değil.");
                return sonuc;
            }

            Firma? ayniVergiNo = await tabFirma.VergiKimlikNoIleOkuAsync(firma.VergiKimlikNo);
            if (ayniVergiNo != null && ayniVergiNo.Id != firma.Id)
            {
                sonuc.HataEkle("Bu vergi kimlik no başka bir firma kaydında kullanılıyor.");
                return sonuc;
            }

            firma.KullaniciId = mevcut.KullaniciId;

            await using SqlTransaction transaction = (SqlTransaction)await connection.BeginTransactionAsync();
            try
            {
                TABFirma txFirma = new TABFirma(connection, transaction);
                await txFirma.GuncelleAsync(firma);

                TABFirmaLog tabFirmaLog = new TABFirmaLog(connection, transaction);
                await tabFirmaLog.EkleAsync(firma, "Update");

                await transaction.CommitAsync();
                sonuc.Nesne = firma;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Firma güncellenemedi. FirmaId: {FirmaId}, KullaniciId: {KullaniciId}", firma.Id, kullaniciId);
                sonuc.HataEkle("Firma güncellenemedi.");
            }
            }
            catch (Exception ex)
            {
                BeklenmeyenHata(sonuc, ex, "Firma güncelleme işlemi tamamlanamadı. FirmaId: {FirmaId}, KullaniciId: {KullaniciId}", "Firma güncellenemedi.", firma.Id, kullaniciId);
            }

            return sonuc;
        }

        public async Task<Sonuc<int>> KaydetAsync(Basvuru basvuru, int kullaniciId)
        {
            return basvuru.AktifBolum switch
            {
                1 => await KaydetAsama1Async(basvuru, kullaniciId),
                2 => await KaydetAsama2Async(basvuru, kullaniciId),
                3 => await KaydetAsama3Async(basvuru, kullaniciId),
                4 => await KaydetAsama4Async(basvuru, kullaniciId),
                5 => await KaydetAsama5Async(basvuru, kullaniciId),
                6 => await KaydetAsama6Async(basvuru, kullaniciId),
                7 => await KaydetAsama7Async(basvuru, kullaniciId),
                _ => await KaydetAsama1Async(basvuru, kullaniciId)
            };
        }

        public async Task<Sonuc<int>> KaydetAsama1Async(Basvuru basvuru, int kullaniciId)
        {
            Sonuc<int> sonuc = new Sonuc<int>();
            try
            {
                basvuru.KullaniciId = kullaniciId;
                basvuru.AktifBolum = 1;
                BasvuruNormalizeEt(basvuru);
                SonucHatalariniAktar(basvuru.IlkBolumDogrula(new Sonuc()), sonuc);

                if (!sonuc.Basarili)
                    return sonuc;

                await using SqlConnection connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                Basvuru? mevcut = null;
                if (basvuru.Id > 0)
                {
                    mevcut = await BasvuruOnBasvuruYetkiKontrolAsync(connection, basvuru.Id, kullaniciId, sonuc);
                    if (!sonuc.Basarili || mevcut == null)
                        return sonuc;

                    Asama1Kopyala(mevcut, basvuru);
                    basvuru = mevcut;
                }

                await BasvuruReferanslariniDogrulaVeYukleAsync(connection, basvuru, kullaniciId, sonuc);
                if (!sonuc.Basarili)
                    return sonuc;

                sonuc.Nesne = await BasvuruAsama1KaydetVeLoglaAsync(connection, basvuru, mevcut == null ? "YeniKayit" : "Asama1Update");
            }
            catch (Exception ex)
            {
                BeklenmeyenHata(sonuc, ex, "Başvuru birinci aşama kaydedilemedi. BasvuruId: {BasvuruId}, KullaniciId: {KullaniciId}", "Başvuru kaydedilemedi.", basvuru.Id, kullaniciId);
            }

            return sonuc;
        }

        public async Task<Sonuc<int>> KaydetAsama2Async(Basvuru basvuru, int kullaniciId)
        {
            return await KaydetMevcutAsamaAsync(basvuru, kullaniciId, 2, Asama2Kopyala, null, "Asama2Update");
        }

        public async Task<Sonuc<int>> KaydetAsama3Async(Basvuru basvuru, int kullaniciId)
        {
            return await KaydetMevcutAsamaAsync(basvuru, kullaniciId, 3, Asama3Kopyala, YatirimBolumuDogrula, "Asama3Update");
        }

        public async Task<Sonuc<int>> KaydetAsama4Async(Basvuru basvuru, int kullaniciId)
        {
            return await KaydetMevcutAsamaAsync(basvuru, kullaniciId, 4, Asama4Kopyala, null, "Asama4Update");
        }

        public async Task<Sonuc<int>> KaydetAsama5Async(Basvuru basvuru, int kullaniciId)
        {
            return await KaydetMevcutAsamaAsync(basvuru, kullaniciId, 5, Asama5Kopyala, null, "Asama5Update");
        }

        public async Task<Sonuc<int>> KaydetAsama6Async(Basvuru basvuru, int kullaniciId)
        {
            return await KaydetMevcutAsamaAsync(basvuru, kullaniciId, 6, Asama6Kopyala, null, "Asama6Update");
        }

        public async Task<Sonuc<int>> KaydetAsama7Async(Basvuru basvuru, int kullaniciId)
        {
            return await KaydetMevcutAsamaAsync(basvuru, kullaniciId, 7, Asama7Kopyala, null, "Asama7Update");
        }

        public async Task<Sonuc<BasvuruUygulamaAdresi>> UygulamaAdresiKaydetAsync(BasvuruUygulamaAdresi adres, int kullaniciId)
        {
            Sonuc<BasvuruUygulamaAdresi> sonuc = new Sonuc<BasvuruUygulamaAdresi>();

            try
            {
                UygulamaAdresiNormalizeEt(adres);
                UygulamaAdresiDogrula(adres, sonuc);
                if (!sonuc.Basarili)
                    return sonuc;

                await using SqlConnection connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                Basvuru? mevcut = await BasvuruOnBasvuruYetkiKontrolAsync(connection, adres.BasvuruId, kullaniciId, sonuc);
                if (!sonuc.Basarili || mevcut == null)
                    return sonuc;

                TABIlce tabIlce = new TABIlce(connection);
                Ilce? ilce = adres.IlceId.HasValue ? await tabIlce.OkuAsync(adres.IlceId.Value) : null;
                if (ilce == null || !ilce.Aktif || ilce.IlId != mevcut.IlId)
                {
                    sonuc.HataEkle("Seçilen ilçe başvuru iline ait değil veya pasif.");
                    return sonuc;
                }

                if (adres.Id > 0)
                {
                    TABBasvuru kontrolTablosu = new TABBasvuru(connection);
                    BasvuruUygulamaAdresi? eskiAdres = await kontrolTablosu.UygulamaAdresiOkuAsync(adres.BasvuruId, adres.Id);
                    if (eskiAdres == null)
                    {
                        sonuc.HataEkle("Adres kaydı bulunamadı.");
                        return sonuc;
                    }
                }

                bool yeniKayit = adres.Id <= 0;
                await using SqlTransaction transaction = (SqlTransaction)await connection.BeginTransactionAsync();
                try
                {
                    TABBasvuru tabBasvuru = new TABBasvuru(connection, transaction);
                    int adresId = await tabBasvuru.UygulamaAdresiKaydetAsync(adres);

                    BasvuruUygulamaAdresi? kayitliAdres = await tabBasvuru.UygulamaAdresiOkuAsync(adres.BasvuruId, adresId);
                    mevcut.YatirimAdresleri = (await new TABBasvuru(connection, transaction).OkuAsync(adres.BasvuruId))?.YatirimAdresleri ?? mevcut.YatirimAdresleri;

                    TABBasvuruLog tabBasvuruLog = new TABBasvuruLog(connection, transaction);
                    await tabBasvuruLog.EkleAsync(
                        mevcut,
                        yeniKayit ? "UygulamaAdresiYeniKayit" : "UygulamaAdresiUpdate",
                        kayitliAdres ?? adres);

                    await transaction.CommitAsync();
                    sonuc.Nesne = kayitliAdres ?? adres;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Uygulama adresi kaydedilemedi. BasvuruId: {BasvuruId}, AdresId: {AdresId}, KullaniciId: {KullaniciId}", adres.BasvuruId, adres.Id, kullaniciId);
                    sonuc.HataEkle("Uygulama adresi kaydedilemedi.");
                }
            }
            catch (Exception ex)
            {
                BeklenmeyenHata(sonuc, ex, "Uygulama adresi kaydedilemedi. BasvuruId: {BasvuruId}, KullaniciId: {KullaniciId}", "Uygulama adresi kaydedilemedi.", adres.BasvuruId, kullaniciId);
            }

            return sonuc;
        }

        public async Task<Sonuc> UygulamaAdresiSilAsync(int basvuruId, int adresId, int kullaniciId)
        {
            Sonuc sonuc = new Sonuc();

            try
            {
                if (basvuruId <= 0 || adresId <= 0)
                {
                    sonuc.HataEkle("Adres kaydı seçilmelidir.");
                    return sonuc;
                }

                await using SqlConnection connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                Basvuru? mevcut = await BasvuruOnBasvuruYetkiKontrolAsync(connection, basvuruId, kullaniciId, sonuc);
                if (!sonuc.Basarili || mevcut == null)
                    return sonuc;

                TABBasvuru kontrolTablosu = new TABBasvuru(connection);
                BasvuruUygulamaAdresi? eskiAdres = await kontrolTablosu.UygulamaAdresiOkuAsync(basvuruId, adresId);
                if (eskiAdres == null)
                {
                    sonuc.HataEkle("Adres kaydı bulunamadı.");
                    return sonuc;
                }

                await using SqlTransaction transaction = (SqlTransaction)await connection.BeginTransactionAsync();
                try
                {
                    TABBasvuru tabBasvuru = new TABBasvuru(connection, transaction);
                    await tabBasvuru.UygulamaAdresiSilAsync(basvuruId, adresId);

                    mevcut.YatirimAdresleri = (await tabBasvuru.OkuAsync(basvuruId))?.YatirimAdresleri ?? new List<BasvuruUygulamaAdresi>();
                    TABBasvuruLog tabBasvuruLog = new TABBasvuruLog(connection, transaction);
                    await tabBasvuruLog.EkleAsync(mevcut, "UygulamaAdresiSil", eskiAdres);

                    await transaction.CommitAsync();
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Uygulama adresi silinemedi. BasvuruId: {BasvuruId}, AdresId: {AdresId}, KullaniciId: {KullaniciId}", basvuruId, adresId, kullaniciId);
                    sonuc.HataEkle("Uygulama adresi silinemedi.");
                }
            }
            catch (Exception ex)
            {
                BeklenmeyenHata(sonuc, ex, "Uygulama adresi silinemedi. BasvuruId: {BasvuruId}, AdresId: {AdresId}, KullaniciId: {KullaniciId}", "Uygulama adresi silinemedi.", basvuruId, adresId, kullaniciId);
            }

            return sonuc;
        }

        private async Task<Sonuc<int>> KaydetMevcutAsamaAsync(
            Basvuru gelen,
            int kullaniciId,
            int asama,
            Action<Basvuru, Basvuru> asamaKopyala,
            Action<Basvuru, Sonuc>? asamaDogrula,
            string logIslem)
        {
            Sonuc<int> sonuc = new Sonuc<int>();

            try
            {
                if (gelen.Id <= 0)
                {
                    sonuc.HataEkle("Önce birinci aşama kaydedilmelidir.");
                    return sonuc;
                }

                gelen.AktifBolum = asama;
                BasvuruNormalizeEt(gelen);

                await using SqlConnection connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                Basvuru? mevcut = await BasvuruOnBasvuruYetkiKontrolAsync(connection, gelen.Id, kullaniciId, sonuc);
                if (!sonuc.Basarili || mevcut == null)
                    return sonuc;

                asamaKopyala(mevcut, gelen);
                mevcut.AktifBolum = asama;

                asamaDogrula?.Invoke(mevcut, sonuc);
                if (!sonuc.Basarili)
                    return sonuc;

                sonuc.Nesne = await BasvuruKaydetVeLoglaAsync(connection, mevcut, logIslem);
            }
            catch (Exception ex)
            {
                BeklenmeyenHata(sonuc, ex, "Başvuru aşaması kaydedilemedi. BasvuruId: {BasvuruId}, Asama: {Asama}, KullaniciId: {KullaniciId}", "Başvuru kaydedilemedi.", gelen.Id, asama, kullaniciId);
            }

            return sonuc;
        }

        private async Task<int> BasvuruKaydetVeLoglaAsync(SqlConnection connection, Basvuru basvuru, string logIslem)
        {
            await using SqlTransaction transaction = (SqlTransaction)await connection.BeginTransactionAsync();

            try
            {
                TABBasvuru tabBasvuru = new TABBasvuru(connection, transaction);
                int basvuruId = await tabBasvuru.KaydetAsync(basvuru);

                TABBasvuruLog tabBasvuruLog = new TABBasvuruLog(connection, transaction);
                await tabBasvuruLog.EkleAsync(basvuru, logIslem, BasvuruLogDetayiOlustur(basvuru, logIslem));

                await transaction.CommitAsync();
                return basvuruId;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Başvuru kaydedilemedi. BasvuruId: {BasvuruId}", basvuru.Id);
                throw;
            }
        }

        private async Task<int> BasvuruAsama1KaydetVeLoglaAsync(SqlConnection connection, Basvuru basvuru, string logIslem)
        {
            await using SqlTransaction transaction = (SqlTransaction)await connection.BeginTransactionAsync();

            try
            {
                TABBasvuru tabBasvuru = new TABBasvuru(connection, transaction);
                int basvuruId = await tabBasvuru.KaydetAsama1Async(basvuru);

                TABBasvuruLog tabBasvuruLog = new TABBasvuruLog(connection, transaction);
                await tabBasvuruLog.EkleAsync(basvuru, logIslem, BasvuruLogDetayiOlustur(basvuru, logIslem));

                await transaction.CommitAsync();
                return basvuruId;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Başvuru birinci aşama kaydedilemedi. BasvuruId: {BasvuruId}", basvuru.Id);
                throw;
            }
        }

        private async Task<Basvuru?> BasvuruOnBasvuruYetkiKontrolAsync(SqlConnection connection, int basvuruId, int kullaniciId, Sonuc sonuc)
        {
            TABBasvuru tabBasvuru = new TABBasvuru(connection);
            Basvuru? mevcut = await tabBasvuru.OkuAsync(basvuruId);
            if (mevcut == null || mevcut.KullaniciId != kullaniciId)
            {
                sonuc.HataEkle("Başvuru kaydı bulunamadı veya bu işlem için yetkiniz yok.");
                return null;
            }

            if (!string.Equals(mevcut.Durum, Basvuru.OnBasvuruDurumu, StringComparison.OrdinalIgnoreCase))
            {
                sonuc.HataEkle("Bu kayıt ön başvuru aşamasında olmadığı için başvuru kullanıcı ekranından güncellenemez.");
                return null;
            }

            if (mevcut.FirmaId.HasValue)
            {
                TABFirmaKullanici tabFirmaKullanici = new TABFirmaKullanici(connection);
                if (!await tabFirmaKullanici.IliskiVarMiAsync(mevcut.FirmaId.Value, kullaniciId))
                {
                    sonuc.HataEkle("Başvurunun firması ile kullanıcı ilişkili değil.");
                    return null;
                }
            }

            return mevcut;
        }

        private async Task BasvuruReferanslariniDogrulaVeYukleAsync(SqlConnection connection, Basvuru basvuru, int kullaniciId, Sonuc sonuc)
        {
            TABIl ilTablosu = new TABIl(connection);
            Il? il = basvuru.IlId.HasValue ? await ilTablosu.OkuAsync(basvuru.IlId.Value) : null;
            if (il == null || !il.Aktif)
            {
                sonuc.HataEkle("Başvuru ili bulunamadı veya pasif.");
                return;
            }

            TABFirma firmaTablosu = new TABFirma(connection);
            Firma? firma = basvuru.FirmaId.HasValue ? await firmaTablosu.OkuAsync(basvuru.FirmaId.Value) : null;

            TABFirmaKullanici firmaKullaniciTablosu = new TABFirmaKullanici(connection);
            if (firma == null || !await firmaKullaniciTablosu.IliskiVarMiAsync(firma.Id, kullaniciId))
            {
                sonuc.HataEkle("Firma bulunamadı veya bu işlem için yetkiniz yok.");
                return;
            }

            TABDonem donemTablosu = new TABDonem(connection);
            Donem? donem = basvuru.DonemId.HasValue ? await donemTablosu.OkuAsync(basvuru.DonemId.Value) : null;
            if (donem == null)
            {
                sonuc.HataEkle("Başvuru dönemi bulunamadı.");
                return;
            }

            if (!donem.SecilebilirMi())
            {
                sonuc.HataEkle("Seçilen başvuru dönemi başvuruya kapalı.");
                return;
            }

            basvuru.Firma = firma;
            basvuru.TicaretUnvani = firma.TicaretUnvani;
            basvuru.VergiKimlikNo = firma.VergiKimlikNo;
            basvuru.Donem = donem;
            basvuru.BasvuruDonemi = donem.Ad;
            basvuru.Il = il;
            basvuru.IlAdi = il.Ad;
        }

        public async Task<Sonuc<int>> OrtaklikKaydetAsync(Basvuru basvuru, int kullaniciId)
        {
            Sonuc<int> sonuc = new Sonuc<int>();

            try
            {
                if (basvuru.Id <= 0)
                {
                    sonuc.HataEkle("Başvuru kaydı oluşturulmadan ortaklık bilgileri kaydedilemez.");
                    return sonuc;
                }

                basvuru.BagliOrtakIsletmeVarMi = basvuru.BagliOrtakIsletmeVarMi?.Trim() ?? "";
                basvuru.BagliOrtakAciklama = basvuru.BagliOrtakAciklama?.Trim() ?? "";
                basvuru.AktifBolum = Math.Clamp(basvuru.AktifBolum, 1, 7);

                await using SqlConnection connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                Basvuru? mevcut = await BasvuruOnBasvuruYetkiKontrolAsync(connection, basvuru.Id, kullaniciId, sonuc);
                if (!sonuc.Basarili || mevcut == null)
                    return sonuc;

                mevcut.OzelSektorPayi = basvuru.OzelSektorPayi;
                mevcut.BagliOrtakIsletmeVarMi = basvuru.BagliOrtakIsletmeVarMi;
                mevcut.BagliOrtakAciklama = basvuru.BagliOrtakAciklama;
                mevcut.AktifBolum = basvuru.AktifBolum;

                await using SqlTransaction transaction = (SqlTransaction)await connection.BeginTransactionAsync();
                try
                {
                    TABBasvuru tabBasvuru = new TABBasvuru(connection, transaction);
                    await tabBasvuru.OrtaklikKaydetAsync(mevcut);

                    TABBasvuruLog tabBasvuruLog = new TABBasvuruLog(connection, transaction);
                    await tabBasvuruLog.EkleAsync(mevcut, "OrtaklikKaydet", new
                    {
                        mevcut.OzelSektorPayi,
                        mevcut.BagliOrtakIsletmeVarMi,
                        mevcut.BagliOrtakAciklama
                    });

                    await transaction.CommitAsync();
                    sonuc.Nesne = mevcut.Id;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Ortaklık bilgileri kaydedilemedi. BasvuruId: {BasvuruId}, KullaniciId: {KullaniciId}", basvuru.Id, kullaniciId);
                    sonuc.HataEkle("Ortaklık bilgileri kaydedilemedi.");
                }
            }
            catch (Exception ex)
            {
                BeklenmeyenHata(sonuc, ex, "Ortaklık bilgileri kaydedilemedi. BasvuruId: {BasvuruId}, KullaniciId: {KullaniciId}", "Ortaklık bilgileri kaydedilemedi.", basvuru.Id, kullaniciId);
            }

            return sonuc;
        }

        private static void BasvuruNormalizeEt(Basvuru basvuru)
        {
            if (string.IsNullOrWhiteSpace(basvuru.Durum) ||
                string.Equals(basvuru.Durum, "Taslak", StringComparison.OrdinalIgnoreCase))
            {
                basvuru.Durum = Basvuru.OnBasvuruDurumu;
            }

            basvuru.TicaretUnvani = basvuru.TicaretUnvani?.Trim() ?? "";
            basvuru.VergiKimlikNo = basvuru.VergiKimlikNo?.Trim() ?? "";
            basvuru.BasvuruDonemi = basvuru.BasvuruDonemi?.Trim() ?? "";
            basvuru.DonemId = basvuru.DonemId.GetValueOrDefault() > 0 ? basvuru.DonemId : null;
            basvuru.IlId = basvuru.IlId.GetValueOrDefault() > 0 ? basvuru.IlId : null;
            basvuru.IlAdi = basvuru.IlAdi?.Trim() ?? "";
            basvuru.BasvuruKonusu = basvuru.BasvuruKonusu?.Trim() ?? "";
            basvuru.IrtibatTelefon = basvuru.IrtibatTelefon?.Trim() ?? "";
            basvuru.IrtibatEposta = basvuru.IrtibatEposta?.Trim() ?? "";
            basvuru.YatirimAdi = basvuru.YatirimAdi?.Trim() ?? "";
            basvuru.DegerZinciriId = basvuru.DegerZinciriId.GetValueOrDefault() > 0 ? basvuru.DegerZinciriId : null;
            basvuru.DegerZinciriAsamalari = basvuru.DegerZinciriAsamalari
                .Select(x => x?.Trim() ?? "")
                .Where(x => x.Length > 0)
                .Distinct()
                .ToList();
            basvuru.HarcamaTurleri = basvuru.HarcamaTurleri
                .Select(x => x?.Trim() ?? "")
                .Where(x => x.Length > 0)
                .Distinct()
                .ToList();

            basvuru.YatirimAdresleri = basvuru.YatirimAdresleri
                .Select((adres, index) => new BasvuruUygulamaAdresi
                {
                    Id = adres.Id,
                    BasvuruId = adres.BasvuruId,
                    SiraNo = adres.SiraNo > 0 ? adres.SiraNo : index + 1,
                    IlceId = adres.IlceId.GetValueOrDefault() > 0 ? adres.IlceId : null,
                    IlId = adres.IlId.GetValueOrDefault() > 0 ? adres.IlId : null,
                    IlKod = adres.IlKod.GetValueOrDefault() > 0 ? adres.IlKod : null,
                    IlAdi = adres.IlAdi?.Trim() ?? "",
                    IlceAdi = adres.IlceAdi?.Trim() ?? "",
                    TamAdres = adres.TamAdres?.Trim() ?? "",
                    YatirimYeriStatusu = adres.YatirimYeriStatusu,
                    KiraVeyaTahsisSuresi = adres.KiraVeyaTahsisSuresi.GetValueOrDefault() > 0 ? adres.KiraVeyaTahsisSuresi : null,
                    KiraTahsisBitisTarihi = adres.KiraTahsisBitisTarihi,
                    YapiRuhsatiDurumu = adres.YapiRuhsatiDurumu
                })
                .Where(adres =>
                    adres.IlceId.HasValue ||
                    !string.IsNullOrWhiteSpace(adres.TamAdres) ||
                    adres.YatirimYeriStatusu.HasValue ||
                    adres.KiraVeyaTahsisSuresi.HasValue ||
                    adres.KiraTahsisBitisTarihi.HasValue ||
                    adres.YapiRuhsatiDurumu.HasValue)
                .ToList();

            basvuru.YatirimAdresSayisi = basvuru.YatirimAdresleri.Count;
        }

        private static void FirmaNormalizeEt(Firma firma)
        {
            firma.VergiKimlikNo = firma.VergiKimlikNo?.Trim() ?? "";
            firma.TicaretUnvani = firma.TicaretUnvani?.Trim() ?? "";
            firma.TicaretSicilNo = firma.TicaretSicilNo?.Trim() ?? "";
            firma.MersisNo = firma.MersisNo?.Trim() ?? "";
            firma.NaceKodu = firma.NaceKodu?.Trim() ?? "";
            firma.WebSitesi = firma.WebSitesi?.Trim() ?? "";
            firma.Telefon = firma.Telefon?.Trim() ?? "";
            firma.KepAdresi = firma.KepAdresi?.Trim() ?? "";
            firma.Eposta = firma.Eposta?.Trim() ?? "";
        }

        private static void Asama1Kopyala(Basvuru hedef, Basvuru kaynak)
        {
            hedef.FirmaId = kaynak.FirmaId;
            hedef.DonemId = kaynak.DonemId;
            hedef.IlId = kaynak.IlId;
            hedef.BasvuruKonusu = kaynak.BasvuruKonusu;
            hedef.BasvuruSahibiTuru = kaynak.BasvuruSahibiTuru;
            hedef.SonIkiYildirFaalMi = kaynak.SonIkiYildirFaalMi;
            hedef.OzelSektorPayi = kaynak.OzelSektorPayi;
            hedef.BagliOrtakIsletmeVarMi = kaynak.BagliOrtakIsletmeVarMi;
            hedef.BagliOrtakAciklama = kaynak.BagliOrtakAciklama;
            hedef.AktifBolum = kaynak.AktifBolum;
        }

        private static void Asama2Kopyala(Basvuru hedef, Basvuru kaynak)
        {
            hedef.Telefon = kaynak.Telefon;
            hedef.IrtibatKisisi = kaynak.IrtibatKisisi;
            hedef.IrtibatUnvani = kaynak.IrtibatUnvani;
            hedef.IrtibatTelefon = kaynak.IrtibatTelefon;
            hedef.IrtibatEposta = kaynak.IrtibatEposta;
            hedef.IletisimAdresi = kaynak.IletisimAdresi;
            hedef.YetkiliKisiler = kaynak.YetkiliKisiler;
        }

        private static void Asama3Kopyala(Basvuru hedef, Basvuru kaynak)
        {
            hedef.YatirimAdi = kaynak.YatirimAdi;
            hedef.YatirimTuru = kaynak.YatirimTuru;
            hedef.DegerZinciriId = kaynak.DegerZinciriId;
            hedef.DegerZinciriAsamalari = kaynak.DegerZinciriAsamalari;
            hedef.HarcamaTurleri = kaynak.HarcamaTurleri;
        }

        private static void Asama4Kopyala(Basvuru hedef, Basvuru kaynak)
        {
            hedef.YatirimAdresSayisi = hedef.YatirimAdresleri.Count;
        }

        private static void Asama5Kopyala(Basvuru hedef, Basvuru kaynak)
        {
            hedef.ToplamYatirimTutari = kaynak.ToplamYatirimTutari;
            hedef.UygunHarcamaTutari = kaynak.UygunHarcamaTutari;
            hedef.TalepEdilenDestekTutari = kaynak.TalepEdilenDestekTutari;
            hedef.BasvuruSahibiKatkisi = kaynak.BasvuruSahibiKatkisi;
            hedef.DestekOrani = kaynak.DestekOrani;
            hedef.YatiriminAmaci = kaynak.YatiriminAmaci;
        }

        private static void Asama6Kopyala(Basvuru hedef, Basvuru kaynak)
        {
            hedef.OncekiYilNetSatis = kaynak.OncekiYilNetSatis;
            hedef.SonYilNetSatis = kaynak.SonYilNetSatis;
            hedef.OncekiYilAktifToplami = kaynak.OncekiYilAktifToplami;
            hedef.SonYilAktifToplami = kaynak.SonYilAktifToplami;
        }

        private static void Asama7Kopyala(Basvuru hedef, Basvuru kaynak)
        {
            hedef.BelgePaketiDosyaAdi = kaynak.BelgePaketiDosyaAdi;
            hedef.TaahhutDosyaAdi = kaynak.TaahhutDosyaAdi;
            hedef.BelgeBeyani = kaynak.BelgeBeyani;
            hedef.BelgeGruplari = kaynak.BelgeGruplari;
        }

        private static object BasvuruLogDetayiOlustur(Basvuru basvuru, string logIslem)
        {
            return logIslem switch
            {
                "YeniKayit" or "Asama1Update" => new
                {
                    basvuru.FirmaId,
                    basvuru.DonemId,
                    basvuru.IlId,
                    basvuru.BasvuruKonusu,
                    basvuru.BasvuruSahibiTuru,
                    basvuru.SonIkiYildirFaalMi,
                    basvuru.OzelSektorPayi,
                    basvuru.BagliOrtakIsletmeVarMi,
                    basvuru.BagliOrtakAciklama
                },
                "Asama2Update" => new
                {
                    basvuru.Telefon,
                    basvuru.IrtibatKisisi,
                    basvuru.IrtibatUnvani,
                    basvuru.IrtibatTelefon,
                    basvuru.IrtibatEposta,
                    basvuru.IletisimAdresi,
                    basvuru.YetkiliKisiler
                },
                "Asama3Update" => new
                {
                    basvuru.YatirimAdi,
                    basvuru.YatirimTuru,
                    basvuru.DegerZinciriId,
                    basvuru.DegerZinciriAsamalari,
                    basvuru.HarcamaTurleri
                },
                "Asama4Update" => new
                {
                    basvuru.YatirimAdresleri
                },
                "Asama5Update" => new
                {
                    basvuru.ToplamYatirimTutari,
                    basvuru.UygunHarcamaTutari,
                    basvuru.TalepEdilenDestekTutari,
                    basvuru.BasvuruSahibiKatkisi,
                    basvuru.DestekOrani,
                    basvuru.YatiriminAmaci
                },
                "Asama6Update" => new
                {
                    basvuru.OncekiYilNetSatis,
                    basvuru.SonYilNetSatis,
                    basvuru.OncekiYilAktifToplami,
                    basvuru.SonYilAktifToplami
                },
                "Asama7Update" => new
                {
                    basvuru.BelgePaketiDosyaAdi,
                    basvuru.TaahhutDosyaAdi,
                    basvuru.BelgeBeyani,
                    basvuru.BelgeGruplari
                },
                _ => basvuru
            };
        }

        private static void SonucHatalariniAktar(Sonuc kaynak, Sonuc hedef)
        {
            foreach (string hata in kaynak.Hatalar)
            {
                hedef.HataEkle(hata);
            }
        }

        private static void YatirimBolumuDogrula(Basvuru basvuru, Sonuc sonuc)
        {
            if (string.IsNullOrWhiteSpace(basvuru.YatirimAdi))
                sonuc.HataEkle("Yatırım adı girilmelidir.");

            if (string.IsNullOrWhiteSpace(basvuru.YatirimTuru))
                sonuc.HataEkle("Yatırım türü seçilmelidir.");

            if (basvuru.DegerZinciriAsamalari == null || !basvuru.DegerZinciriAsamalari.Any(x => !string.IsNullOrWhiteSpace(x)))
                sonuc.HataEkle("En az bir değer zinciri aşaması seçilmelidir.");

            if (basvuru.HarcamaTurleri == null || !basvuru.HarcamaTurleri.Any(x => !string.IsNullOrWhiteSpace(x)))
                sonuc.HataEkle("En az bir talep edilen harcama türü seçilmelidir.");
        }

        private static void UygulamaAdresiNormalizeEt(BasvuruUygulamaAdresi adres)
        {
            adres.SiraNo = adres.SiraNo <= 0 ? 1 : adres.SiraNo;
            adres.IlceId = adres.IlceId.GetValueOrDefault() > 0 ? adres.IlceId : null;
            adres.TamAdres = adres.TamAdres?.Trim() ?? "";
            adres.KiraVeyaTahsisSuresi = adres.KiraVeyaTahsisSuresi.GetValueOrDefault() > 0 ? adres.KiraVeyaTahsisSuresi : null;
        }

        private static void UygulamaAdresiDogrula(BasvuruUygulamaAdresi adres, Sonuc sonuc)
        {
            if (adres.BasvuruId <= 0)
                sonuc.HataEkle("Başvuru kaydı seçilmelidir.");

            if (!adres.IlceId.HasValue)
                sonuc.HataEkle("İlçe seçilmelidir.");

            if (string.IsNullOrWhiteSpace(adres.TamAdres))
                sonuc.HataEkle("Tam adres girilmelidir.");

            if (!adres.KiraTahsisBitisTarihi.HasValue)
                sonuc.HataEkle("Kira/tahsis bitiş tarihi girilmelidir.");
        }

        private void BeklenmeyenHata(Sonuc sonuc, Exception ex, string logMesaji, string kullaniciMesaji, params object[] logParametreleri)
        {
            _logger.LogError(ex, logMesaji, logParametreleri);
            sonuc.HataEkle(kullaniciMesaji);
        }
    }
}

