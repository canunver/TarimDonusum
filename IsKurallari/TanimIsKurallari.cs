using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text;
using TarimDonusum.Araclar;
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
            if (il.ilceler.Any(x => x.SegeKademesi < 1 || x.SegeKademesi > 6))
                sonuc.HataEkle("SEGE kademesi 1 ile 6 arasında bir tamsayı olmalıdır.");
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
                if (donem.onBasvuruBaslangicTarihi.HasValue && donem.onBasvuruBitisTarihi.HasValue &&
                    donem.onBasvuruBaslangicTarihi.Value.Date > donem.onBasvuruBitisTarihi.Value.Date)
                    sonuc.HataEkle("Ön başvuru başlangıç tarihi bitiş tarihinden sonra olamaz.");
                if (donem.onBasvuruCevrimKuru.HasValue && donem.onBasvuruCevrimKuru <= 0)
                    sonuc.HataEkle("Ön başvuru çevrim kuru sıfırdan büyük olmalıdır.");
                if (donem.basvuruCevrimKuru.HasValue && donem.basvuruCevrimKuru <= 0)
                    sonuc.HataEkle("Başvuru çevrim kuru sıfırdan büyük olmalıdır.");
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

        public async Task<Sonuc<DonemTahminAyari>> DonemTahminleriniOkuAsync(int donemId, Kullanici? kullanici)
        {
            Sonuc<DonemTahminAyari> sonuc = new(); if (!SistemYoneticisiMi(kullanici, sonuc)) return sonuc;
            if (donemId <= 0) { sonuc.HataEkle("Dönem seçilmelidir."); return sonuc; }
            try
            {
                await using SqlConnection connection = new(_connectionString); await connection.OpenAsync();
                Donem? donem = await new TABDonem(connection, _localizer).OkuAsync(donemId);
                if (donem == null) { sonuc.HataEkle("Dönem bulunamadı."); return sonuc; }
                List<DonemTahminSatiri> kayitlar = await new TABDonemTahmini(connection, _localizer).ListeleAsync(donemId);
                sonuc.nesne = new() { donemId = donem.id, donemYili = donem.yil, tahminler = Enumerable.Range(donem.yil + 1, 7).Select(y => kayitlar.FirstOrDefault(x => x.yil == y) ?? new DonemTahminSatiri { yil = y }).ToList() };
            }
            catch (Exception ex) { _logger.LogError(ex, "Dönem tahminleri okunamadı. DonemId: {DonemId}", donemId); sonuc.HataEkle("Dönem tahminleri okunamadı."); }
            return sonuc;
        }

        public async Task<Sonuc<int>> DonemTahminleriniKaydetAsync(DonemTahminAyari model, Kullanici? kullanici)
        {
            Sonuc<int> sonuc = new(); if (!SistemYoneticisiMi(kullanici, sonuc)) return sonuc;
            if (model.donemId <= 0) sonuc.HataEkle("Dönem seçilmelidir."); model.tahminler ??= [];
            try
            {
                await using SqlConnection connection = new(_connectionString); await connection.OpenAsync(); Donem? donem = await new TABDonem(connection, _localizer).OkuAsync(model.donemId);
                if (donem == null) { sonuc.HataEkle("Dönem bulunamadı."); return sonuc; }
                int[] beklenen = Enumerable.Range(donem.yil + 1, 7).ToArray();
                if (model.tahminler.Count != 7 || !model.tahminler.Select(x => x.yil).Order().SequenceEqual(beklenen)) sonuc.HataEkle($"{donem.yil + 1}-{donem.yil + 7} yıllarının tamamı ve yalnızca birer kez gönderilmelidir.");
                if (model.tahminler.Any(x => !x.kurTahminiTL.HasValue || !x.enflasyonTahminiYuzde.HasValue)) sonuc.HataEkle("Kur ve enflasyon tahminlerinin tamamı girilmelidir.");
                if (model.tahminler.Any(x => x.kurTahminiTL <= 0)) sonuc.HataEkle("Kur tahmini sıfırdan büyük bir TL değeri olmalıdır.");
                if (model.tahminler.Any(x => x.enflasyonTahminiYuzde < 0 || x.enflasyonTahminiYuzde > 1000)) sonuc.HataEkle("Enflasyon tahmini yüzde 0 ile 1000 arasında olmalıdır.");
                if (!sonuc.basarili) return sonuc;
                await using SqlTransaction transaction = (SqlTransaction)await connection.BeginTransactionAsync(); await new TABDonemTahmini(connection, _localizer, transaction).KaydetAsync(model.donemId, model.tahminler); await transaction.CommitAsync();
                sonuc.nesne = model.donemId; sonuc.mesaj = "Kur ve enflasyon tahminleri kaydedildi.";
            }
            catch (Exception ex) { _logger.LogError(ex, "Dönem tahminleri kaydedilemedi. DonemId: {DonemId}", model.donemId); sonuc.HataEkle("Dönem tahminleri kaydedilemedi."); }
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

        public async Task<Sonuc<List<Poz>>> PozlariListeleAsync(Kullanici? kullanici)
        {
            Sonuc<List<Poz>> sonuc = new();
            if (!SistemYoneticisiMi(kullanici, sonuc)) return sonuc;
            try
            {
                await using SqlConnection connection = new(_connectionString);
                await connection.OpenAsync();
                sonuc.nesne = await new TABPoz(connection, _localizer).ListeleAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Poz sözlüğü okunamadı.");
                sonuc.HataEkle("Poz sözlüğü okunamadı.");
            }
            return sonuc;
        }

        public async Task<Sonuc<int>> PozKaydetAsync(Poz model, Kullanici? kullanici)
        {
            Sonuc<int> sonuc = new();
            if (!SistemYoneticisiMi(kullanici, sonuc)) return sonuc;
            model.pozNo = model.pozNo?.Trim() ?? "";
            model.ad = model.ad?.Trim() ?? "";
            model.birim = model.birim?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(model.pozNo)) sonuc.HataEkle("Poz numarası zorunludur.");
            if (model.pozNo.Length > 50) sonuc.HataEkle("Poz numarası en fazla 50 karakter olabilir.");
            if (string.IsNullOrWhiteSpace(model.ad)) sonuc.HataEkle("Poz adı/tanımı zorunludur.");
            if (model.ad.Length > 1000) sonuc.HataEkle("Poz adı/tanımı en fazla 1000 karakter olabilir.");
            if (string.IsNullOrWhiteSpace(model.birim) || model.birim.Length > 20) sonuc.HataEkle("Geçerli bir poz birimi girilmelidir.");
            if (!Enum.IsDefined(typeof(enumPozHesaplamaTuru), model.hesaplamaTuru)) sonuc.HataEkle("Geçerli bir hesaplama türü seçilmelidir.");
            if (!sonuc.basarili) return sonuc;
            try
            {
                await using SqlConnection connection = new(_connectionString);
                await connection.OpenAsync();
                TABPoz tablo = new(connection, _localizer);
                if (model.id > 0)
                {
                    if (!await tablo.GuncelleAsync(model)) { sonuc.HataEkle("Poz bulunamadı."); return sonuc; }
                }
                else model.id = await tablo.EkleAsync(model);
                sonuc.nesne = model.id;
                sonuc.mesaj = "Poz kaydedildi.";
            }
            catch (SqlException ex) when (ex.Number is 2601 or 2627)
            {
                sonuc.HataEkle("Aynı poz numarası zaten kullanılıyor.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Poz kaydedilemedi. PozId: {PozId}", model.id);
                sonuc.HataEkle("Poz kaydedilemedi.");
            }
            return sonuc;
        }

        public async Task<Sonuc<object>> PozExcelYukleAsync(Stream excelStream, Kullanici? kullanici)
        {
            Sonuc<object> sonuc = new();
            if (!SistemYoneticisiMi(kullanici, sonuc)) return sonuc;
            if (excelStream == null || (excelStream.CanSeek && excelStream.Length == 0)) { sonuc.HataEkle("Excel dosyası boştur."); return sonuc; }
            List<string> satirHatalari = new();
            Dictionary<string, Poz> aktarilacak = new(StringComparer.OrdinalIgnoreCase);
            try
            {
                Tablo excel = OrtakFonksiyonlar.NewTablo();
                excel.DosyaOkuAc(excelStream);
                int sheetNo = 0;
                int sonSatir = excel.SonDoluSatir(sheetNo), sonSutun = excel.SonDoluSutun(sheetNo);
                
                for (int r = 0; r <= sonSatir; r++)
                {
                    string pozNo = excel.HucreDegerAl(r, 0).Trim();
                    if (string.IsNullOrWhiteSpace(pozNo)) continue;

                    string ad = excel.HucreDegerAl(r, 1).Trim();
                    string birim = excel.HucreDegerAl(r, 2).Trim();
                    if (string.IsNullOrWhiteSpace(pozNo)) continue; // Metraj alt detay satırı.
                    if (string.IsNullOrWhiteSpace(ad) || string.IsNullOrWhiteSpace(birim))
                    {
                        satirHatalari.Add($"{r + 1}. satır: poz adı ve birim zorunludur.");
                        continue;
                    }
                    enumPozHesaplamaTuru hesaplama =  BirimdenHesaplamaTuru(birim);
                    bool aktif = true;
                    aktarilacak[pozNo] = new Poz { pozNo = pozNo, ad = ad, birim = birim, hesaplamaTuru = hesaplama, aktif = aktif };
                }
                if (aktarilacak.Count == 0)
                {
                    sonuc.HataEkle("Excel'de 'Poz No, Ad/Tanım, Birim' başlıkları ve aktarılabilir poz satırı bulunamadı.");
                    return sonuc;
                }

                await using SqlConnection connection = new(_connectionString);
                await connection.OpenAsync();
                await using SqlTransaction transaction = (SqlTransaction)await connection.BeginTransactionAsync();
                TABPoz tablo = new(connection, _localizer, transaction);
                Dictionary<string, Poz> mevcutlar = (await tablo.ListeleAsync()).ToDictionary(x => x.pozNo, StringComparer.OrdinalIgnoreCase);
                int eklenen = 0, guncellenen = 0;
                foreach (Poz poz in aktarilacak.Values)
                {
                    if (mevcutlar.TryGetValue(poz.pozNo, out Poz? mevcut))
                    {
                        poz.id = mevcut.id;
                        await tablo.GuncelleAsync(poz);
                        guncellenen++;
                    }
                    else { poz.id = await tablo.EkleAsync(poz); eklenen++; }
                }
                await transaction.CommitAsync();
                sonuc.nesne = new { eklenen, guncellenen, atlanan = satirHatalari.Count, satirHatalari = satirHatalari.Take(50).ToList() };
                sonuc.mesaj = $"Excel aktarımı tamamlandı. {eklenen} poz eklendi, {guncellenen} poz güncellendi" + (satirHatalari.Count > 0 ? $", {satirHatalari.Count} satır atlandı." : ".");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Poz sözlüğü Excel'den aktarılamadı.");
                sonuc.HataEkle("Excel dosyası okunamadı veya pozlar aktarılamadı.");
            }
            return sonuc;
        }

        private static enumPozHesaplamaTuru PozHesaplamaTuruOku(string deger, string birim)
        {
            string metin = MetinNormalize(deger);
            if (int.TryParse(deger, out int no) && Enum.IsDefined(typeof(enumPozHesaplamaTuru), no)) return (enumPozHesaplamaTuru)no;
            return metin switch { "adet" => enumPozHesaplamaTuru.Adet, "uzunluk" => enumPozHesaplamaTuru.Uzunluk, "alan" => enumPozHesaplamaTuru.Alan, "hacim" => enumPozHesaplamaTuru.Hacim, "agirlik" => enumPozHesaplamaTuru.Agirlik, _ => BirimdenHesaplamaTuru(birim) };
        }

        private static enumPozHesaplamaTuru BirimdenHesaplamaTuru(string birim)
        {
            string metin = MetinNormalize(birim).Replace("³", "3").Replace("²", "2");
            if (metin.Contains("m3")) return enumPozHesaplamaTuru.Hacim;
            if (metin.Contains("m2")) return enumPozHesaplamaTuru.Alan;
            if (metin is "kg" or "ton" or "t") return enumPozHesaplamaTuru.Agirlik;
            if (metin is "m" or "mt") return enumPozHesaplamaTuru.Uzunluk;
            return enumPozHesaplamaTuru.Adet;
        }

        private static bool AktifOku(string deger)
        {
            string metin = MetinNormalize(deger);
            return metin is not ("0" or "hayir" or "pasif" or "false");
        }

        private static string MetinNormalize(string? deger)
        {
            string metin = (deger ?? "").Trim().ToLower(new CultureInfo("tr-TR")).Normalize(NormalizationForm.FormD);
            return new string(metin.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark && char.IsLetterOrDigit(c)).ToArray()).Replace('ı', 'i');
        }

        public async Task<Sonuc<object>> PozDonemFiyatSayfaVerisiAsync(int donemId, Kullanici? kullanici, bool listele = true)
        {
            Sonuc<object> sonuc = new();
            if (!SistemYoneticisiMi(kullanici, sonuc)) return sonuc;
            try
            {
                await using SqlConnection connection = new(_connectionString);
                await connection.OpenAsync();
                List<Donem> donemler = await new TABDonem(connection, _localizer).ListeleAsync();
                int seciliDonemId = donemId > 0 ? donemId : donemler.OrderByDescending(x => x.yil).ThenByDescending(x => x.id).FirstOrDefault()?.id ?? 0;
                if (seciliDonemId > 0 && !donemler.Any(x => x.id == seciliDonemId)) { sonuc.HataEkle("Dönem bulunamadı."); return sonuc; }
                List<PozDonemFiyat> fiyatlar = listele && seciliDonemId > 0
                    ? await new TABPozDonemFiyat(connection, _localizer).ListeleAsync(seciliDonemId)
                    : new();
                sonuc.nesne = new { donemler, seciliDonemId, fiyatlar };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Poz dönem fiyatları okunamadı. DonemId: {DonemId}", donemId);
                sonuc.HataEkle("Poz dönem fiyatları okunamadı.");
            }
            return sonuc;
        }

        public async Task<Sonuc> PozDonemFiyatlariKaydetAsync(PozDonemFiyatKayit model, Kullanici? kullanici)
        {
            Sonuc sonuc = new();
            if (!SistemYoneticisiMi(kullanici, sonuc)) return sonuc;
            model ??= new PozDonemFiyatKayit();
            model.fiyatlar ??= new();
            if (model.donemId <= 0) sonuc.HataEkle("Dönem seçilmelidir.");
            if (model.fiyatlar.GroupBy(x => x.pozId).Any(x => x.Key <= 0 || x.Count() > 1)) sonuc.HataEkle("Poz fiyat listesi geçersizdir.");
            if (model.fiyatlar.Any(x => x.birimFiyat.HasValue && x.birimFiyat < 0)) sonuc.HataEkle("Birim fiyat negatif olamaz.");
            if (!sonuc.basarili) return sonuc;
            try
            {
                await using SqlConnection connection = new(_connectionString);
                await connection.OpenAsync();
                List<Donem> donemler = await new TABDonem(connection, _localizer).ListeleAsync();
                if (!donemler.Any(x => x.id == model.donemId)) { sonuc.HataEkle("Dönem bulunamadı."); return sonuc; }
                HashSet<int> pozIdleri = (await new TABPoz(connection, _localizer).ListeleAsync()).Select(x => x.id).ToHashSet();
                if (model.fiyatlar.Any(x => !pozIdleri.Contains(x.pozId))) { sonuc.HataEkle("Fiyat listesindeki pozlardan biri bulunamadı."); return sonuc; }
                await using SqlTransaction transaction = (SqlTransaction)await connection.BeginTransactionAsync();
                TABPozDonemFiyat tablo = new(connection, _localizer, transaction);
                foreach (PozDonemFiyat fiyat in model.fiyatlar)
                {
                    fiyat.donemId = model.donemId;
                    if (fiyat.birimFiyat.HasValue) await tablo.KaydetAsync(fiyat);
                    else await tablo.SilAsync(model.donemId, fiyat.pozId);
                }
                await transaction.CommitAsync();
                sonuc.mesaj = "Poz birim fiyatları kaydedildi.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Poz dönem fiyatları kaydedilemedi. DonemId: {DonemId}", model.donemId);
                sonuc.HataEkle("Poz dönem fiyatları kaydedilemedi.");
            }
            return sonuc;
        }

        public async Task<Sonuc<object>> PozDonemFiyatExcelYukleAsync(int donemId, Stream excelStream, Kullanici? kullanici)
        {
            Sonuc<object> sonuc = new();
            if (!SistemYoneticisiMi(kullanici, sonuc)) return sonuc;
            if (donemId <= 0) { sonuc.HataEkle("Dönem seçilmelidir."); return sonuc; }
            if (excelStream == null || (excelStream.CanSeek && excelStream.Length == 0)) { sonuc.HataEkle("Excel dosyası boştur."); return sonuc; }

            List<string> satirHatalari = new();
            Dictionary<string, decimal> aktarilacak = new(StringComparer.OrdinalIgnoreCase);
            try
            {
                Tablo excel = OrtakFonksiyonlar.NewTablo();
                excel.DosyaOkuAc(excelStream);
                for (int sheetNo = 0; sheetNo < excel.SheetSayisi(); sheetNo++)
                {
                    string sheetAdi = excel.SheetAdiAl(sheetNo);
                    int sonSatir = excel.SonDoluSatir(sheetNo), sonSutun = excel.SonDoluSutun(sheetNo);

                    for (int r = 0; r <= sonSatir; r++)
                    {
                        string pozNo = excel.HucreDegerAl(r, 0).Trim();
                        double fiyat = excel.HucreDegerAlDbl(r, 1);
                        if (string.IsNullOrWhiteSpace(pozNo)) continue;
                        aktarilacak[pozNo] = Convert.ToDecimal(fiyat);
                    }
                }
                if (aktarilacak.Count == 0)
                {
                    sonuc.HataEkle("Excel'de 'Poz No' ve 'Birim Fiyat' başlıkları ile aktarılabilir bir fiyat satırı bulunamadı.");
                    return sonuc;
                }

                await using SqlConnection connection = new(_connectionString);
                await connection.OpenAsync();
                if (!(await new TABDonem(connection, _localizer).ListeleAsync()).Any(x => x.id == donemId))
                { sonuc.HataEkle("Dönem bulunamadı."); return sonuc; }

                Dictionary<string, Poz> pozlar = (await new TABPoz(connection, _localizer).ListeleAsync())
                    .ToDictionary(x => x.pozNo, StringComparer.OrdinalIgnoreCase);
                await using SqlTransaction transaction = (SqlTransaction)await connection.BeginTransactionAsync();
                TABPozDonemFiyat tablo = new(connection, _localizer, transaction);
                int yuklenen = 0;
                foreach ((string pozNo, decimal fiyat) in aktarilacak)
                {
                    if (!pozlar.TryGetValue(pozNo, out Poz? poz))
                    { satirHatalari.Add($"Poz sözlüğünde bulunamadı: {pozNo}"); continue; }
                    await tablo.KaydetAsync(new PozDonemFiyat { donemId = donemId, pozId = poz.id, birimFiyat = fiyat });
                    yuklenen++;
                }
                await transaction.CommitAsync();
                sonuc.nesne = new { yuklenen, atlanan = satirHatalari.Count, satirHatalari = satirHatalari.Take(50).ToList() };
                sonuc.mesaj = $"Excel aktarımı tamamlandı. {yuklenen} pozun TL birim fiyatı yüklendi" +
                    (satirHatalari.Count > 0 ? $", {satirHatalari.Count} satır atlandı." : ".");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Poz dönem fiyatları Excel'den aktarılamadı. DonemId: {DonemId}", donemId);
                sonuc.HataEkle("Excel dosyası okunamadı veya fiyatlar aktarılamadı.");
            }
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
