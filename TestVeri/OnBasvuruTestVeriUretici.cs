using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Data.SqlClient;
using TarimDonusum.IsKurallari;
using TarimDonusum.Models;
using TarimDonusum.Tablolar;
using Sektor = TarimDonusum.TestVeri.TestVeriKaynaklari.Sektor;

namespace TarimDonusum.TestVeri;

public sealed class TestVeriHatasi(string adim, IEnumerable<string> hatalar)
    : Exception($"{adim}: {string.Join(" ", hatalar.DefaultIfEmpty("Bilinmeyen hata."))}");

/// <summary>
/// TEST: Ön başvurunun tüm sayfalarını rastgele ama tutarlı verilerle doldurur ve kaydeder.
/// Kayıtlar normal kullanıcı akışındaki iş kuralı metotlarıyla yapılır; doğrulamalar aynen çalışır.
/// Test süreci bitince TestVeri klasörü ile birlikte silinebilir.
/// </summary>
public sealed class OnBasvuruTestVeriUretici(BasvuruIsKurallari isKurallari, IConfiguration configuration, Kullanici kullanici)
{
    private const string ZorunluBelgeFormAd = "Basvuru_ZorunluBelge";
    private const string AdliSicilFormAd = "Basvuru_AdliSicil";
    private const string ImzaYetkiFormAd = "Basvuru_ImzaYetki";
    private const string YatirimYeriFormAdOnEki = "BYAT_";
    private static readonly string[] TumBaglamlar = ["existing", "planned"];
    private static readonly string[] MakineDurumlari = ["YENİ DESTEK İSTENİYOR", "YENİ DESTEK İSTEMİYOR", "MEVCUT KULLANILACAK", "MEVCUT KULLANILMAYACAK"];

    private readonly TestVeriKaynaklari _r = new();
    /// <summary>Ürün (tür + sıra no) ile o ürüne ait girdilerin sıra numaraları; tedarik kayıtları bu eşleşmeden üretilir.</summary>
    private readonly List<(enumYatirimOnBilgiTuru tur, int siraNo, List<int> girdiSiraNolari)> _urunGirdileri = [];
    private readonly string _baglanti = configuration.GetConnectionString("DefaultConnection") ?? "";

    public int BasvuruId { get; private set; }
    /// <summary>Üretim sırasında kullanıcıya bildirilecek notlar (ör. kapalı dönem kullanıldı).</summary>
    public List<string> Notlar { get; } = [];

    private static T Al<T>(Sonuc<T> sonuc, string adim) => sonuc.basarili ? sonuc.nesne : throw new TestVeriHatasi(adim, sonuc.hatalar);
    private static void Kontrol(Sonuc sonuc, string adim) { if (!sonuc.basarili) throw new TestVeriHatasi(adim, sonuc.hatalar); }
    private async Task<Basvuru> OkuAsync() => Al(await isKurallari.OkuAsync(BasvuruId, kullanici), "Ön başvuru okunamadı");
    private async Task DosyaYukleAsync(string formAd, int dosyaNo, string baslik) =>
        Kontrol(await isKurallari.BasvuruDosyasiKaydetAsync(BasvuruId, formAd, dosyaNo, TestVeriKaynaklari.DosyaAdi(baslik), TestVeriKaynaklari.Pdf(baslik), kullanici), $"{baslik} yüklenemedi");

    public async Task<int> UretAsync()
    {
        Donem donem = await DonemSecAsync();
        TestFinansProfili profil = TestFinansProfili.Olustur(_r, donem);
        (Il il, List<Ilce> ilceler, DegerZinciri zincir) = await IlVeDegerZinciriSecAsync();
        Sektor sektor = TestVeriKaynaklari.SektorBul(zincir.ad);

        bool uretici = _r.Olasilik(0.2);
        enumBasvuruSahibiTuru sahipTuru = uretici ? _r.Sec([enumBasvuruSahibiTuru.UreticiOrgutu, enumBasvuruSahibiTuru.Kooperatif, enumBasvuruSahibiTuru.Birlik]) : enumBasvuruSahibiTuru.Isletme;
        enumHukukiTurSirketTuru hukukiTur = uretici ? enumHukukiTurSirketTuru.UreticiOrgutuKooperatifBirlik
            : _r.Olasilik(0.85) ? _r.Sec([enumHukukiTurSirketTuru.AnonimSirket, enumHukukiTurSirketTuru.LimitedSirket]) : _r.Sec([enumHukukiTurSirketTuru.KollektifSirket, enumHukukiTurSirketTuru.KomanditSirket]);

        Firma firma = await FirmaHazirlaAsync(donem, sektor, hukukiTur, il);
        await BasvuruSahibiKaydetAsync(donem, il, zincir, firma, sahipTuru, hukukiTur);
        await AdliSicilKaydetAsync(hukukiTur);

        int kadin = (int)Math.Round(profil.CalisanSayisi * _r.Sayi(25, 60) / 100m), erkek = profil.CalisanSayisi - kadin;
        await OrtaklarVeMaliKaydetAsync(profil);

        List<DegerZinciriAsama> asamalar = Al(await isKurallari.DegerZinciriAsamalariListeleAsync(kullanici, zincir.id, BasvuruId, 0), "Değer zinciri aşamaları okunamadı");
        if (asamalar.Count == 0) throw new TestVeriHatasi("Değer zinciri", [$"{zincir.ad} için aşama tanımlı değil."]);
        await AdresleriKaydetAsync(donem, il, ilceler, asamalar, sektor);
        await YatirimBilgileriKaydetAsync(sektor);

        Basvuru b = await OkuAsync();
        await UrunVeGirdileriKaydetAsync(b, sektor, profil);
        await TeknikProjeKaydetAsync();
        b = await OkuAsync();
        await MakineleriKaydetAsync(b, sektor);
        await BinalariKaydetAsync(b);
        await IstihdamKaydetAsync(profil, kadin, erkek);
        b = await OkuAsync();
        await TedarikKaydetAsync(b, sektor);
        await ButceVeFinansKaydetAsync(b, donem, profil);
        await ZorunluBelgeleriYukleAsync();
        b = await OkuAsync();
        await CevreselSosyalKaydetAsync(b, sektor);
        await TaahhutKaydetAsync();
        return BasvuruId;
    }

    private async Task<Donem> DonemSecAsync()
    {
        List<Donem> donemler = Al(await isKurallari.DonemleriListeleAsync(), "Dönemler okunamadı");
        List<Donem> acik = donemler.Where(x => x.OnBasvuruyaAcikMi()).ToList();
        if (acik.Count > 0) return _r.Sec(acik);
        Donem? son = donemler.OrderByDescending(x => x.yil).ThenByDescending(x => x.id).FirstOrDefault()
            ?? throw new TestVeriHatasi("Dönem", ["Tanımlı dönem bulunmuyor."]);
        Notlar.Add($"Ön başvuruya açık dönem olmadığı için \"{son.ad}\" dönemi kullanıldı.");
        return son;
    }

    private async Task<(Il, List<Ilce>, DegerZinciri)> IlVeDegerZinciriSecAsync()
    {
        foreach (Il il in _r.Karistir(Al(await isKurallari.IlleriListeleAsync(), "İller okunamadı")))
        {
            List<DegerZinciri> zincirler = (await isKurallari.DegerZincirleriListeleAsync(kullanici, il.id, 0, 0)).nesne?.Where(x => x.aktif).ToList() ?? [];
            if (zincirler.Count == 0) continue;
            List<Ilce> ilceler = (await isKurallari.IlceleriListeleAsync(il.id)).nesne?.Where(x => x.Aktif).ToList() ?? [];
            if (ilceler.Count > 0) return (il, ilceler, _r.Sec(zincirler));
        }
        throw new TestVeriHatasi("İl / değer zinciri", ["Değer zinciri tanımlı ve ilçesi olan il bulunamadı."]);
    }

    private async Task<Firma> FirmaHazirlaAsync(Donem donem, Sektor sektor, enumHukukiTurSirketTuru hukukiTur, Il il)
    {
        await using SqlConnection connection = new(_baglanti);
        await connection.OpenAsync();
        TABFirma tabFirma = new(connection);
        string vkn;
        do vkn = _r.Vkn(); while (await tabFirma.VergiKimlikNoIleOkuAsync(0, vkn) != null);

        List<Nace> naceler = [.. await new TABNace(connection).AraAsync("10."), .. await new TABNace(connection).AraAsync("01.")];
        if (naceler.Count == 0) naceler = await new TABNace(connection).AraAsync("");
        string ek = hukukiTur switch
        {
            enumHukukiTurSirketTuru.AnonimSirket => "A.Ş.",
            enumHukukiTurSirketTuru.LimitedSirket => "Ltd. Şti.",
            enumHukukiTurSirketTuru.KollektifSirket => "Kollektif Şti.",
            enumHukukiTurSirketTuru.KomanditSirket => "Komandit Şti.",
            _ => "Tarımsal Kalkınma Kooperatifi"
        };
        string soyad = _r.Soyadi();
        int yil = (donem.yil > 0 ? donem.yil : DateTime.Today.Year) - _r.Sayi(4, 25);
        Firma firma = new()
        {
            vergiKimlikNo = vkn,
            ticaretUnvani = $"TEST {soyad} {sektor.Tesis.Split(' ')[0]} Gıda Tarım Sanayi ve Ticaret {ek}",
            ticaretSicilNo = _r.Sayi(10000, 999999).ToString(),
            kurulusTarihi = new DateTime(yil, _r.Sayi(1, 12), _r.Sayi(1, 28)),
            mersisNo = $"0{vkn}000{_r.Sayi(10, 99)}",
            naceKodu = naceler.Count > 0 ? _r.Sec(naceler).kod : "",
            telefon = _r.Telefon(),
            webSitesi = $"www.{TestVeriKaynaklari.Ascii(soyad).ToLowerInvariant()}gida.com.tr",
            eposta = $"info@{TestVeriKaynaklari.Ascii(soyad).ToLowerInvariant()}gida.com.tr",
            kepAdresi = $"{TestVeriKaynaklari.Ascii(soyad).ToLowerInvariant()}gida@hs01.kep.tr",
            faaliyetKonusu = $"{sektor.Faaliyet} faaliyetleri",
            adres = _r.Adres("Merkez", il.ad)
        };
        Sonuc<int> sonuc = await isKurallari.FirmaEkleGuncelleAsync(firma, kullanici);
        if (sonuc.basarili && sonuc.nesne > 0) { firma.id = sonuc.nesne; return firma; }

        // VKN servisi tanımlıysa uydurma firma doğrulanamaz; kullanıcının mevcut firmalarından biri kullanılır.
        List<Firma> mevcutlar = await tabFirma.AraAsync("", kullanici.Id);
        if (mevcutlar.Count == 0) throw new TestVeriHatasi("Firma", sonuc.hatalar.Append("Kullanıcıya bağlı mevcut firma da bulunamadı."));
        Firma secilen = _r.Sec(mevcutlar);
        Notlar.Add($"Yeni firma oluşturulamadığı için mevcut \"{secilen.ticaretUnvani}\" firması kullanıldı ({sonuc.hataStr}).");
        return await tabFirma.OkuAsync(secilen.id) ?? secilen;
    }

    private async Task BasvuruSahibiKaydetAsync(Donem donem, Il il, DegerZinciri zincir, Firma firma, enumBasvuruSahibiTuru sahipTuru, enumHukukiTurSirketTuru hukukiTur)
    {
        bool kadin = _r.Olasilik(0.5);
        string kisi = _r.AdSoyad(kadin);
        Basvuru b = new();
        b.basvuruFirma.donemId = donem.id;
        b.basvuruFirma.ilId = il.id;
        b.basvuruFirma.firmaId = firma.id;
        b.basvuruFirma.degerZinciriId = zincir.id;
        b.basvuruFirma.basvuruKonusu = "";
        b.basvuruFirma.basvuruSahibiTuru = sahipTuru;
        b.basvuruFirma.hukukiTurSirketTuru = hukukiTur;
        b.irtibat = new() { kisi = kisi, unvan = _r.Sec(TestVeriKaynaklari.Unvanlar), telefon = _r.Telefon(), ePosta = _r.Eposta(kisi), adres = _r.Adres("Merkez", il.ad), yetkiliKisiler = kisi };
        BasvuruId = Al(await isKurallari.KaydetBasvuruSahibiAsync(b, kullanici), "Başvuru sahibi kaydedilemedi");
    }

    private async Task AdliSicilKaydetAsync(enumHukukiTurSirketTuru hukukiTur)
    {
        string anaGorev = hukukiTur == enumHukukiTurSirketTuru.AnonimSirket || hukukiTur == enumHukukiTurSirketTuru.UreticiOrgutuKooperatifBirlik
            ? "Yönetim kurulu üye" : "Temsil ve ilzama yetkili";
        List<BasvuruAdliSicilKisi> kisiler = Enumerable.Range(1, _r.Sayi(1, 3)).Select(i =>
        {
            bool kadin = _r.Olasilik(0.4);
            return new BasvuruAdliSicilKisi
            {
                basvuruId = BasvuruId, siraNo = i, tckn = _r.Tckn(), ad = kadin ? _r.KadinAdi() : _r.ErkekAdi(), soyad = _r.Soyadi(),
                dogumTarihi = _r.DogumTarihi(28, 68), gorev = i == 1 ? anaGorev : _r.Sec(["Şirket Ortağı", anaGorev, "Adli Sicil Kontrolüne Tabi Ek Kişi"]),
                yetkiKapsami = i == 1 ? "Münferiden temsil ve ilzama yetkili" : "Müştereken temsile yetkili", aciklama = "Test verisi"
            };
        }).ToList();
        Kontrol(await isKurallari.KaydetAdliSicilKisileriAsync(BasvuruId, kisiler, kullanici), "Adli sicil kişileri kaydedilemedi");
        foreach (BasvuruAdliSicilKisi kisi in (await OkuAsync()).AdliSicilKisileri)
        {
            await DosyaYukleAsync(AdliSicilFormAd, kisi.id, $"Adli sicil kaydi {kisi.ad} {kisi.soyad}");
            await DosyaYukleAsync(ImzaYetkiFormAd, kisi.id, $"Imza sirkuleri {kisi.ad} {kisi.soyad}");
        }
    }

    private async Task OrtaklarVeMaliKaydetAsync(TestFinansProfili p)
    {
        int ortakSayisi = _r.Sayi(1, 4);
        int toplamPay = _r.Olasilik(0.7) ? 100 : _r.Sayi(80, 99);
        List<int> paylar = PaylariBol(toplamPay, ortakSayisi);
        foreach (int pay in paylar)
        {
            bool kadin = _r.Olasilik(0.4);
            BasvuruOrtak ortak = new()
            {
                basvuruId = BasvuruId, adUnvan = _r.AdSoyad(kadin), tcknVkn = _r.Tckn(), kisiTuru = "Gerçek Kişi", payOrani = pay,
                ozelKamuNiteligi = "Özel", dogumTarihi = _r.DogumTarihi(24, 70), cinsiyet = kadin ? "Kadın" : "Erkek"
            };
            Kontrol(await isKurallari.KaydetOrtaklarAsync(new BasvuruOrtaklik { basvuruId = BasvuruId, ortaklar = [ortak] }, kullanici), "Ortak kaydedilemedi");
        }

        // Firma büyüklüğü finansal profilden gelir: yatırımı ve krediyi taşıyabilecek satış ve aktif büyüklüğü.
        bool ihracat = _r.Olasilik(0.6);
        int ihracatOrani = _r.Sayi(5, 40);
        bool denetim = _r.Olasilik(0.4);
        BasvuruMali mali = new()
        {
            basvuruId = BasvuruId,
            ozelSektorPayi = Math.Max(toplamPay, _r.Sayi(Math.Max(75, toplamPay), 100)),
            halkaAciklikOrani = _r.Olasilik(0.2) ? _r.Sayi(1, 20) : 0,
            oncekiYilNetSatis = p.OncekiYilNetSatis,
            sonYilNetSatis = p.SonYilNetSatis,
            oncekiYilAktifToplami = p.OncekiYilAktif,
            sonYilAktifToplami = p.SonYilAktif,
            oncekiYilIhracatSatis = ihracat ? Math.Round(p.OncekiYilNetSatis * (ihracatOrani - _r.Sayi(0, 4)) / 100m / 1000) * 1000 : 0,
            sonYilIhracatSatis = ihracat ? Math.Round(p.SonYilNetSatis * ihracatOrani / 100m / 1000) * 1000 : 0,
            oncekiYilCalisanSayisi = Math.Max(1, p.CalisanSayisi - _r.Sayi(0, 8)),
            sonYilCalisanSayisi = p.CalisanSayisi,
            aciklama = "Test verisi: mali tablolar bağımsız denetim raporu ve beyannamelerle uyumludur.",
            belgeReferanslariJson = "",
            bagimsizDenetimeTabiMi = denetim
        };
        Kontrol(await isKurallari.KaydetMaliAsync(mali, kullanici), "Mali bilgiler kaydedilemedi");
        if (denetim)
            Kontrol(await isKurallari.DenetimDosyasiKaydetAsync(BasvuruId, "Bagimsiz_denetim_raporu.pdf", TestVeriKaynaklari.Pdf("Bagimsiz denetim raporu"), kullanici), "Bağımsız denetim raporu yüklenemedi");
    }

    private List<int> PaylariBol(int toplam, int adet)
    {
        List<int> kesimler = Enumerable.Range(1, toplam - 1).OrderBy(_ => _r.Sayi(0, 10000)).Take(adet - 1).Append(0).Append(toplam).OrderBy(x => x).ToList();
        return kesimler.Zip(kesimler.Skip(1), (a, b) => b - a).ToList();
    }

    private async Task AdresleriKaydetAsync(Donem donem, Il il, List<Ilce> ilceler, List<DegerZinciriAsama> asamalar, Sektor sektor)
    {
        int sinir = donem.uygulamaAdresiSinirliMi > 0 ? Math.Min(donem.uygulamaAdresiSinirliMi, 3) : 3;
        // Tedarik kayıtlarının ilçesi girdinin adresinden gelir; birden fazla ilçeden tedarik için adresler farklı ilçelerde açılır.
        int enFazla = Math.Min(sinir, ilceler.Count);
        int adet = enFazla >= 2 ? _r.Sayi(2, enFazla) : 1;
        if (adet < 2) Notlar.Add("Dönemin adres sınırı veya ilçe sayısı nedeniyle tek yatırım adresi açıldı; tedarik kayıtları tek ilçeden.");
        List<Ilce> adresIlceleri = _r.AltKume(ilceler, adet, adet);
        for (int i = 1; i <= adet; i++)
        {
            Ilce ilce = adresIlceleri[i - 1];
            List<int> yatirimTurleri = _r.Olasilik(0.4) ? [(int)enumYatirimTuru.Yeni]
                : _r.AltKume([(int)enumYatirimTuru.KapasiteArtirimi, (int)enumYatirimTuru.Modernizasyon], 1, 2);
            List<int> harcamaTurleri = [(int)enumHarcamaTuru.MakineEkipman, .. _r.AltKume([(int)enumHarcamaTuru.YapimIsleri, (int)enumHarcamaTuru.Danismanlik, (int)enumHarcamaTuru.TedarikciGelistirmeHarcamalari, (int)enumHarcamaTuru.YazilimDonanım], 0, 3)];
            enumUygulamaAdresiYatirimYeriStatusu statu = _r.Sec([enumUygulamaAdresiYatirimYeriStatusu.Mulkiyet, enumUygulamaAdresiYatirimYeriStatusu.Mulkiyet, enumUygulamaAdresiYatirimYeriStatusu.Kira,
                enumUygulamaAdresiYatirimYeriStatusu.Tahsis, enumUygulamaAdresiYatirimYeriStatusu.IrtifakHakki, enumUygulamaAdresiYatirimYeriStatusu.OrganizeSanayi_IhtisasAlaniTahsisi]);
            bool kiraTahsis = statu != enumUygulamaAdresiYatirimYeriStatusu.Mulkiyet;
            int sure = _r.Sayi(10, 30);
            decimal enlem = _r.Tutar(37m, 41m, 1) + _r.Sayi(0, 9999) / 10000m, boylam = _r.Tutar(27m, 43m, 1) + _r.Sayi(0, 9999) / 10000m;
            BasvuruUygulamaAdresi adres = new()
            {
                basvuruId = BasvuruId, siraNo = i, ilceId = ilce.Id, ilId = il.id,
                tamAdres = _r.Adres(ilce.Ad, il.ad),
                yatirimYeriStatusu = statu,
                kiraVeyaTahsisSuresi = kiraTahsis ? sure : null,
                kiraTahsisBitisTarihi = kiraTahsis ? DateTime.Today.AddYears(sure).Date : null,
                yapiRuhsatiDurumu = harcamaTurleri.Contains((int)enumHarcamaTuru.YapimIsleri)
                    ? _r.Sec([enumUygulamaAdresiYapiRuhsatiDurumu.YapiRuhsatiMevcut, enumUygulamaAdresiYapiRuhsatiDurumu.YapiRuhsatiBasvurusuYapildi, enumUygulamaAdresiYapiRuhsatiDurumu.RuhsatGerekmedigineDairYaziMevcut, enumUygulamaAdresiYapiRuhsatiDurumu.HenuzTeminEdilmedi])
                    : enumUygulamaAdresiYapiRuhsatiDurumu.YapimIsiYok,
                konumlar = Enumerable.Range(1, _r.Sayi(1, 2)).Select(k => new BasvuruUygulamaAdresiKonum
                {
                    siraNo = k, mahalle = $"{_r.Sec(TestVeriKaynaklari.Mahalleler)} Mahallesi", ada = _r.Sayi(100, 2999).ToString(), parsel = _r.Sayi(1, 80).ToString(),
                    koordinatTuru = enumKoordinatTuru.ITRF96, minEnlem = enlem, maxEnlem = enlem + 0.0030m, minBoylam = boylam, maxBoylam = boylam + 0.0040m
                }).ToList(),
                kumelenmeOrganizeAlanTuru = (enumKumelenmeOrganizeAlanTuru)_r.Sayi(1, 5),
                kullanimHakkiBaslangicTarihi = DateTime.Today.AddYears(-_r.Sayi(0, 8)).AddDays(-_r.Sayi(0, 300)).Date,
                donemleriKapsiyorMu = true,
                izinTakvimAciklama = "Gerekli izinler yatırım takvimine uygun olarak alınacaktır.",
                degerZinciriAsamalari = _r.AltKume(asamalar, 1, 3).Select(x => new DegerZinciriAsama { id = x.id, yapilacakFaaliyetler = $"{x.ad} aşamasında {sektor.Faaliyet} yapılacaktır." }).ToList(),
                yatirimTurleri = yatirimTurleri,
                harcamaTurleri = harcamaTurleri,
                yatirimFaaliyetleri = $"{sektor.Tesis} kapsamında {sektor.Faaliyet}",
                yatirimGirdileri = string.Join(", ", _r.AltKume(sektor.Girdiler, 2, 4)),
                yatirimCiktilari = string.Join(", ", _r.AltKume(sektor.Urunler, 2, 4))
            };
            Kontrol(await isKurallari.UygulamaAdresiKaydetAsync(adres, kullanici), $"{i}. yatırım adresi kaydedilemedi");
        }
        foreach (BasvuruUygulamaAdresi adres in (await OkuAsync()).YatirimAdresleri)
        {
            await DosyaYukleAsync($"{YatirimYeriFormAdOnEki}{adres.id}", 1, $"Adres belgesi {adres.siraNo}");
            await DosyaYukleAsync($"{YatirimYeriFormAdOnEki}{adres.id}", 2, $"Kullanim hakki belgesi {adres.siraNo}");
            await DosyaYukleAsync($"{YatirimYeriFormAdOnEki}{adres.id}", 3, $"Kanit belgesi {adres.siraNo}");
        }
    }

    private async Task YatirimBilgileriKaydetAsync(Sektor sektor)
    {
        DateTime baslangic = DateTime.Today.AddMonths(_r.Sayi(2, 6));
        BasvuruYatirim yatirim = new()
        {
            basvuruId = BasvuruId,
            yatirimAdi = $"{sektor.Tesis} {_r.Sec(["Kuruluş", "Modernizasyon", "Kapasite Artırımı", "Teknoloji Yatırımı"])} Projesi",
            yatiriminAmaci = $"Yatırımla {sektor.Faaliyet} kapasitesinin artırılması, ürün kalitesinin yükseltilmesi, yerel üreticilerle sözleşmeli tedarik zincirinin güçlendirilmesi ve yeni istihdam sağlanması amaçlanmaktadır.",
            planlananBaslangicTarihi = baslangic,
            planlananTamamlanmaTarihi = baslangic.AddMonths(_r.Sayi(8, 18))
        };
        Kontrol(await isKurallari.KaydetYatirimBilgileriAsync(yatirim, kullanici), "Yatırım bilgileri kaydedilemedi");
    }

    private static enumDegerZinciriAsamaTuru RastgeleAsama(TestVeriKaynaklari r) => (enumDegerZinciriAsamaTuru)r.Sayi(1, 3);

    /// <summary>Birim başına satış fiyatı (TL) aralığı; ürün geliri bu fiyattan miktara çevrilir.</summary>
    private decimal SatisFiyati(string birim) => birim switch
    {
        "Ton" => _r.Tutar(30_000, 250_000, 100),
        "Litre" => _r.Tutar(25, 160, 1),
        "Adet" => _r.Tutar(15, 250, 1),
        _ => _r.Tutar(30, 320, 1)
    };

    private decimal GirdiFiyati(string birim) => birim switch
    {
        "Ton" => _r.Tutar(6_000, 40_000, 100),
        "Litre" => _r.Tutar(8, 45, 1),
        "Adet" => _r.Tutar(2, 25, 1),
        _ => _r.Tutar(5, 60, 1)
    };

    private async Task UrunVeGirdileriKaydetAsync(Basvuru b, Sektor sektor, TestFinansProfili p)
    {
        // Önce ürün listesi belirlenir, sonra 1. yıl geliri (ve mevcut ürünlerde bugünkü gelir) ürünlere paylaştırılır.
        List<(BasvuruUygulamaAdresi adres, bool mevcut, string ad, string birim)> liste = [];
        List<string> urunAdlari = _r.Karistir(sektor.Urunler);
        for (int i = 0, adet = _r.Sayi(3, 5); i < adet; i++)
        {
            BasvuruUygulamaAdresi adres = b.YatirimAdresleri[i % b.YatirimAdresleri.Count];
            bool mevcutOlabilir = !adres.yatirimTurleri.Contains((int)enumYatirimTuru.Yeni);
            string ad = i < urunAdlari.Count ? urunAdlari[i] : $"{urunAdlari[i % urunAdlari.Count]} ({i + 1}. çeşit)";
            liste.Add((adres, mevcutOlabilir && _r.Olasilik(0.5), ad, _r.Sec(["Kg", "Kg", "Ton", "Litre", "Adet"])));
        }
        List<decimal> gelirler = TestFinansProfili.Bol(_r, p.BirinciYilGelir, liste.Count);
        int mevcutAdet = liste.Count(x => x.mevcut);
        List<decimal> mevcutGelirler = TestFinansProfili.Bol(_r, p.MevcutGelir, mevcutAdet);
        int mevcutSira = 0, yeniSira = 0, m = 0;
        for (int i = 0; i < liste.Count; i++)
        {
            (BasvuruUygulamaAdresi adres, bool mevcut, string ad, string birim) = liste[i];
            decimal fiyat = SatisFiyati(birim);
            decimal satis = Math.Max(1, Math.Round(gelirler[i] / fiyat));
            decimal uretim = Math.Round(satis * 100m / _r.Sayi(92, 100));
            decimal kapasite = Math.Round(uretim * 100m / _r.Sayi(80, 92));
            BasvuruYatirimOnBilgi urun = new()
            {
                basvuruId = BasvuruId, uygulamaAdresiId = adres.id, degerZinciriAsamaTuru = enumDegerZinciriAsamaTuru.IslemeIleriIsleme,
                tur = mevcut ? enumYatirimOnBilgiTuru.MevcutUrun : enumYatirimOnBilgiTuru.UretilecekUrun, siraNo = mevcut ? ++mevcutSira : ++yeniSira,
                ad = ad, birim = birim, miktar = kapasite, birinciYilKapasite = kapasite, birinciYilUretimMiktari = uretim, satisMiktari = satis, birimSatisFiyati = fiyat
            };
            if (mevcut)
            {
                decimal mevcutFiyat = Math.Round(fiyat * _r.Sayi(85, 95) / 100m, 2);
                decimal mevcutSatis = Math.Max(1, Math.Round(mevcutGelirler[m++] / mevcutFiyat));
                decimal mevcutUretim = Math.Round(mevcutSatis * 100m / _r.Sayi(92, 100));
                // Yatırımdan sonra kapasite artar; mevcut kapasite 1. yıl kapasitesini aşmaz.
                decimal mevcutKapasite = Math.Min(kapasite, Math.Round(mevcutUretim * 100m / _r.Sayi(85, 98)));
                mevcutUretim = Math.Min(mevcutUretim, mevcutKapasite);
                urun.mevcutKapasite = mevcutKapasite; urun.mevcutUretimMiktari = mevcutUretim;
                urun.mevcutSatisMiktari = Math.Min(mevcutSatis, mevcutUretim); urun.mevcutBirimSatisFiyati = mevcutFiyat;
            }
            Kontrol(await isKurallari.BasvuruYatirimOnBilgisiKaydetAsync(urun, kullanici), $"Ürün ({urun.ad}) kaydedilemedi");
            _urunGirdileri.Add((urun.tur, urun.siraNo, []));
        }

        // Her ürünün 10 girdisi vardır: önce sektörün ham maddeleri, sonra yardımcı madde ve işletme malzemeleri.
        // Girdiler adreslere sırayla dağıtılır; böylece her ürünün tedariki birden fazla ilçeden yapılır.
        // Toplam yıllık maliyet (miktar x birim fiyat) profildeki girdi maliyetine eşittir; ham maddeler maliyetin büyük kısmıdır.
        List<(int urun, string ad, bool hamMadde)> girdiler = [];
        for (int k = 0; k < liste.Count; k++)
        {
            List<string> ham = _r.AltKume(sektor.Girdiler, Math.Min(3, sektor.Girdiler.Length), Math.Min(4, sektor.Girdiler.Length));
            List<string> yardimci = _r.AltKume(TestVeriKaynaklari.GenelGirdiler, 10 - ham.Count, 10 - ham.Count);
            girdiler.AddRange(ham.Select(x => (k, $"{x} ({liste[k].ad})", true)).Concat(yardimci.Select(x => (k, $"{x} ({liste[k].ad})", false))));
        }
        List<decimal> agirliklar = girdiler.Select(x => (decimal)(x.hamMadde ? _r.Sayi(30, 60) : _r.Sayi(2, 8))).ToList();
        decimal agirlikToplami = agirliklar.Sum();
        for (int i = 0; i < girdiler.Count; i++)
        {
            (int urunNo, string ad, bool hamMadde) = girdiler[i];
            string birim = hamMadde ? _r.Sec(["Kg", "Kg", "Ton", "Litre"]) : _r.Sec(["Adet", "Adet", "Kg"]);
            decimal fiyat = GirdiFiyati(birim);
            decimal maliyet = p.GirdiMaliyeti * agirliklar[i] / agirlikToplami;
            BasvuruYatirimOnBilgi girdi = new()
            {
                basvuruId = BasvuruId, uygulamaAdresiId = b.YatirimAdresleri[(urunNo + i) % b.YatirimAdresleri.Count].id, degerZinciriAsamaTuru = RastgeleAsama(_r),
                tur = enumYatirimOnBilgiTuru.Girdi, siraNo = i + 1, ad = ad, birim = birim, miktar = Math.Max(1, Math.Round(maliyet / fiyat)), birimFiyat = fiyat,
                giderTuru = hamMadde ? GirdiGiderTuru.IlkMaddeKullanimi : _r.Sec([GirdiGiderTuru.YardimciMadde, GirdiGiderTuru.IsletmeMalzemesiKullanimi]),
                sabitOran = _r.Sayi(0, 15)
            };
            Kontrol(await isKurallari.BasvuruYatirimOnBilgisiKaydetAsync(girdi, kullanici), $"Girdi ({girdi.ad}) kaydedilemedi");
            _urunGirdileri[urunNo].girdiSiraNolari.Add(girdi.siraNo);
        }
    }

    private async Task TeknikProjeKaydetAsync()
    {
        JsonArray gunes = [];
        if (_r.Olasilik(0.5))
        {
            int panel = _r.Sayi(40, 1200), guc = _r.Sec([450, 500, 550, 600]);
            gunes.Add(new JsonObject { ["type"] = "Güneş Enerjisi", ["panels"] = panel.ToString(), ["panelPower"] = guc.ToString(), ["totalPower"] = Math.Round(panel * guc / 1000m, 2).ToString(TestVeriKaynaklari.Tr) });
        }
        else gunes.Add(new JsonObject { ["type"] = "Güneş Enerjisi", ["panels"] = "", ["panelPower"] = "", ["totalPower"] = "" });
        JsonObject json = new()
        {
            ["investmentName"] = "", ["applicantName"] = "", ["version"] = "DB ÖB Version 1.0.00", ["investmentPurpose"] = "",
            ["solarRows"] = gunes,
            ["installedRows"] = new JsonArray(
                new JsonObject { ["type"] = "Güneş Enerjisi (AC)", ["power"] = _r.Olasilik(0.3) ? _r.Sayi(20, 500).ToString() : "" },
                new JsonObject { ["type"] = "Biyogaz", ["power"] = _r.Olasilik(0.15) ? _r.Sayi(50, 1000).ToString() : "" },
                new JsonObject { ["type"] = "Biyokütle", ["power"] = _r.Olasilik(0.15) ? _r.Sayi(50, 1000).ToString() : "" })
        };
        Kontrol(await isKurallari.KaydetDbCtpTeknikProjeAsync(new BasvuruDbCtpTeknikProje { basvuruId = BasvuruId, dbCtpTeknikProjeJson = json.ToJsonString() }, kullanici), "Teknik proje kaydedilemedi");
    }

    private async Task MakineleriKaydetAsync(Basvuru b, Sektor sektor)
    {
        // Makine adları Tanımlar > Uygun Harcama Listesi'nden seçilir; liste boşsa örnek adlar kullanılır.
        IReadOnlyList<UygunHarcamaTanim> tanimlar = UygunHarcamaTanimSaglayici.Aktifler;
        // En az 15 makine: önce Uygun Harcama Listesi'nden, liste yetmezse örnek adlardan (adlar tekrarlanmaz).
        int adet = _r.Sayi(15, 22);
        List<(string ad, int? id)> secimler = _r.Karistir(tanimlar).Select(x => (x.ad, (int?)x.id))
            .Concat(_r.Karistir(TestVeriKaynaklari.YedekMakineler).Select(x => (x, (int?)null)))
            .DistinctBy(x => x.Item1.Trim().ToLowerInvariant()).Take(adet).ToList();
        int sira = 0;
        foreach ((string ad, int? harcamaId) in secimler)
        {
            BasvuruUygulamaAdresi adres = _r.Sec(b.YatirimAdresleri);
            bool mevcutOlabilir = !adres.yatirimTurleri.Contains((int)enumYatirimTuru.Yeni);
            string durum = sira == 0 || _r.Olasilik(0.75) || !mevcutOlabilir && _r.Olasilik(0.6) ? MakineDurumlari[0]
                : mevcutOlabilir ? _r.Sec(MakineDurumlari) : MakineDurumlari[1];
            sira++;
            BasvuruMakine makine = new()
            {
                basvuruId = BasvuruId, uygulamaAdresiId = adres.id, degerZinciriAsamaTuru = RastgeleAsama(_r), uygunHarcamaId = harcamaId,
                siraNo = sira, ad = ad, birim = "Adet", miktar = _r.MakineAdedi(), durum = durum,
                marka = _r.Sec(TestVeriKaynaklari.Markalar), model = $"{(char)('A' + _r.Sayi(0, 25))}{(char)('A' + _r.Sayi(0, 25))}-{_r.Sayi(100, 9999)}",
                kapasiteOzellikleri = $"{_r.Sayi(1, 50) * 100} kg/saat, {_r.Sayi(2, 90)} kW",
                kullanimAmaci = $"{sektor.Faaliyet} sürecinde kullanılacaktır.",
                kapasiteSecimGerekcesi = "Planlanan yıllık üretim miktarı ve vardiya düzenine göre seçilmiştir.",
                yerlesimPlaniSiraNo = sira, aciklama = ""
            };
            Kontrol(await isKurallari.BasvuruMakinesiKaydetAsync(makine, kullanici), $"Makine ({ad}) kaydedilemedi");
        }
    }

    private async Task BinalariKaydetAsync(Basvuru b)
    {
        bool yapimVar = b.YatirimAdresleri.Any(x => x.harcamaTurleri.Contains((int)enumHarcamaTuru.YapimIsleri));
        if (!yapimVar && !_r.Olasilik(0.5)) return;
        int sira = 0;
        foreach (string ad in _r.AltKume(TestVeriKaynaklari.Binalar, 1, 3))
        {
            bool yeni = yapimVar && _r.Olasilik(0.75);
            BasvuruBina bina = new()
            {
                basvuruId = BasvuruId, uygulamaAdresiId = _r.Sec(b.YatirimAdresleri).id, degerZinciriAsamaTuru = RastgeleAsama(_r), siraNo = ++sira, ad = ad,
                mevcutYeni = yeni ? "Yeni" : "Mevcut",
                yatirimSekli = yeni ? "Yeni yapım" : yapimVar ? _r.Sec(["Genişletme / Modernizasyon", "Değişiklik yok / Kullanılacak"]) : "Değişiklik yok / Kullanılacak",
                destekTalebi = yeni ? "Evet" : "Hayır", vaziyetPlaniNo = $"VP-{sira:00}"
            };
            Kontrol(await isKurallari.BasvuruBinasiKaydetAsync(bina, kullanici), $"Bina ({ad}) kaydedilemedi");
        }
    }

    private async Task IstihdamKaydetAsync(TestFinansProfili p, int kadin, int erkek)
    {
        BasvuruIstihdam istihdam = new()
        {
            basvuruId = BasvuruId,
            oncekiYilKadin = Math.Max(0, kadin - _r.Sayi(0, 4)), oncekiYilErkek = Math.Max(0, erkek - _r.Sayi(0, 4)),
            sonYilKadin = kadin, sonYilErkek = erkek,
            gerekceVarsayimlarDogrulamaYaklasimi = "İstihdam artışı yeni üretim hattının vardiya ihtiyacına göre planlanmıştır; SGK bildirgeleriyle doğrulanacaktır."
        };
        Kontrol(await isKurallari.IstihdamKaydetAsync(istihdam, kullanici), "İstihdam bilgileri kaydedilemedi");
        // Mevcut çalışanlar satırlara paylaştırılır; yatırımla çalışan sayısı %10-25 artar.
        List<string> birimler = _r.AltKume(TestVeriKaynaklari.IstihdamBirimleri, 3, 6);
        List<decimal> mevcut = TestFinansProfili.Bol(_r, p.CalisanSayisi, birimler.Count);
        List<decimal> artis = TestFinansProfili.Bol(_r, Math.Max(birimler.Count, Math.Round(p.CalisanSayisi * _r.Sayi(10, 25) / 100m)), birimler.Count);
        for (int i = 0; i < birimler.Count; i++)
        {
            decimal baz = _r.Sayi(35, 80) * 1000m;
            BasvuruIstihdamSatir satir = new()
            {
                basvuruId = BasvuruId, siraNo = i + 1, birimUnite = birimler[i], gorevUretimHatti = _r.Sec(TestVeriKaynaklari.Gorevler),
                cinsiyet = _r.Olasilik(0.5) ? "Kadın" : "Erkek", yasDurumu = _r.Olasilik(0.5) ? "Genç" : "40 yaş ve üstü",
                mevcutCalisan = Math.Max(0, mevcut[i]), netCalisanArtisi = Math.Max(1, artis[i]), bazAylikBrutUcret = baz, hedefAylikBrutUcret = baz + _r.Sayi(2, 12) * 1000m
            };
            Kontrol(await isKurallari.IstihdamSatiriKaydetAsync(satir, kullanici), "İstihdam planı satırı kaydedilemedi");
        }
    }

    private async Task TedarikKaydetAsync(Basvuru b, Sektor sektor)
    {
        // Her ürün, kendi 10 girdisinin hepsi için tedarik kaydı alır; girdiler farklı adreslerde olduğundan kayıtlar birden fazla ilçededir.
        foreach ((enumYatirimOnBilgiTuru tur, int siraNo, List<int> girdiSiraNolari) in _urunGirdileri)
        {
            BasvuruYatirimOnBilgi? urun = b.YatirimOnBilgileri.FirstOrDefault(x => x.tur == tur && x.siraNo == siraNo);
            if (urun == null) continue;
            foreach (BasvuruYatirimOnBilgi girdi in b.YatirimOnBilgileri.Where(x => x.tur == enumYatirimOnBilgiTuru.Girdi && girdiSiraNolari.Contains(x.siraNo)))
            {
                BasvuruTedarikciEntegrasyonu kayit = new()
                {
                    basvuruId = BasvuruId, urunId = urun.id, girdiIdleri = [girdi.id],
                    mevcutYillikMiktar = Math.Round((girdi.miktar ?? 0) * _r.Sayi(20, 80) / 100m), mevcutBirimFiyat = Math.Round((girdi.birimFiyat ?? 0) * 0.9m, 2),
                    mevcutKayitliCiftci = _r.Sayi(0, 60), eklenecekKayitliCiftci = _r.Sayi(1, 150), tedarikSekli = _r.Sayi(1, 2),
                    kisaAciklama = $"{girdi.ad} bölgedeki üreticilerden sözleşmeli olarak temin edilecektir."
                };
                Kontrol(await isKurallari.TedarikciEntegrasyonuKaydetAsync(kayit, kullanici), $"Tedarik kaydı ({girdi.ad}) kaydedilemedi");
            }
        }
        Kontrol(await isKurallari.TedarikciEntegrasyonuAciklamaKaydetAsync(BasvuruId, $"{sektor.Tesis} için hammadde, bölgedeki kayıtlı çiftçilerle yapılacak alım sözleşmeleriyle sağlanacak; üreticilere teknik destek verilecektir.", kullanici), "Tedarikçi entegrasyonu açıklaması kaydedilemedi");
    }

    private async Task ButceVeFinansKaydetAsync(Basvuru b, Donem donem, TestFinansProfili p)
    {
        // Toplam yatırım yaklaşık 2 milyon EUR; finansman oranı azami orana ve azami destek tutarına göre sınırlanır.
        decimal azamiDestek = donem.maksimumDestekTutari > 0 ? donem.maksimumDestekTutari.Value : 2_500_000m;
        decimal azamiOran = donem.AzamiDestekOrani(b.YatirimAdresleri.Select(x => x.ilceId));
        if (azamiOran <= 0) azamiOran = 70;
        decimal c = p.ToplamYatirimTl;
        int ustOran = (int)Math.Min(azamiOran, Math.Floor(azamiDestek * 0.98m / p.ToplamYatirimEur * 100));
        int oran = _r.Sayi(Math.Min(50, ustOran), Math.Max(1, ustOran));

        bool yapim = b.YatirimAdresleri.Any(x => x.harcamaTurleri.Contains((int)enumHarcamaTuru.YapimIsleri));
        decimal a = Math.Round(c * _r.Sayi(82, 90) / 100m);
        decimal a1 = yapim ? Math.Round(a * _r.Sayi(15, 30) / 100m) : 0, a3 = Math.Round(a * _r.Sayi(3, 5) / 100m), a4 = Math.Round(a * _r.Sayi(2, 5) / 1000m);
        decimal a2 = a - a1 - a3 - a4;
        decimal bToplam = c - a;
        decimal[] bTutar = [.. TestFinansProfili.Bol(_r, bToplam, 7)];
        bTutar[4] += bTutar[0] / 2; bTutar[0] -= bTutar[0] / 2; // İşletme sermayesi (B5) uygun olmayan harcamaların büyük kalemidir.

        // Finansman: Dünya Bankası (F) + özkaynak (D) + banka kredisi (E) + diğer (G) = toplam yatırım.
        decimal f = Math.Round(c * oran / 100m), kalan = c - f;
        decimal g = _r.Olasilik(0.2) ? Math.Round(kalan * _r.Sayi(5, 15) / 100m) : 0;
        decimal d = Math.Round((kalan - g) * _r.Sayi(60, 100) / 100m), e = kalan - g - d;

        JsonObject butce = new();
        void Yaz(string kod, decimal tutar) => butce[kod] = new JsonObject { ["amount"] = tutar == 0 ? "" : TestVeriKaynaklari.ParaMetni(tutar) };
        Yaz("A1", a1); Yaz("A2", a2); Yaz("A3", a3); Yaz("A4", a4);
        for (int i = 0; i < 7; i++) Yaz($"B{i + 1}", bTutar[i]);
        Yaz("D", d); Yaz("E", e); Yaz("F", f); Yaz("G", g);

        // Girdi dışı işletme giderleri: personel satırları çalışan sayısından, diğerleri kalan tutarın paylaştırılmasıyla.
        decimal kisiBasiYillik = _r.Tutar(650_000, 1_000_000, 1000);
        int yonetim = Math.Max(2, (int)Math.Round(p.CalisanSayisi * 0.12)), pazarlama = Math.Max(1, (int)Math.Round(p.CalisanSayisi * 0.08));
        decimal personel = (yonetim + pazarlama) * kisiBasiYillik;
        if (personel > p.DigerIsletmeGiderleri * 0.6m) { kisiBasiYillik = Math.Round(p.DigerIsletmeGiderleri * 0.6m / (yonetim + pazarlama)); personel = (yonetim + pazarlama) * kisiBasiYillik; }
        (string grup, string kalem, string birim, decimal minFiyat, decimal maxFiyat)[] digerTanimlar =
        [
            ("Genel Üretim Giderleri", "Elektrik Giderleri", "kWh", 3, 5),
            ("Genel Üretim Giderleri", "Su / Gaz Giderleri", "", 1, 1),
            ("Genel Üretim Giderleri", "Yakıt Giderleri", "", 1, 1),
            ("Genel Üretim Giderleri", "Bakım Onarım Giderleri", "Yıl", 1, 1),
            ("Genel Üretim Giderleri", "Sigorta Giderleri", "Yıl", 1, 1),
            ("Genel Yönetim Giderleri", "Haberleşme Giderleri", "Yıl", 1, 1),
            ("Genel Yönetim Giderleri", "Kira Giderleri", "Yıl", 1, 1),
            ("Beklenebilecek Farklar", "Beklenebilecek Farklar", "", 1, 1),
            ("Pazarlama, Satış ve Dağıtım Giderleri", "Nakliye / Dağıtım Giderleri", "", 1, 1),
            ("Pazarlama, Satış ve Dağıtım Giderleri", "Diğer Giderler", "", 1, 1)
        ];
        List<decimal> digerTutarlar = TestFinansProfili.Bol(_r, p.DigerIsletmeGiderleri - personel, digerTanimlar.Length, 5, 30);
        JsonArray giderler = [];
        decimal giderToplam = 0, sabitToplam = 0;
        void Satir(string grup, string kalem, string birim, decimal miktar, decimal fiyat, int sabit)
        {
            giderToplam += miktar * fiyat; sabitToplam += miktar * fiyat * sabit / 100m;
            giderler.Add(new JsonObject
            {
                ["group"] = grup, ["item"] = kalem, ["qty"] = miktar.ToString(TestVeriKaynaklari.Tr), ["unit"] = birim,
                ["unitPrice"] = fiyat.ToString("#,##0.00", TestVeriKaynaklari.Tr), ["fixedPct"] = sabit, ["variablePct"] = 100 - sabit
            });
        }
        for (int i = 0; i < digerTanimlar.Length; i++)
        {
            var t = digerTanimlar[i];
            // Elektrik kWh x birim fiyat; diğer kalemler yıllık tutar olarak (miktar 1) girilir.
            decimal fiyat = t.birim == "kWh" ? Math.Round(_r.Tutar(t.minFiyat, t.maxFiyat, 1) + _r.Sayi(0, 99) / 100m, 2) : digerTutarlar[i];
            decimal miktar = t.birim == "kWh" ? Math.Max(1, Math.Round(digerTutarlar[i] / fiyat)) : 1;
            Satir(t.grup, t.kalem, t.birim, miktar, fiyat, t.kalem is "Sigorta Giderleri" or "Kira Giderleri" or "Haberleşme Giderleri" ? _r.Sayi(80, 100) : _r.Sayi(0, 30));
        }
        Satir("Genel Yönetim Giderleri", "Personel Giderleri", "Kişi/Yıl", yonetim, kisiBasiYillik, _r.Sayi(80, 100));
        Satir("Pazarlama, Satış ve Dağıtım Giderleri", "Personel Giderleri", "Kişi/Yıl", pazarlama, kisiBasiYillik, _r.Sayi(70, 100));

        JsonObject ozet = new()
        {
            ["investmentBudgetData"] = butce,
            ["operatingExpenseRows"] = giderler,
            ["totals"] = new JsonObject
            {
                ["budget"] = new JsonObject { ["investment"] = c, ["finance"] = c, ["gap"] = 0 },
                ["operatingExpense"] = new JsonObject { ["total"] = giderToplam + p.GirdiMaliyeti, ["fixed"] = sabitToplam, ["variable"] = giderToplam + p.GirdiMaliyeti - sabitToplam }
            }
        };
        Kontrol(await isKurallari.KaydetYatirimOzetiAsync(new BasvuruYatirimOzeti { basvuruId = BasvuruId, yatirimOzetiJson = ozet.ToJsonString() }, kullanici), "Bütçe ve giderler kaydedilemedi");

        int yatirimSuresi = _r.Sayi(6, 18);
        BasvuruFinans finans = new()
        {
            basvuruId = BasvuruId, talepEdilenFinansmanOrani = oran, basvuruSahibiKatkisi = 0, onBasvuruSahibiKatkisi = 0,
            talepEdilenVadeSuresiAy = _r.Sayi(5, 8) * 12, yatirimSuresiAy = yatirimSuresi, odemeSuresiAy = _r.Sayi(0, Math.Min(12, 24 - yatirimSuresi)),
            detayliFinansmanKaydi = false
        };
        Kontrol(await isKurallari.KaydetFinansAsync(finans, kullanici), "Finansman bilgileri kaydedilemedi");
    }

    private async Task ZorunluBelgeleriYukleAsync()
    {
        Basvuru b = await OkuAsync();
        foreach (BasvuruOrtaklikDosya belge in b.ZorunluBelgeler.Where(x => !x.dosyaId.HasValue && !FirmaGecmisYilKurallari.BelgeEngelli(b, ZorunluBelgeFormAd, x.dosyaNo)))
            await DosyaYukleAsync(ZorunluBelgeFormAd, belge.dosyaNo, string.IsNullOrWhiteSpace(belge.dosyaTuru) ? $"Zorunlu belge {belge.dosyaNo}" : belge.dosyaTuru);
    }

    private async Task CevreselSosyalKaydetAsync(Basvuru b, Sektor sektor)
    {
        foreach (BasvuruUygulamaAdresi adres in b.YatirimAdresleri.Where(x => x.yatirimTurleri.Any(t => t is >= 1 and <= 4)))
        {
            bool yeni = adres.yatirimTurleri.Contains((int)enumYatirimTuru.Yeni);
            bool mevcut = adres.yatirimTurleri.Any(t => t is (int)enumYatirimTuru.KapasiteArtirimi or (int)enumYatirimTuru.Modernizasyon or (int)enumYatirimTuru.TeknolojiYenileme);
            List<string> baglamlar = [];
            if (yeni) baglamlar.Add("planned");
            if (mevcut) baglamlar.Add("existing");
            JsonObject cevaplar = new();
            foreach (CevreselSosyalSoruGrubu grup in CevreselSosyalAnketTanimSaglayici.Tum)
                foreach (CevreselSosyalSoru soru in grup.Questions)
                {
                    IEnumerable<string> soruBaglamlari = soru.Scope == "global" ? new[] { "global" }
                        : (soru.Contexts ?? TumBaglamlar).Intersect(baglamlar).Intersect(grup.Scopes ?? TumBaglamlar);
                    foreach (string baglam in soruBaglamlari)
                        CevapEkle(cevaplar, $"csf_{soru.Id.Replace('.', '_')}_{baglam}", soru, b, sektor);
                }
            JsonObject json = new() { ["mode"] = yeni && mevcut ? "both" : yeni ? "planned" : "existing", ["answers"] = cevaplar, ["declaration"] = true };
            Kontrol(await isKurallari.KaydetCevreselSosyalAsync(new BasvuruCevreselSosyal { basvuruId = BasvuruId, uygulamaAdresiId = adres.id, basvuruSayfasi = false, cevreselSosyalJson = json.ToJsonString() }, kullanici),
                $"{adres.siraNo}. adresin çevresel-sosyal anketi kaydedilemedi");
        }
    }

    private void CevapEkle(JsonObject cevaplar, string onEk, CevreselSosyalSoru soru, Basvuru b, Sektor sektor)
    {
        // Belge isteyen cevaplardan kaçınılır; kapsam dışı bırakan sorular her zaman "Hayır" cevaplanır.
        string SecenekSec(IReadOnlyList<string> secenekler)
        {
            if (soru.Exclusion && secenekler.Contains("Hayır")) return "Hayır";
            List<string> uygun = secenekler.Where(x => soru.DocOn?.Contains(x) != true).ToList();
            return _r.Sec(uygun.Count > 0 ? uygun : secenekler.ToList());
        }
        string cevap;
        switch (soru.AnswerType)
        {
            case "yesno": cevap = SecenekSec(["Evet", "Hayır"]); break;
            case "select": cevap = soru.Options is { Count: > 0 } ? SecenekSec(soru.Options) : "Diğer"; break;
            case "percent": cevap = _r.Sayi(40, 100).ToString(); break;
            case "staff":
                foreach (string grup in new[] { "dogrudan", "yuklenici", "tedarikci", "gocmen" })
                    foreach (string cinsiyet in new[] { "kadin", "erkek" })
                        cevaplar[$"{onEk}_{grup}_{cinsiyet}"] = (grup == "gocmen" ? _r.Sayi(0, 2) : _r.Sayi(0, 40)).ToString();
                cevaplar[$"{onEk}_explain"] = "Personel sayıları istihdam planıyla uyumludur.";
                return;
            case "number": cevap = _r.Sayi(1, 500).ToString(); break;
            default:
                cevap = soru.Id switch
                {
                    "1.1" => b.basvuruFirma.firma.ticaretUnvani ?? "",
                    "1.2" => b.yatirim.yatirimAdi ?? "",
                    "1.3" => b.YatirimAdresleri.FirstOrDefault()?.tamAdres ?? "",
                    _ => $"{sektor.Tesis} kapsamında gerekli önlemler alınmaktadır (test cevabı)."
                };
                break;
        }
        cevaplar[$"{onEk}_answer"] = cevap;
        if (soru.AlwaysExplain || soru.ExplainOn?.Contains(cevap) == true)
            cevaplar[$"{onEk}_explain"] = "Açıklama: ilgili mevzuata uygun şekilde planlanmıştır (test verisi).";
    }

    private async Task TaahhutKaydetAsync()
    {
        JsonObject json = new();
        foreach (TaahhutBeyanTanim tanim in TaahhutBeyanTanimSaglayici.Aktifler) json[tanim.anahtar] = true;
        Kontrol(await isKurallari.KaydetTaahhutBeyanlariAsync(new BasvuruTaahhutBeyanlar { basvuruId = BasvuruId, taahhutBeyanlarJson = json.ToJsonString() }, kullanici), "Taahhüt ve beyanlar kaydedilemedi");
        Kontrol(await isKurallari.TaahhutDosyasiKaydetAsync(BasvuruId, "Taahhutname.pdf", TestVeriKaynaklari.Pdf("Taahhutname"), "Test taahhütnamesi", kullanici), "Taahhütname yüklenemedi");
    }
}
