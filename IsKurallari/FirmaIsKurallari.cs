using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Localization;
using System.Text.Json;
using TarimDonusum.Models;
using TarimDonusum.Tablolar;

namespace TarimDonusum.IsKurallari
{
    public class FirmaIsKurallari
    {
        private readonly string _connectionString;
        private readonly ILogger<FirmaIsKurallari> _logger;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public FirmaIsKurallari(IConfiguration configuration, ILogger<FirmaIsKurallari> logger,
            IStringLocalizer<SharedResource> localizer)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
            _logger = logger;
            _localizer = localizer;
        }

        public async Task<Sonuc<List<Firma>>> AraAsync(FirmaArama arama, Kullanici? kullanici)
        {
            Sonuc<List<Firma>> sonuc = new();
            if (!KullaniciKontrolEt(kullanici, sonuc)) return sonuc;
            string metin = arama.AramaMetni?.Trim() ?? "";
            bool basvuran = kullanici!.Yetkiler.Any(x => x.Rol == KullaniciRol.BasvuruKullanicisi);
            if (string.IsNullOrWhiteSpace(metin) && !basvuran)
            {
                sonuc.HataEkle("Arama metni girilmelidir.");
                return sonuc;
            }
            try
            {
                await using SqlConnection connection = new(_connectionString);
                await connection.OpenAsync();
                sonuc.nesne = await new TABFirma(connection).AraAsync(metin, basvuran ? kullanici.Id : null);
            }
            catch (Exception ex) { Hata(sonuc, ex, "Firmalar aranamadı."); }
            return sonuc;
        }

        public async Task<Sonuc<Firma>> OkuAsync(int id, Kullanici? kullanici)
        {
            Sonuc<Firma> sonuc = new();
            if (!KullaniciKontrolEt(kullanici, sonuc)) return sonuc;
            try
            {
                await using SqlConnection connection = new(_connectionString);
                await connection.OpenAsync();
                TABFirma tab = new(connection);
                Firma? firma = await tab.OkuAsync(id);
                if (firma == null) sonuc.HataEkle("Firma bulunamadı.");
                else if (!await FirmaErisimiVarMiAsync(connection, firma.id, kullanici!))
                    sonuc.HataEkle("Bu firmaya erişim yetkiniz yok.");
                else sonuc.nesne = firma;
            }
            catch (Exception ex) { Hata(sonuc, ex, "Firma okunamadı."); }
            return sonuc;
        }

        public async Task<Sonuc<int>> KaydetAsync(Firma firma, Kullanici? kullanici)
        {
            Sonuc<int> sonuc = new();
            if (!KullaniciKontrolEt(kullanici, sonuc)) return sonuc;
            NormalizeEt(firma);
            firma.Dogrula(sonuc);
            if (!sonuc.basarili) return sonuc;
            try
            {
                await using SqlConnection connection = new(_connectionString);
                await connection.OpenAsync();
                if (firma.id > 0 && !await FirmaErisimiVarMiAsync(connection, firma.id, kullanici!))
                {
                    sonuc.HataEkle("Bu firmaya erişim yetkiniz yok."); return sonuc;
                }
                Firma? ayni = await new TABFirma(connection).VergiKimlikNoIleOkuAsync(0, firma.vergiKimlikNo!);
                if (ayni != null && ayni.id != firma.id)
                {
                    sonuc.HataEkle("Bu vergi kimlik numarasıyla kayıtlı başka bir firma bulunmaktadır."); return sonuc;
                }
                await using SqlTransaction tx = (SqlTransaction)await connection.BeginTransactionAsync();
                TABFirma tab = new(connection, null, tx);
                bool yeni = firma.id == 0;
                if (yeni)
                {
                    await tab.EkleAsync(firma);
                    if (kullanici!.Yetkiler.Any(x => x.Rol == KullaniciRol.BasvuruKullanicisi))
                    {
                        await new TABFirmaKullanici(connection, null, tx).EkleYoksaAsync(new FirmaKullanici
                        {
                            FirmaId = firma.id,
                            KullaniciId = kullanici.Id,
                            IliskiyiKuranKullaniciId = kullanici.Id
                        });
                    }
                }
                else await tab.GuncelleAsync(firma);
                await new TABFirmaLog(connection, null, tx).EkleAsync(firma, yeni ? "FirmaEklendi" : "FirmaGuncellendi", kullanici!.Id);
                await tx.CommitAsync();
                sonuc.nesne = firma.id;
                sonuc.mesaj = "Firma kaydedildi.";
            }
            catch (Exception ex) { Hata(sonuc, ex, "Firma kaydedilemedi."); }
            return sonuc;
        }

        public async Task<Sonuc<List<Kullanici>>> BasvuranAraAsync(string aramaMetni, Kullanici? kullanici)
        {
            Sonuc<List<Kullanici>> sonuc = new();
            if (!KullaniciKontrolEt(kullanici, sonuc)) return sonuc;
            if (string.IsNullOrWhiteSpace(aramaMetni)) { sonuc.HataEkle("Kullanıcı arama metni girilmelidir."); return sonuc; }
            try
            {
                await using SqlConnection connection = new(_connectionString);
                await connection.OpenAsync();
                sonuc.nesne = await new TABKullanici(connection).AraAsync(aramaMetni.Trim(), null, KullaniciRol.BasvuruKullanicisi);
                sonuc.nesne = sonuc.nesne.Where(x => x.Aktif).ToList();
            }
            catch (Exception ex) { Hata(sonuc, ex, "Başvuranlar aranamadı."); }
            return sonuc;
        }

        public Task<Sonuc> BasvuranEkleAsync(int firmaId, int kullaniciId, Kullanici? yapan)
            => BasvuranDegistirAsync(firmaId, kullaniciId, yapan, true);

        public Task<Sonuc> BasvuranCikarAsync(int firmaId, int kullaniciId, Kullanici? yapan)
            => BasvuranDegistirAsync(firmaId, kullaniciId, yapan, false);

        public async Task<Sonuc<List<FirmaLogGorunum>>> LoglariOkuAsync(int firmaId, Kullanici? kullanici)
        {
            Sonuc<List<FirmaLogGorunum>> sonuc = new();
            if (!KullaniciKontrolEt(kullanici, sonuc)) return sonuc;
            try
            {
                await using SqlConnection connection = new(_connectionString);
                await connection.OpenAsync();
                if (!await FirmaErisimiVarMiAsync(connection, firmaId, kullanici!))
                    sonuc.HataEkle("Bu firmaya erişim yetkiniz yok.");
                else sonuc.nesne = await new TABFirmaLog(connection).ListeleAsync(firmaId);
            }
            catch (Exception ex) { Hata(sonuc, ex, "Firma logları okunamadı."); }
            return sonuc;
        }

        private async Task<Sonuc> BasvuranDegistirAsync(int firmaId, int kullaniciId, Kullanici? yapan, bool ekle)
        {
            Sonuc sonuc = new();
            if (!KullaniciKontrolEt(yapan, sonuc)) return sonuc;
            try
            {
                await using SqlConnection connection = new(_connectionString);
                await connection.OpenAsync();
                if (!await FirmaErisimiVarMiAsync(connection, firmaId, yapan!))
                { sonuc.HataEkle("Bu firmaya erişim yetkiniz yok."); return sonuc; }
                Firma? firma = await new TABFirma(connection).OkuAsync(firmaId);
                Kullanici? hedef = await new TABKullanici(connection).OkuAsync(kullaniciId);
                if (firma == null || hedef == null || !hedef.Aktif)
                { sonuc.HataEkle("Firma veya kullanıcı bulunamadı."); return sonuc; }
                List<KullaniciYetki> hedefYetkileri =
                    await new TABKullaniciYetki(connection).KullaniciYetkileriniListeleAsync(kullaniciId);
                if (!hedefYetkileri.Any(x => x.Rol == KullaniciRol.BasvuruKullanicisi))
                { sonuc.HataEkle("Yalnızca başvuran tipindeki kullanıcılar firmaya eklenebilir."); return sonuc; }
                await using SqlTransaction tx = (SqlTransaction)await connection.BeginTransactionAsync();
                TABFirmaKullanici iliski = new(connection, null, tx);
                if (ekle)
                    await iliski.EkleYoksaAsync(new FirmaKullanici { FirmaId = firmaId, KullaniciId = kullaniciId, IliskiyiKuranKullaniciId = yapan!.Id });
                else
                    await iliski.PasifYapAsync(firmaId, kullaniciId);
                await new TABFirmaLog(connection, null, tx).EkleAsync(firma, ekle ? "BasvuranEklendi" : "BasvuranCikarildi",
                    yapan!.Id, new { KullaniciId = kullaniciId, hedef.Ad, hedef.Soyad });
                await tx.CommitAsync();
                sonuc.mesaj = ekle ? "Başvuran firmaya eklendi." : "Başvuran firmadan çıkarıldı.";
            }
            catch (Exception ex) { Hata(sonuc, ex, ekle ? "Başvuran eklenemedi." : "Başvuran çıkarılamadı."); }
            return sonuc;
        }

        private static async Task<bool> FirmaErisimiVarMiAsync(SqlConnection connection, int firmaId, Kullanici kullanici)
        {
            if (!kullanici.Yetkiler.Any(x => x.Rol == KullaniciRol.BasvuruKullanicisi)) return true;
            return await new TABFirmaKullanici(connection).IliskiVarMiAsync(firmaId, kullanici.Id);
        }

        private bool KullaniciKontrolEt(Kullanici? kullanici, Sonuc sonuc)
        {
            if (kullanici != null) return true;
            sonuc.HataEkle("Oturum kullanıcısı bulunamadı."); return false;
        }

        private static void NormalizeEt(Firma f)
        {
            f.vergiKimlikNo = f.vergiKimlikNo?.Trim() ?? ""; f.ticaretUnvani = f.ticaretUnvani?.Trim() ?? "";
            f.ticaretSicilNo = f.ticaretSicilNo?.Trim() ?? ""; f.mersisNo = f.mersisNo?.Trim() ?? "";
            f.naceKodu = f.naceKodu?.Trim() ?? ""; f.webSitesi = f.webSitesi?.Trim() ?? "";
            f.telefon = f.telefon?.Trim() ?? ""; f.kepAdresi = f.kepAdresi?.Trim() ?? "";
            f.eposta = f.eposta?.Trim() ?? ""; f.faaliyetKonusu = f.faaliyetKonusu?.Trim() ?? ""; f.adres = f.adres?.Trim() ?? "";
        }

        private void Hata(Sonuc sonuc, Exception ex, string mesaj)
        {
            _logger.LogError(ex, mesaj); sonuc.HataEkle(mesaj);
        }
    }
}
