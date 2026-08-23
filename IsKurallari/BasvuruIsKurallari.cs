using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Localization;
using System.Text.Json;
using TarimDonusum.Araclar;
using TarimDonusum.Models;
using TarimDonusum.Tablolar;

namespace TarimDonusum.IsKurallari
{
    public class BasvuruIsKurallari
    {
        private const string BasvuruZorunluBelgelerFormAd = "ZorunluBelgeler";
        private const string BasvuruZorunluBelgeFormAd = "Basvuru_ZorunluBelge";
        private const string BasvuruBagliBelgeFormAd = "Basvuru_BagliBelge";
        private const string BasvuruMaliBelgeFormAd = "Basvuru_MaliBelge";
        private const string BasvuruMaliOrtakBelgeFormAdPrefix = "BOMALI_";
        private const string BasvuruAdliSicilFormAd = "Basvuru_AdliSicil";
        private const string BasvuruImzaYetkiFormAd = "Basvuru_ImzaYetki";
        private const string BasvuruOrtakUboKycFormAd = "Basvuru_OrtakUboKyc";
        private const string BasvuruOrtakUboKycFormAdPrefix = "OBOKYC_";
        private const string BasvuruYatirimYeriFormAdPrefix = "BYAT_";
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

        public async Task<Sonuc<List<Basvuru>>> KullaniciBasvuruVersiyonlariniListeleAsync(Kullanici kullanici)
        {
            Sonuc<List<Basvuru>> sonuc = new Sonuc<List<Basvuru>>();

            try
            {
                await using SqlConnection connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                TABBasvuru tabBasvuru = new TABBasvuru(connection, _localizer);
                sonuc = BasvuruKullanicisiMi(kullanici)
                    ? await tabBasvuru.KullaniciBasvuruVersiyonlariniListeleAsync(kullanici.Id)
                    : await tabBasvuru.TumVersiyonlariListeleAsync();
            }
            catch (Exception ex)
            {
                BeklenmeyenHata(sonuc, ex, "Başvuru sürümleri listelenemedi. KullaniciId: {KullaniciId}", "Başvuru kayıtları listelenemedi.", kullanici.Id);
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

        public async Task<Sonuc<List<Basvuru>>> TumVersiyonlariListeleAsync()
        {
            Sonuc<List<Basvuru>> sonuc = new Sonuc<List<Basvuru>>();

            try
            {
                await using SqlConnection connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                TABBasvuru tabBasvuru = new TABBasvuru(connection, _localizer);
                sonuc = await tabBasvuru.TumVersiyonlariListeleAsync();
            }
            catch (Exception ex)
            {
                BeklenmeyenHata(sonuc, ex, "Tüm başvuru sürümleri listelenemedi.", "Başvuru kayıtları listelenemedi.");
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

        public async Task<Sonuc> IncelemeyeGonderAsync(int basvuruId, Kullanici kullanici)
        {
            Sonuc sonuc = new();
            if (basvuruId <= 0)
            {
                HataEkle(sonuc, "Business.Application.RecordRequired");
                return sonuc;
            }

            Sonuc<Basvuru> okumaSonucu = await OkuAsync(basvuruId, kullanici);
            if (!okumaSonucu.basarili || okumaSonucu.nesne == null)
            {
                SonucHatalariniAktar(okumaSonucu, sonuc);
                return sonuc;
            }

            Basvuru basvuru = okumaSonucu.nesne;
            if (!BasvuruKullanicisiMi(kullanici))
                BasvuruKullanicisiYetkiHatasiEkle(sonuc);
            if (basvuru.durum != enumBasvuruDurum.OnBasvuruDurumu &&
                basvuru.durum != enumBasvuruDurum.OnBasvuruDuzeltmeDurumu)
                HataEkle(sonuc, "Business.Application.Submit.AlreadySubmitted");

            IncelemeyeGonderimEksikleriniDogrula(basvuru, sonuc);
            if (!sonuc.basarili) return sonuc;

            try
            {
                await using SqlConnection connection = new(_connectionString);
                await connection.OpenAsync();
                await using SqlTransaction transaction = (SqlTransaction)await connection.BeginTransactionAsync();
                TABBasvuru tabBasvuru = new(connection, _localizer, transaction);
                if (!await tabBasvuru.IncelemeyeGonderAsync(basvuruId))
                {
                    HataEkle(sonuc, "Business.Application.Submit.AlreadySubmitted");
                    await transaction.RollbackAsync();
                    return sonuc;
                }

                TABBasvuruLog tabLog = new(connection, _localizer, transaction);
                await tabLog.EkleAsync(basvuruId, kullanici, "OnBasvuruIncelemeyeGonderildi",
                    new { EskiDurum = enumBasvuruDurum.OnBasvuruDurumu, YeniDurum = enumBasvuruDurum.OnBasvuruIncelemeDurumu });
                await transaction.CommitAsync();
                sonuc.mesaj = Metin("Business.Application.Submit.Success");
            }
            catch (Exception ex)
            {
                BeklenmeyenHata(sonuc, ex,
                    "Ön başvuru incelemeye gönderilemedi. BasvuruId: {BasvuruId}, KullaniciId: {KullaniciId}",
                    "Business.Application.Submit.Failed", basvuruId, kullanici.Id);
            }

            return sonuc;
        }

        public async Task<Sonuc> OnBasvuruDenetimiKaydetAsync(Basvuru denetim, Kullanici kullanici, bool sonuclandir)
        {
            Sonuc sonuc = new();
            if (BasvuruKullanicisiMi(kullanici))
                sonuc.HataEkle("Başvuru kullanıcıları ön başvuru denetimi yapamaz.");
            if (denetim.Id <= 0)
                sonuc.HataEkle("Başvuru seçilmelidir.");
            if (!sonuclandir && string.IsNullOrWhiteSpace(denetim.DenetimGerekcesi))
                sonuc.HataEkle("Gerekçe girilmelidir.");
            if (!sonuclandir && (!denetim.DenetimSonucu.HasValue ||
                denetim.DenetimSonucu == enumOnBasvuruDenetimSonucu.Tanimsiz))
                sonuc.HataEkle("Denetim sonucu seçilmelidir.");
            if (!sonuc.basarili) return sonuc;

            try
            {
                await using SqlConnection connection = new(_connectionString);
                await connection.OpenAsync();
                await using SqlTransaction transaction = (SqlTransaction)await connection.BeginTransactionAsync();
                TABBasvuru tabBasvuru = new(connection, _localizer, transaction);
                enumBasvuruDurum? yeniDurum = null;
                bool kaydedildi;
                int? yeniRevizyonId = null;
                if (sonuclandir)
                {
                    Basvuru? kayitliDenetim = await tabBasvuru.OnBasvuruDenetimBilgisiOkuAsync(denetim.Id);
                    if (kayitliDenetim == null)
                    {
                        if (await tabBasvuru.TamBasvuruOlusturulmusMuAsync(denetim.Id))
                        {
                            await transaction.RollbackAsync();
                            sonuc.mesaj = "Ön başvuru daha önce onaylanmış ve tam başvuru kaydı oluşturulmuş.";
                            return sonuc;
                        }

                        sonuc.HataEkle("Ön başvuru aktif inceleme kaydı bulunamadı. Kayıt daha önce sonuçlandırılmış veya revizyonu değişmiş olabilir.");
                    }
                    else
                    {
                        if (!DenetimListesiTamamMi(kayitliDenetim.SistemDenetimAnketi, true))
                            sonuc.HataEkle("Sonuçlandırmadan önce sistem kontrol listesi kaydedilmelidir.");
                        if (string.IsNullOrWhiteSpace(kayitliDenetim.DenetimAnketi))
                            sonuc.HataEkle("Sonuçlandırmadan önce uzman kontrol listesi kaydedilmelidir.");
                        if (string.IsNullOrWhiteSpace(kayitliDenetim.DenetimGerekcesi) ||
                            !kayitliDenetim.DenetimSonucu.HasValue ||
                            kayitliDenetim.DenetimSonucu == enumOnBasvuruDenetimSonucu.Tanimsiz)
                            sonuc.HataEkle("Sonuçlandırmadan önce sonuç ve sonuç gerekçesi kaydedilmelidir.");
                    }

                    if (!sonuc.basarili)
                    {
                        await transaction.RollbackAsync();
                        return sonuc;
                    }

                    denetim.SistemDenetimAnketi = kayitliDenetim!.SistemDenetimAnketi;
                    denetim.DenetimAnketi = kayitliDenetim.DenetimAnketi;
                    denetim.DenetimGerekcesi = kayitliDenetim.DenetimGerekcesi;
                    denetim.DenetimSonucu = kayitliDenetim.DenetimSonucu;
                }

                if (sonuclandir &&
                    denetim.DenetimSonucu == enumOnBasvuruDenetimSonucu.DuzeltmeIcinIadeEdildi)
                {
                    kaydedildi = true;
                    if (kaydedildi)
                    {
                        yeniRevizyonId = await tabBasvuru.YeniRevizyonOlusturAsync(denetim.Id);
                        Sonuc<Dictionary<int, int>> dosyaKopyalama =
                            await _dosyaYonetimIsKurallari.BasvuruDosyalariniKopyalaAsync(denetim.Id, yeniRevizyonId.Value);
                        if (!dosyaKopyalama.basarili)
                        {
                            SonucHatalariniAktar(dosyaKopyalama, sonuc);
                            await transaction.RollbackAsync();
                            return sonuc;
                        }

                        await tabBasvuru.DosyaReferanslariniGuncelleAsync(
                            yeniRevizyonId.Value,
                            dosyaKopyalama.nesne);
                        kaydedildi = await tabBasvuru.BasvuruAnaDurumGuncelleAsync(
                            yeniRevizyonId.Value,
                            enumBasvuruDurum.OnBasvuruIncelemeDurumu,
                            enumBasvuruDurum.OnBasvuruDurumu);
                        yeniDurum = enumBasvuruDurum.OnBasvuruDurumu;
                    }
                }
                else if (sonuclandir && denetim.DenetimSonucu == enumOnBasvuruDenetimSonucu.KabulEdildi)
                {
                    yeniRevizyonId = await tabBasvuru.YeniRevizyonOlusturAsync(denetim.Id, enumBasvuruKayitTuru.Basvuru);
                    Sonuc<Dictionary<int, int>> dosyaKopyalama =
                        await _dosyaYonetimIsKurallari.BasvuruDosyalariniKopyalaAsync(denetim.Id, yeniRevizyonId.Value);
                    if (!dosyaKopyalama.basarili)
                    {
                        SonucHatalariniAktar(dosyaKopyalama, sonuc);
                        await transaction.RollbackAsync();
                        return sonuc;
                    }
                    await tabBasvuru.DosyaReferanslariniGuncelleAsync(yeniRevizyonId.Value, dosyaKopyalama.nesne);
                    yeniDurum = enumBasvuruDurum.BasvuruDurumu;
                    kaydedildi = await tabBasvuru.BasvuruAnaDurumGuncelleAsync(
                        yeniRevizyonId.Value,
                        enumBasvuruDurum.OnBasvuruIncelemeDurumu,
                        enumBasvuruDurum.BasvuruDurumu);
                }
                else if (sonuclandir)
                {
                    yeniDurum = enumBasvuruDurum.IptalDurumu;
                    kaydedildi = await tabBasvuru.BasvuruAnaDurumGuncelleAsync(
                        denetim.Id, enumBasvuruDurum.OnBasvuruIncelemeDurumu, yeniDurum.Value);
                }
                else
                {
                    kaydedildi = await tabBasvuru.OnBasvuruDenetimTaslagiKaydetAsync(denetim);
                }

                if (!kaydedildi)
                {
                    sonuc.HataEkle("Başvuru inceleme aşamasında değil veya durumu daha önce değiştirilmiş.");
                    await transaction.RollbackAsync();
                    return sonuc;
                }

                await new TABBasvuruLog(connection, _localizer, transaction).EkleAsync(
                    denetim.Id,
                    kullanici,
                    yeniRevizyonId.HasValue && denetim.DenetimSonucu == enumOnBasvuruDenetimSonucu.DuzeltmeIcinIadeEdildi
                        ? "OnBasvuruDuzeltmeIcinIadeEdildi"
                        : yeniRevizyonId.HasValue ? "OnBasvuruOnaylandiTamBasvuruOlusturuldu"
                        : sonuclandir ? "OnBasvuruDenetimiTamamlandi" : "OnBasvuruDenetimTaslagiKaydedildi",
                    new
                    {
                        denetim.DenetimAnketi,
                        denetim.DenetimGerekcesi,
                        denetim.DenetimSonucu,
                        YeniDurum = yeniDurum,
                        YeniRevizyonBasvuruId = yeniRevizyonId
                    });
                if (yeniRevizyonId.HasValue)
                {
                    await new TABBasvuruLog(connection, _localizer, transaction).EkleAsync(
                        yeniRevizyonId.Value,
                        kullanici,
                        denetim.DenetimSonucu == enumOnBasvuruDenetimSonucu.KabulEdildi
                            ? "TamBasvuruKaydiOlusturuldu"
                            : "DuzeltmeRevizyonuOlusturuldu",
                        new { KaynakBasvuruId = denetim.Id, YeniBasvuruId = yeniRevizyonId.Value });
                }
                await transaction.CommitAsync();
                sonuc.mesaj = yeniRevizyonId.HasValue && denetim.DenetimSonucu == enumOnBasvuruDenetimSonucu.DuzeltmeIcinIadeEdildi
                    ? "Ön başvuru düzeltme için iade edildi ve yeni revizyon oluşturuldu."
                    : yeniRevizyonId.HasValue
                        ? "Ön başvuru onaylandı ve tam başvuru kaydı oluşturuldu."
                        : sonuclandir
                            ? "Ön başvuru denetimi sonuçlandırıldı."
                            : "Ön başvuru denetimi kaydedildi.";
            }
            catch (Exception ex)
            {
                BeklenmeyenHata(sonuc, ex,
                    "Ön başvuru denetimi kaydedilemedi. BasvuruId: {BasvuruId}, KullaniciId: {KullaniciId}",
                    sonuclandir
                        ? "Ön başvuru denetimi sonuçlandırılırken başvuru kaydı oluşturulamadı. Lütfen tekrar deneyiniz."
                        : "Ön başvuru denetimi kaydedilemedi. Lütfen tekrar deneyiniz.",
                    denetim.Id, kullanici.Id);
            }

            return sonuc;
        }

        public void DenetimListeleriniIlkDegerle(Basvuru b, bool sistemListesiniYenidenUret = false)
        {
            if (b.kayitTuru != enumBasvuruKayitTuru.OnBasvuru) return;
            Firma f = b.basvuruFirma.firma;
            bool temelBilgiler = !string.IsNullOrWhiteSpace(f.ticaretUnvani) && !string.IsNullOrWhiteSpace(f.vergiKimlikNo)
                && (!string.IsNullOrWhiteSpace(f.mersisNo) || !string.IsNullOrWhiteSpace(f.ticaretSicilNo))
                && !string.IsNullOrWhiteSpace(f.adres) && !string.IsNullOrWhiteSpace(f.telefon)
                && !string.IsNullOrWhiteSpace(b.irtibat.kisi) && !string.IsNullOrWhiteSpace(b.irtibat.telefon);
            bool sahipTuru = b.basvuruFirma.basvuruSahibiTuru.HasValue && b.basvuruFirma.basvuruSahibiTuru != enumBasvuruSahibiTuru.Tanimsiz;
            bool temsilYetki = !string.IsNullOrWhiteSpace(b.irtibat.yetkiliKisiler) || b.AdliSicilKisileri.Count > 0;
            bool yatirim = !string.IsNullOrWhiteSpace(b.yatirim.yatirimAdi)
                && !string.IsNullOrWhiteSpace(b.yatirim.yatiriminAmaci)
                && !string.IsNullOrWhiteSpace(b.yatirim.yatirimFaaliyetleri)
                && !string.IsNullOrWhiteSpace(b.yatirim.yatirimGirdileri)
                && !string.IsNullOrWhiteSpace(b.yatirim.yatirimCiktilari);
            bool yatirimOzeti = !string.IsNullOrWhiteSpace(b.yatirimOzeti.yatirimOzetiJson);
            bool yatirimYeri = b.YatirimAdresleri.Count > 0 && b.YatirimAdresleri.All(x => x.ilId.HasValue && x.ilceId.HasValue && !string.IsNullOrWhiteSpace(x.tamAdres));
            bool degerZinciri = b.basvuruFirma.ilId > 0 && b.yatirim.degerZinciriId.GetValueOrDefault() > 0 && b.yatirim.degerZinciriAsamalari.Count > 0;
            bool finans = b.finans.toplamYatirimTutari.GetValueOrDefault() > 0 && b.finans.talepEdilenFinansmanOrani.GetValueOrDefault() > 0
                && b.finans.talepEdilenVadeSuresiAy.GetValueOrDefault() > 0
                && b.finans.yatirimSuresiAy.GetValueOrDefault() > 0;
            bool mali = b.mali.oncekiYilNetSatis.GetValueOrDefault() > 0 && b.mali.sonYilNetSatis.GetValueOrDefault() > 0
                && b.mali.oncekiYilAktifToplami.GetValueOrDefault() > 0 && b.mali.sonYilAktifToplami.GetValueOrDefault() > 0;
            bool ortaklik = b.ortaklik.ortaklar.Count == 0 || b.ortaklik.ortaklar.All(x => !string.IsNullOrWhiteSpace(x.adUnvan)
                && !string.IsNullOrWhiteSpace(x.tcknVkn) && x.payOrani.HasValue);
            bool faaliyet = !string.IsNullOrWhiteSpace(f.faaliyetKonusu) && !string.IsNullOrWhiteSpace(f.naceKodu);
            bool beyanlar = b.TaahhutDosyaId.HasValue && !string.IsNullOrWhiteSpace(b.TaahhutBeyanlarJson);
            bool cevresel = !string.IsNullOrWhiteSpace(b.cevreselSosyal.cevreselSosyalJson);
            bool belgeler = b.ZorunluBelgeler.Count > 0 && b.ZorunluBelgeler.All(x => x.dosyaId.HasValue);
            bool teknik = !string.IsNullOrWhiteSpace(b.dbCtpTeknikProje.dbCtpTeknikProjeJson);
            bool tamlik = temelBilgiler && sahipTuru && temsilYetki && yatirim && yatirimOzeti && yatirimYeri && degerZinciri
                && finans && mali && ortaklik && faaliyet && beyanlar && cevresel && belgeler && teknik;

            object Madde(int no, string konu, string soru, string kaynak, bool tam) => new
            {
                no, konu, soru, kaynak,
                sonuc = tam ? "Tam" : "Eksik",
                aciklama = tam ? "Sistem kontrolü sağlandı." : "Sistem ilgili alan/belgeyi eksik görüyor."
            };
            var maddeler = new[]
            {
                Madde(1, "Online başvuru formu", "Başvuru sahibi, ön başvuru formundaki zorunlu alanların tamamını doldurmuş mu?", "Ön başvuru online formu", tamlik),
                Madde(2, "Başvuru sahibi temel bilgileri", "Unvan, vergi kimlik numarası, MERSİS/ticaret sicil bilgileri, adres, iletişim ve yetkili kişi bilgileri girilmiş mi?", "Başvuru formu, MERSİS/Ticaret sicil", temelBilgiler),
                Madde(3, "Başvuru sahibi türü", "Başvuru sahibinin şirket, kooperatif veya üretici örgütü olduğuna ilişkin bilgi/belge sunulmuş mu?", "Ticaret sicil/oda/kuruluş belgeleri", sahipTuru),
                Madde(4, "Temsil ve yetki bilgileri", "Başvuruyu yapan kişinin başvuru sahibi adına işlem yapmaya yetkili olduğuna ilişkin bilgi/belge sunulmuş mu?", "İmza sirküleri, yetki belgesi", temsilYetki),
                Madde(5, "Yatırım bilgileri", "Yatırımın amacı, faaliyetleri ve çıktıları açıklanmış mı?", "Ön başvuru online formu", yatirim),
                Madde(6, "Ön iş planı / yatırım özeti", "Ön iş planı / yatırım özeti doldurulmuş mu?", "Ön iş planı / yatırım özeti", yatirimOzeti),
                Madde(7, "Yatırım yeri bilgisi", "Yatırımın uygulanacağı il, ilçe, adres ve varsa organize bölge bilgisi girilmiş mi?", "Başvuru formu / yatırım yeri beyanı", yatirimYeri),
                Madde(8, "İl-değer zinciri seçimi", "Başvuru sahibi yatırımın hangi değer zinciri kapsamında olduğunu ve yatırım yerini seçmiş mi?", "İl-değer zinciri seçimi", degerZinciri),
                Madde(9, "Talep edilen finansman bilgisi", "Tahmini yatırım tutarı, talep edilen RFF/kredi tutarı, para birimi ve önerilen vade bilgisi girilmiş mi?", "Ön finansman bilgisi / bütçe özeti", finans),
                Madde(10, "Mali bilgiler", "Son iki mali yıla ilişkin net satış hasılatı veya mali bilanço/varlık toplamı bilgileri sisteme girilmiş/yüklenmiş mi?", "Mali tablolar / bilanço / gelir tablosu", mali),
                Madde(11, "Ortaklık ve sermaye yapısı", "Sermaye yapısı ve tüzel kişi ortaklar beyan edilmiş mi?", "Ortaklık beyanı, ticaret sicil, ortaklık belgeleri", ortaklik),
                Madde(12, "Faaliyet/NACE bilgisi", "Başvuru sahibinin faaliyet konusu ve NACE/faaliyet alanı bilgisi sunulmuş mu?", "NACE/faaliyet alanı kaydı", faaliyet),
                Madde(13, "Beyan ve taahhütler", "Başvuru beyanı, doğruluk taahhüdü, çifte finansman beyanı, izleme/veri paylaşımı taahhüdü alınmış mı?", "Beyan ve taahhüt formları", beyanlar),
                Madde(14, "Banka veri paylaşım rızası", "Ziraat Bankası ile finansal/kambiyo uygunluk kontrolü için veri paylaşımına yönelik açık rıza/taahhüt alınmış mı?", "Veri aktarımı/açık rıza/taahhüt beyanı", beyanlar),
                Madde(15, "Çevresel-sosyal ön bilgi", "Yatırımın Dünya Bankası hariç tutma listesi ve ESMS ön taraması için gerekli temel bilgiler sunulmuş mu?", "Çevresel-sosyal ön bilgi / ESMS ön tarama", cevresel),
                Madde(16, "Destekleyici belgeler", "Sistem tarafından zorunlu tutulan tüm belgeler okunabilir, eksiksiz ve uygun formatta yüklenmiş mi?", "Sisteme yüklenen belgeler", belgeler),
                Madde(17, "Teknik proje", "Teknik proje formu doldurulmuş mu?", "Teknik Proje", teknik)
            };
            if (sistemListesiniYenidenUret || DenetimListesiBosMu(b.SistemDenetimAnketi))
                b.SistemDenetimAnketi = JsonSerializer.Serialize(maddeler);
            if (string.IsNullOrWhiteSpace(b.DenetimAnketi)
                || b.DenetimAnketi.Contains("Uzman kontrol maddesi", StringComparison.OrdinalIgnoreCase))
                b.DenetimAnketi = UzmanKontrolListesi.Json;
        }

        public async Task<Sonuc<string>> SistemDenetimListesiniYenidenUretAsync(int basvuruId, Kullanici kullanici)
        {
            Sonuc<string> sonuc = new();
            if (BasvuruKullanicisiMi(kullanici))
                sonuc.HataEkle("Başvuru kullanıcıları sistem denetim listesini yeniden üretemez.");
            if (basvuruId <= 0)
                sonuc.HataEkle("Başvuru seçilmelidir.");
            if (!sonuc.basarili) return sonuc;

            Sonuc<Basvuru> okumaSonucu = await OkuAsync(basvuruId);
            if (!okumaSonucu.basarili || okumaSonucu.nesne == null)
            {
                SonucHatalariniAktar(okumaSonucu, sonuc);
                return sonuc;
            }

            Basvuru basvuru = okumaSonucu.nesne;
            if (basvuru.kayitTuru != enumBasvuruKayitTuru.OnBasvuru ||
                basvuru.durum != enumBasvuruDurum.OnBasvuruIncelemeDurumu ||
                basvuru.basvuruFirma.siraNo != 0)
            {
                sonuc.HataEkle("Sistem sonuçları yalnızca incelemedeki güncel ön başvuru için yeniden üretilebilir.");
                return sonuc;
            }

            DenetimListeleriniIlkDegerle(basvuru, true);
            Sonuc kaydetmeSonucu = await DenetimListesiKaydetAsync(new DenetimListesiKayit
            {
                basvuruId = basvuruId,
                listeTuru = "sistem",
                json = basvuru.SistemDenetimAnketi
            }, kullanici);
            SonucHatalariniAktar(kaydetmeSonucu, sonuc);
            if (sonuc.basarili)
            {
                sonuc.nesne = basvuru.SistemDenetimAnketi;
                sonuc.mesaj = "Sistem sonuçları güncel başvuru verilerinden yeniden üretildi.";
            }
            return sonuc;
        }
        private static bool DenetimListesiBosMu(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return true;

            try
            {
                using JsonDocument belge = JsonDocument.Parse(json);
                return belge.RootElement.ValueKind != JsonValueKind.Array ||
                       belge.RootElement.GetArrayLength() == 0;
            }
            catch (JsonException)
            {
                return true;
            }
        }
        public async Task<Sonuc> DenetimListesiKaydetAsync(DenetimListesiKayit model, Kullanici kullanici)
        {
            Sonuc sonuc = new();
            bool sistem = string.Equals(model.listeTuru, "sistem", StringComparison.OrdinalIgnoreCase);
            bool uzman = string.Equals(model.listeTuru, "uzman", StringComparison.OrdinalIgnoreCase);
            if (BasvuruKullanicisiMi(kullanici)) sonuc.HataEkle("Başvuru kullanıcıları denetim listesi kaydedemez.");
            if (model.basvuruId <= 0) sonuc.HataEkle("Başvuru seçilmelidir.");
            if (!sistem && !uzman) sonuc.HataEkle("Liste türü geçersizdir.");
            try
            {
                using JsonDocument belge = JsonDocument.Parse(model.json ?? "");
                if (belge.RootElement.ValueKind != JsonValueKind.Array) sonuc.HataEkle("Kontrol listesi geçersizdir.");
            }
            catch { sonuc.HataEkle("Kontrol listesi geçersizdir."); }
            if (!sonuc.basarili) return sonuc;
            try
            {
                await using SqlConnection connection = new(_connectionString);
                await connection.OpenAsync();
                bool kaydedildi = await new TABBasvuru(connection, _localizer).DenetimListesiKaydetAsync(model.basvuruId, model.json ?? "", sistem);
                if (!kaydedildi) sonuc.HataEkle("Başvuru inceleme aşamasında değil veya liste kaydedilemedi.");
                else sonuc.mesaj = sistem ? "Sistem sonuçları kaydedildi." : "Uzman sonuçları kaydedildi.";
            }
            catch (Exception ex)
            {
                BeklenmeyenHata(sonuc, ex, "Denetim listesi kaydedilemedi. BasvuruId: {BasvuruId}", "Denetim listesi kaydedilemedi.", model.basvuruId);
            }
            return sonuc;
        }

        private static bool DenetimListesiTamamMi(string? json, bool sistemListesi)
        {
            if (string.IsNullOrWhiteSpace(json)) return false;

            try
            {
                using JsonDocument belge = JsonDocument.Parse(json);
                if (belge.RootElement.ValueKind != JsonValueKind.Array || belge.RootElement.GetArrayLength() == 0)
                    return false;

                string[] gecerliSonuclar = sistemListesi
                    ? ["Tam", "Eksik"]
                    : ["E", "H", "UD"];
                return belge.RootElement.EnumerateArray().All(madde =>
                    madde.TryGetProperty("sonuc", out JsonElement sonuc) &&
                    sonuc.ValueKind == JsonValueKind.String &&
                    gecerliSonuclar.Contains(sonuc.GetString(), StringComparer.OrdinalIgnoreCase));
            }
            catch (JsonException)
            {
                return false;
            }
        }
        private void IncelemeyeGonderimEksikleriniDogrula(Basvuru b, Sonuc sonuc)
        {
            void Eksikse(bool kosul, string kaynak)
            {
                if (kosul) HataEkle(sonuc, kaynak);
            }

            Donem? donem = b.basvuruFirma.donem;
            decimal altLimit = donem?.minimumYatirimTutari ?? 0;
            decimal ustLimit = donem?.maksimumYatirimTutari ?? 0;
            bool MaliDegerUygun(decimal? deger) =>
                deger.HasValue && (altLimit <= 0 || deger.Value >= altLimit) && (ustLimit <= 0 || deger.Value <= ustLimit);
            bool maliOlcekUygun = MaliDegerUygun(b.mali.oncekiYilNetSatis)
                || MaliDegerUygun(b.mali.sonYilNetSatis)
                || MaliDegerUygun(b.mali.oncekiYilAktifToplami)
                || MaliDegerUygun(b.mali.sonYilAktifToplami);

            bool cevreselKapsamDisi = false;
            if (!string.IsNullOrWhiteSpace(b.cevreselSosyal.cevreselSosyalJson))
            {
                try
                {
                    using JsonDocument doc = JsonDocument.Parse(b.cevreselSosyal.cevreselSosyalJson);
                    if (doc.RootElement.TryGetProperty("answers", out JsonElement answers)
                        && answers.TryGetProperty("csf_2_1_global_answer", out JsonElement cevap))
                    {
                        cevreselKapsamDisi = string.Equals(cevap.GetString(), "Evet", StringComparison.OrdinalIgnoreCase)
                            || string.Equals(cevap.GetString(), "Yes", StringComparison.OrdinalIgnoreCase);
                    }
                }
                catch
                {
                    HataEkle(sonuc, "Basvuru.Summary.Error.EsfRequired");
                }
            }

            Eksikse(b.basvuruFirma.donemId <= 0, "Basvuru.Summary.Error.PeriodRequired");
            Eksikse(b.basvuruFirma.ilId <= 0, "Basvuru.Summary.Error.ProvinceRequired");
            Eksikse(b.basvuruFirma.firmaId <= 0, "Basvuru.Summary.Error.CompanyRequired");
            Eksikse(string.IsNullOrWhiteSpace(b.irtibat.kisi), "Basvuru.Summary.Error.ContactPersonRequired");
            Eksikse(string.IsNullOrWhiteSpace(b.irtibat.telefon), "Basvuru.Summary.Error.ContactPhoneRequired");
            Eksikse(!b.basvuruFirma.sonIkiYildirFaalMi.HasValue, "Basvuru.Summary.Error.ActiveTwoYearsRequired");
            Eksikse(!b.basvuruFirma.basvuruSahibiTuru.HasValue || b.basvuruFirma.basvuruSahibiTuru == enumBasvuruSahibiTuru.Tanimsiz, "Basvuru.Summary.Error.ApplicantTypeRequired");
            Eksikse(!b.basvuruFirma.hukukiTurSirketTuru.HasValue || b.basvuruFirma.hukukiTurSirketTuru == enumHukukiTurSirketTuru.Tanimsiz, "Basvuru.Summary.Error.LegalTypeRequired");
            Eksikse(string.IsNullOrWhiteSpace(b.yatirim.yatirimAdi), "Basvuru.Summary.Error.InvestmentNameRequired");
            Eksikse(b.yatirim.yatirimTurleri.Count == 0, "Basvuru.Summary.Error.InvestmentTypeRequired");
            Eksikse(b.YatirimAdresleri.Count == 0, "Basvuru.Summary.Error.InvestmentAddressRequired");
            Eksikse(b.yatirim.harcamaTurleri.Count == 0, "Basvuru.Summary.Error.ExpenseTypeRequired");
            Eksikse(string.IsNullOrWhiteSpace(b.yatirim.yatiriminAmaci), "Basvuru.Summary.Error.InvestmentPurposeRequired");
            Eksikse(string.IsNullOrWhiteSpace(b.yatirim.yatirimFaaliyetleri), "Basvuru.Summary.Error.InvestmentActivitiesRequired");
            Eksikse(string.IsNullOrWhiteSpace(b.yatirim.yatirimGirdileri), "Basvuru.Summary.Error.InvestmentInputsRequired");
            Eksikse(string.IsNullOrWhiteSpace(b.yatirim.yatirimCiktilari), "Basvuru.Summary.Error.InvestmentOutputsRequired");
            Eksikse(!b.yatirim.degerZinciriId.HasValue || b.yatirim.degerZinciriId <= 0, "Basvuru.Summary.Error.ValueChainRequired");
            Eksikse(b.yatirim.degerZinciriAsamalari.Count == 0, "Basvuru.Summary.Error.ValueChainStageRequired");
            Eksikse(!maliOlcekUygun, "Basvuru.Summary.Error.FinancialScaleRequired");
            Eksikse(b.basvuruFirma.donem.destekOrani.GetValueOrDefault() > 0
                && b.finans.talepEdilenFinansmanOrani.GetValueOrDefault() > b.basvuruFirma.donem.destekOrani.GetValueOrDefault(), "Basvuru.Finance.RateLimitExceeded");
            Eksikse(b.finans.yatirimSuresiAy.GetValueOrDefault() <= 0, "Basvuru.Finance.InvestmentDurationRequired");
            Eksikse(!b.mali.bagimsizDenetimeTabiMi.HasValue, "Basvuru.Summary.Error.AuditChoiceRequired");
            Eksikse(b.mali.bagimsizDenetimeTabiMi == true && !b.mali.denetimDosyaId.HasValue, "Basvuru.Summary.Error.AuditFileRequired");
            Eksikse(string.IsNullOrWhiteSpace(b.yatirimOzeti.yatirimOzetiJson), "Basvuru.Summary.Error.InvestmentSummaryRequired");
            Eksikse(string.IsNullOrWhiteSpace(b.dbCtpTeknikProje.dbCtpTeknikProjeJson), "Basvuru.Summary.Error.DbCtpRequired");
            Eksikse(b.ZorunluBelgeler.Any(x => !x.dosyaId.HasValue), "Basvuru.Summary.Error.RequiredDocumentsRequired");
            Eksikse(b.ortaklik.ortaklar.Any(x => string.Equals(x.kisiTuru, "Tüzel Kişi", StringComparison.OrdinalIgnoreCase))
                && b.ortaklik.bagliOrtakDosyalari.Any(x => !x.dosyaId.HasValue), "Basvuru.Summary.Error.LegalPartnerDocumentsRequired");
            Eksikse(b.AdliSicilKisileri.Count == 0, "Basvuru.Summary.Error.CriminalPeopleRequired");
            Eksikse(b.AdliSicilKisileri.Any(x => !x.dosyaId.HasValue), "Basvuru.Summary.Error.CriminalFilesRequired");
            Eksikse(string.IsNullOrWhiteSpace(b.cevreselSosyal.cevreselSosyalJson), "Basvuru.Summary.Error.EsfRequired");
            Eksikse(cevreselKapsamDisi, "Basvuru.Summary.Error.EsfExclusion");
            Eksikse(!b.TaahhutDosyaId.HasValue, "Basvuru.Summary.Error.CommitmentRequired");
            Eksikse(string.IsNullOrWhiteSpace(b.TaahhutBeyanlarJson), "Basvuru.Summary.Error.DeclarationsRequired");
        }

#if false
        public async Task<Sonuc> IncelemeyeGonderAsync(int basvuruId, Kullanici kullanici)
        {
            Sonuc sonuc = new();
            if (basvuruId <= 0)
            {
                HataEkle(sonuc, "Business.Application.RecordRequired");
                return sonuc;
            }

            Sonuc<Basvuru> okumaSonucu = await OkuAsync(basvuruId, kullanici);
            if (!okumaSonucu.basarili || okumaSonucu.nesne == null)
            {
                SonucHatalariniAktar(okumaSonucu, sonuc);
                return sonuc;
            }

            Basvuru basvuru = okumaSonucu.nesne;
            if (!BasvuruKullanicisiMi(kullanici))
                BasvuruKullanicisiYetkiHatasiEkle(sonuc);
            if (basvuru.durum != enumBasvuruDurum.OnBasvuruDurumu &&
                basvuru.durum != enumBasvuruDurum.OnBasvuruDuzeltmeDurumu)
                HataEkle(sonuc, "Business.Application.Submit.AlreadySubmitted");

            IncelemeyeGonderimEksikleriniDogrula(basvuru, sonuc);
            if (!sonuc.basarili) return sonuc;

            try
            {
                await using SqlConnection connection = new(_connectionString);
                await connection.OpenAsync();
                await using SqlTransaction transaction = (SqlTransaction)await connection.BeginTransactionAsync();
                TABBasvuru tabBasvuru = new(connection, _localizer, transaction);
                if (!await tabBasvuru.IncelemeyeGonderAsync(basvuruId))
                {
                    HataEkle(sonuc, "Business.Application.Submit.AlreadySubmitted");
                    await transaction.RollbackAsync();
                    return sonuc;
                }

                TABBasvuruLog tabLog = new(connection, _localizer, transaction);
                await tabLog.EkleAsync(basvuruId, kullanici, "OnBasvuruIncelemeyeGonderildi",
                    new { EskiDurum = enumBasvuruDurum.OnBasvuruDurumu, YeniDurum = enumBasvuruDurum.OnBasvuruIncelemeDurumu });
                await transaction.CommitAsync();
                sonuc.mesaj = Metin("Business.Application.Submit.Success");
            }
            catch (Exception ex)
            {
                BeklenmeyenHata(sonuc, ex,
                    "Ön başvuru incelemeye gönderilemedi. BasvuruId: {BasvuruId}, KullaniciId: {KullaniciId}",
                    "Business.Application.Submit.Failed", basvuruId, kullanici.Id);
            }

            return sonuc;
        }

        private static bool DenetimListesiTamamMi(string? json, bool sistemListesi)
        {
            if (string.IsNullOrWhiteSpace(json)) return false;

            try
            {
                using JsonDocument belge = JsonDocument.Parse(json);
                if (belge.RootElement.ValueKind != JsonValueKind.Array || belge.RootElement.GetArrayLength() == 0)
                    return false;

                string[] gecerliSonuclar = sistemListesi
                    ? ["Tam", "Eksik"]
                    : ["E", "H", "UD"];
                return belge.RootElement.EnumerateArray().All(madde =>
                    madde.TryGetProperty("sonuc", out JsonElement sonuc) &&
                    sonuc.ValueKind == JsonValueKind.String &&
                    gecerliSonuclar.Contains(sonuc.GetString(), StringComparer.OrdinalIgnoreCase));
            }
            catch (JsonException)
            {
                return false;
            }
        }
        private void IncelemeyeGonderimEksikleriniDogrula(Basvuru b, Sonuc sonuc)
        {
            void Eksikse(bool kosul, string kaynak)
            {
                if (kosul) HataEkle(sonuc, kaynak);
            }

            Donem? donem = b.basvuruFirma.donem;
            decimal altLimit = donem?.minimumYatirimTutari ?? 0;
            decimal ustLimit = donem?.maksimumYatirimTutari ?? 0;
            bool MaliDegerUygun(decimal? deger) =>
                deger.HasValue && (altLimit <= 0 || deger.Value >= altLimit) && (ustLimit <= 0 || deger.Value <= ustLimit);
            bool maliOlcekUygun = MaliDegerUygun(b.mali.oncekiYilNetSatis)
                || MaliDegerUygun(b.mali.sonYilNetSatis)
                || MaliDegerUygun(b.mali.oncekiYilAktifToplami)
                || MaliDegerUygun(b.mali.sonYilAktifToplami);

            bool cevreselKapsamDisi = false;
            if (!string.IsNullOrWhiteSpace(b.cevreselSosyal.cevreselSosyalJson))
            {
                try
                {
                    using JsonDocument doc = JsonDocument.Parse(b.cevreselSosyal.cevreselSosyalJson);
                    if (doc.RootElement.TryGetProperty("answers", out JsonElement answers)
                        && answers.TryGetProperty("csf_2_1_global_answer", out JsonElement cevap))
                    {
                        cevreselKapsamDisi = string.Equals(cevap.GetString(), "Evet", StringComparison.OrdinalIgnoreCase)
                            || string.Equals(cevap.GetString(), "Yes", StringComparison.OrdinalIgnoreCase);
                    }
                }
                catch
                {
                    HataEkle(sonuc, "Basvuru.Summary.Error.EsfRequired");
                }
            }

            Eksikse(b.basvuruFirma.donemId <= 0, "Basvuru.Summary.Error.PeriodRequired");
            Eksikse(b.basvuruFirma.ilId <= 0, "Basvuru.Summary.Error.ProvinceRequired");
            Eksikse(b.basvuruFirma.firmaId <= 0, "Basvuru.Summary.Error.CompanyRequired");
            Eksikse(string.IsNullOrWhiteSpace(b.irtibat.kisi), "Basvuru.Summary.Error.ContactPersonRequired");
            Eksikse(string.IsNullOrWhiteSpace(b.irtibat.telefon), "Basvuru.Summary.Error.ContactPhoneRequired");
            Eksikse(!b.basvuruFirma.sonIkiYildirFaalMi.HasValue, "Basvuru.Summary.Error.ActiveTwoYearsRequired");
            Eksikse(!b.basvuruFirma.basvuruSahibiTuru.HasValue || b.basvuruFirma.basvuruSahibiTuru == enumBasvuruSahibiTuru.Tanimsiz, "Basvuru.Summary.Error.ApplicantTypeRequired");
            Eksikse(!b.basvuruFirma.hukukiTurSirketTuru.HasValue || b.basvuruFirma.hukukiTurSirketTuru == enumHukukiTurSirketTuru.Tanimsiz, "Basvuru.Summary.Error.LegalTypeRequired");
            Eksikse(string.IsNullOrWhiteSpace(b.yatirim.yatirimAdi), "Basvuru.Summary.Error.InvestmentNameRequired");
            Eksikse(b.yatirim.yatirimTurleri.Count == 0, "Basvuru.Summary.Error.InvestmentTypeRequired");
            Eksikse(b.YatirimAdresleri.Count == 0, "Basvuru.Summary.Error.InvestmentAddressRequired");
            Eksikse(b.yatirim.harcamaTurleri.Count == 0, "Basvuru.Summary.Error.ExpenseTypeRequired");
            Eksikse(string.IsNullOrWhiteSpace(b.yatirim.yatiriminAmaci), "Basvuru.Summary.Error.InvestmentPurposeRequired");
            Eksikse(string.IsNullOrWhiteSpace(b.yatirim.yatirimFaaliyetleri), "Basvuru.Summary.Error.InvestmentActivitiesRequired");
            Eksikse(string.IsNullOrWhiteSpace(b.yatirim.yatirimGirdileri), "Basvuru.Summary.Error.InvestmentInputsRequired");
            Eksikse(string.IsNullOrWhiteSpace(b.yatirim.yatirimCiktilari), "Basvuru.Summary.Error.InvestmentOutputsRequired");
            Eksikse(!b.yatirim.degerZinciriId.HasValue || b.yatirim.degerZinciriId <= 0, "Basvuru.Summary.Error.ValueChainRequired");
            Eksikse(b.yatirim.degerZinciriAsamalari.Count == 0, "Basvuru.Summary.Error.ValueChainStageRequired");
            Eksikse(!maliOlcekUygun, "Basvuru.Summary.Error.FinancialScaleRequired");
            Eksikse(b.basvuruFirma.donem.destekOrani.GetValueOrDefault() > 0
                && b.finans.talepEdilenFinansmanOrani.GetValueOrDefault() > b.basvuruFirma.donem.destekOrani.GetValueOrDefault(), "Basvuru.Finance.RateLimitExceeded");
            Eksikse(b.finans.yatirimSuresiAy.GetValueOrDefault() <= 0, "Basvuru.Finance.InvestmentDurationRequired");
            Eksikse(!b.mali.bagimsizDenetimeTabiMi.HasValue, "Basvuru.Summary.Error.AuditChoiceRequired");
            Eksikse(b.mali.bagimsizDenetimeTabiMi == true && !b.mali.denetimDosyaId.HasValue, "Basvuru.Summary.Error.AuditFileRequired");
            Eksikse(string.IsNullOrWhiteSpace(b.yatirimOzeti.yatirimOzetiJson), "Basvuru.Summary.Error.InvestmentSummaryRequired");
            Eksikse(string.IsNullOrWhiteSpace(b.dbCtpTeknikProje.dbCtpTeknikProjeJson), "Basvuru.Summary.Error.DbCtpRequired");
            Eksikse(b.ZorunluBelgeler.Any(x => !x.dosyaId.HasValue), "Basvuru.Summary.Error.RequiredDocumentsRequired");
            Eksikse(b.ortaklik.ortaklar.Any(x => string.Equals(x.kisiTuru, "Tüzel Kişi", StringComparison.OrdinalIgnoreCase))
                && b.ortaklik.bagliOrtakDosyalari.Any(x => !x.dosyaId.HasValue), "Basvuru.Summary.Error.LegalPartnerDocumentsRequired");
            Eksikse(b.AdliSicilKisileri.Count == 0, "Basvuru.Summary.Error.CriminalPeopleRequired");
            Eksikse(b.AdliSicilKisileri.Any(x => !x.dosyaId.HasValue), "Basvuru.Summary.Error.CriminalFilesRequired");
            Eksikse(string.IsNullOrWhiteSpace(b.cevreselSosyal.cevreselSosyalJson), "Basvuru.Summary.Error.EsfRequired");
            Eksikse(cevreselKapsamDisi, "Basvuru.Summary.Error.EsfExclusion");
            Eksikse(!b.TaahhutDosyaId.HasValue, "Basvuru.Summary.Error.CommitmentRequired");
            Eksikse(string.IsNullOrWhiteSpace(b.TaahhutBeyanlarJson), "Basvuru.Summary.Error.DeclarationsRequired");
        }
#endif

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
            else if (firmaId <= 0 && !OrtakFonksiyonlar.VKNGecerliMi(vergiKimlikNo))
            {
                sonuc.HataEkle("VKN 10 haneli ve geçerli olmalıdır.");
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
                    await tabFirmaLog.EkleAsync(firma, "YeniKayit", kullaniciId);

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

                if (mevcut != null && AnaBasvuruBilgileriDegisti(mevcut.basvuruFirma, firmaBasvuru))
                {
                    sonuc.HataEkle("Başvuru dönemi, il ve firma ilk kayıttan sonra değiştirilemez.");
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

                if (mevcut?.kayitTuru == enumBasvuruKayitTuru.Basvuru)
                {
                    bool degisiklikVar = mevcut.basvuruFirma.onBasvuruSonrasiDegisiklikVarMi == true ||
                        basvuru.basvuruFirma.onBasvuruSonrasiDegisiklikVarMi == true ||
                        BasvuruSahibiBilgileriDegisti(mevcut, basvuru);
                    basvuru.basvuruFirma.onBasvuruSonrasiDegisiklikVarMi = degisiklikVar;
                    basvuru.basvuruFirma.onBasvuruSonrasiDegisiklikSebebi =
                        basvuru.basvuruFirma.onBasvuruSonrasiDegisiklikSebebi?.Trim();

                    if (degisiklikVar && string.IsNullOrWhiteSpace(basvuru.basvuruFirma.onBasvuruSonrasiDegisiklikSebebi))
                    {
                        sonuc.HataEkle("Başvuru sahibi bilgileri değiştirildiği için değişiklik gerekçesi girilmelidir.");
                        return sonuc;
                    }

                    if (!degisiklikVar)
                        basvuru.basvuruFirma.onBasvuruSonrasiDegisiklikSebebi = "";
                }
                if (mevcut != null && AnaBasvuruBilgileriDegisti(mevcut.basvuruFirma, basvuru.basvuruFirma))
                {
                    sonuc.HataEkle("Başvuru dönemi, il ve firma ilk kayıttan sonra değiştirilemez.");
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
                if (finans.toplamYatirimTutari.HasValue && finans.talepEdilenFinansmanOrani.HasValue)
                {
                    finans.talepEdilenDestekTutari = Math.Round(finans.toplamYatirimTutari.Value * finans.talepEdilenFinansmanOrani.Value / 100m, 2);
                    finans.basvuruSahibiKatkisi = finans.toplamYatirimTutari.Value - finans.talepEdilenDestekTutari.Value;
                    finans.onBasvuruSahibiKatkisi = finans.basvuruSahibiKatkisi;
                }
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

                decimal azamiFinansmanOrani = mevcut?.basvuruFirma.donem.destekOrani.GetValueOrDefault() ?? 0;
                if (azamiFinansmanOrani > 0 && finans.talepEdilenFinansmanOrani.GetValueOrDefault() > azamiFinansmanOrani)
                {
                    sonuc.HataEkle($"Talep edilen finansman oranı dönem için tanımlanan %{azamiFinansmanOrani:0.##} oranını aşamaz.");
                    return sonuc;
                }
                finans.digerFinansmanKaynaklariAciklama = mevcut?.finans.digerFinansmanKaynaklariAciklama;

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

        public async Task<Sonuc<int>> KaydetDbCtpTeknikProjeAsync(BasvuruDbCtpTeknikProje teknikProje, Kullanici kullanici)
        {
            Sonuc<int> sonuc = new Sonuc<int>();
            if (kullanici == null)
            {
                HataEkle(sonuc, "Business.User.InfoMissing");
                return sonuc;
            }
            try
            {
                teknikProje.Dogrula(sonuc);
                if (!string.IsNullOrWhiteSpace(teknikProje.dbCtpTeknikProjeJson))
                {
                    try
                    {
                        using JsonDocument document = JsonDocument.Parse(teknikProje.dbCtpTeknikProjeJson);
                        JsonElement root = document.RootElement;
                        bool MetinVar(string alan) => root.TryGetProperty(alan, out JsonElement value)
                            && value.ValueKind == JsonValueKind.String
                            && !string.IsNullOrWhiteSpace(value.GetString());
                        bool DoluSatirVar(string liste, params string[] alanlar) => root.TryGetProperty(liste, out JsonElement rows)
                            && rows.ValueKind == JsonValueKind.Array
                            && rows.EnumerateArray().Any(row => alanlar.All(alan => row.TryGetProperty(alan, out JsonElement value)
                                && value.ValueKind == JsonValueKind.String
                                && !string.IsNullOrWhiteSpace(value.GetString())));

                        if (!MetinVar("investmentName"))
                            sonuc.HataEkle("DB C-TP Teknik Proje için yatırımın adı girilmelidir.");
                        if (!DoluSatirVar("plannedProducts", "product", "capacity"))
                            sonuc.HataEkle("Yatırım sonrası üretilecek en az bir ürün ve kapasitesi girilmelidir.");
                        if (!DoluSatirVar("machineryRows", "name", "purpose") && !DoluSatirVar("buildingRows", "name"))
                            sonuc.HataEkle("En az bir makine-ekipman veya bina/yapı satırı girilmelidir.");
                    }
                    catch (JsonException)
                    {
                        sonuc.HataEkle("DB C-TP Teknik Proje verisi okunamadı.");
                    }
                }
                if (!sonuc.basarili)
                    return sonuc;

                await using SqlConnection connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();
                Basvuru? mevcut = await BasvuruOnBasvuruYetkiKontrolAsync(connection, teknikProje.basvuruId, kullanici, sonuc);
                if (!sonuc.basarili || mevcut == null)
                    return sonuc;

                await using SqlTransaction transaction = (SqlTransaction)await connection.BeginTransactionAsync();
                try
                {
                    TABBasvuru tabBasvuru = new TABBasvuru(connection, null, transaction);
                    await tabBasvuru.DbCtpTeknikProjeKaydetAsync(teknikProje);
                    TABBasvuruLog tabBasvuruLog = new TABBasvuruLog(connection, null, transaction);
                    await tabBasvuruLog.EkleAsync(teknikProje.basvuruId, kullanici, "KaydetDbCtpTeknikProjeAsync", teknikProje);
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
                BeklenmeyenHata(sonuc, ex, "DB C-TP Teknik Proje kaydedilemedi. BasvuruId: {BasvuruId}, KullaniciId: {KullaniciId}", "Başvuru kaydedilemedi.", teknikProje.basvuruId, kullanici.Id);
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
            Sonuc<int> sonuc = new();
            if (kullanici == null) { HataEkle(sonuc, "Business.User.InfoMissing"); return sonuc; }
            ortaklik ??= new BasvuruOrtaklik();
            ortaklik.ortaklar ??= new List<BasvuruOrtak>();
            if (ortaklik.basvuruId <= 0) HataEkle(sonuc, "Business.Application.RecordRequired");
            if (ortaklik.ortaklar.Count != 1) sonuc.HataEkle("Kaydetme isteğinde yalnızca bir ortak gönderilmelidir.");
            if (!sonuc.basarili) return sonuc;

            try
            {
                await using SqlConnection connection = new(_connectionString);
                await connection.OpenAsync();
                Basvuru? mevcut = await BasvuruOnBasvuruYetkiKontrolAsync(connection, ortaklik.basvuruId, kullanici, sonuc);
                if (!sonuc.basarili || mevcut == null) return sonuc;

                BasvuruOrtak ortak = ortaklik.ortaklar[0];
                ortak.sahiplikNiteligi = ortak.SahiplikNiteligiHesapla(DateTime.Today);
                string kimlik = TcknVknNormalizeEt(ortak.tcknVkn);
                BasvuruOrtak? eski = ortak.id > 0
                    ? mevcut.ortaklik.ortaklar.FirstOrDefault(x => x.id == ortak.id)
                    : null;
                eski ??= mevcut.ortaklik.ortaklar.FirstOrDefault(x =>
                    !string.IsNullOrWhiteSpace(kimlik) &&
                    TcknVknNormalizeEt(x.tcknVkn) == kimlik);

                if (eski == null)
                {
                    ortak.id = 0;
                    ortak.siraNo = mevcut.ortaklik.ortaklar.Select(x => x.siraNo).DefaultIfEmpty(0).Max() + 1;
                    mevcut.ortaklik.ortaklar.Add(ortak);
                }
                else
                {
                    ortak.id = eski.id;
                    ortak.siraNo = eski.siraNo;
                    mevcut.ortaklik.ortaklar[mevcut.ortaklik.ortaklar.IndexOf(eski)] = ortak;
                }

                mevcut.ortaklik.Dogrula(sonuc, ortak.siraNo);
                if (!sonuc.basarili) return sonuc;

                await using SqlTransaction transaction = (SqlTransaction)await connection.BeginTransactionAsync();
                TABBasvuru tabBasvuru = new(connection, null, transaction);
                int ortakId = await tabBasvuru.BasvuruOrtakiKaydetAsync(ortaklik.basvuruId, ortak);
                if (ortakId <= 0) { await transaction.RollbackAsync(); sonuc.HataEkle("Ortak kaydı kaydedilemedi."); return sonuc; }
                await tabBasvuru.OrtaklikKaydetAsync(mevcut, false);
                await new TABBasvuruLog(connection, null, transaction).EkleAsync(ortaklik.basvuruId, kullanici, "KaydetOrtakAsync", new { OrtakId = ortakId, ortak });
                await transaction.CommitAsync();
                sonuc.nesne = ortakId;
            }
            catch (Exception ex)
            {
                BeklenmeyenHata(sonuc, ex, "Ortak/pay sahibi bilgisi kaydedilemedi. BasvuruId: {BasvuruId}", "Ortak/pay sahibi bilgisi kaydedilemedi.", ortaklik.basvuruId);
            }
            return sonuc;
        }

        public async Task<Sonuc<int>> KaydetBasvuruBagliOrtakAsync(BasvuruOrtak ortak, Kullanici kullanici)
        {
            Sonuc<int> sonuc = new();
            if (kullanici == null) { HataEkle(sonuc, "Business.User.InfoMissing"); return sonuc; }
            if (ortak == null || ortak.basvuruId <= 0) { HataEkle(sonuc, "Business.Application.RecordRequired"); return sonuc; }

            ortak.adUnvan = ortak.adUnvan?.Trim() ?? "";
            ortak.tcknVkn = TcknVknNormalizeEt(ortak.tcknVkn);
            ortak.iliskiTuru = ortak.iliskiTuru?.Trim() ?? "";
            ortak.belgeReferansi = ortak.belgeReferansi?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(ortak.adUnvan)) sonuc.HataEkle("İşletme unvanı girilmelidir.");
            if (ortak.tcknVkn.Length != 10 || !ortak.tcknVkn.All(char.IsDigit)) sonuc.HataEkle("10 haneli VKN girilmelidir.");
            if (ortak.iliskiTuru != "Bağlı İşletme" && ortak.iliskiTuru != "Ortak İşletme") sonuc.HataEkle("İlişki türü seçilmelidir.");
            if (!ortak.hesabaDahilOran.HasValue || ortak.hesabaDahilOran < 0 || ortak.hesabaDahilOran > 100) sonuc.HataEkle("Dahil oranı 0 ile 100 arasında olmalıdır.");
            if (new[] { ortak.oncekiYilNetSatis, ortak.sonYilNetSatis, ortak.oncekiYilAktifToplami, ortak.sonYilAktifToplami }.Any(x => x.HasValue && x < 0))
                sonuc.HataEkle("Mali tutarlar negatif olamaz.");
            if (!sonuc.basarili) return sonuc;

            try
            {
                await using SqlConnection connection = new(_connectionString);
                await connection.OpenAsync();
                Basvuru? mevcut = await BasvuruOnBasvuruYetkiKontrolAsync(connection, ortak.basvuruId, kullanici, sonuc);
                if (!sonuc.basarili || mevcut == null) return sonuc;
                if (mevcut.kayitTuru != enumBasvuruKayitTuru.Basvuru)
                {
                    sonuc.HataEkle("Bağlı/ortak işletme mali bilgileri yalnızca başvuru aşamasında güncellenebilir.");
                    return sonuc;
                }

                BasvuruOrtak? kayitli = ortak.id > 0 ? mevcut.ortaklik.ortaklar.FirstOrDefault(x => x.id == ortak.id) : null;
                if (ortak.id > 0 && kayitli == null) { sonuc.HataEkle("Güncellenecek bağlı/ortak işletme kaydı bulunamadı."); return sonuc; }
                if (kayitli != null)
                {
                    ortak.siraNo = kayitli.siraNo;
                    ortak.adUnvan = kayitli.adUnvan;
                    ortak.tcknVkn = kayitli.tcknVkn;
                    ortak.kisiTuru = kayitli.kisiTuru;
                    ortak.payOrani = kayitli.payOrani;
                    ortak.ozelKamuNiteligi = kayitli.ozelKamuNiteligi;
                    ortak.nihaiFaydalaniciBilgisi = kayitli.nihaiFaydalaniciBilgisi;
                    ortak.uboKycBelgeAdi = kayitli.uboKycBelgeAdi;
                    ortak.uboKycDosyaId = kayitli.uboKycDosyaId;
                }
                else
                {
                    if (mevcut.ortaklik.ortaklar.Any(x => TcknVknNormalizeEt(x.tcknVkn) == ortak.tcknVkn))
                    {
                        sonuc.HataEkle("Bu VKN ile kayıtlı bir işletme zaten bulunmaktadır.");
                        return sonuc;
                    }
                    ortak.id = 0;
                    ortak.siraNo = mevcut.ortaklik.ortaklar.Select(x => x.siraNo).DefaultIfEmpty(0).Max() + 1;
                    ortak.kisiTuru = "Tüzel Kişi";
                    ortak.payOrani = 0;
                    ortak.ozelKamuNiteligi = "Özel";
                }

                await using SqlTransaction transaction = (SqlTransaction)await connection.BeginTransactionAsync();
                TABBasvuru tabBasvuru = new(connection, null, transaction);
                int ortakId = await tabBasvuru.BasvuruOrtakiKaydetAsync(ortak.basvuruId, ortak);
                if (ortakId <= 0) { await transaction.RollbackAsync(); sonuc.HataEkle("Bağlı/ortak işletme kaydı kaydedilemedi."); return sonuc; }
                await new TABBasvuruLog(connection, null, transaction).EkleAsync(ortak.basvuruId, kullanici, "KaydetBasvuruBagliOrtakAsync", new { OrtakId = ortakId, ortak });
                await transaction.CommitAsync();
                sonuc.nesne = ortakId;
            }
            catch (Exception ex)
            {
                BeklenmeyenHata(sonuc, ex, "Başvuru bağlı/ortak işletme mali bilgisi kaydedilemedi. BasvuruId: {BasvuruId}", "Bağlı/ortak işletme mali bilgisi kaydedilemedi.", ortak.basvuruId);
            }
            return sonuc;
        }
        public async Task<Sonuc<int>> OrtakSilAsync(int basvuruId, int ortakId, Kullanici kullanici)
        {
            Sonuc<int> sonuc = new();
            try
            {
                await using SqlConnection connection = new(_connectionString);
                await connection.OpenAsync();
                Basvuru? mevcut = await BasvuruOnBasvuruYetkiKontrolAsync(connection, basvuruId, kullanici, sonuc);
                if (!sonuc.basarili || mevcut == null) return sonuc;
                BasvuruOrtak? ortak = mevcut.ortaklik.ortaklar.FirstOrDefault(x => x.id == ortakId);
                if (ortak == null) { sonuc.HataEkle("Silinecek ortak kaydı bulunamadı."); return sonuc; }
                mevcut.ortaklik.ortaklar.Remove(ortak);
                mevcut.ortaklik.Dogrula(sonuc);
                if (!sonuc.basarili) return sonuc;
                await using SqlTransaction transaction = (SqlTransaction)await connection.BeginTransactionAsync();
                TABBasvuru tabBasvuru = new(connection, null, transaction);
                await tabBasvuru.BasvuruOrtakiSilAsync(basvuruId, ortakId);
                await tabBasvuru.OrtaklikKaydetAsync(mevcut, false);
                await transaction.CommitAsync();
                sonuc.nesne = ortakId;
            }
            catch (Exception ex) { BeklenmeyenHata(sonuc, ex, "Ortak silinemedi. BasvuruId: {BasvuruId}", "Ortak silinemedi.", basvuruId); }
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

                if (YatirimYeriBelgeFormAdMi(formAd))
                {
                    if (!int.TryParse(formAd[BasvuruYatirimYeriFormAdPrefix.Length..], out int adresId)
                        || mevcut.YatirimAdresleri.All(x => x.id != adresId))
                    {
                        sonuc.HataEkle("Önce yatırım yeri kaydedilmelidir.");
                        return sonuc;
                    }
                }
                if (MaliOrtakBelgeFormAdMi(formAd))
                {
                    string idMetni = formAd[BasvuruMaliOrtakBelgeFormAdPrefix.Length..];
                    if (!int.TryParse(idMetni, out int ortakId)
                        || mevcut.ortaklik.ortaklar.All(x => x.id != ortakId))
                    {
                        HataEkle(sonuc, "Business.Application.PartnerRecordRequired");
                        return sonuc;
                    }
                }

                if (UboKycFormAdMi(formAd))
                {
                    string tcknVkn = UboKycFormAdKimlikOku(formAd);
                    if (!await BasvuruOrtakKimlikVarMiAsync(connection, basvuruId, tcknVkn))
                    {
                        HataEkle(sonuc, "Business.Application.UboKycPartnerRequired");
                        return sonuc;
                    }
                }

                if ((string.Equals(formAd, BasvuruAdliSicilFormAd, StringComparison.OrdinalIgnoreCase) || string.Equals(formAd, BasvuruImzaYetkiFormAd, StringComparison.OrdinalIgnoreCase))
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

                if (string.Equals(formAd, BasvuruMaliBelgeFormAd, StringComparison.OrdinalIgnoreCase))
                {
                    Dictionary<string, BasvuruMaliBelgeReferansi> referanslar;
                    try { referanslar = JsonSerializer.Deserialize<Dictionary<string, BasvuruMaliBelgeReferansi>>(mevcut.mali.belgeReferanslariJson ?? "") ?? new(); }
                    catch { referanslar = new(); }
                    string[] anahtarlar = ["netSatis.onceki", "netSatis.son", "aktif.onceki", "aktif.son", "ihracat.onceki", "ihracat.son", "calisan.onceki", "calisan.son"];
                    if (dosyaNo >= 1 && dosyaNo <= anahtarlar.Length)
                    {
                        referanslar[anahtarlar[dosyaNo - 1]] = new BasvuruMaliBelgeReferansi { dosyaId = dosyaSonuc.nesne.Id, dosyaAdi = dosyaSonuc.nesne.DosyaAdi };
                        mevcut.mali.belgeReferanslariJson = JsonSerializer.Serialize(referanslar);
                        await new TABBasvuru(connection).BasvuruMaliGuncelleAsync(mevcut.mali);
                    }
                }

                if (MaliOrtakBelgeFormAdMi(formAd))
                {
                    string idMetni = formAd[BasvuruMaliOrtakBelgeFormAdPrefix.Length..];
                    if (int.TryParse(idMetni, out int ortakId))
                    {
                        BasvuruOrtak? ortak = mevcut.ortaklik.ortaklar.FirstOrDefault(x => x.id == ortakId);
                        if (ortak != null)
                        {
                            ortak.belgeReferansi = JsonSerializer.Serialize(new BasvuruMaliBelgeReferansi { dosyaId = dosyaSonuc.nesne.Id, dosyaAdi = dosyaSonuc.nesne.DosyaAdi });
                            await new TABBasvuru(connection).BasvuruOrtakiKaydetAsync(basvuruId, ortak);
                        }
                    }
                }
                if (YatirimYeriBelgeFormAdMi(formAd)
                    && int.TryParse(formAd[BasvuruYatirimYeriFormAdPrefix.Length..], out int yatirimYeriId))
                {
                    await new TABBasvuru(connection).YatirimYeriDosyasiGuncelleAsync(basvuruId, yatirimYeriId, dosyaNo, dosyaSonuc.nesne.Id, dosyaSonuc.nesne.DosyaAdi);
                }
                if (string.Equals(formAd, BasvuruImzaYetkiFormAd, StringComparison.OrdinalIgnoreCase))
                {
                    await new TABBasvuru(connection).BasvuruImzaYetkiDosyasiGuncelleAsync(basvuruId, dosyaNo, dosyaSonuc.nesne.Id, dosyaSonuc.nesne.DosyaAdi);
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

        public async Task<Sonuc<int>> KaydetTaahhutBeyanlariAsync(BasvuruTaahhutBeyanlar beyanlar, Kullanici kullanici)
        {
            Sonuc<int> sonuc = new Sonuc<int>();
            if (kullanici == null)
            {
                HataEkle(sonuc, "Business.User.InfoMissing");
                return sonuc;
            }
            try
            {
                beyanlar.Dogrula(sonuc);
                if (!string.IsNullOrWhiteSpace(beyanlar.taahhutBeyanlarJson))
                {
                    try
                    {
                        using JsonDocument document = JsonDocument.Parse(beyanlar.taahhutBeyanlarJson);
                        JsonElement root = document.RootElement;
                        bool Onayli(string alan) => root.ValueKind == JsonValueKind.Object
                            && root.TryGetProperty(alan, out JsonElement value)
                            && value.ValueKind == JsonValueKind.True;
                        if (!Onayli("dogrulukBeyani")) sonuc.HataEkle("Başvuru bilgilerinin doğruluğu ve eksiksizliği beyan edilmelidir.");
                        if (!Onayli("cifteFinansmanBeyani")) sonuc.HataEkle("Çifte finansman bulunmadığı beyan edilmelidir.");
                        if (!Onayli("bankaVeriPaylasimRizasi")) sonuc.HataEkle("Banka veri paylaşım rızası verilmelidir.");
                        if (!Onayli("izlemeDenetimKabulu")) sonuc.HataEkle("İzleme, raporlama ve denetim süreçleri kabul edilmelidir.");
                    }
                    catch (JsonException)
                    {
                        sonuc.HataEkle("Taahhüt ve beyan verisi okunamadı.");
                    }
                }
                if (!sonuc.basarili) return sonuc;
                await using SqlConnection connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();
                Basvuru? mevcut = await BasvuruOnBasvuruYetkiKontrolAsync(connection, beyanlar.basvuruId, kullanici, sonuc);
                if (!sonuc.basarili || mevcut == null) return sonuc;
                await using SqlTransaction transaction = (SqlTransaction)await connection.BeginTransactionAsync();
                try
                {
                    TABBasvuru tabBasvuru = new TABBasvuru(connection, null, transaction);
                    await tabBasvuru.TaahhutBeyanlariKaydetAsync(beyanlar);
                    TABBasvuruLog tabBasvuruLog = new TABBasvuruLog(connection, null, transaction);
                    await tabBasvuruLog.EkleAsync(beyanlar.basvuruId, kullanici, "KaydetTaahhutBeyanlariAsync", beyanlar);
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
                BeklenmeyenHata(sonuc, ex, "Taahhüt beyanları kaydedilemedi. BasvuruId: {BasvuruId}, KullaniciId: {KullaniciId}", "Başvuru kaydedilemedi.", beyanlar.basvuruId, kullanici.Id);
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

                Basvuru? mevcut = await BasvuruGoruntulemeYetkiKontrolAsync(connection, basvuruId, kullanici, sonuc);
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
            if (adres == null)
            {
                HataEkle(sonuc, "Adres Gelmedi");
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
                BeklenmeyenHata(sonuc, ex, "Uygulama adresi kaydedilemedi. BasvuruId: {BasvuruId}, KullaniciId: {KullaniciId}", "Uygulama adresi kaydedilemedi.", adres==null?"NULL": adres.basvuruId, kullanici.Id);
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


        private static bool AnaBasvuruBilgileriDegisti(BasvuruFirma mevcut, BasvuruFirma gelen)
        {
            return mevcut.donemId != gelen.donemId ||
                   mevcut.ilId != gelen.ilId ||
                   mevcut.firmaId != gelen.firmaId;
        }
        private static bool BasvuruSahibiBilgileriDegisti(Basvuru mevcut, Basvuru gelen)
        {
            static string Metin(string? deger) => (deger ?? "").Trim();
            return mevcut.basvuruFirma.firmaId != gelen.basvuruFirma.firmaId ||
                   mevcut.basvuruFirma.donemId != gelen.basvuruFirma.donemId ||
                   mevcut.basvuruFirma.ilId != gelen.basvuruFirma.ilId ||
                   mevcut.basvuruFirma.sonIkiYildirFaalMi != gelen.basvuruFirma.sonIkiYildirFaalMi ||
                   mevcut.basvuruFirma.basvuruSahibiTuru != gelen.basvuruFirma.basvuruSahibiTuru ||
                   mevcut.basvuruFirma.hukukiTurSirketTuru != gelen.basvuruFirma.hukukiTurSirketTuru ||
                   Metin(mevcut.irtibat.kisi) != Metin(gelen.irtibat.kisi) ||
                   Metin(mevcut.irtibat.unvan) != Metin(gelen.irtibat.unvan) ||
                   Metin(mevcut.irtibat.telefon) != Metin(gelen.irtibat.telefon) ||
                   Metin(mevcut.irtibat.ePosta) != Metin(gelen.irtibat.ePosta) ||
                   Metin(mevcut.irtibat.adres) != Metin(gelen.irtibat.adres);
        }
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

            bool duzenlenebilir = mevcut.durum == enumBasvuruDurum.OnBasvuruDurumu ||
                mevcut.durum == enumBasvuruDurum.OnBasvuruDuzeltmeDurumu ||
                (mevcut.durum == enumBasvuruDurum.BasvuruDurumu && mevcut.kayitTuru == enumBasvuruKayitTuru.Basvuru);
            if (!duzenlenebilir)
            {
                sonuc.HataEkle("Bu kayıt mevcut aşamasında güncellenemez.");
                return null;
            }

            if (mevcut.basvuruFirma.siraNo != 0)
            {
                sonuc.HataEkle("Bu başvuru sürümü arşivlenmiştir ve salt okunurdur.");
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

        private async Task<Basvuru?> BasvuruGoruntulemeYetkiKontrolAsync(
            SqlConnection connection,
            int basvuruId,
            Kullanici kullanici,
            Sonuc sonuc)
        {
            TABBasvuru tabBasvuru = new(connection, _localizer);
            Basvuru? mevcut = await tabBasvuru.OkuAsync(basvuruId);
            if (mevcut == null)
            {
                HataEkle(sonuc, "Business.Application.NotFoundOrUnauthorized");
                return null;
            }

            if (BasvuruKullanicisiMi(kullanici) && mevcut.basvuruFirma.firmaId > 0)
            {
                TABFirmaKullanici tabFirmaKullanici = new(connection);
                if (!await tabFirmaKullanici.IliskiVarMiAsync(mevcut.basvuruFirma.firmaId, kullanici.Id))
                {
                    HataEkle(sonuc, "Business.Application.ViewUnauthorized");
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

            if (string.Equals(formAd, BasvuruMaliBelgeFormAd, StringComparison.OrdinalIgnoreCase) && dosyaNo >= 1 && dosyaNo <= 8)
                return "Mali veri belge referansı";

            if (YatirimYeriBelgeFormAdMi(formAd) && dosyaNo >= 1 && dosyaNo <= 3)
                return dosyaNo switch { 1 => "Yatırım yeri belge referansı", 2 => "Tapu/kira/tahsis referansı", _ => "İzin/ruhsat kanıt referansı" };

            if (MaliOrtakBelgeFormAdMi(formAd) && dosyaNo == 1)
                return "Bağlı/ortak işletme mali belge referansı";

            if (string.Equals(formAd, BasvuruAdliSicilFormAd, StringComparison.OrdinalIgnoreCase) && dosyaNo > 0)
                return Metin("Basvuru.Criminal.FileColumn");

            if (string.Equals(formAd, BasvuruImzaYetkiFormAd, StringComparison.OrdinalIgnoreCase) && dosyaNo > 0)
                return "İmza/yetki belgesi";

            if (string.Equals(formAd, BasvuruOrtakUboKycFormAd, StringComparison.OrdinalIgnoreCase) && dosyaNo > 0)
                return Metin("Business.Application.UboKycDocument");

            if (UboKycFormAdMi(formAd) && dosyaNo == 1)
                return Metin("Business.Application.UboKycDocument");

            return "";
        }

        private static bool YatirimYeriBelgeFormAdMi(string? formAd)
        {
            return !string.IsNullOrWhiteSpace(formAd)
                && formAd.StartsWith(BasvuruYatirimYeriFormAdPrefix, StringComparison.OrdinalIgnoreCase)
                && formAd.Length > BasvuruYatirimYeriFormAdPrefix.Length;
        }
        private static bool MaliOrtakBelgeFormAdMi(string? formAd)
        {
            return !string.IsNullOrWhiteSpace(formAd)
                && formAd.StartsWith(BasvuruMaliOrtakBelgeFormAdPrefix, StringComparison.OrdinalIgnoreCase)
                && formAd.Length > BasvuruMaliOrtakBelgeFormAdPrefix.Length;
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
                || string.Equals(formAd, BasvuruMaliBelgeFormAd, StringComparison.OrdinalIgnoreCase)
                || MaliOrtakBelgeFormAdMi(formAd)
                || YatirimYeriBelgeFormAdMi(formAd)
                || string.Equals(formAd, BasvuruAdliSicilFormAd, StringComparison.OrdinalIgnoreCase)
                || string.Equals(formAd, BasvuruImzaYetkiFormAd, StringComparison.OrdinalIgnoreCase)
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

