using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using TarimDonusum.Models;
using TarimDonusum.Tablolar;

namespace TarimDonusum.IsKurallari
{
    public class TanimIsKurallari
    {
        private readonly string _connectionString;
        private readonly ILogger<TanimIsKurallari> _logger;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public TanimIsKurallari(IConfiguration configuration, ILogger<TanimIsKurallari> logger, IStringLocalizer<SharedResource> localizer)
        {
            _logger = logger;
            _localizer = localizer;
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        }

        public async Task<Sonuc<List<Birim>>> BirimleriListeleAsync(Kullanici? kullanici)
        {
            Sonuc<List<Birim>> sonuc = new Sonuc<List<Birim>>();

            try
            {
                if (!SistemYoneticisiMi(kullanici, sonuc))
                    return sonuc;

                await using SqlConnection connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                TABBirim tabBirim = new TABBirim(connection, _localizer);
                sonuc.nesne = await tabBirim.ListeleAsync(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Birim listesi okunamadı.");
                sonuc.HataEkle(Metin("Business.Unit.ListFailed"));
            }

            return sonuc;
        }

        public async Task<Sonuc<List<Il>>> IlleriIlceleriyleListeleAsync(Kullanici? kullanici)
        {
            Sonuc<List<Il>> sonuc = new();
            try
            {
                await using SqlConnection connection = new(_connectionString);
                await connection.OpenAsync();
                List<Il> iller = await new TABIl(connection, _localizer).ListeleAsync(false);
                List<Ilce> ilceler = await new TABIlce(connection, _localizer).ListeleAsync(null, false);
                foreach (Il il in iller)
                    il.ilceler = ilceler.Where(x => x.IlId == il.id).ToList();
                sonuc.nesne = iller;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "İl ve ilçe listesi okunamadı.");
                sonuc.HataEkle("İl ve ilçe listesi okunamadı.");
            }
            return sonuc;
        }

        public async Task<Sonuc<int>> IlKaydetAsync(Il il, Kullanici? kullanici)
        {
            Sonuc<int> sonuc = new();
            if (!SistemYoneticisiMi(kullanici, sonuc))
                return sonuc;

            il.ad = il.ad?.Trim() ?? "";
            il.ilceler ??= new();
            if (il.kod <= 0) sonuc.HataEkle("İl kodu zorunludur.");
            if (string.IsNullOrWhiteSpace(il.ad)) sonuc.HataEkle("İl adı zorunludur.");
            if (il.ilceler.Any(x => string.IsNullOrWhiteSpace(x.Ad)))
                sonuc.HataEkle("İlçe adı boş bırakılamaz.");
            if (il.ilceler.GroupBy(x => x.Ad.Trim(), StringComparer.CurrentCultureIgnoreCase).Any(x => x.Count() > 1))
                sonuc.HataEkle("Aynı ilçe adı birden fazla kez kullanılamaz.");
            if (!sonuc.basarili) return sonuc;

            try
            {
                await using SqlConnection connection = new(_connectionString);
                await connection.OpenAsync();
                await using SqlTransaction transaction = (SqlTransaction)await connection.BeginTransactionAsync();
                TABIl tabIl = new(connection, _localizer, transaction);
                TABIlce tabIlce = new(connection, _localizer, transaction);

                if (il.id > 0)
                {
                    if (!await tabIl.GuncelleAsync(il))
                    {
                        sonuc.HataEkle("İl bulunamadı.");
                        await transaction.RollbackAsync();
                        return sonuc;
                    }
                }
                else
                {
                    il.id = await tabIl.EkleAsync(il);
                }

                foreach (Ilce ilce in il.ilceler)
                {
                    ilce.Ad = ilce.Ad.Trim();
                    ilce.IlId = il.id;
                    if (ilce.Id > 0)
                    {
                        if (!await tabIlce.GuncelleAsync(ilce))
                        {
                            sonuc.HataEkle($"İlçe bulunamadı: {ilce.Ad}");
                            await transaction.RollbackAsync();
                            return sonuc;
                        }
                    }
                    else
                    {
                        ilce.Id = await tabIlce.EkleAsync(ilce);
                    }
                }
                await transaction.CommitAsync();
                sonuc.nesne = il.id;
                sonuc.mesaj = "İl ve ilçeleri kaydedildi.";
            }
            catch (SqlException ex) when (ex.Number is 2601 or 2627)
            {
                sonuc.HataEkle("Aynı kod/ad bilgisine sahip il veya ilçe zaten bulunuyor.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "İl ve ilçeleri kaydedilemedi. IlId: {IlId}", il.id);
                sonuc.HataEkle("İl ve ilçeleri kaydedilemedi.");
            }
            return sonuc;
        }

        public async Task<Sonuc<List<Donem>>> DonemleriListeleAsync(Kullanici? kullanici)
        {
            Sonuc<List<Donem>> sonuc = new();
            try
            {
                await using SqlConnection connection = new(_connectionString);
                await connection.OpenAsync();
                sonuc.nesne = await new TABDonem(connection, _localizer).ListeleAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Dönem listesi okunamadı.");
                sonuc.HataEkle("Dönem listesi okunamadı.");
            }
            return sonuc;
        }

        public async Task<Sonuc<int>> DonemKaydetAsync(Donem donem, Kullanici? kullanici)
        {
            Sonuc<int> sonuc = new();
            try
            {
                if (!SistemYoneticisiMi(kullanici, sonuc))
                    return sonuc;

                donem.ad = donem.ad?.Trim() ?? "";
                donem.aciklama = donem.aciklama?.Trim() ?? "";
                if (donem.yil < 2000 || donem.yil > 2200)
                    sonuc.HataEkle("Geçerli bir dönem yılı girilmelidir.");
                if (string.IsNullOrWhiteSpace(donem.ad))
                    sonuc.HataEkle("Dönem adı zorunludur.");
                if (donem.basvuruBaslangicTarihi.HasValue && donem.basvuruBitisTarihi.HasValue &&
                    donem.basvuruBaslangicTarihi.Value.Date > donem.basvuruBitisTarihi.Value.Date)
                    sonuc.HataEkle("Başvuru başlangıç tarihi bitiş tarihinden sonra olamaz.");
                if (!sonuc.basarili)
                    return sonuc;

                await using SqlConnection connection = new(_connectionString);
                await connection.OpenAsync();
                TABDonem tablo = new(connection, _localizer);
                if (donem.id > 0)
                {
                    if (!await tablo.GuncelleAsync(donem))
                    {
                        sonuc.HataEkle("Dönem bulunamadı.");
                        return sonuc;
                    }
                    sonuc.nesne = donem.id;
                }
                else
                {
                    sonuc.nesne = await tablo.EkleAsync(donem);
                }
                sonuc.mesaj = "Dönem kaydedildi.";
            }
            catch (SqlException ex) when (ex.Number is 2601 or 2627)
            {
                sonuc.HataEkle("Aynı ada sahip başka bir dönem bulunmaktadır.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Dönem kaydedilemedi. DonemId: {DonemId}", donem.id);
                sonuc.HataEkle("Dönem kaydedilemedi.");
            }
            return sonuc;
        }

        public async Task<Sonuc<List<Birim>>> DashboardBirimleriListeleAsync(Kullanici? kullanici)
        {
            Sonuc<List<Birim>> sonuc = new();
            if (kullanici == null)
            {
                sonuc.HataEkle("Oturum kullanıcısı bulunamadı.");
                return sonuc;
            }

            try
            {
                await using SqlConnection connection = new(_connectionString);
                await connection.OpenAsync();
                TABBirim tabBirim = new(connection, _localizer);
                List<Birim> birimler = await tabBirim.ListeleAsync(true);

                bool sistemYoneticisi = kullanici.Yetkiler.Any(y => y.Rol == KullaniciRol.SistemYoneticisi);
                if (!sistemYoneticisi)
                {
                    HashSet<int> yetkiliBirimler = kullanici.Yetkiler
                        .Where(y => y.Rol == KullaniciRol.BirimKullanicisi && y.Birim.HasValue)
                        .Select(y => y.Birim!.Value)
                        .ToHashSet();
                    birimler = birimler.Where(b => yetkiliBirimler.Contains(b.id)).ToList();
                }

                sonuc.nesne = birimler;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Dashboard birim listesi okunamadı. KullaniciId: {KullaniciId}", kullanici.Id);
                sonuc.HataEkle("Dashboard birim listesi okunamadı.");
            }
            return sonuc;
        }

        public async Task<Sonuc<int>> BirimKaydetAsync(Birim birim, Kullanici? kullanici)
        {
            Sonuc<int> sonuc = new Sonuc<int>();

            try
            {
                if (!SistemYoneticisiMi(kullanici, sonuc))
                    return sonuc;

                await using SqlConnection connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                TABBirim tabBirim = new TABBirim(connection, _localizer);
                await BirimDogrulaAsync(tabBirim, birim, sonuc);
                if (!sonuc.basarili)
                    return sonuc;

                if (birim.id > 0)
                {
                    bool guncellendi = await tabBirim.GuncelleAsync(birim);
                    if (!guncellendi)
                    {
                        sonuc.HataEkle(Metin("Business.Unit.NotFound"));
                        return sonuc;
                    }

                    sonuc.nesne = birim.id;
                }
                else
                {
                    sonuc.nesne = await tabBirim.EkleAsync(birim);
                }

                sonuc.mesaj = Metin("Business.Unit.Saved");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Birim kaydedilemedi. BirimId: {BirimId}", birim.id);
                sonuc.HataEkle(Metin("Business.Unit.SaveFailed"));
            }

            return sonuc;
        }

        public async Task<Sonuc> BirimPasifYapAsync(int id, Kullanici? kullanici)
        {
            Sonuc sonuc = new Sonuc();

            try
            {
                if (!SistemYoneticisiMi(kullanici, sonuc))
                    return sonuc;

                if (id <= 0)
                {
                    sonuc.HataEkle(Metin("Business.Unit.NotFound"));
                    return sonuc;
                }

                await using SqlConnection connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                TABBirim tabBirim = new TABBirim(connection, _localizer);
                bool guncellendi = await tabBirim.PasifYapAsync(id);
                if (!guncellendi)
                {
                    sonuc.HataEkle(Metin("Business.Unit.NotFound"));
                    return sonuc;
                }

                sonuc.mesaj = Metin("Business.Unit.Deactivated");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Birim pasif yapılamadı. BirimId: {BirimId}", id);
                sonuc.HataEkle(Metin("Business.Unit.DeactivateFailed"));
            }

            return sonuc;
        }

        private async Task BirimDogrulaAsync(TABBirim tabBirim, Birim birim, Sonuc sonuc)
        {
            birim.birimAdi = birim.birimAdi?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(birim.birimAdi))
                sonuc.HataEkle(Metin("Business.Unit.NameRequired"));

            if (!Enum.IsDefined(typeof(enumBirimTuru), birim.birimTuru))
                sonuc.HataEkle(Metin("Business.Unit.TypeRequired"));

            if (birim.siraNo <= 0)
                sonuc.HataEkle(Metin("Business.Unit.OrderRequired"));

            if (birim.birimTuru == enumBirimTuru.Merkez)
                birim.ilKod = null;

            if (birim.birimTuru == enumBirimTuru.Tasra)
            {
                if (!birim.ilKod.HasValue || birim.ilKod <= 0)
                {
                    sonuc.HataEkle(Metin("Business.Unit.ProvinceRequired"));
                }
                else if (!await tabBirim.IlKoduVarMiAsync(birim.ilKod.Value))
                {
                    sonuc.HataEkle(Metin("Business.Unit.ProvinceNotFound"));
                }
            }
        }

        public async Task<Sonuc<List<DegerZinciri>>> DegerZincirleriniListeleAsync(Kullanici? kullanici)
        {
            Sonuc<List<DegerZinciri>> sonuc = new();
            if (!SistemYoneticisiMi(kullanici, sonuc)) return sonuc;
            try
            {
                await using SqlConnection connection = new(_connectionString);
                await connection.OpenAsync();
                sonuc.nesne = await new TABDegerZinciri(connection, _localizer).YonetimListesiAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Değer zincirleri okunamadı.");
                sonuc.HataEkle("Değer zincirleri okunamadı.");
            }
            return sonuc;
        }

        public async Task<Sonuc<int>> DegerZinciriKaydetAsync(DegerZinciri model, Kullanici? kullanici)
        {
            Sonuc<int> sonuc = new();
            if (!SistemYoneticisiMi(kullanici, sonuc)) return sonuc;
            model.ad = model.ad?.Trim() ?? "";
            model.aciklama = model.aciklama?.Trim() ?? "";
            model.asamalar ??= new();
            foreach (DegerZinciriAsama asama in model.asamalar)
            {
                asama.ad = asama.ad?.Trim() ?? "";
                asama.aciklama = asama.aciklama?.Trim() ?? "";
            }
            if (string.IsNullOrWhiteSpace(model.ad)) sonuc.HataEkle("Değer zinciri adı zorunludur.");
            if (model.asamalar.Any(x => x.siraNo <= 0)) sonuc.HataEkle("Aşama sıra numarası sıfırdan büyük olmalıdır.");
            if (model.asamalar.Any(x => string.IsNullOrWhiteSpace(x.ad))) sonuc.HataEkle("Aşama adı boş bırakılamaz.");
            if (model.asamalar.GroupBy(x => x.siraNo).Any(x => x.Count() > 1)) sonuc.HataEkle("Aynı sıra numarası birden fazla aşamada kullanılamaz.");
            if (!sonuc.basarili) return sonuc;
            try
            {
                await using SqlConnection connection = new(_connectionString);
                await connection.OpenAsync();
                await using SqlTransaction transaction = (SqlTransaction)await connection.BeginTransactionAsync();
                TABDegerZinciri tablo = new(connection, _localizer, transaction);
                TABDegerZinciriAsama asamaTablo = new(connection, _localizer, transaction);
                if (model.id > 0)
                {
                    if (!await tablo.GuncelleAsync(model))
                    {
                        sonuc.HataEkle("Değer zinciri bulunamadı.");
                        await transaction.RollbackAsync();
                        return sonuc;
                    }
                }
                else model.id = await tablo.EkleAsync(model);

                foreach (DegerZinciriAsama asama in model.asamalar)
                {
                    asama.degerZinciriId = model.id;
                    if (asama.id > 0)
                    {
                        if (!await asamaTablo.GuncelleAsync(asama))
                        {
                            sonuc.HataEkle($"Aşama bulunamadı: {asama.ad}");
                            await transaction.RollbackAsync();
                            return sonuc;
                        }
                    }
                    else asama.id = await asamaTablo.EkleAsync(asama);
                }
                await transaction.CommitAsync();
                sonuc.nesne = model.id;
                sonuc.mesaj = "Değer zinciri ve aşamaları kaydedildi.";
            }
            catch (SqlException ex) when (ex.Number is 2601 or 2627)
            {
                sonuc.HataEkle("Aynı ad veya aşama sıra numarası zaten kullanılıyor.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Değer zinciri kaydedilemedi. Id: {Id}", model.id);
                sonuc.HataEkle("Değer zinciri kaydedilemedi.");
            }
            return sonuc;
        }

        public async Task<Sonuc<List<Il>>> DegerZinciriIlleriniListeleAsync(int degerZinciriId, Kullanici? kullanici)
        {
            Sonuc<List<Il>> sonuc = new();
            if (!SistemYoneticisiMi(kullanici, sonuc)) return sonuc;
            try
            {
                await using SqlConnection connection = new(_connectionString);
                await connection.OpenAsync();
                sonuc.nesne = await new TABDegerZinciriIl(connection, _localizer).IlleriListeleAsync(degerZinciriId);
            }
            catch (Exception ex) { _logger.LogError(ex, "Değer zinciri illeri okunamadı."); sonuc.HataEkle("İl kısıtları okunamadı."); }
            return sonuc;
        }

        public async Task<Sonuc<int>> DegerZinciriIlEkleAsync(int degerZinciriId, int ilId, Kullanici? kullanici)
        {
            Sonuc<int> sonuc = new();
            if (!SistemYoneticisiMi(kullanici, sonuc)) return sonuc;
            if (degerZinciriId <= 0 || ilId <= 0) { sonuc.HataEkle("Değer zinciri ve il seçilmelidir."); return sonuc; }
            try
            {
                await using SqlConnection connection = new(_connectionString);
                await connection.OpenAsync();
                if (await new TABIl(connection, _localizer).OkuAsync(ilId) == null) { sonuc.HataEkle("İl bulunamadı."); return sonuc; }
                sonuc.nesne = await new TABDegerZinciriIl(connection, _localizer).EkleAsync(degerZinciriId, ilId);
                sonuc.mesaj = "İl kısıtı eklendi.";
            }
            catch (SqlException ex) when (ex.Number is 2601 or 2627) { sonuc.HataEkle("Bu il zaten listede."); }
            catch (Exception ex) { _logger.LogError(ex, "İl kısıtı eklenemedi."); sonuc.HataEkle("İl kısıtı eklenemedi."); }
            return sonuc;
        }

        public async Task<Sonuc> DegerZinciriIlSilAsync(int degerZinciriId, int ilId, Kullanici? kullanici)
        {
            Sonuc sonuc = new();
            if (!SistemYoneticisiMi(kullanici, sonuc)) return sonuc;
            try
            {
                await using SqlConnection connection = new(_connectionString);
                await connection.OpenAsync();
                if (!await new TABDegerZinciriIl(connection, _localizer).SilAsync(degerZinciriId, ilId))
                    sonuc.HataEkle("İl kısıtı bulunamadı.");
                else sonuc.mesaj = "İl kısıtı kaldırıldı.";
            }
            catch (Exception ex) { _logger.LogError(ex, "İl kısıtı silinemedi."); sonuc.HataEkle("İl kısıtı silinemedi."); }
            return sonuc;
        }

        private bool SistemYoneticisiMi(Kullanici? kullanici, Sonuc sonuc)
        {
            if (kullanici?.Yetkiler.Any(y => y.Rol == KullaniciRol.SistemYoneticisi) == true)
                return true;

            sonuc.HataEkle(Metin("Business.Authorization.SystemAdminRequired"));
            return false;
        }

        private string Metin(string key)
        {
            string value = _localizer[key].Value;
            return string.IsNullOrWhiteSpace(value) || string.Equals(value, key, StringComparison.Ordinal) ? key : value;
        }
    }
}
