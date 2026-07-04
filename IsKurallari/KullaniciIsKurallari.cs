using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using TarimDonusum.Araclar;
using TarimDonusum.Models;
using TarimDonusum.Tablolar;

namespace TarimDonusum.IsKurallari
{
    public class KullaniciIsKurallari
    {
        private readonly string _connectionString;
        private readonly ILogger<KullaniciIsKurallari> _logger;

        public KullaniciIsKurallari(IConfiguration configuration, ILogger<KullaniciIsKurallari> logger)
        {
            _logger = logger;
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        }

        public async Task<Sonuc<int>> YeniBasvuruKullanicisiAsync(Kullanici kullanici)
        {
            Sonuc<int> sonuc = new Sonuc<int>();

            try
            {
                if (!ConnectionStringKontrolEt(sonuc))
                    return sonuc;

                KullaniciNormalizeEt(kullanici);

                Sonuc dogrulamaSonucu = kullanici.Dogrula(new Sonuc());
                SonucHatalariniAktar(dogrulamaSonucu, sonuc);

                if (!sonuc.basarili)
                    return sonuc;

                await using SqlConnection connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                await BenzerKayitKontrolEtAsync(connection, kullanici, sonuc);

                if (!sonuc.basarili)
                    return sonuc;

                sonuc.nesne = await YeniBasvuruKullanicisiKaydetAsync(connection, kullanici, sonuc);
            }
            catch (Exception ex)
            {
                BeklenmeyenHata(sonuc, ex, "Yeni başvuru kullanıcısı oluşturma işlemi tamamlanamadı.", "Kullanıcı kaydı oluşturulamadı.");
            }

            return sonuc;
        }

        public async Task<Sonuc<Kullanici>> KullaniciOkuAsync(int kullaniciId, string kulKod = "", string sifre = "")
        {
            Sonuc<Kullanici> sonuc = new Sonuc<Kullanici>();

            try
            {
                if (!ConnectionStringKontrolEt(sonuc))
                    return sonuc;

                await using SqlConnection connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                TABKullanici tabKullanici = new TABKullanici(connection);
                TABKullaniciYetki tabKullaniciYetki = new TABKullaniciYetki(connection);

                Kullanici? kullanici = await tabKullanici.OkuAsync(kullaniciId, kulKod, sifre);
                if (kullanici == null)
                {
                    if (sonuc.basarili)
                        sonuc.HataEkle(kullaniciId > 0 ? "Kullanıcı bulunamadı." : "Kullanıcı kodu veya şifre hatalı.");

                    return sonuc;
                }

                await KullaniciYetkileriniYukleAsync(tabKullaniciYetki, kullanici);

                sonuc.nesne = kullanici;
            }
            catch (Exception ex)
            {
                BeklenmeyenHata(sonuc, ex, "Kullanıcı okunamadı. KullaniciId: {KullaniciId}", "Kullanıcı bilgileri okunamadı.", kullaniciId);
            }

            return sonuc;
        }

        public async Task<Sonuc<Kullanici>> IlkAktifKullaniciyiOkuAsync()
        {
            Sonuc<Kullanici> sonuc = new Sonuc<Kullanici>();

            try
            {
                if (!ConnectionStringKontrolEt(sonuc))
                    return sonuc;

                await using SqlConnection connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                TABKullanici tabKullanici = new TABKullanici(connection);
                Kullanici? kullanici = await tabKullanici.IlkAktifKullaniciyiOkuAsync();
                if (kullanici == null)
                {
                    sonuc.HataEkle("Test girişi için aktif kullanıcı bulunamadı.");
                    return sonuc;
                }

                TABKullaniciYetki tabKullaniciYetki = new TABKullaniciYetki(connection);
                await KullaniciYetkileriniYukleAsync(tabKullaniciYetki, kullanici);

                sonuc.nesne = kullanici;
            }
            catch (Exception ex)
            {
                BeklenmeyenHata(sonuc, ex, "İlk aktif kullanıcı okunamadı.", "Test girişi için kullanıcı okunamadı.");
            }

            return sonuc;
        }

        private static async Task KullaniciYetkileriniYukleAsync(
            TABKullaniciYetki tabKullaniciYetki,
            Kullanici kullanici)
        {
            kullanici.Yetkiler = await tabKullaniciYetki.KullaniciYetkileriniListeleAsync(kullanici.Id);
        }

        private static async Task BenzerKayitKontrolEtAsync(
            SqlConnection connection,
            Kullanici kullanici,
            Sonuc sonuc)
        {
            TABKullanici tabKullanici = new TABKullanici(connection);

            if (await tabKullanici.BenzerKayitVarMiAsync(kullanici.TCKN, kullanici.Eposta, kullanici.Telefon))
                sonuc.HataEkle("Benzer bir kullanıcı kaydı bulunduğu için kayıt yapılamaz.");
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
                KullaniciYetki basvuranYetkisi = await BasvuranYetkisiEkleAsync(connection, transaction, kullanici, kullaniciId);
                await KullaniciLogEkleAsync(connection, transaction, kullanici, kullaniciId, kullaniciId, "YeniBasvuruKullanicisi", basvuranYetkisi);

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
                sonuc.HataEkle("Kullanıcı kaydı oluşturulamadı.");
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
            sonuc.HataEkle("Veritabanı bağlantı ayarı bulunamadı.");
            return false;
        }

        private void BeklenmeyenHata(Sonuc sonuc, Exception ex, string logMesaji, string kullaniciMesaji, params object[] logParametreleri)
        {
            _logger.LogError(ex, logMesaji, logParametreleri);
            sonuc.HataEkle(kullaniciMesaji);
        }

        private static async Task<int> KullaniciEkleAsync(
            SqlConnection connection,
            SqlTransaction transaction,
            Kullanici kullanici)
        {
            TABKullanici tabKullanici = new TABKullanici(connection, transaction);

            return await tabKullanici.EkleAsync(kullanici);
        }

        private static async Task<KullaniciYetki> BasvuranYetkisiEkleAsync(
            SqlConnection connection,
            SqlTransaction transaction,
            Kullanici kullanici,
            int kullaniciId)
        {
            TABKullaniciYetki tabKullaniciYetki = new TABKullaniciYetki(connection, transaction);
            KullaniciYetki basvuranYetkisi = new KullaniciYetki
            {
                KullaniciId = kullaniciId,
                Rol = KullaniciRol.Basvuran,
                YetkiKodu = 11111,
                Birim = null
            };

            await tabKullaniciYetki.EkleAsync(basvuranYetkisi);
            kullanici.Yetkiler.Add(basvuranYetkisi);

            return basvuranYetkisi;
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
            TABKullaniciLog tabKullaniciLog = new TABKullaniciLog(connection, transaction);
            KullaniciLog log = new KullaniciLog
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
