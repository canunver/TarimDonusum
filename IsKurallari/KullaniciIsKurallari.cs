using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Security.Cryptography;
using TarimDonusum.Araclar;
using TarimDonusum.Models;
using TarimDonusum.Tablolar;

namespace TarimDonusum.IsKurallari
{
    public class KullaniciIsKurallari
    {
        private readonly string _connectionString;
        private readonly ILogger<KullaniciIsKurallari> _logger;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public KullaniciIsKurallari(IConfiguration configuration, ILogger<KullaniciIsKurallari> logger, IStringLocalizer<SharedResource> localizer)
        {
            _logger = logger;
            _localizer = localizer;
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        }

        public async Task<Sonuc<int>> YeniBasvuruKullanicisiAsync(Kullanici kullanici)
        {
            Sonuc<int> sonuc = new();

            try
            {
                if (!ConnectionStringKontrolEt(sonuc))
                    return sonuc;

                KullaniciNormalizeEt(kullanici);

                Sonuc dogrulamaSonucu = kullanici.Dogrula(new Sonuc());
                SonucHatalariniAktar(dogrulamaSonucu, sonuc);

                if (!sonuc.basarili)
                    return sonuc;

                await using SqlConnection connection = new(_connectionString);
                await connection.OpenAsync();

                await BenzerKayitKontrolEtAsync(connection, kullanici, sonuc);

                if (!sonuc.basarili)
                    return sonuc;

                sonuc.nesne = await YeniBasvuruKullanicisiKaydetAsync(connection, kullanici, sonuc);
            }
            catch (Exception ex)
            {
                BeklenmeyenHata(sonuc, ex, "Yeni başvuru kullanıcısı oluşturma işlemi tamamlanamadı.", "Business.User.CreateFailed");
            }

            return sonuc;
        }

        public async Task<Sonuc<Kullanici>> KullaniciOkuAsync(int kullaniciId, string kulKod = "", string sifre = "")
        {
            Sonuc<Kullanici> sonuc = new();

            try
            {
                if (!ConnectionStringKontrolEt(sonuc))
                    return sonuc;

                await using SqlConnection connection = new(_connectionString);
                await connection.OpenAsync();

                TABKullanici tabKullanici = new(connection);
                TABKullaniciYetki tabKullaniciYetki = new(connection);

                Kullanici? kullanici = await tabKullanici.OkuAsync(kullaniciId, kulKod, sifre);
                if (kullanici == null)
                {
                    if (sonuc.basarili)
                        HataEkle(sonuc, kullaniciId > 0 ? "Business.User.NotFound" : "Business.User.InvalidCredentials");

                    return sonuc;
                }

                await KullaniciYetkileriniYukleAsync(tabKullaniciYetki, kullanici);

                sonuc.nesne = kullanici;
            }
            catch (Exception ex)
            {
                BeklenmeyenHata(sonuc, ex, "Kullanıcı okunamadı. KullaniciId: {KullaniciId}", "Business.User.ReadFailed", kullaniciId);
            }

            return sonuc;
        }

        public async Task<Sonuc<Kullanici>> IlkAktifKullaniciyiOkuAsync()
        {
            Sonuc<Kullanici> sonuc = new();

            try
            {
                if (!ConnectionStringKontrolEt(sonuc))
                    return sonuc;

                await using SqlConnection connection = new(_connectionString);
                await connection.OpenAsync();

                TABKullanici tabKullanici = new(connection);
                Kullanici? kullanici = await tabKullanici.IlkAktifKullaniciyiOkuAsync();
                if (kullanici == null)
                {
                    HataEkle(sonuc, "Business.User.TestActiveUserNotFound");
                    return sonuc;
                }

                TABKullaniciYetki tabKullaniciYetki = new(connection);
                await KullaniciYetkileriniYukleAsync(tabKullaniciYetki, kullanici);

                sonuc.nesne = kullanici;
            }
            catch (Exception ex)
            {
                BeklenmeyenHata(sonuc, ex, "İlk aktif kullanıcı okunamadı.", "Business.User.TestUserReadFailed");
            }

            return sonuc;
        }

        public async Task<Sonuc<List<Kullanici>>> KullanicilariAraAsync(KullaniciArama arama, Kullanici? islemYapan)
        {
            Sonuc<List<Kullanici>> sonuc = new();
            if (!SistemYoneticisiMi(islemYapan, sonuc)) return sonuc;
            try
            {
                await using SqlConnection connection = new(_connectionString);
                await connection.OpenAsync();
                TABKullanici tabKullanici = new(connection);
                TABKullaniciYetki tabYetki = new(connection);
                sonuc.nesne = await tabKullanici.AraAsync(arama.AdSoyad, arama.BirimId, arama.KullaniciTipi);
                foreach (Kullanici kullanici in sonuc.nesne)
                    kullanici.Yetkiler = await tabYetki.KullaniciYetkileriniListeleAsync(kullanici.Id);
            }
            catch (Exception ex) { BeklenmeyenHata(sonuc, ex, "Kullanıcılar aranamadı.", "Kullanıcılar aranamadı."); }
            return sonuc;
        }

        public async Task<Sonuc<int>> KullaniciKaydetAsync(KullaniciKayit kayit, Kullanici? islemYapan)
        {
            Sonuc<int> sonuc = new();
            if (!SistemYoneticisiMi(islemYapan, sonuc)) return sonuc;
            Kullanici k = kayit.Kullanici;
            k.Ad = k.Ad?.Trim() ?? ""; k.Soyad = k.Soyad?.Trim() ?? "";
            k.TCKN = k.TCKN?.Trim() ?? ""; k.Eposta = k.Eposta?.Trim() ?? "";
            k.Telefon = OrtakFonksiyonlar.TelNormalize(k.Telefon);
            if (string.IsNullOrWhiteSpace(k.Ad) || string.IsNullOrWhiteSpace(k.Soyad)) sonuc.HataEkle("Ad ve soyad girilmelidir.");
            if (kayit.Yetkiler.Count == 0) sonuc.HataEkle("En az bir kullanıcı tipi seçilmelidir.");
            if (kayit.Yetkiler.Any(y => y.Rol == KullaniciRol.BasvuruKullanicisi)
                && kayit.Yetkiler.Count != 1)
                sonuc.HataEkle("Başvuru kullanıcısı tipi tek başına olmalıdır; başka kullanıcı tipleriyle birlikte seçilemez.");
            if (kayit.Yetkiler.GroupBy(y => new { y.Rol, y.Birim }).Any(g => g.Count() > 1))
                sonuc.HataEkle("Aynı kullanıcı tipi ve birim birden fazla eklenemez.");
            foreach (KullaniciYetki y in kayit.Yetkiler)
            {
                if (!Enum.IsDefined(typeof(KullaniciRol), y.Rol)) sonuc.HataEkle("Geçersiz kullanıcı tipi.");
                if (y.Rol == KullaniciRol.BirimKullanicisi)
                {
                    if (!y.Birim.HasValue) sonuc.HataEkle("Birim kullanıcısı için birim seçilmelidir.");
                    if (!Enum.IsDefined(typeof(KullaniciIslemRolu), y.YetkiKodu) || y.YetkiKodu == 0)
                        sonuc.HataEkle("Birim kullanıcısı için rol seçilmelidir.");
                }
                else { y.Birim = null; y.YetkiKodu = 0; }
            }
            if (!sonuc.basarili) return sonuc;

            try
            {
                await using SqlConnection connection = new(_connectionString);
                await connection.OpenAsync();
                await using SqlTransaction transaction = (SqlTransaction)await connection.BeginTransactionAsync();
                TABKullanici tabKullanici = new(connection, null, transaction);
                TABKullaniciYetki tabYetki = new(connection, null, transaction);
                if (k.Id == 0)
                {
                    k.Parola = "Aa1!" + Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
                    Sonuc dogrulama = k.Dogrula(new Sonuc());
                    SonucHatalariniAktar(dogrulama, sonuc);
                    if (!sonuc.basarili) { await transaction.RollbackAsync(); return sonuc; }
                    k.KayitTarihi = DateTime.Now;
                    k.Id = await tabKullanici.EkleAsync(k);
                }
                else
                {
                    Kullanici? mevcut = await tabKullanici.OkuAsync(k.Id);
                    if (mevcut == null) { sonuc.HataEkle("Kullanıcı bulunamadı."); await transaction.RollbackAsync(); return sonuc; }
                    k.TCKN = mevcut.TCKN; k.Eposta = mevcut.Eposta; k.Telefon = mevcut.Telefon; k.KayitTarihi = mevcut.KayitTarihi;
                    await tabKullanici.GuncelleAsync(k);
                    await tabYetki.SilKullaniciYetkileriAsync(k.Id);
                }
                foreach (KullaniciYetki y in kayit.Yetkiler)
                {
                    y.Id = 0; y.KullaniciId = k.Id; await tabYetki.EkleAsync(y);
                }
                await transaction.CommitAsync(); sonuc.nesne = k.Id; sonuc.mesaj = "Kullanıcı kaydedildi.";
            }
            catch (Exception ex) { BeklenmeyenHata(sonuc, ex, "Kullanıcı kaydedilemedi.", "Kullanıcı kaydedilemedi."); }
            return sonuc;
        }

        public async Task<Sonuc<ParolaBaglantisiSonucu>> ParolaBaglantisiOlusturAsync(int kullaniciId, Kullanici? islemYapan)
        {
            Sonuc<ParolaBaglantisiSonucu> sonuc = new();
            if (!SistemYoneticisiMi(islemYapan, sonuc)) return sonuc;
            try
            {
                await using SqlConnection connection = new(_connectionString);
                await connection.OpenAsync();
                TABKullanici tabKullanici = new(connection);
                Kullanici? kullanici = await tabKullanici.OkuAsync(kullaniciId);
                if (kullanici == null) { sonuc.HataEkle(Metin("Business.User.NotFound")); return sonuc; }

                string token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
                    .Replace("+", "-").Replace("/", "_").TrimEnd('=');
                TABKullaniciParolaToken tabToken = new(connection);
                await tabToken.EkleAsync(kullaniciId, TokenHash(token), DateTime.Now.AddHours(2));
                sonuc.nesne = new ParolaBaglantisiSonucu
                {
                    Token = token,
                    Eposta = kullanici.Eposta,
                    AdSoyad = $"{kullanici.Ad} {kullanici.Soyad}"
                };
            }
            catch (Exception ex) { BeklenmeyenHata(sonuc, ex, "Parola bağlantısı oluşturulamadı.", "Parola bağlantısı oluşturulamadı."); }
            return sonuc;
        }

        public async Task<Sonuc> ParolaBelirleAsync(string token, string parola, string parolaTekrar)
        {
            Sonuc sonuc = new();
            if (string.IsNullOrWhiteSpace(token)) sonuc.HataEkle(Metin("Kullanici.PasswordLink.Invalid"));
            if (!string.Equals(parola, parolaTekrar, StringComparison.Ordinal)) sonuc.HataEkle(Metin("Kullanici.PasswordLink.Mismatch"));
            if (!OrtakFonksiyonlar.ParolaGecerliMi(parola)) sonuc.HataEkle(Metin("Kullanici.PasswordLink.Weak"));
            if (!sonuc.basarili) return sonuc;
            try
            {
                await using SqlConnection connection = new(_connectionString);
                await connection.OpenAsync();
                await using SqlTransaction transaction = (SqlTransaction)await connection.BeginTransactionAsync();
                TABKullaniciParolaToken tabToken = new(connection, null, transaction);
                string hash = TokenHash(token);
                int? kullaniciId = await tabToken.GecerliKullaniciIdAsync(hash);
                if (!kullaniciId.HasValue)
                {
                    sonuc.HataEkle(Metin("Kullanici.PasswordLink.Invalid"));
                    await transaction.RollbackAsync();
                    return sonuc;
                }
                TABKullanici tabKullanici = new(connection, null, transaction);
                await tabKullanici.ParolaDegistirAsync(new Kullanici { Id = kullaniciId.Value, Parola = parola });
                await tabToken.KullanildiYapAsync(hash);
                await transaction.CommitAsync();
                sonuc.mesaj = Metin("Kullanici.PasswordLink.Success");
            }
            catch (Exception ex) { BeklenmeyenHata(sonuc, ex, "Parola belirlenemedi.", "Parola belirlenemedi."); }
            return sonuc;
        }

        private static string TokenHash(string token)
        {
            return Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token)));
        }

        private static bool SistemYoneticisiMi(Kullanici? kullanici, Sonuc sonuc)
        {
            if (kullanici?.Yetkiler.Any(y => y.Rol == KullaniciRol.SistemYoneticisi) == true) return true;
            sonuc.HataEkle("Bu işlem için sistem yöneticisi yetkisi gereklidir."); return false;
        }

        private static async Task KullaniciYetkileriniYukleAsync(
            TABKullaniciYetki tabKullaniciYetki,
            Kullanici kullanici)
        {
            kullanici.Yetkiler = await tabKullaniciYetki.KullaniciYetkileriniListeleAsync(kullanici.Id);
        }

        private async Task BenzerKayitKontrolEtAsync(
            SqlConnection connection,
            Kullanici kullanici,
            Sonuc sonuc)
        {
            TABKullanici tabKullanici = new(connection);

            if (await tabKullanici.BenzerKayitVarMiAsync(kullanici.TCKN, kullanici.Eposta, kullanici.Telefon))
                sonuc.HataEkle(Metin("Business.User.SimilarRecordExists"));
        }

        private async Task<int> YeniBasvuruKullanicisiKaydetAsync(
            SqlConnection connection,
            Kullanici kullanici,
            Sonuc sonuc)
        {
            await using SqlTransaction transaction = (SqlTransaction)await connection.BeginTransactionAsync();

            try
            {
                int kullaniciId = await KullaniciEkleAsync(connection, transaction, kullanici);
                KullaniciYetki basvuruKullanicisiYetkisi = await BasvuruKullanicisiYetkisiEkleAsync(connection, transaction, kullanici, kullaniciId);
                await KullaniciLogEkleAsync(connection, transaction, kullanici, kullaniciId, kullaniciId, "YeniBasvuruKullanicisi", basvuruKullanicisiYetkisi);

                await transaction.CommitAsync();

                _logger.LogInformation(
                    "Yeni başvuru kullanıcısı oluşturuldu. KullaniciId: {KullaniciId}, TCKN: {TCKN}",
                    kullaniciId,
                    kullanici.TCKN);

                return kullaniciId;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                _logger.LogError(ex, "Yeni başvuru kullanıcısı oluşturulamadı. TCKN: {TCKN}", kullanici.TCKN);
                HataEkle(sonuc, "Business.User.CreateFailed");
                return 0;
            }
        }

        private static void SonucHatalariniAktar(Sonuc kaynak, Sonuc hedef)
        {
            foreach (string hata in kaynak.hatalar)
            {
                hedef.HataEkle(hata);
            }
        }

        private bool ConnectionStringKontrolEt(Sonuc sonuc)
        {
            if (!string.IsNullOrWhiteSpace(_connectionString))
                return true;

            _logger.LogError("ConnectionStrings:DefaultConnection tanımlı değil.");
            HataEkle(sonuc, "Business.Database.ConnectionSettingMissing");
            return false;
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

        private string Metin(string key)
        {
            string value = _localizer[key].Value;
            return string.IsNullOrWhiteSpace(value) || string.Equals(value, key, StringComparison.Ordinal) ? key : value;
        }

        private static async Task<int> KullaniciEkleAsync(
            SqlConnection connection,
            SqlTransaction transaction,
            Kullanici kullanici)
        {
            TABKullanici tabKullanici = new(connection, null, transaction);

            return await tabKullanici.EkleAsync(kullanici);
        }

        private static async Task<KullaniciYetki> BasvuruKullanicisiYetkisiEkleAsync(
            SqlConnection connection,
            SqlTransaction transaction,
            Kullanici kullanici,
            int kullaniciId)
        {
            TABKullaniciYetki tabKullaniciYetki = new(connection, null, transaction);
            KullaniciYetki basvuruKullanicisiYetkisi = new()
            {
                KullaniciId = kullaniciId,
                Rol = KullaniciRol.BasvuruKullanicisi,
                YetkiKodu = 11111,
                Birim = null
            };

            await tabKullaniciYetki.EkleAsync(basvuruKullanicisiYetkisi);
            kullanici.Yetkiler.Add(basvuruKullanicisiYetkisi);

            return basvuruKullanicisiYetkisi;
        }

        private static async Task KullaniciLogEkleAsync(
            SqlConnection connection,
            SqlTransaction transaction,
            Kullanici kullanici,
            int kullaniciId,
            int? islemYapanKullaniciId,
            string islem,
            KullaniciYetki basvuranYetkisi)
        {
            TABKullaniciLog tabKullaniciLog = new(connection, null, transaction);
            KullaniciLog log = new()
            {
                KullaniciId = kullaniciId,
                IslemYapanKullaniciId = islemYapanKullaniciId,
                IslemTarihi = DateTime.Now,
                Islem = islem,
                JsonText = KullaniciLogJsonOlustur(kullanici, kullaniciId, islem, basvuranYetkisi)
            };

            await tabKullaniciLog.EkleAsync(log);
        }

        private static string KullaniciLogJsonOlustur(
            Kullanici kullanici,
            int kullaniciId,
            string islem,
            KullaniciYetki basvuranYetkisi)
        {
            object logNesnesi = new
            {
                Islem = islem,
                Kullanici = new
                {
                    Id = kullaniciId,
                    kullanici.TCKN,
                    kullanici.Ad,
                    kullanici.Soyad,
                    kullanici.DogumTarihi,
                    kullanici.Cinsiyet,
                    kullanici.Eposta,
                    kullanici.Telefon,
                    kullanici.KayitTarihi,
                    kullanici.Aktif
                },
                Yetki = new
                {
                    basvuranYetkisi.Id,
                    basvuranYetkisi.KullaniciId,
                    Rol = basvuranYetkisi.Rol.ToString(),
                    RolKodu = (int)basvuranYetkisi.Rol,
                    basvuranYetkisi.YetkiKodu,
                    basvuranYetkisi.Birim
                }
            };

            return JsonSerializer.Serialize(logNesnesi);
        }

        private static void KullaniciNormalizeEt(Kullanici kullanici)
        {
            kullanici.TCKN = kullanici.TCKN?.Trim() ?? "";
            kullanici.Eposta = kullanici.Eposta?.Trim() ?? "";

            string telefon = OrtakFonksiyonlar.TelNormalize(kullanici.Telefon);
            if (!string.IsNullOrWhiteSpace(telefon))
                kullanici.Telefon = telefon;
        }
    }
}
