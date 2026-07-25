using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Localization;
using TarimDonusum.Models;
using TarimDonusum.Tablolar;

namespace TarimDonusum.IsKurallari
{
    public interface IDosyaYetkiKontrol
    {
        Task<bool> GorebilirAsync(string modulKod, string? formAd, string? formAnahtar, int? dosyaNo);
        Task<bool> EkleyebilirAsync(string modulKod, string formAd, string formAnahtar);
        Task<bool> GuncelleyebilirAsync(string modulKod, string formAd, string formAnahtar, int dosyaNo);
        Task<bool> SilebilirAsync(string modulKod, string formAd, string formAnahtar, int dosyaNo);
    }

    public class DosyaYonetimIsKurallari
    {
        private const int PaketBoyutu = 1024 * 1024;

        private readonly string _connectionString;
        private readonly ILogger<DosyaYonetimIsKurallari> _logger;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public DosyaYonetimIsKurallari(IConfiguration configuration, ILogger<DosyaYonetimIsKurallari> logger, IStringLocalizer<SharedResource> localizer)
        {
            _logger = logger;
            _localizer = localizer;
            _connectionString = configuration.GetConnectionString("DosyaConnection")
                ?? configuration.GetConnectionString("DefaultConnection")
                ?? "";
        }

        public async Task<Sonuc<List<DosyaBilgisi>>> DosyaListeleAsync(
            string modulKod,
            IDosyaYetkiKontrol yetki,
            string? formAd = null,
            string? formAnahtar = null)
        {
            Sonuc<List<DosyaBilgisi>> sonuc = new Sonuc<List<DosyaBilgisi>>();

            try
            {
                if (string.IsNullOrWhiteSpace(modulKod))
                {
                    HataEkle(sonuc, "Business.File.ModuleCodeRequired");
                    return sonuc;
                }

                if (!await yetki.GorebilirAsync(modulKod, formAd, formAnahtar, null))
                {
                    HataEkle(sonuc, "Business.File.ViewFilesUnauthorized");
                    return sonuc;
                }

                await using SqlConnection connection = await BaglantiAcAsync();
                TABDosya tabDosya = new TABDosya(connection);
                sonuc.nesne = await tabDosya.ListeleAsync(modulKod.Trim(), Temizle(formAd), Temizle(formAnahtar));
            }
            catch (Exception ex)
            {
                BeklenmeyenHata(sonuc, ex, "Dosya listesi okunamadı. ModulKod: {ModulKod}", "Business.File.ListReadFailed", modulKod);
            }

            return sonuc;
        }

        public async Task<Sonuc<Dictionary<int, int>>> BasvuruDosyalariniKopyalaAsync(int kaynakBasvuruId, int hedefBasvuruId)
        {
            Sonuc<Dictionary<int, int>> sonuc = new();
            if (kaynakBasvuruId <= 0 || hedefBasvuruId <= 0)
            {
                sonuc.HataEkle("Dosya kopyalama için kaynak ve hedef başvuru seçilmelidir.");
                return sonuc;
            }

            try
            {
                await using SqlConnection connection = await BaglantiAcAsync();
                await using SqlTransaction transaction = (SqlTransaction)await connection.BeginTransactionAsync();
                try
                {
                    const string kaynakSql = @"
                        SELECT Id
                        FROM dbo.DosyaBilgisi
                        WHERE ModulKod = N'Basvuru' AND FormAnahtar = @KaynakAnahtar
                        ORDER BY Id;";
                    List<int> kaynakDosyaIdleri = [];
                    await using (SqlCommand kaynakCommand = new(kaynakSql, connection, transaction))
                    {
                        kaynakCommand.Parameters.AddWithValue("@KaynakAnahtar", kaynakBasvuruId.ToString());
                        await using SqlDataReader reader = await kaynakCommand.ExecuteReaderAsync();
                        while (await reader.ReadAsync())
                            kaynakDosyaIdleri.Add(reader.GetInt32(0));
                    }

                    foreach (int kaynakDosyaId in kaynakDosyaIdleri)
                    {
                        const string bilgiSql = @"
                            INSERT INTO dbo.DosyaBilgisi
                                (ModulKod, FormAd, FormAnahtar, DosyaNo, DosyaAdi, Buyukluk, IlkYuklemeTarihi, STarihi, Aciklama)
                            OUTPUT INSERTED.Id
                            SELECT ModulKod, FormAd, @HedefAnahtar, DosyaNo, DosyaAdi, Buyukluk, GETDATE(), GETDATE(), Aciklama
                            FROM dbo.DosyaBilgisi
                            WHERE Id = @KaynakDosyaId;";
                        int hedefDosyaId;
                        await using (SqlCommand bilgiCommand = new(bilgiSql, connection, transaction))
                        {
                            bilgiCommand.Parameters.AddWithValue("@HedefAnahtar", hedefBasvuruId.ToString());
                            bilgiCommand.Parameters.AddWithValue("@KaynakDosyaId", kaynakDosyaId);
                            hedefDosyaId = Convert.ToInt32(await bilgiCommand.ExecuteScalarAsync());
                        }

                        const string icerikSql = @"
                            INSERT INTO dbo.DosyaIcerik (DosyaId, PaketNo, PaketIcerik)
                            SELECT @HedefDosyaId, PaketNo, PaketIcerik
                            FROM dbo.DosyaIcerik
                            WHERE DosyaId = @KaynakDosyaId;";
                        await using SqlCommand icerikCommand = new(icerikSql, connection, transaction);
                        icerikCommand.Parameters.AddWithValue("@HedefDosyaId", hedefDosyaId);
                        icerikCommand.Parameters.AddWithValue("@KaynakDosyaId", kaynakDosyaId);
                        await icerikCommand.ExecuteNonQueryAsync();
                        sonuc.nesne[kaynakDosyaId] = hedefDosyaId;
                    }

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
                BeklenmeyenHata(sonuc, ex,
                    "Başvuru dosyaları kopyalanamadı. KaynakBasvuruId: {KaynakBasvuruId}, HedefBasvuruId: {HedefBasvuruId}",
                    "Başvuru dosyaları kopyalanamadı.", kaynakBasvuruId, hedefBasvuruId);
            }

            return sonuc;
        }

        public async Task<Sonuc<Dosya>> DosyaGetirAsync(DosyaAnahtari anahtar, IDosyaYetkiKontrol yetki)
        {
            Sonuc<Dosya> sonuc = new Sonuc<Dosya>();

            try
            {
                if (!AnahtarDogrula(anahtar, sonuc))
                    return sonuc;

                if (!await yetki.GorebilirAsync(anahtar.ModulKod, anahtar.FormAd, anahtar.FormAnahtar, anahtar.DosyaNo))
                {
                    HataEkle(sonuc, "Business.File.ViewFileUnauthorized");
                    return sonuc;
                }

                await using SqlConnection connection = await BaglantiAcAsync();
                TABDosya tabDosya = new TABDosya(connection);
                Dosya? dosya = await tabDosya.GetirAsync(anahtar);

                if (dosya == null)
                {
                    HataEkle(sonuc, "Business.File.NotFound");
                    return sonuc;
                }

                sonuc.nesne = dosya;
            }
            catch (Exception ex)
            {
                BeklenmeyenHata(sonuc, ex, "Dosya okunamadı. ModulKod: {ModulKod}, FormAd: {FormAd}, FormAnahtar: {FormAnahtar}, DosyaNo: {DosyaNo}", "Business.File.ReadFailed", anahtar.ModulKod, anahtar.FormAd, anahtar.FormAnahtar, anahtar.DosyaNo);
            }

            return sonuc;
        }

        public async Task<Sonuc<Dosya>> DosyaGetirAsync(int dosyaId, IDosyaYetkiKontrol yetki)
        {
            Sonuc<Dosya> sonuc = new Sonuc<Dosya>();

            try
            {
                if (dosyaId <= 0)
                {
                    HataEkle(sonuc, "Business.File.FileRequired");
                    return sonuc;
                }

                await using SqlConnection connection = await BaglantiAcAsync();
                TABDosya tabDosya = new TABDosya(connection);
                Dosya? dosya = await tabDosya.GetirAsync(dosyaId);

                if (dosya == null)
                {
                    HataEkle(sonuc, "Business.File.NotFound");
                    return sonuc;
                }

                if (!await yetki.GorebilirAsync(dosya.ModulKod, dosya.FormAd, dosya.FormAnahtar, dosya.DosyaNo))
                {
                    HataEkle(sonuc, "Business.File.ViewFileUnauthorized");
                    return sonuc;
                }

                sonuc.nesne = dosya;
            }
            catch (Exception ex)
            {
                BeklenmeyenHata(sonuc, ex, "Dosya okunamadı. DosyaId: {DosyaId}", "Business.File.ReadFailed", dosyaId);
            }

            return sonuc;
        }

        public async Task<Sonuc<DosyaBilgisi>> DosyaEkleVeyaGuncelleAsync(DosyaKaydetModel dosya, IDosyaYetkiKontrol yetki)
        {
            Sonuc<DosyaBilgisi> sonuc = new Sonuc<DosyaBilgisi>();

            try
            {
                if (!DosyaDogrula(dosya, sonuc))
                    return sonuc;

                await using SqlConnection connection = await BaglantiAcAsync();
                await using SqlTransaction transaction = (SqlTransaction)await connection.BeginTransactionAsync();

                try
                {
                    TABDosya tabDosya = new TABDosya(connection, null, transaction);
                    TABDosyaLog tabDosyaLog = new TABDosyaLog(connection, null, transaction);

                    DosyaBilgisi? mevcut = null;
                    int dosyaNo = dosya.DosyaNo;
                    bool yeniKayit = dosyaNo == 0;

                    if (yeniKayit)
                    {
                        if (!await yetki.EkleyebilirAsync(dosya.ModulKod, dosya.FormAd, dosya.FormAnahtar))
                        {
                            HataEkle(sonuc, "Business.File.AddUnauthorized");
                            await transaction.RollbackAsync();
                            return sonuc;
                        }

                        dosyaNo = await tabDosya.SonrakiDosyaNoAsync(dosya);
                    }
                    else
                    {
                        mevcut = await tabDosya.BilgiGetirAsync(dosya);
                        if (mevcut == null)
                        {
                            if (!await yetki.EkleyebilirAsync(dosya.ModulKod, dosya.FormAd, dosya.FormAnahtar))
                            {
                                HataEkle(sonuc, "Business.File.AddUnauthorized");
                                await transaction.RollbackAsync();
                                return sonuc;
                            }

                            yeniKayit = true;
                        }
                        else if (!await yetki.GuncelleyebilirAsync(dosya.ModulKod, dosya.FormAd, dosya.FormAnahtar, dosyaNo))
                        {
                            HataEkle(sonuc, "Business.File.UpdateUnauthorized");
                            await transaction.RollbackAsync();
                            return sonuc;
                        }
                    }

                    int dosyaId;
                    if (yeniKayit)
                    {
                        dosyaId = await tabDosya.EkleAsync(dosya, dosyaNo);
                    }
                    else
                    {
                        dosyaId = mevcut!.Id;
                        await tabDosya.GuncelleAsync(dosyaId, dosya);
                        await tabDosya.IcerikSilAsync(dosyaId);
                    }

                    await IcerikPaketleriniYazAsync(tabDosya, dosyaId, dosya.Icerik);

                    DosyaAnahtari logAnahtari = new DosyaAnahtari
                    {
                        ModulKod = dosya.ModulKod,
                        FormAd = dosya.FormAd,
                        FormAnahtar = dosya.FormAnahtar,
                        DosyaNo = dosyaNo
                    };

                    await tabDosyaLog.EkleAsync(logAnahtari, dosyaId, yeniKayit ? "Ekle" : "Guncelle", new
                    {
                        dosya.ModulKod,
                        dosya.FormAd,
                        dosya.FormAnahtar,
                        DosyaNo = dosyaNo,
                        dosya.DosyaAdi,
                        Buyukluk = dosya.Icerik.LongLength,
                        dosya.Aciklama
                    });

                    await transaction.CommitAsync();

                    TABDosya tabDosyaOkuma = new TABDosya(connection);
                    sonuc.nesne = (await tabDosyaOkuma.BilgiGetirAsync(logAnahtari))!;
                    sonuc.mesaj = yeniKayit ? Metin("Business.File.Added") : Metin("Business.File.Updated");
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                BeklenmeyenHata(sonuc, ex, "Dosya kaydedilemedi. ModulKod: {ModulKod}, FormAd: {FormAd}, FormAnahtar: {FormAnahtar}, DosyaNo: {DosyaNo}", "Business.File.SaveFailed", dosya.ModulKod, dosya.FormAd, dosya.FormAnahtar, dosya.DosyaNo);
            }

            return sonuc;
        }

        public async Task<Sonuc> DosyaSilAsync(DosyaAnahtari anahtar, IDosyaYetkiKontrol yetki)
        {
            Sonuc sonuc = new Sonuc();

            try
            {
                if (!AnahtarDogrula(anahtar, sonuc))
                    return sonuc;

                if (!await yetki.SilebilirAsync(anahtar.ModulKod, anahtar.FormAd, anahtar.FormAnahtar, anahtar.DosyaNo))
                {
                    HataEkle(sonuc, "Business.File.DeleteUnauthorized");
                    return sonuc;
                }

                await using SqlConnection connection = await BaglantiAcAsync();
                await using SqlTransaction transaction = (SqlTransaction)await connection.BeginTransactionAsync();

                try
                {
                    TABDosya tabDosya = new TABDosya(connection, null, transaction);
                    TABDosyaLog tabDosyaLog = new TABDosyaLog(connection, null, transaction);

                    DosyaBilgisi? mevcut = await tabDosya.BilgiGetirAsync(anahtar);
                    if (mevcut == null)
                    {
                        HataEkle(sonuc, "Business.File.NotFound");
                        await transaction.RollbackAsync();
                        return sonuc;
                    }

                    await tabDosyaLog.EkleAsync(anahtar, mevcut.Id, "Sil", mevcut);
                    await tabDosya.SilAsync(mevcut.Id);

                    await transaction.CommitAsync();
                    sonuc.mesaj = Metin("Business.File.Deleted");
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                BeklenmeyenHata(sonuc, ex, "Dosya silinemedi. ModulKod: {ModulKod}, FormAd: {FormAd}, FormAnahtar: {FormAnahtar}, DosyaNo: {DosyaNo}", "Business.File.DeleteFailed", anahtar.ModulKod, anahtar.FormAd, anahtar.FormAnahtar, anahtar.DosyaNo);
            }

            return sonuc;
        }

        private async Task<SqlConnection> BaglantiAcAsync()
        {
            if (string.IsNullOrWhiteSpace(_connectionString))
                throw new InvalidOperationException(Metin("Business.File.ConnectionStringMissing"));

            SqlConnection connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            return connection;
        }

        private static async Task IcerikPaketleriniYazAsync(TABDosya tabDosya, int dosyaId, byte[] icerik)
        {
            int paketNo = 1;
            for (int konum = 0; konum < icerik.Length; konum += PaketBoyutu)
            {
                int uzunluk = Math.Min(PaketBoyutu, icerik.Length - konum);
                byte[] paket = new byte[uzunluk];
                Buffer.BlockCopy(icerik, konum, paket, 0, uzunluk);
                await tabDosya.IcerikEkleAsync(dosyaId, paketNo, paket);
                paketNo++;
            }
        }

        private bool AnahtarDogrula(DosyaAnahtari anahtar, Sonuc sonuc)
        {
            if (string.IsNullOrWhiteSpace(anahtar.ModulKod))
                HataEkle(sonuc, "Business.File.ModuleCodeRequired");
            if (string.IsNullOrWhiteSpace(anahtar.FormAd))
                HataEkle(sonuc, "Business.File.FormNameRequired");
            if (string.IsNullOrWhiteSpace(anahtar.FormAnahtar))
                HataEkle(sonuc, "Business.File.FormKeyRequired");
            if (anahtar.DosyaNo <= 0)
                HataEkle(sonuc, "Business.File.FileNoRequired");

            AnahtarTemizle(anahtar);
            return sonuc.basarili;
        }

        private bool DosyaDogrula(DosyaKaydetModel dosya, Sonuc sonuc)
        {
            if (string.IsNullOrWhiteSpace(dosya.ModulKod))
                HataEkle(sonuc, "Business.File.ModuleCodeRequired");
            if (string.IsNullOrWhiteSpace(dosya.FormAd))
                HataEkle(sonuc, "Business.File.FormNameRequired");
            if (string.IsNullOrWhiteSpace(dosya.FormAnahtar))
                HataEkle(sonuc, "Business.File.FormKeyRequired");
            if (dosya.DosyaNo < 0)
                HataEkle(sonuc, "Business.File.FileNoCannotBeNegative");
            if (string.IsNullOrWhiteSpace(dosya.DosyaAdi))
                HataEkle(sonuc, "Business.File.FileNameRequired");
            if (dosya.Icerik == null || dosya.Icerik.Length == 0)
                HataEkle(sonuc, "Business.File.ContentRequired");

            AnahtarTemizle(dosya);
            dosya.DosyaAdi = dosya.DosyaAdi?.Trim() ?? "";
            dosya.Aciklama = Temizle(dosya.Aciklama);
            return sonuc.basarili;
        }

        private static void AnahtarTemizle(DosyaAnahtari anahtar)
        {
            anahtar.ModulKod = anahtar.ModulKod?.Trim() ?? "";
            anahtar.FormAd = anahtar.FormAd?.Trim() ?? "";
            anahtar.FormAnahtar = anahtar.FormAnahtar?.Trim() ?? "";
        }

        private static string? Temizle(string? deger)
        {
            return string.IsNullOrWhiteSpace(deger) ? null : deger.Trim();
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
    }
}
