using System.Text.Json;
using System.Text.Json.Nodes;
using TarimDonusum.IsKurallari;
using TarimDonusum.Models;

namespace TarimDonusum.TestVeri;

/// <summary>
/// TEST: Ön başvurudan oluşan başvurunun yalnızca başvuruda girilen bölümlerini rastgele verilerle doldurur ve kaydeder
/// (bilanço, mali belgeler, yatırım/değer zinciri/finansman ayrıntıları, uygun harcama, teknik özellik ve teklifler,
/// ürün süreçleri, bölüm/mahal, metraj, SGK/tedarik belgeleri, zorunlu belgeler, izleme göstergeleri, taahhüt/beyan).
/// Kayıtlar ekranların kullandığı iş kuralı metotlarıyla yapılır. TestVeri klasörü ile birlikte silinebilir.
/// </summary>
public sealed class BasvuruTestVeriUretici(BasvuruIsKurallari isKurallari, Kullanici kullanici)
{
    private const string MaliBelgeFormAd = "Basvuru_MaliBelge";
    private const string ZorunluBelgeMerkeziFormAd = "Basvuru_ZorunluBelgeMerkezi";
    private const string IstihdamSgkFormAd = "Basvuru_IstihdamSgk";
    private const string YeniDestekIsteniyor = "YENİ DESTEK İSTENİYOR";
    // Başvurunun Zorunlu Belgeler sayfasında ilgili sayfalardan yüklenen (merkezi yüklenmeyen) belge numaraları.
    private static readonly HashSet<int> SayfadanYuklenenBelgeler = [1, 2, 7, 8, 9, 10, 11, 14, 15, 16, 19, 23, 25, 26, 48];

    private readonly TestVeriKaynaklari _r = new();
    private int _basvuruId;
    public List<string> Notlar { get; } = [];

    private static T Al<T>(Sonuc<T> sonuc, string adim) => sonuc.basarili ? sonuc.nesne : throw new TestVeriHatasi(adim, sonuc.hatalar);
    private static void Kontrol(Sonuc sonuc, string adim) { if (!sonuc.basarili) throw new TestVeriHatasi(adim, sonuc.hatalar); }
    private async Task<Basvuru> OkuAsync() => Al(await isKurallari.OkuAsync(_basvuruId, kullanici), "Başvuru okunamadı");
    private async Task<BasvuruDosyaYuklemeSonucu> DosyaYukleAsync(string formAd, int dosyaNo, string baslik) =>
        Al(await isKurallari.BasvuruDosyasiKaydetAsync(_basvuruId, formAd, dosyaNo, TestVeriKaynaklari.DosyaAdi(baslik), TestVeriKaynaklari.Pdf(baslik), kullanici), $"{baslik} yüklenemedi");

    public async Task<string> DoldurAsync(int basvuruId)
    {
        _basvuruId = basvuruId;
        Basvuru b = await OkuAsync();
        if (b.kayitTuru != enumBasvuruKayitTuru.Basvuru || b.durum != enumBasvuruDurum.BasvuruDurumu)
            throw new TestVeriHatasi("Başvuru", ["Yalnızca düzenlenebilir durumdaki başvuru doldurulabilir."]);
        TestVeriKaynaklari.Sektor sektor = TestVeriKaynaklari.SektorBul(b.yatirim.degerZinciriAsamalari.FirstOrDefault()?.dz.ad ?? b.yatirim.yatirimAdi ?? "");

        await BilancoVeMaliKaydetAsync(b);
        await YatirimVeDegerZinciriKaydetAsync(b, sektor);
        await FinansmanKaydetAsync(await OkuAsync());
        await EnerjiKaydetAsync(b);
        await MakinelerKaydetAsync(await OkuAsync());
        await TeknikOzellikVeTeklifKaydetAsync(await OkuAsync());
        await UrunSurecleriKaydetAsync(await OkuAsync(), sektor);
        await BinaMahalVeMetrajKaydetAsync(await OkuAsync());
        b = await OkuAsync();
        await DosyaYukleAsync(IstihdamSgkFormAd, 1, "SGK hizmet listesi");
        foreach (BasvuruTedarikciEntegrasyonu tedarik in b.TedarikciEntegrasyonlari)
            await DosyaYukleAsync($"BTED_{tedarik.id}", 1, $"Tedarik sozlesmesi {tedarik.tarimsalUrun}");
        await ZorunluBelgeleriYukleAsync(b);
        await IzlemeGostergeleriKaydetAsync(b);
        await TaahhutKaydetAsync(await OkuAsync());
        return string.Join(" ", Notlar.Prepend("Başvurunun başvuruya özel bölümleri test verileriyle dolduruldu ve kaydedildi."));
    }

    private async Task BilancoVeMaliKaydetAsync(Basvuru b)
    {
        decimal sonSatis = b.mali.sonYilNetSatis ?? _r.Tutar(50_000_000, 500_000_000, 1000);
        decimal oncekiSatis = b.mali.oncekiYilNetSatis ?? Math.Round(sonSatis * 0.9m);
        decimal[] satis = [sonSatis, oncekiSatis, Math.Round(oncekiSatis * _r.Sayi(80, 98) / 100m)];
        decimal[] aktif = [b.mali.sonYilAktifToplami ?? sonSatis, b.mali.oncekiYilAktifToplami ?? oncekiSatis, 0];
        aktif[2] = Math.Round(aktif[1] * _r.Sayi(80, 98) / 100m);
        decimal[] ihracat = [b.mali.sonYilIhracatSatis ?? 0, b.mali.oncekiYilIhracatSatis ?? 0, Math.Round((b.mali.oncekiYilIhracatSatis ?? 0) * _r.Sayi(70, 100) / 100m)];

        Dictionary<string, decimal?[]> satirlar = BilancoGelirTanimlari.GirisSatirlari.ToDictionary(x => x.Kod, _ => new decimal?[3]);
        for (int i = 0; i < 3; i++)
        {
            if (!FirmaGecmisYilKurallari.Girilebilir(b, i + 1)) continue;
            decimal Oran(decimal deger, int min, int max) => Math.Round(deger * _r.Sayi(min, max) / 100m / 1000) * 1000;
            decimal indirim = Oran(satis[i], 1, 3), donen = Oran(aktif[i], 42, 60), kvyk = Oran(aktif[i], 18, 28), uvyk = Oran(aktif[i], 8, 20);
            decimal smm = Oran(satis[i], 60, 70), faaliyet = Oran(satis[i], 8, 12), finansman = Oran(satis[i], 1, 3);
            decimal kar = satis[i] - smm - faaliyet - finansman;
            Dictionary<string, decimal> d = new()
            {
                ["DONEN_VARLIKLAR"] = donen, ["TICARI_ALACAKLAR"] = Oran(donen, 25, 45), ["STOKLAR"] = Oran(donen, 15, 35), ["DURAN_VARLIKLAR"] = aktif[i] - donen,
                ["KISA_VADELI_YABANCI_KAYNAKLAR"] = kvyk, ["UZUN_VADELI_YABANCI_KAYNAKLAR"] = uvyk, ["OZ_KAYNAKLAR"] = aktif[i] - kvyk - uvyk,
                ["BRUT_SATISLAR"] = satis[i] + indirim, ["SATIS_INDIRIMLERI"] = indirim, ["IHRACAT_SATISLARI"] = Math.Min(ihracat[i], satis[i]),
                ["SATISLARIN_MALIYETI"] = smm, ["FAALIYET_GIDERLERI"] = faaliyet, ["DIGER_OLAGAN_GELIR_KARLAR"] = Oran(satis[i], 0, 2),
                ["DIGER_OLAGAN_GIDER_ZARARLAR"] = Oran(satis[i], 0, 1), ["FINANSMAN_GIDERLERI"] = finansman, ["OLAGANDISI_GELIR_KARLAR"] = 0,
                ["OLAGANDISI_GIDER_ZARARLAR"] = 0, ["VERGI_YASAL_YUKUMLULUK"] = Math.Max(0, Math.Round(kar * 0.25m / 1000) * 1000), ["DONEM_AMORTISMAN"] = Oran(aktif[i] - donen, 4, 10)
            };
            foreach ((string kod, decimal deger) in d) satirlar[kod][i] = deger;
        }
        Kontrol(await isKurallari.KaydetBilancoGelirAsync(new BasvuruBilancoGelir
        {
            basvuruId = _basvuruId,
            satirlar = satirlar.Select(x => new BasvuruBilancoGelirSatiri { kod = x.Key, yil_1 = x.Value[0], yil_2 = x.Value[1], yil_3 = x.Value[2] }).ToList()
        }, kullanici), "Bilanço ve gelir tablosu kaydedilemedi");

        // Mali veri belgeleri: tek numaralar önceki yıl (dönem yılı - 2), çift numaralar son yıl (dönem yılı - 1).
        JsonObject referanslar = new();
        (string anahtar, int no, string baslik)[] belgeler =
        [
            ("netSatis.onceki", 1, "Gelir tablosu onceki yil"), ("netSatis.son", 2, "Gelir tablosu son yil"),
            ("aktif.onceki", 3, "Bilanco onceki yil"), ("aktif.son", 4, "Bilanco son yil"),
            ("ihracat.onceki", 5, "Ihracat belgesi onceki yil"), ("ihracat.son", 6, "Ihracat belgesi son yil")
        ];
        foreach ((string anahtar, int no, string baslik) in belgeler)
        {
            if (!FirmaGecmisYilKurallari.Girilebilir(b, no % 2 == 1 ? 2 : 1)) continue;
            BasvuruDosyaYuklemeSonucu dosya = await DosyaYukleAsync(MaliBelgeFormAd, no, baslik);
            referanslar[anahtar] = new JsonObject { ["dosyaId"] = dosya.DosyaId, ["dosyaAdi"] = dosya.DosyaAdi };
        }
        Basvuru guncel = await OkuAsync();
        BasvuruMali mali = guncel.mali;
        mali.basvuruId = _basvuruId;
        mali.aciklama = "Test verisi: mali veriler bağımsız denetim raporu, kurumlar vergisi beyannamesi ve bilanço ile uyumludur.";
        mali.belgeReferanslariJson = referanslar.ToJsonString();
        mali.bagimsizDenetimeTabiMi ??= false;
        Kontrol(await isKurallari.KaydetMaliAsync(mali, kullanici), "Mali veriler kaydedilemedi");
    }

    private async Task YatirimVeDegerZinciriKaydetAsync(Basvuru b, TestVeriKaynaklari.Sektor sektor)
    {
        BasvuruYatirim y = b.yatirim;
        y.basvuruId = _basvuruId;
        y.planlananBaslangicTarihi ??= DateTime.Today.AddMonths(_r.Sayi(2, 6));
        y.planlananTamamlanmaTarihi ??= y.planlananBaslangicTarihi.Value.AddMonths(_r.Sayi(8, 18));
        Kontrol(await isKurallari.KaydetYatirimBilgileriAsync(y, kullanici), "Yatırım bilgileri kaydedilemedi");

        string[] teyit = ["Evet", "Evet", "Hayır", "Kurum teyidi bekleniyor"];
        BasvuruYatirim zincir = new()
        {
            basvuruId = _basvuruId, uygulamaAdresiId = null, degerZinciriAsamalari = b.yatirim.degerZinciriAsamalari,
            ilDegerZinciriEslesmesi = "Evet",
            tarimGidaBaglantiTuru = _r.Sec(["Yukarı yönlü tedarik bağlantısı", "Tarımsal girdi kullanan ürün", "Gıda odaklı çıktı", "Birden fazla bağlantı"]),
            tarimGidaBaglantiAciklamasi = $"Yatırım, {string.Join(", ", sektor.Girdiler.Take(2))} gibi tarımsal girdileri doğrudan kullanmaktadır.",
            yatirimAlaniTipolojisi = $"{sektor.Tesis}; {sektor.Faaliyet}",
            degerZinciriUygunlukAciklamasi = "Yatırım, il için tanımlı değer zinciri kapsamındaki aşamalarda yer almaktadır.",
            oncelikliYatirimUyumu = _r.Sec(teyit), oncelikliYatirimKonuKodu = $"ODY-{_r.Sayi(1, 40):00}",
            ithalatBagimliligiUyumu = _r.Sec(teyit), ithalatBagimliligiUrunKodu = $"GTİP {_r.Sayi(1000, 2400)}.{_r.Sayi(10, 99)}",
            hedefUrunlerPazarCiktisi = $"{string.Join(", ", sektor.Urunler.Take(3))} iç pazar ve ihracat pazarlarına sunulacaktır.",
            rekabetcilikAciklamasi = "Yeni teknoloji ile birim maliyet düşürülecek, ürün kalitesi ve raf ömrü artırılacaktır."
        };
        Kontrol(await isKurallari.KaydetDegerZinciriAsync(zincir, kullanici), "Değer zinciri bilgileri kaydedilemedi");
    }

    private async Task FinansmanKaydetAsync(Basvuru b)
    {
        BasvuruFinans f = b.finans;
        f.basvuruId = _basvuruId;
        // Başvuru kuru ön başvurudan farklı olabildiği için oran azami destek tutarını aşmayacak şekilde düşürülür.
        decimal azamiDestek = b.basvuruFirma.donem.maksimumDestekTutari.GetValueOrDefault();
        decimal toplamEur = b.HesaplananToplamYatirimTutariEur.GetValueOrDefault();
        if (azamiDestek > 0 && toplamEur > 0)
            f.talepEdilenFinansmanOrani = Math.Min(f.talepEdilenFinansmanOrani ?? 50, Math.Floor(azamiDestek * 0.98m / toplamEur * 100));
        f.finansmanParaBirimi = _r.Sec(["EUR", "EUR", "TL"]);
        f.digerFinansmanKaynaklari = _r.Sec(["Özkaynak", "Özkaynak ve banka kredisi", "Ortak sermaye artırımı"]);
        f.digerFinansmanKaynaklariAciklama = "Kalan tutar şirket özkaynakları ve ticari banka kredisiyle karşılanacaktır.";
        f.oncekiRffSozlesmesiKapaliMi = _r.Sec(["Evet", "Hayır", "Uygulanamaz"]);
        f.bankaTeminatMektubuSaglanabilirMi = _r.Olasilik(0.8) ? "Evet" : "Hayır";
        f.detayliFinansmanKaydi = true;
        Kontrol(await isKurallari.KaydetFinansAsync(f, kullanici), "Finansman bilgileri kaydedilemedi");
    }

    private async Task EnerjiKaydetAsync(Basvuru b)
    {
        // Teknik Proje (Girdiler): yenilenebilir enerji (güneş paneli ve kurulu güç) kayıtları.
        if (b.YatirimOnBilgileri.Any(x => x.tur is enumYatirimOnBilgiTuru.EnerjiKullanimi or enumYatirimOnBilgiTuru.KuruluGuc) || !_r.Olasilik(0.6)) return;
        int panel = _r.Sayi(40, 1200), guc = _r.Sec([450, 500, 550, 600]);
        Kontrol(await isKurallari.BasvuruYatirimOnBilgisiKaydetAsync(new BasvuruYatirimOnBilgi
        {
            basvuruId = _basvuruId, tur = enumYatirimOnBilgiTuru.EnerjiKullanimi, siraNo = 1, ad = "Güneş Enerjisi", miktar = panel, birim = "Adet",
            tekPanelGucu = guc, tekPanelGucuBirim = "W", toplamGuc = Math.Round(panel * guc / 1000m, 2), toplamGucBirim = "kWp"
        }, kullanici), "Güneş enerjisi bilgisi kaydedilemedi");
        if (_r.Olasilik(0.3))
            Kontrol(await isKurallari.BasvuruYatirimOnBilgisiKaydetAsync(new BasvuruYatirimOnBilgi
            {
                basvuruId = _basvuruId, tur = enumYatirimOnBilgiTuru.KuruluGuc, siraNo = 2, ad = "Biyogaz", miktar = _r.Sayi(50, 1000), birim = "kWe"
            }, kullanici), "Kurulu güç bilgisi kaydedilemedi");
    }

    private async Task MakinelerKaydetAsync(Basvuru b)
    {
        // Teknik Proje (Makine Ekipman): her makineye Tanımlar > Uygun Harcama Listesi'nden kalem seçilir.
        IReadOnlyList<UygunHarcamaTanim> aktifler = UygunHarcamaTanimSaglayici.Aktifler;
        if (aktifler.Count == 0) { Notlar.Add("Uygun Harcama Listesi'nde aktif kalem olmadığı için makinelere uygun harcama seçilmedi."); return; }
        foreach (BasvuruMakine m in b.Makineler)
        {
            UygunHarcamaTanim? ayniAd = aktifler.FirstOrDefault(x => string.Equals(x.ad, m.ad, StringComparison.CurrentCultureIgnoreCase));
            m.uygunHarcamaId = UygunHarcamaTanimSaglayici.Bul(m.uygunHarcamaId)?.aktif == true ? m.uygunHarcamaId : (ayniAd ?? _r.Sec(aktifler)).id;
            m.basvuruId = _basvuruId;
            m.teklifKontroluYapilsin = false;
            Kontrol(await isKurallari.BasvuruMakinesiKaydetAsync(m, kullanici), $"Makine ({m.ad}) kaydedilemedi");
        }
    }

    private async Task TeknikOzellikVeTeklifKaydetAsync(Basvuru b)
    {
        // Yeni Makine Ekipman Listesi: yeni destek istenen makinelere teknik özellik, teklif ve teklif belgesi.
        decimal kur = b.basvuruFirma.donem.basvuruCevrimKuru.GetValueOrDefault(50);
        string[] tedarikciler = ["Anadolu Makine San. A.Ş.", "Ege Tarım Teknolojileri Ltd. Şti.", "Marmara Endüstri A.Ş.", "Konya Gıda Makineleri Ltd. Şti.", "Karadeniz Mühendislik A.Ş."];
        (string baslik, string aciklama)[] ozellikler =
        [
            ("Kapasite", $"En az {_r.Sayi(1, 50) * 100} kg/saat"), ("Motor gücü", $"{_r.Sayi(2, 90)} kW, 380 V"), ("Malzeme", "Gıdayla temas eden yüzeyler AISI 304 paslanmaz çelik"),
            ("Kontrol sistemi", "PLC kontrollü, dokunmatik operatör paneli"), ("Garanti", "En az 2 yıl üretici garantisi ve yedek parça temini"), ("Sertifika", "CE belgeli")
        ];
        List<BasvuruMakine> destekli = b.Makineler.Where(x => x.durum == YeniDestekIsteniyor).ToList();
        decimal a2 = TestFinansProfili.ButceKalemi(b.yatirimOzeti.yatirimOzetiJson, "A2");
        if (a2 <= 0) a2 = destekli.Count * 1_500_000m;
        List<decimal> makinePaylari = TestFinansProfili.Bol(_r, a2, destekli.Count, 10, 40);
        foreach (BasvuruMakine m in destekli)
        {
            decimal tlFiyat = Math.Round(makinePaylari[destekli.IndexOf(m)] / Math.Max(1, m.miktar));
            int sira = m.teknikOzellikler.Select(x => x.siraNo).DefaultIfEmpty(0).Max();
            foreach ((string baslik, string aciklama) in _r.AltKume(ozellikler, 2, 4).Where(x => m.teknikOzellikler.All(o => o.baslik != x.baslik)))
                Kontrol(await isKurallari.MakineOzellikKaydetAsync(new BasvuruMakineOzellikKayit
                {
                    basvuruId = _basvuruId,
                    ozellik = new BasvuruMakineOzellik { makineId = m.id, siraNo = ++sira, baslik = baslik, aciklamaAsgariGereklilik = aciklama, zorunluMu = _r.Olasilik(0.6) }
                }, kullanici), $"{m.ad} teknik özelliği kaydedilemedi");

            if (m.teklifler.Count > 0) continue;
            List<string> secilenTedarikciler = _r.AltKume(tedarikciler, 2, 3);
            for (int i = 0; i < secilenTedarikciler.Count; i++)
            {
                string para = _r.Sec(["TL", "TL", "EUR", "USD"]);
                decimal teklifKuru = para switch { "EUR" => kur, "USD" => Math.Round(kur * 0.92m, 4), _ => 1 };
                DateTime tarih = DateTime.Today.AddDays(-_r.Sayi(5, 60));
                BasvuruMakineAltKayitlari alt = Al(await isKurallari.MakineTeklifKaydetAsync(new BasvuruMakineTeklifKayit
                {
                    basvuruId = _basvuruId,
                    teklif = new BasvuruMakineTeklif
                    {
                        makineId = m.id, siraNo = i + 1, basvuruyaEsas = i == 0, tedarikci = secilenTedarikciler[i], marka = string.IsNullOrWhiteSpace(m.marka) ? _r.Sec(TestVeriKaynaklari.Markalar) : m.marka,
                        model = string.IsNullOrWhiteSpace(m.model) ? $"M-{_r.Sayi(100, 999)}" : m.model, paraBirimi = para, kur = teklifKuru,
                        birimFiyat = Math.Round(tlFiyat * (i == 0 ? 1 : 1 + _r.Sayi(3, 15) / 100m) / teklifKuru, 2), teklifTarihi = tarih, gecerlilikTarihi = tarih.AddDays(_r.Sayi(60, 120)),
                        aciklama = "Nakliye ve kurulum dahildir."
                    }
                }, kullanici), $"{m.ad} teklifi kaydedilemedi");
                BasvuruMakineTeklif? teklif = alt.teklifler.FirstOrDefault(x => x.siraNo == i + 1);
                if (teklif != null) await DosyaYukleAsync($"BMT_{teklif.id}", 1, $"Teklif {secilenTedarikciler[i]}");
            }
        }
    }

    private async Task UrunSurecleriKaydetAsync(Basvuru b, TestVeriKaynaklari.Sektor sektor)
    {
        // Ürün - Süreç - Makine: yeni üretilecek her ürün için süreçler ve süreçte kullanılan makineler.
        string[] surecAdlari = ["Hammadde kabul ve ön temizleme", "Yıkama ve ayıklama", "İşleme", "Isıl işlem", "Soğutma", "Paketleme", "Depolama ve sevkiyat"];
        List<BasvuruYatirimOnBilgi> urunler = b.YatirimOnBilgileri.Where(x => x.tur == enumYatirimOnBilgiTuru.UretilecekUrun).ToList();
        if (urunler.Count == 0 || b.Makineler.Count == 0 || b.UrunSurecleri.Count > 0) return;
        foreach (BasvuruYatirimOnBilgi urun in urunler)
        {
            List<string> adlar = _r.AltKume(surecAdlari, 1, 3);
            for (int i = 0; i < adlar.Count; i++)
                Kontrol(await isKurallari.BasvuruUrunSureciKaydetAsync(new BasvuruUrunSurec { basvuruId = _basvuruId, urunId = urun.id, siraNo = i + 1, surecAdi = adlar[i] }, kullanici), $"{urun.ad} süreci kaydedilemedi");
        }
        foreach (BasvuruUrunSurec surec in (await OkuAsync()).UrunSurecleri)
        {
            int sira = 0;
            foreach (BasvuruMakine m in _r.AltKume(b.Makineler, 1, 3))
                Kontrol(await isKurallari.BasvuruUrunSurecMakinesiKaydetAsync(new BasvuruUrunSurecMakine
                {
                    basvuruId = _basvuruId, surecId = surec.id, makineId = m.id, siraNo = ++sira, adet = _r.Sayi(1, (int)Math.Max(1, m.miktar)),
                    yerlesimPlaniNo = $"YP-{m.siraNo:00}", girdilerMiktarlar = $"{_r.Sec(sektor.Girdiler)}: {_r.Sayi(1, 40) * 100} kg/gün",
                    ciktilarMiktarlar = $"{_r.Sec(sektor.Urunler)}: {_r.Sayi(1, 35) * 100} kg/gün", islemeKapasitesi = $"{_r.Sayi(1, 50) * 50} kg/saat",
                    gunlukCalismaSuresi = _r.Sayi(4, 16), gunlukCalismaSuresiBirimi = "Saat", aciklama = $"{surec.surecAdi} sürecinde kullanılır."
                }, kullanici), $"{surec.surecAdi} süreç makinesi kaydedilemedi");
        }
    }

    private async Task BinaMahalVeMetrajKaydetAsync(Basvuru b)
    {
        if (b.Binalar.Count == 0 && b.YatirimAdresleri.Any(x => x.harcamaTurleri.Contains((int)enumHarcamaTuru.YapimIsleri)))
        {
            Kontrol(await isKurallari.BasvuruBinasiKaydetAsync(new BasvuruBina
            {
                basvuruId = _basvuruId, uygulamaAdresiId = _r.Sec(b.YatirimAdresleri).id, degerZinciriAsamaTuru = enumDegerZinciriAsamaTuru.IslemeIleriIsleme, siraNo = 1,
                ad = _r.Sec(TestVeriKaynaklari.Binalar), mevcutYeni = "Yeni", yatirimSekli = "Yeni yapım", destekTalebi = "Evet", vaziyetPlaniNo = "VP-01"
            }, kullanici), "Bina kaydedilemedi");
            b = await OkuAsync();
        }
        foreach (BasvuruBina bina in b.Binalar.Where(x => x.mahaller.Count == 0))
        {
            int sira = 0;
            foreach (string mahal in _r.AltKume(TestVeriKaynaklari.Mahaller, 2, 5))
                Kontrol(await isKurallari.BasvuruBinaMahaliKaydetAsync(new BasvuruBinaMahal { basvuruId = _basvuruId, binaId = bina.id, siraNo = ++sira, mahalAdi = mahal, alanM2 = _r.Sayi(15, 1500) }, kullanici),
                    $"{bina.ad} bölüm/mahal kaydedilemedi");
        }

        // Metraj: yalnızca yeni binalara, dönemin poz fiyatlarından seçilen pozlarla.
        BasvuruMetrajVerisi veri = Al(await isKurallari.MetrajOkuAsync(_basvuruId, kullanici), "Metraj okunamadı");
        List<PozDonemFiyat> pozlar = veri.pozlar.Where(x => x.birimFiyat > 0).ToList();
        if (pozlar.Count == 0) { Notlar.Add("Dönem için fiyatlı poz bulunmadığından metraj girilmedi."); return; }
        foreach (BasvuruBina bina in b.Binalar.Where(x => string.Equals(x.mevcutYeni, "Yeni", StringComparison.OrdinalIgnoreCase)))
        {
            if (veri.binalar.FirstOrDefault(x => x.id == bina.id)?.bolumler.Count > 0) continue;
            List<enumMetrajKategori> kategoriler = _r.AltKume(Enum.GetValues<enumMetrajKategori>(), 2, 4).OrderBy(x => x).ToList();
            for (int i = 0; i < kategoriler.Count; i++)
                Kontrol(await isKurallari.MetrajBolumKaydetAsync(new BasvuruMetrajBolum { basvuruId = _basvuruId, binaId = bina.id, siraNo = i + 1, kategori = kategoriler[i] }, kullanici), $"{bina.ad} yapım bölümü kaydedilemedi");
        }
        veri = Al(await isKurallari.MetrajOkuAsync(_basvuruId, kullanici), "Metraj okunamadı");
        List<BasvuruMetrajBolum> bosBolumler = veri.binalar.SelectMany(x => x.bolumler).Where(x => x.pozlar.Count == 0).ToList();
        decimal a1 = TestFinansProfili.ButceKalemi(b.yatirimOzeti.yatirimOzetiJson, "A1");
        if (a1 <= 0) a1 = 3_000_000m;
        List<decimal> bolumPaylari = TestFinansProfili.Bol(_r, a1, bosBolumler.Count);
        for (int i = 0; i < bosBolumler.Count; i++)
        {
            // Bölüm payına sığan (en az birkaç birim girilebilen) pozlar seçilir; her pozun miktarı payını karşılayacak kadardır.
            List<PozDonemFiyat> uygun = pozlar.Where(x => x.birimFiyat <= bolumPaylari[i] / 5).ToList();
            if (uygun.Count == 0) uygun = pozlar.OrderBy(x => x.birimFiyat).Take(20).ToList();
            List<PozDonemFiyat> secilen = _r.AltKume(uygun, 1, 3);
            List<decimal> pozPaylari = TestFinansProfili.Bol(_r, bolumPaylari[i], secilen.Count);
            for (int j = 0; j < secilen.Count; j++)
                Kontrol(await isKurallari.MetrajPozKaydetAsync(new BasvuruMetrajPoz
                {
                    basvuruId = _basvuruId, bolumId = bosBolumler[i].id, pozId = secilen[j].pozId, siraNo = j + 1,
                    detaylar = [MetrajDetayi(secilen[j], pozPaylari[j] / secilen[j].birimFiyat!.Value)]
                }, kullanici), $"{bosBolumler[i].ad} pozu kaydedilemedi");
        }
    }

    /// <summary>Hedef metraj miktarını veren tek detay satırı (ağırlık pozlarında hacim x birim ağırlık).</summary>
    private static BasvuruMetrajDetay MetrajDetayi(PozDonemFiyat poz, decimal hedefMiktar)
    {
        bool ton = poz.birim is "Ton" or "ton";
        bool agirlik = poz.hesaplamaTuru == (int)enumPozHesaplamaTuru.Agirlik || ton || poz.birim is "Kg" or "kg";
        BasvuruMetrajDetay d = new() { siraNo = 1, aciklama = "Proje metrajı", benzerSayisi = 1 };
        decimal hedef = agirlik ? hedefMiktar / (ton ? 7.85m : 7850m) : hedefMiktar;
        switch (agirlik ? enumPozHesaplamaTuru.Hacim : (enumPozHesaplamaTuru)poz.hesaplamaTuru)
        {
            case enumPozHesaplamaTuru.Adet: d.adet = Math.Max(1, Math.Round(hedef)); break;
            case enumPozHesaplamaTuru.Uzunluk: d.boy = Math.Max(0.01m, Math.Round(hedef, 2)); break;
            case enumPozHesaplamaTuru.Alan: d.en = 10; d.boy = Math.Max(0.01m, Math.Round(hedef / 10, 2)); break;
            default: d.en = 10; d.yukseklik = 1; d.boy = Math.Max(0.01m, Math.Round(hedef / 10, 3)); break;
        }
        if (agirlik) { d.urunCinsi = "Nervürlü beton çelik çubuğu"; d.birimAgirlik = ton ? 7.85m : 7850m; }
        return d;
    }

    private async Task ZorunluBelgeleriYukleAsync(Basvuru b)
    {
        HashSet<int> yuklenenler = b.TumBasvuruDosyalari.Where(x => x.FormAd == ZorunluBelgeMerkeziFormAd).Select(x => x.DosyaNo).ToHashSet();
        foreach (ZorunluBelgeTanimi belge in ZorunluBelgeTanimlari.BasvuruIcin(b.basvuruFirma.basvuruSahibiTuru)
                     .Where(x => !SayfadanYuklenenBelgeler.Contains(x.No) && !yuklenenler.Contains(x.No)))
            await DosyaYukleAsync(ZorunluBelgeMerkeziFormAd, belge.No, belge.Ad.Length > 60 ? belge.Ad[..60] : belge.Ad);
    }

    private async Task IzlemeGostergeleriKaydetAsync(Basvuru b)
    {
        DateTime baslangic = b.yatirim.planlananBaslangicTarihi ?? DateTime.Today;
        Kontrol(await isKurallari.BasvuruIzlemeUstBilgiKaydetAsync(new BasvuruIzlemeUstBilgi
        {
            basvuruId = _basvuruId, baslangicTarihi = baslangic, hedefTarihi = baslangic.AddYears(_r.Sayi(2, 5)), veriSorumlusu = string.IsNullOrWhiteSpace(b.irtibat.kisi) ? _r.AdSoyad(false) : b.irtibat.kisi
        }, kullanici), "İzleme üst bilgileri kaydedilemedi");
        foreach (IzlemeGostergesiTanim tanim in IzlemeGostergesiTanimSaglayici.BasvuruIcin(b).Where(x => x.aktif && b.IzlemeGostergeleri.All(g => g.gostergeKodu != x.kod)))
        {
            int bas = _r.Sayi(0, 200);
            Kontrol(await isKurallari.BasvuruIzlemeGostergesiKaydetAsync(new BasvuruIzlemeGostergesi
            {
                basvuruId = _basvuruId, gostergeKodu = tanim.kod, baslangicDegeri = bas.ToString(), hedefDeger = (bas + _r.Sayi(1, 150)).ToString(),
                kadinKirilimi = $"%{_r.Sayi(10, 60)}", gencKirilimi = $"%{_r.Sayi(10, 50)}", aciklama = "Test verisi: değerler yıllık olarak raporlanacaktır."
            }, kullanici), $"{tanim.ad} izleme göstergesi kaydedilemedi");
        }
    }

    private async Task TaahhutKaydetAsync(Basvuru b)
    {
        await TaahhutDosyasiYukleAsync();
        BasvuruAdliSicilKisi? yetkili = b.AdliSicilKisileri.FirstOrDefault();
        string kisi = yetkili != null ? $"{yetkili.ad} {yetkili.soyad}" : b.irtibat.kisi ?? _r.AdSoyad(false);
        string gorev = string.IsNullOrWhiteSpace(b.irtibat.unvan) ? "Genel Müdür" : b.irtibat.unvan;
        var satirlar = BeyanTanimlari.Maddeler.Select((_, i) => new { no = i + 1, kabul = 1, aciklama = "" }).ToList();
        Kontrol(await isKurallari.KaydetTaahhutBeyanlariAsync(new BasvuruTaahhutBeyanlar
        {
            basvuruId = _basvuruId, basvuruSayfasi = true, yetkiliKisi = kisi, gorevi = gorev, aciklama = "Test verisi.",
            taahhutBeyanlarJson = JsonSerializer.Serialize(new { satirlar, yetkiliKisi = kisi, gorevi = gorev })
        }, kullanici), "Taahhüt ve beyanlar kaydedilemedi");
    }

    private async Task TaahhutDosyasiYukleAsync() =>
        Kontrol(await isKurallari.TaahhutDosyasiKaydetAsync(_basvuruId, "Taahhut_beyan_formu.pdf", TestVeriKaynaklari.Pdf("Taahhut ve beyan formu"), "Test taahhüt/beyan formu", kullanici), "Taahhüt/beyan dosyası yüklenemedi");
}
