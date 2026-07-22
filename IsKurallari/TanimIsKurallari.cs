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
