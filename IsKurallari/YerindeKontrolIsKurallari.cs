using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Localization;
using TarimDonusum.Models;
using TarimDonusum.Tablolar;

namespace TarimDonusum.IsKurallari
{
    /// <summary>Yerinde Kontrol (DB 1201): yalnızca uzmanlar (sistem yöneticisi, birim kullanıcısı) kullanır.</summary>
    public class YerindeKontrolIsKurallari
    {
        private readonly string _connectionString;
        private readonly ILogger<YerindeKontrolIsKurallari> _logger;
        private readonly IStringLocalizer<SharedResource> _localizer;
        private readonly BasvuruIsKurallari _basvuru;

        public YerindeKontrolIsKurallari(IConfiguration configuration, ILogger<YerindeKontrolIsKurallari> logger, IStringLocalizer<SharedResource> localizer, BasvuruIsKurallari basvuru)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
            _logger = logger;
            _localizer = localizer;
            _basvuru = basvuru;
        }

        public static bool UzmanMi(Kullanici? kullanici) =>
            kullanici != null
            && !kullanici.Yetkiler.Any(x => x.Rol == KullaniciRol.BasvuruKullanicisi)
            && kullanici.Yetkiler.Any(x => x.Rol is KullaniciRol.SistemYoneticisi or KullaniciRol.BirimKullanicisi);

        private static bool YetkiKontrol(Kullanici? kullanici, Sonuc sonuc)
        {
            if (UzmanMi(kullanici)) return true;
            sonuc.HataEkle("Yerinde kontrol yalnızca uzmanlar tarafından yapılabilir.");
            return false;
        }

        /// <summary>Kullanıcının erişebildiği başvuru sürümleri (birim kullanıcısında birimin illeriyle sınırlı).</summary>
        private async Task<List<Basvuru>> ErisilebilirBasvurularAsync(Kullanici kullanici, Sonuc sonuc)
        {
            Sonuc<List<Basvuru>> liste = await _basvuru.KullaniciBasvuruVersiyonlariniListeleAsync(kullanici);
            if (!liste.basarili) foreach (string hata in liste.hatalar) sonuc.HataEkle(hata);
            return liste.nesne ?? [];
        }

        public async Task<Sonuc<List<YerindeKontrolListeSatiri>>> ListeleAsync(Kullanici? kullanici)
        {
            Sonuc<List<YerindeKontrolListeSatiri>> sonuc = new();
            if (!YetkiKontrol(kullanici, sonuc)) return sonuc;
            try
            {
                List<Basvuru> basvurular = (await ErisilebilirBasvurularAsync(kullanici!, sonuc)).Where(YerindeKontrolTanimlari.KontrolEdilebilir).ToList();
                if (!sonuc.basarili) return sonuc;
                await using SqlConnection connection = new(_connectionString);
                await connection.OpenAsync();
                ILookup<int, YerindeKontrol> kontroller = (await new TABYerindeKontrol(connection, _localizer).ListeleAsync()).ToLookup(x => x.basvuruId);
                sonuc.nesne = basvurular.OrderByDescending(x => x.Id)
                    .Select(b => new YerindeKontrolListeSatiri { basvuru = b, kontroller = kontroller[b.Id].ToList() }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Yerinde kontrol listesi okunamadı.");
                sonuc.HataEkle("Yerinde kontrol listesi okunamadı.");
            }
            return sonuc;
        }

        /// <summary>Başvuru için yeni yerinde kontrol açar; adres, yapı ve makine satırları başvurudaki beyanlarla doldurulur.</summary>
        public async Task<Sonuc<int>> BaslatAsync(int basvuruId, Kullanici? kullanici)
        {
            Sonuc<int> sonuc = new();
            if (!YetkiKontrol(kullanici, sonuc)) return sonuc;
            try
            {
                List<Basvuru> erisilebilir = await ErisilebilirBasvurularAsync(kullanici!, sonuc);
                if (!sonuc.basarili) return sonuc;
                if (!erisilebilir.Any(x => x.Id == basvuruId && YerindeKontrolTanimlari.KontrolEdilebilir(x)))
                {
                    sonuc.HataEkle("Yerinde kontrol yalnızca incelemedeki veya kabul edilmiş güncel başvuru için başlatılabilir.");
                    return sonuc;
                }
                Sonuc<Basvuru> okuma = await _basvuru.OkuAsync(basvuruId);
                if (!okuma.basarili || okuma.nesne == null) { sonuc.HataEkle("Başvuru okunamadı."); return sonuc; }
                Basvuru b = okuma.nesne;

                YerindeKontrol k = new()
                {
                    basvuruId = basvuruId,
                    olusturanKullaniciId = kullanici!.Id,
                    uzman1KullaniciId = kullanici.Id,
                    uzman1Adi = $"{kullanici.Ad} {kullanici.Soyad}".Trim(),
                    adresler = b.YatirimAdresleri.OrderBy(x => x.siraNo).Select((a, i) => new YerindeKontrolAdres
                    {
                        uygulamaAdresiId = a.id, siraNo = i + 1,
                        degerZinciriAsamalari = string.Join(", ", a.degerZinciriAsamalari.Select(x => x.ad).Where(x => !string.IsNullOrWhiteSpace(x)))
                    }).ToList(),
                    yapilar = b.Binalar.OrderBy(x => x.siraNo).Select((y, i) => new YerindeKontrolYapi
                    {
                        binaId = y.id, siraNo = i + 1, yapiAdi = y.ad, mevcutDurum = y.mevcutYeni, kullanimAmaci = y.yatirimSekli,
                        alanBeyan = y.mahaller.Count > 0 ? y.mahaller.Sum(x => x.alanM2) : null
                    }).ToList(),
                    makineler = b.Makineler.OrderBy(x => x.siraNo).Select((m, i) => new YerindeKontrolMakine
                    {
                        makineId = m.id, siraNo = i + 1,
                        mevcutDurum = m.durum.StartsWith("MEVCUT", StringComparison.OrdinalIgnoreCase) ? "Mevcut" : "Yeni",
                        projedeKullanilacak = m.durum != "MEVCUT KULLANILMAYACAK",
                        makineAdi = m.ad, uygunHarcamaId = m.uygunHarcamaId, miktarBeyan = m.miktar,
                        markaModelBeyan = string.Join(" / ", new[] { m.marka, m.model }.Where(x => !string.IsNullOrWhiteSpace(x))),
                        kullanimAmaciBeyan = m.kullanimAmaci,
                        teknikOzelliklerBeyan = string.Join("; ", new[] { m.kapasiteOzellikleri }.Concat(m.teknikOzellikler.OrderBy(x => x.siraNo).Select(x => $"{x.baslik}: {x.aciklamaAsgariGereklilik}"))
                            .Where(x => !string.IsNullOrWhiteSpace(x)))
                    }).ToList(),
                    sorular = YerindeKontrolTanimlari.Sorular.Select(x => new YerindeKontrolSoru { soruKodu = x.Kod, referans = x.Referans }).ToList()
                };
                string seriNoOnEki = $"{YerindeKontrol.FormKodu}/{b.basvuruFirma.il.kod:00}/{b.basvuruFirma.degerZinciriId}/";

                await using SqlConnection connection = new(_connectionString);
                await connection.OpenAsync();
                await using SqlTransaction transaction = (SqlTransaction)await connection.BeginTransactionAsync();
                try
                {
                    sonuc.nesne = await new TABYerindeKontrol(connection, _localizer, transaction).EkleAsync(k, seriNoOnEki);
                    await new TABBasvuruLog(connection, _localizer, transaction).EkleAsync(basvuruId, kullanici, "YerindeKontrolBaslatildi", new { YerindeKontrolId = sonuc.nesne });
                    await transaction.CommitAsync();
                    sonuc.mesaj = "Yerinde kontrol başlatıldı.";
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Yerinde kontrol başlatılamadı. BasvuruId: {BasvuruId}", basvuruId);
                sonuc.HataEkle("Yerinde kontrol başlatılamadı.");
            }
            return sonuc;
        }

        public async Task<Sonuc<YerindeKontrolFormModel>> OkuAsync(int id, Kullanici? kullanici)
        {
            Sonuc<YerindeKontrolFormModel> sonuc = new();
            if (!YetkiKontrol(kullanici, sonuc)) return sonuc;
            try
            {
                YerindeKontrol? k;
                await using (SqlConnection connection = new(_connectionString))
                {
                    await connection.OpenAsync();
                    k = await new TABYerindeKontrol(connection, _localizer).OkuAsync(id);
                }
                if (k == null) { sonuc.HataEkle("Yerinde kontrol kaydı bulunamadı."); return sonuc; }
                Sonuc<Basvuru> okuma = await _basvuru.OkuAsync(k.basvuruId);
                if (!okuma.basarili || okuma.nesne == null) { sonuc.HataEkle("Yerinde kontrolün başvurusu okunamadı."); return sonuc; }
                List<Basvuru> erisilebilir = await ErisilebilirBasvurularAsync(kullanici!, sonuc);
                if (!sonuc.basarili) return sonuc;
                if (!erisilebilir.Any(x => x.BasvuruAnaId == okuma.nesne.BasvuruAnaId))
                {
                    sonuc.HataEkle("Bu yerinde kontrol kaydını görüntüleme yetkiniz bulunmuyor.");
                    return sonuc;
                }
                sonuc.nesne = new YerindeKontrolFormModel { kontrol = k, basvuru = okuma.nesne, duzenlenebilir = k.durum == enumYerindeKontrolDurum.Taslak };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Yerinde kontrol okunamadı. Id: {Id}", id);
                sonuc.HataEkle("Yerinde kontrol okunamadı.");
            }
            return sonuc;
        }

        private static readonly int[] DilimOrtaMeridyenleri = [27, 30, 33, 36, 39, 42, 45];

        /// <summary>
        /// Doğrulama bölümleri: 1 (adres ve GPS), 1.1 (yapılar), 1.2 (makine-ekipman), 2 (yatırımın gerçekleştirilmesi).
        /// Uzman "bilgiler doğru" derse düzeltme alanları temizlenir; "Hayır" derse yerinde tespit edilen doğru bilgiler zorunludur.
        /// </summary>
        public async Task<Sonuc<int>> BolumKaydetAsync(YerindeKontrolBolumKayit model, Kullanici? kullanici)
        {
            Sonuc<int> sonuc = new();
            Sonuc<YerindeKontrolFormModel> mevcut = await OkuAsync(model.id, kullanici);
            if (!mevcut.basarili || mevcut.nesne == null) { foreach (string hata in mevcut.hatalar) sonuc.HataEkle(hata); return sonuc; }
            if (!mevcut.nesne.duzenlenebilir) { sonuc.HataEkle("Tamamlanmış yerinde kontrol değiştirilemez."); return sonuc; }
            YerindeKontrol k = mevcut.nesne.kontrol;
            string yorum = (model.yorum ?? "").Trim();
            if (yorum.Length > 4000) sonuc.HataEkle("Yorum en fazla 4000 karakter olabilir.");
            static string M(string? x) => (x ?? "").Trim();
            static bool Negatif(params decimal?[] x) => x.Any(y => y < 0);

            switch (model.bolum)
            {
                case "1":
                    model.adresler = (model.adresler ?? []).Where(x => k.adresler.Any(a => a.id == x.id)).ToList();
                    foreach (YerindeKontrolAdres a in model.adresler)
                    {
                        YerindeKontrolAdres kayitli = k.adresler.First(x => x.id == a.id);
                        string ad = $"{kayitli.siraNo}. adres";
                        a.notlar = M(a.notlar);
                        if (a.adresAyniMi != false) a.notlar = "";
                        else if (a.notlar.Length == 0) sonuc.HataEkle($"{ad}: Adres bilgileri doğru değilse gözlemlenen adres / notlar girilmelidir.");
                        if (a.notlar.Length > 4000) sonuc.HataEkle($"{ad}: Notlar en fazla 4000 karakter olabilir.");

                        // Konumlar başvurudaki adresin konumlarıdır; başvuruda konumu olmayan adreste tek, konumsuz satır açılır.
                        List<int> basvuruKonumlari = mevcut.nesne.basvuru.YatirimAdresleri.FirstOrDefault(x => x.id == kayitli.uygulamaAdresiId)?.konumlar.Select(x => x.id).ToList() ?? [];
                        a.konumlar = (a.konumlar ?? []).Where(x => basvuruKonumlari.Count == 0 ? x.adresKonumId == null : x.adresKonumId.HasValue && basvuruKonumlari.Contains(x.adresKonumId.Value))
                            .GroupBy(x => x.adresKonumId).Select(x => x.First()).ToList();
                        for (int no = 0; no < a.konumlar.Count; no++)
                        {
                            YerindeKontrolKonum konum = a.konumlar[no];
                            string konumAdi = $"{ad} {(basvuruKonumlari.Count == 0 ? "" : $"{basvuruKonumlari.IndexOf(konum.adresKonumId!.Value) + 1}. konum")}".Trim();
                            konum.notlar = M(konum.notlar);
                            if (!Enum.IsDefined(konum.koordinatTuru)) konum.koordinatTuru = enumKoordinatTuru.ITRF96;
                            if (konum.konumDogruMu != false)
                            {
                                konum.notlar = ""; konum.dilimOrtaMeridyen = null;
                                konum.olculenEnDusukY = konum.olculenEnDusukX = konum.olculenEnYuksekY = konum.olculenEnYuksekX = null;
                                continue;
                            }
                            if (!konum.olculenEnDusukY.HasValue && !konum.olculenEnDusukX.HasValue && !konum.olculenEnYuksekY.HasValue && !konum.olculenEnYuksekX.HasValue)
                                sonuc.HataEkle($"{konumAdi}: Konum bilgileri doğru değilse ölçülen GPS koordinatları girilmelidir.");
                            if (konum.dilimOrtaMeridyen.HasValue && !DilimOrtaMeridyenleri.Contains(konum.dilimOrtaMeridyen.Value))
                                sonuc.HataEkle($"{konumAdi}: Dilim orta meridyeni {string.Join(", ", DilimOrtaMeridyenleri)} değerlerinden biri olmalıdır.");
                            if (Negatif(konum.olculenEnDusukY, konum.olculenEnDusukX, konum.olculenEnYuksekY, konum.olculenEnYuksekX)) sonuc.HataEkle($"{konumAdi}: Koordinatlar negatif olamaz.");
                            if (konum.olculenEnDusukY > konum.olculenEnYuksekY || konum.olculenEnDusukX > konum.olculenEnYuksekX) sonuc.HataEkle($"{konumAdi}: En düşük koordinat en yüksek koordinattan büyük olamaz.");
                            if (konum.notlar.Length > 4000) sonuc.HataEkle($"{konumAdi}: Notlar en fazla 4000 karakter olabilir.");
                        }
                    }
                    break;

                case "1.1":
                    model.yapilar = (model.yapilar ?? []).Where(x => k.yapilar.Any(y => y.id == x.id)).ToList();
                    foreach (YerindeKontrolYapi y in model.yapilar)
                    {
                        string ad = k.yapilar.First(x => x.id == y.id).yapiAdi;
                        y.ekBulgular = M(y.ekBulgular); y.fotografNo = M(y.fotografNo);
                        if (y.fotografNo.Length > 100) sonuc.HataEkle($"{ad}: Fotoğraf no en fazla 100 karakter olabilir.");
                        if (y.bilgilerDogruMu != false) { y.uzunlukBulgu = y.genislikBulgu = y.alanBulgu = null; y.ekBulgular = ""; continue; }
                        if (!y.uzunlukBulgu.HasValue && !y.genislikBulgu.HasValue && !y.alanBulgu.HasValue && y.ekBulgular.Length == 0)
                            sonuc.HataEkle($"{ad}: Bilgiler doğru değilse yerinde ölçülen uzunluk, genişlik, alan veya ek bulgu girilmelidir.");
                        if (Negatif(y.uzunlukBulgu, y.genislikBulgu, y.alanBulgu)) sonuc.HataEkle($"{ad}: Ölçüler negatif olamaz.");
                        if (y.ekBulgular.Length > 2000) sonuc.HataEkle($"{ad}: Ek bulgular en fazla 2000 karakter olabilir.");
                    }
                    break;

                case "1.2":
                    model.makineler = (model.makineler ?? []).Where(x => k.makineler.Any(m => m.id == x.id)).ToList();
                    foreach (YerindeKontrolMakine m in model.makineler)
                    {
                        string ad = k.makineler.First(x => x.id == m.id).makineAdi;
                        m.markaModelBulgu = M(m.markaModelBulgu); m.seriNoBulgu = M(m.seriNoBulgu); m.kullanimAmaciBulgu = M(m.kullanimAmaciBulgu);
                        m.teknikOzelliklerBulgu = M(m.teknikOzelliklerBulgu); m.fotografNo = M(m.fotografNo);
                        if (m.fotografNo.Length > 100) sonuc.HataEkle($"{ad}: Fotoğraf no en fazla 100 karakter olabilir.");
                        if (m.bilgilerDogruMu != false)
                        {
                            m.miktarBulgu = null; m.markaModelBulgu = m.seriNoBulgu = m.kullanimAmaciBulgu = m.teknikOzelliklerBulgu = "";
                            continue;
                        }
                        if (!m.miktarBulgu.HasValue && new[] { m.markaModelBulgu, m.seriNoBulgu, m.kullanimAmaciBulgu, m.teknikOzelliklerBulgu }.All(x => x.Length == 0))
                            sonuc.HataEkle($"{ad}: Bilgiler doğru değilse yerinde tespit edilen miktar, marka/model, seri no, kullanım amacı veya teknik özellik girilmelidir.");
                        if (Negatif(m.miktarBulgu)) sonuc.HataEkle($"{ad}: Miktar negatif olamaz.");
                        if (m.markaModelBulgu.Length > 300 || m.seriNoBulgu.Length > 150 || m.kullanimAmaciBulgu.Length > 2000 || m.teknikOzelliklerBulgu.Length > 2000)
                            sonuc.HataEkle($"{ad}: Bulgu alanlarından biri izin verilen uzunluğu aşıyor.");
                    }
                    break;

                case "2":
                    model.sorular = (model.sorular ?? []).Where(x => YerindeKontrolTanimlari.Sorular.Any(t => t.Kod == x.soruKodu)).ToList();
                    foreach (YerindeKontrolSoru s in model.sorular)
                    {
                        s.cevap = M(s.cevap); s.bulgu = M(s.bulgu); s.fotografNo = M(s.fotografNo); s.referans = M(s.referans); s.yorum = M(s.yorum);
                        if (s.cevap is not ("" or "Evet" or "Hayır")) sonuc.HataEkle($"{s.soruKodu}: Cevap Evet veya Hayır olmalıdır.");
                        if (s.dogruMu == false && s.bulgu.Length == 0) sonuc.HataEkle($"{s.soruKodu}: Beyan doğru değilse yerinde kontrol bulgusu girilmelidir.");
                        if (s.bulgu.Length > 4000 || s.yorum.Length > 4000 || s.referans.Length > 500 || s.fotografNo.Length > 100)
                            sonuc.HataEkle($"{s.soruKodu}: Alanlardan biri izin verilen uzunluğu aşıyor.");
                    }
                    break;

                default:
                    sonuc.HataEkle("Bu bölüm doğrulama bölümü değildir.");
                    break;
            }
            if (!sonuc.basarili) return sonuc;

            try
            {
                await using SqlConnection connection = new(_connectionString);
                await connection.OpenAsync();
                await using SqlTransaction transaction = (SqlTransaction)await connection.BeginTransactionAsync();
                TABYerindeKontrol tablo = new(connection, _localizer, transaction);
                try
                {
                    switch (model.bolum)
                    {
                        case "1": await tablo.AdreslerGuncelleAsync(k.id, model.adresler); break;
                        case "1.1": await tablo.YapilarGuncelleAsync(k.id, model.yapilar, yorum); break;
                        case "1.2": await tablo.MakinelerGuncelleAsync(k.id, model.makineler, yorum); break;
                        case "2": await tablo.SorularGuncelleAsync(k.id, model.sorular); break;
                    }
                    await transaction.CommitAsync();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
                sonuc.nesne = k.id;
                sonuc.mesaj = "Bölüm kaydedildi.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Yerinde kontrol bölümü kaydedilemedi. Id: {Id}, Bolum: {Bolum}", model.id, model.bolum);
                sonuc.HataEkle("Bölüm kaydedilemedi.");
            }
            return sonuc;
        }

        /// <summary>GB - Bölüm I: Genel Bölüm (kontrol tarihleri, hazır bulunanlar, uzmanlar).</summary>
        public async Task<Sonuc<int>> GenelBilgilerKaydetAsync(YerindeKontrol model, Kullanici? kullanici)
        {
            Sonuc<int> sonuc = new();
            model.hazirBulunanlar = (model.hazirBulunanlar ?? [])
                .Select(x => new YerindeKontrolHazirBulunan { adSoyad = (x.adSoyad ?? "").Trim(), telefon = (x.telefon ?? "").Trim() })
                .Where(x => x.adSoyad.Length > 0 || x.telefon.Length > 0).ToList();
            model.uzman1Adi = (model.uzman1Adi ?? "").Trim(); model.uzman2Adi = (model.uzman2Adi ?? "").Trim(); model.birimAmiriAdi = (model.birimAmiriAdi ?? "").Trim();
            if (model.baslangicTarihi.HasValue && model.bitisTarihi.HasValue && model.bitisTarihi < model.baslangicTarihi)
                sonuc.HataEkle("Bitiş tarihi başlangıç tarihinden önce olamaz.");
            if (model.hazirBulunanlar.Any(x => x.adSoyad.Length == 0)) sonuc.HataEkle("Hazır bulunan kişilerin adı soyadı girilmelidir.");
            if (model.hazirBulunanlar.Any(x => x.adSoyad.Length > 200 || x.telefon.Length > 30)) sonuc.HataEkle("Hazır bulunan kişi adı en fazla 200, telefon en fazla 30 karakter olabilir.");
            if (model.uzman1Adi.Length > 200 || model.uzman2Adi.Length > 200 || model.birimAmiriAdi.Length > 200) sonuc.HataEkle("Uzman ve birim amiri adları en fazla 200 karakter olabilir.");
            if (!sonuc.basarili) return sonuc;

            Sonuc<YerindeKontrolFormModel> mevcut = await OkuAsync(model.id, kullanici);
            if (!mevcut.basarili || mevcut.nesne == null) { foreach (string hata in mevcut.hatalar) sonuc.HataEkle(hata); return sonuc; }
            if (!mevcut.nesne.duzenlenebilir) { sonuc.HataEkle("Tamamlanmış yerinde kontrol değiştirilemez."); return sonuc; }
            try
            {
                await using SqlConnection connection = new(_connectionString);
                await connection.OpenAsync();
                if (!await new TABYerindeKontrol(connection, _localizer).GenelBilgilerGuncelleAsync(model))
                {
                    sonuc.HataEkle("Yerinde kontrol kaydı güncellenemedi.");
                    return sonuc;
                }
                sonuc.nesne = model.id;
                sonuc.mesaj = "Genel bilgiler kaydedildi.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Yerinde kontrol genel bilgileri kaydedilemedi. Id: {Id}", model.id);
                sonuc.HataEkle("Genel bilgiler kaydedilemedi.");
            }
            return sonuc;
        }
    }
}
