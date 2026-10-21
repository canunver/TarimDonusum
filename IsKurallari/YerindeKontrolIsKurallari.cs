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
        private readonly DosyaYonetimIsKurallari _dosya;

        public YerindeKontrolIsKurallari(IConfiguration configuration, ILogger<YerindeKontrolIsKurallari> logger, IStringLocalizer<SharedResource> localizer, BasvuruIsKurallari basvuru,
            DosyaYonetimIsKurallari dosya)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
            _logger = logger;
            _localizer = localizer;
            _basvuru = basvuru;
            _dosya = dosya;
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

        /// <summary>
        /// "Bilgiler doğru mu?" Hayır ise: alanlar başvurudaki tanımlarla eşleştirilir; değiştirilmeli alanlarda yeni değer, kaldırılmalı alanlarda açıklama zorunludur.
        /// Makinenin kendisi kaldırılmalıysa diğer alanlar değerlendirilmez. Beyan ve alan adı sunucuda tanımdan yazılır.
        /// </summary>
        private static void MakineAlanlariniDogrula(YerindeKontrolMakine m, YerindeKontrolMakine kayitli, Basvuru basvuru, string ad, Sonuc sonuc)
        {
            BasvuruMakine? bm = kayitli.makineId.HasValue ? basvuru.Makineler.FirstOrDefault(x => x.id == kayitli.makineId) : null;
            List<YerindeKontrolMakineAlanTanimi> tanimlar = YerindeKontrolMakineAlanlari.Olustur(kayitli, bm);
            Dictionary<string, YerindeKontrolMakineAlan> gelen = (m.alanlar ?? []).Where(x => x != null).GroupBy(x => x.alanKodu ?? "").ToDictionary(x => x.Key, x => x.First());

            List<YerindeKontrolMakineAlan> alanlar = [];
            foreach (YerindeKontrolMakineAlanTanimi t in tanimlar)
            {
                YerindeKontrolMakineAlan a = gelen.GetValueOrDefault(t.Kod) ?? new YerindeKontrolMakineAlan();
                a.alanKodu = t.Kod; a.alanAdi = t.Ad; a.beyanDeger = t.Beyan;
                a.yeniDeger = (a.yeniDeger ?? "").Trim(); a.aciklama = (a.aciklama ?? "").Trim();
                if (!Enum.IsDefined(a.karar)) a.karar = enumYerindeKontrolAlanKarar.Dogru;
                alanlar.Add(a);
            }
            bool makineKaldirilmali = alanlar.First(x => x.alanKodu == YerindeKontrolMakineAlanlari.Makine).karar == enumYerindeKontrolAlanKarar.Kaldirilmali;
            if (makineKaldirilmali)
                foreach (YerindeKontrolMakineAlan a in alanlar.Where(x => x.alanKodu != YerindeKontrolMakineAlanlari.Makine))
                    a.karar = enumYerindeKontrolAlanKarar.Dogru;

            foreach (YerindeKontrolMakineAlan a in alanlar)
            {
                string alanAdi = $"{ad} – {a.alanAdi}";
                switch (a.karar)
                {
                    case enumYerindeKontrolAlanKarar.Dogru:
                        a.yeniDeger = ""; a.aciklama = "";
                        break;
                    case enumYerindeKontrolAlanKarar.Degistirilmeli:
                        if (a.yeniDeger.Length == 0) sonuc.HataEkle($"{alanAdi}: Değiştirilmeli seçildiyse yeni değer girilmelidir.");
                        else if (a.alanKodu == YerindeKontrolMakineAlanlari.Miktar
                                 && (!decimal.TryParse(a.yeniDeger.Replace(',', '.'), System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out decimal miktar) || miktar < 0))
                            sonuc.HataEkle($"{alanAdi}: Yeni miktar sıfır veya pozitif bir sayı olmalıdır.");
                        break;
                    case enumYerindeKontrolAlanKarar.Kaldirilmali:
                        a.yeniDeger = "";
                        if (a.aciklama.Length == 0) sonuc.HataEkle($"{alanAdi}: Kaldırılmalı seçildiyse açıklama girilmelidir.");
                        break;
                }
                if (a.yeniDeger.Length > 2000 || a.aciklama.Length > 2000) sonuc.HataEkle($"{alanAdi}: Yeni değer ve açıklama en fazla 2000 karakter olabilir.");
            }
            if (alanlar.All(x => x.karar == enumYerindeKontrolAlanKarar.Dogru))
                sonuc.HataEkle($"{ad}: Bilgiler doğru değilse en az bir alan için \"Değiştirilmeli\" veya \"Kaldırılmalı\" seçilmelidir.");
            // Yalnızca doğru olmayan alanlar saklanır; doğru alanlar Excel'de beyan değeriyle yazılır.
            m.alanlar = alanlar.Where(x => x.karar != enumYerindeKontrolAlanKarar.Dogru).ToList();
        }

        private static readonly int[] DilimOrtaMeridyenleri = [27, 30, 33, 36, 39, 42, 45];
        private const int DigerBulgularEnFazla = 20000;

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
            // 3. bölümde (diğer bulgular) metin daha uzun olabilir; 6. bölüm kendi mesajını verir.
            if (model.bolum is "1.1" or "1.2" && yorum.Length > 4000) sonuc.HataEkle("Yorum en fazla 4000 karakter olabilir.");
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
                        YerindeKontrolMakine kayitli = k.makineler.First(x => x.id == m.id);
                        string ad = $"{kayitli.siraNo}. {kayitli.makineAdi}";
                        m.fotografNo = M(m.fotografNo);
                        if (m.fotografNo.Length > 100) sonuc.HataEkle($"{ad}: Fotoğraf no en fazla 100 karakter olabilir.");
                        if (m.bilgilerDogruMu != false) { m.alanlar = []; continue; }
                        MakineAlanlariniDogrula(m, kayitli, mevcut.nesne.basvuru, ad, sonuc);
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

                case "3":
                    if (yorum.Length > DigerBulgularEnFazla) sonuc.HataEkle($"Diğer bulgular en fazla {DigerBulgularEnFazla} karakter olabilir.");
                    break;

                case "4":
                    model.belgeler = (model.belgeler ?? []).Where(x => k.belgeler.Any(b => b.id == x.id)).ToList();
                    foreach (YerindeKontrolBelge d in model.belgeler)
                    {
                        string ad = $"{k.belgeler.First(x => x.id == d.id).siraNo}. doküman";
                        d.belgeNo = M(d.belgeNo); d.konu = M(d.konu);
                        if (d.belgeNo.Length > 150) sonuc.HataEkle($"{ad}: Belge numarası en fazla 150 karakter olabilir.");
                        if (d.konu.Length > 1000) sonuc.HataEkle($"{ad}: Belgenin konusu en fazla 1000 karakter olabilir.");
                        if (d.verilisTarihi.HasValue && d.gecerlilikTarihi.HasValue && d.gecerlilikTarihi < d.verilisTarihi)
                            sonuc.HataEkle($"{ad}: Geçerlilik tarihi veriliş tarihinden önce olamaz.");
                    }
                    break;

                case "5":
                    model.fotograflar = (model.fotograflar ?? []).Where(x => k.fotograflar.Any(f => f.id == x.id)).ToList();
                    foreach (YerindeKontrolFotograf f in model.fotograflar)
                    {
                        string ad = $"\"{k.fotograflar.First(x => x.id == f.id).dosyaAdi}\"";
                        f.kayitNo = M(f.kayitNo); f.neyeAit = M(f.neyeAit); f.notlar = M(f.notlar);
                        if (f.kayitNo.Length == 0) sonuc.HataEkle($"{ad}: Fotoğraf / video kayıt no girilmelidir.");
                        if (f.kayitNo.Length > 50) sonuc.HataEkle($"{ad}: Kayıt no en fazla 50 karakter olabilir.");
                        if (f.neyeAit.Length > 1000 || f.notlar.Length > 2000) sonuc.HataEkle($"{ad}: Neye ait olduğu en fazla 1000, notlar en fazla 2000 karakter olabilir.");
                    }
                    // Diğer tablolardaki "Fotoğraf No" alanları kayıt numarasına atıf yaptığı için tekrar edemez.
                    foreach (string no in model.fotograflar.Where(x => x.kayitNo.Length > 0).GroupBy(x => x.kayitNo, StringComparer.OrdinalIgnoreCase).Where(x => x.Count() > 1).Select(x => x.Key))
                        sonuc.HataEkle($"\"{no}\" kayıt numarası birden fazla dosyada kullanılmış.");
                    break;

                case "6":
                    if (yorum.Length > 4000) sonuc.HataEkle("Çalışma dokümanı en fazla 4000 karakter olabilir.");
                    if (model.calismaKagidiSayisi is < 0 or > 1000) sonuc.HataEkle("Çalışma kağıdı sayısı 0 ile 1000 arasında olmalıdır.");
                    break;

                default:
                    sonuc.HataEkle("Bu bölümde kaydedilecek alan bulunmuyor.");
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
                        case "3": await tablo.DigerBulgularGuncelleAsync(k.id, yorum); break;
                        case "4": await tablo.BelgelerGuncelleAsync(k.id, model.belgeler); break;
                        case "5": await tablo.FotograflarGuncelleAsync(k.id, model.fotograflar); break;
                        case "6": await tablo.CalismaDokumaniGuncelleAsync(k.id, model.calismaKagidiSayisi, yorum); break;
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

        // ---- Kısım 4 (ek dokümanlar) ve Kısım 5 (fotoğraf / video) dosyaları
        // Dosyalar DosyaBilgisi tablosunda ModulKod=YerindeKontrol, FormAd=tür, FormAnahtar=kontrol Id, DosyaNo=satır Id anahtarıyla tutulur.
        private const string DosyaModulKod = "YerindeKontrol";
        public const long BelgeEnBuyuk = 50L * 1024 * 1024;
        public const long ResimEnBuyuk = 20L * 1024 * 1024;
        public const long VideoEnBuyuk = 200L * 1024 * 1024;

        private static DosyaAnahtari DosyaAnahtariOlustur(string tur, int kontrolId, int satirId) =>
            new() { ModulKod = DosyaModulKod, FormAd = tur, FormAnahtar = kontrolId.ToString(), DosyaNo = satirId };

        private static bool TurGecerliMi(string? tur) => tur is YerindeKontrolDosyaTurleri.Belge or YerindeKontrolDosyaTurleri.Fotograf;

        /// <summary>Uzantı ve dosyanın ilk baytları birlikte kontrol edilir; uzantısı değiştirilmiş dosyalar kabul edilmez.</summary>
        private static string? DosyaHatasi(string tur, string dosyaAdi, byte[] icerik)
        {
            string uzanti = YerindeKontrolDosyaTurleri.Uzanti(dosyaAdi);
            bool Basliyor(params byte[] imza) => icerik.Length >= imza.Length && icerik.AsSpan(0, imza.Length).SequenceEqual(imza);
            bool Ascii(int konum, string metin) => icerik.Length >= konum + metin.Length && System.Text.Encoding.ASCII.GetString(icerik, konum, metin.Length) == metin;
            const string mb = " MB";

            if (tur == YerindeKontrolDosyaTurleri.Belge)
            {
                if (!YerindeKontrolDosyaTurleri.BelgeUzantilari.Contains(uzanti)) return "Ek doküman olarak yalnızca PDF dosyası yüklenebilir.";
                if (icerik.LongLength > BelgeEnBuyuk) return $"PDF dosyası en fazla {BelgeEnBuyuk / 1024 / 1024}{mb} olabilir.";
                return Ascii(0, "%PDF") ? null : "Dosya geçerli bir PDF değil.";
            }
            if (YerindeKontrolDosyaTurleri.ResimUzantilari.Contains(uzanti))
            {
                if (icerik.LongLength > ResimEnBuyuk) return $"Fotoğraf en fazla {ResimEnBuyuk / 1024 / 1024}{mb} olabilir.";
                bool gecerli = uzanti == ".png" ? Basliyor(0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A) : Basliyor(0xFF, 0xD8, 0xFF);
                return gecerli ? null : "Dosya geçerli bir JPEG / PNG fotoğrafı değil.";
            }
            if (YerindeKontrolDosyaTurleri.VideoUzantilari.Contains(uzanti))
            {
                if (icerik.LongLength > VideoEnBuyuk) return $"Video en fazla {VideoEnBuyuk / 1024 / 1024}{mb} olabilir.";
                bool gecerli = uzanti switch
                {
                    ".mp4" or ".mov" => Ascii(4, "ftyp") || Ascii(4, "moov") || Ascii(4, "mdat") || Ascii(4, "wide") || Ascii(4, "free"),
                    ".webm" => Basliyor(0x1A, 0x45, 0xDF, 0xA3),
                    ".avi" => Ascii(0, "RIFF") && Ascii(8, "AVI "),
                    _ => false
                };
                return gecerli ? null : "Dosya geçerli bir video dosyası değil.";
            }
            return "Yalnızca JPEG, PNG fotoğraf veya MP4, MOV, WEBM, AVI video yüklenebilir.";
        }

        /// <summary>Dosyayı yükler ve satırını açar; satırın Excel bilgileri (tarih, konu, neye ait...) sonra bölüm kaydıyla girilir.</summary>
        public async Task<Sonuc<object>> DosyaYukleAsync(int kontrolId, string tur, string dosyaAdi, byte[] icerik, Kullanici? kullanici)
        {
            Sonuc<object> sonuc = new();
            dosyaAdi = Path.GetFileName((dosyaAdi ?? "").Trim());
            if (!TurGecerliMi(tur)) { sonuc.HataEkle("Dosya türü geçersiz."); return sonuc; }
            if (dosyaAdi.Length == 0 || icerik == null || icerik.Length == 0) { sonuc.HataEkle("Yüklenecek dosya seçilmelidir."); return sonuc; }
            if (dosyaAdi.Length > 250) { sonuc.HataEkle("Dosya adı en fazla 250 karakter olabilir."); return sonuc; }
            string? hata = DosyaHatasi(tur, dosyaAdi, icerik);
            if (hata != null) { sonuc.HataEkle($"{dosyaAdi}: {hata}"); return sonuc; }

            Sonuc<YerindeKontrolFormModel> mevcut = await OkuAsync(kontrolId, kullanici);
            if (!mevcut.basarili || mevcut.nesne == null) { foreach (string h in mevcut.hatalar) sonuc.HataEkle(h); return sonuc; }
            if (!mevcut.nesne.duzenlenebilir) { sonuc.HataEkle("Tamamlanmış yerinde kontrol değiştirilemez."); return sonuc; }
            try
            {
                int satirId;
                await using (SqlConnection connection = new(_connectionString))
                {
                    await connection.OpenAsync();
                    await using SqlTransaction transaction = (SqlTransaction)await connection.BeginTransactionAsync();
                    try
                    {
                        TABYerindeKontrol tablo = new(connection, _localizer, transaction);
                        satirId = tur == YerindeKontrolDosyaTurleri.Belge ? await tablo.BelgeEkleAsync(kontrolId) : await tablo.FotografEkleAsync(kontrolId);
                        if (satirId <= 0) { await transaction.RollbackAsync(); sonuc.HataEkle("Tamamlanmış yerinde kontrol değiştirilemez."); return sonuc; }

                        DosyaAnahtari anahtar = DosyaAnahtariOlustur(tur, kontrolId, satirId);
                        Sonuc<DosyaBilgisi> kayit = await _dosya.DosyaEkleVeyaGuncelleAsync(new DosyaKaydetModel
                        {
                            ModulKod = anahtar.ModulKod, FormAd = anahtar.FormAd, FormAnahtar = anahtar.FormAnahtar, DosyaNo = anahtar.DosyaNo,
                            DosyaAdi = dosyaAdi, Icerik = icerik,
                            Aciklama = tur == YerindeKontrolDosyaTurleri.Belge ? "Yerinde kontrol ek dokümanı" : "Yerinde kontrol fotoğraf / video kaydı"
                        }, new DosyaYetki(kontrolId));
                        if (!kayit.basarili || kayit.nesne == null)
                        {
                            await transaction.RollbackAsync();
                            foreach (string h in kayit.hatalar) sonuc.HataEkle(h);
                            return sonuc;
                        }
                        await tablo.DosyaBilgisiYazAsync(tur, kontrolId, satirId, kayit.nesne.Id, kayit.nesne.DosyaAdi);
                        await new TABBasvuruLog(connection, _localizer, transaction).EkleAsync(mevcut.nesne.kontrol.basvuruId, kullanici!, "YerindeKontrolDosyaYuklendi",
                            new { YerindeKontrolId = kontrolId, Tur = tur, SatirId = satirId, DosyaId = kayit.nesne.Id, kayit.nesne.DosyaAdi });
                        await transaction.CommitAsync();
                    }
                    catch
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                }

                YerindeKontrol? k = (await OkuAsync(kontrolId, kullanici)).nesne?.kontrol;
                sonuc.nesne = tur == YerindeKontrolDosyaTurleri.Belge ? k?.belgeler.FirstOrDefault(x => x.id == satirId) : k?.fotograflar.FirstOrDefault(x => x.id == satirId);
                sonuc.mesaj = $"{dosyaAdi} yüklendi.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Yerinde kontrol dosyası yüklenemedi. Id: {Id}, Tur: {Tur}", kontrolId, tur);
                sonuc.HataEkle("Dosya yüklenemedi.");
            }
            return sonuc;
        }

        public async Task<Sonuc> DosyaSilAsync(YerindeKontrolDosyaSil model, Kullanici? kullanici)
        {
            Sonuc sonuc = new();
            if (!TurGecerliMi(model.tur)) { sonuc.HataEkle("Dosya türü geçersiz."); return sonuc; }
            Sonuc<YerindeKontrolFormModel> mevcut = await OkuAsync(model.id, kullanici);
            if (!mevcut.basarili || mevcut.nesne == null) { foreach (string h in mevcut.hatalar) sonuc.HataEkle(h); return sonuc; }
            if (!mevcut.nesne.duzenlenebilir) { sonuc.HataEkle("Tamamlanmış yerinde kontrol değiştirilemez."); return sonuc; }
            YerindeKontrol k = mevcut.nesne.kontrol;
            string? dosyaAdi = model.tur == YerindeKontrolDosyaTurleri.Belge ? k.belgeler.FirstOrDefault(x => x.id == model.satirId)?.dosyaAdi : k.fotograflar.FirstOrDefault(x => x.id == model.satirId)?.dosyaAdi;
            if (dosyaAdi == null) { sonuc.HataEkle("Dosya bulunamadı."); return sonuc; }
            try
            {
                await using (SqlConnection connection = new(_connectionString))
                {
                    await connection.OpenAsync();
                    await using SqlTransaction transaction = (SqlTransaction)await connection.BeginTransactionAsync();
                    try
                    {
                        if (await new TABYerindeKontrol(connection, _localizer, transaction).DosyaliSatirSilAsync(model.tur, model.id, model.satirId) != 1)
                        {
                            await transaction.RollbackAsync();
                            sonuc.HataEkle("Dosya silinemedi.");
                            return sonuc;
                        }
                        await new TABBasvuruLog(connection, _localizer, transaction).EkleAsync(k.basvuruId, kullanici!, "YerindeKontrolDosyaSilindi",
                            new { YerindeKontrolId = model.id, Tur = model.tur, model.satirId, DosyaAdi = dosyaAdi });
                        await transaction.CommitAsync();
                    }
                    catch
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                }
                // Satır silindikten sonra dosya içeriği de silinir; başarısız olursa yalnızca loglanır (satır olmadan dosyaya erişilemez).
                Sonuc dosyaSil = await _dosya.DosyaSilAsync(DosyaAnahtariOlustur(model.tur, model.id, model.satirId), new DosyaYetki(model.id));
                if (!dosyaSil.basarili) _logger.LogWarning("Yerinde kontrol dosya içeriği silinemedi. Id: {Id}, Tur: {Tur}, SatirId: {SatirId}: {Hata}", model.id, model.tur, model.satirId, dosyaSil.hataStr);
                sonuc.mesaj = $"{dosyaAdi} silindi.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Yerinde kontrol dosyası silinemedi. Id: {Id}, Tur: {Tur}, SatirId: {SatirId}", model.id, model.tur, model.satirId);
                sonuc.HataEkle("Dosya silinemedi.");
            }
            return sonuc;
        }

        /// <summary>Görüntüleme / indirme: kontrole erişebilen uzman, kontrol tamamlanmış olsa da dosyayı açabilir.</summary>
        public async Task<Sonuc<Dosya>> DosyaGetirAsync(int kontrolId, string tur, int satirId, Kullanici? kullanici)
        {
            Sonuc<Dosya> sonuc = new();
            if (!TurGecerliMi(tur)) { sonuc.HataEkle("Dosya türü geçersiz."); return sonuc; }
            Sonuc<YerindeKontrolFormModel> mevcut = await OkuAsync(kontrolId, kullanici);
            if (!mevcut.basarili || mevcut.nesne == null) { foreach (string h in mevcut.hatalar) sonuc.HataEkle(h); return sonuc; }
            YerindeKontrol k = mevcut.nesne.kontrol;
            int? dosyaId = tur == YerindeKontrolDosyaTurleri.Belge ? k.belgeler.FirstOrDefault(x => x.id == satirId)?.dosyaId : k.fotograflar.FirstOrDefault(x => x.id == satirId)?.dosyaId;
            if (dosyaId == null) { sonuc.HataEkle("Dosya bulunamadı."); return sonuc; }
            Sonuc<Dosya> dosya = await _dosya.DosyaGetirAsync(DosyaAnahtariOlustur(tur, kontrolId, satirId), new DosyaYetki(kontrolId));
            if (!dosya.basarili || dosya.nesne == null || dosya.nesne.Id != dosyaId) { sonuc.HataEkle("Dosya bulunamadı."); return sonuc; }
            sonuc.nesne = dosya.nesne;
            return sonuc;
        }

        /// <summary>Yetki iş kuralında (OkuAsync) kontrol edilir; burada yalnızca anahtarın bu kontrole ait olduğu doğrulanır.</summary>
        private sealed class DosyaYetki(int kontrolId) : IDosyaYetkiKontrol
        {
            private bool Uygun(string modulKod, string? formAd, string? formAnahtar) =>
                modulKod == DosyaModulKod && TurGecerliMi(formAd) && formAnahtar == kontrolId.ToString();

            public Task<bool> GorebilirAsync(string modulKod, string? formAd, string? formAnahtar, int? dosyaNo) => Task.FromResult(Uygun(modulKod, formAd, formAnahtar));
            public Task<bool> EkleyebilirAsync(string modulKod, string formAd, string formAnahtar) => Task.FromResult(Uygun(modulKod, formAd, formAnahtar));
            public Task<bool> GuncelleyebilirAsync(string modulKod, string formAd, string formAnahtar, int dosyaNo) => Task.FromResult(Uygun(modulKod, formAd, formAnahtar));
            public Task<bool> SilebilirAsync(string modulKod, string formAd, string formAnahtar, int dosyaNo) => Task.FromResult(Uygun(modulKod, formAd, formAnahtar));
        }
    }
}
