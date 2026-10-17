using System.Text.Json;
using Microsoft.Data.SqlClient;
using TarimDonusum.IsKurallari;
using TarimDonusum.Models;
using TarimDonusum.Tablolar;

namespace TarimDonusum.TestVeri;

/// <summary>
/// TEST: İncelemedeki ön başvurunun Sistem Kontrolü ve Tamlık-Uygunluk Kontrolü sayfalarını doldurur,
/// karar sayfasına "Kabul" sonucu ve gerekçe yazar ve incelemeyi sonuçlandırır (tam başvuru kaydı oluşur).
/// Test süreci bitince TestVeri klasörü ile birlikte silinebilir.
/// </summary>
public sealed class OnBasvuruIncelemeTestVeriUretici(BasvuruIsKurallari isKurallari, IConfiguration configuration, Kullanici kullanici)
{
    private readonly TestVeriKaynaklari _r = new();
    private readonly string _baglanti = configuration.GetConnectionString("DefaultConnection") ?? "";

    public async Task<string> KabulEtAsync(int basvuruId)
    {
        Sonuc<Basvuru> okuma = await isKurallari.OkuAsync(basvuruId);
        Basvuru b = okuma.basarili && okuma.nesne != null ? okuma.nesne : throw new TestVeriHatasi("Ön başvuru okunamadı", okuma.hatalar);
        if (b.kayitTuru != enumBasvuruKayitTuru.OnBasvuru || b.durum != enumBasvuruDurum.OnBasvuruIncelemeDurumu)
            throw new TestVeriHatasi("İnceleme", ["Yalnızca incelemedeki ön başvuru kabul edilebilir."]);

        Sonuc<string> sistem = await isKurallari.SistemDenetimListesiniYenidenUretAsync(basvuruId, kullanici);
        if (!sistem.basarili) throw new TestVeriHatasi("Sistem Kontrolü kaydedilemedi", sistem.hatalar);

        await TamlikUygunlukKaydetAsync(basvuruId);

        decimal talepEur = b.HesaplananTalepEdilenFinansmanTutari.GetValueOrDefault();
        decimal kur = b.basvuruFirma.donem.onBasvuruCevrimKuru.GetValueOrDefault();
        decimal krediLimiti = Math.Round(talepEur * (kur > 0 ? kur : 1) * _r.Sayi(100, 130) / 100m / 1000) * 1000;
        Basvuru karar = new()
        {
            Id = basvuruId,
            DenetimSonucu = enumOnBasvuruDenetimSonucu.KabulEdildi,
            DenetimGerekcesi = "Test verisi: Sistem ve Tamlık-Uygunluk kontrolleri tamamlandı; ön başvuru uygun bulunmuştur.",
            BankaKrediLimiti = krediLimiti,
            BankaMaksimumAylikOdeme = krediLimiti > 0 ? Math.Round(krediLimiti / _r.Sayi(36, 96) / 100) * 100 : 0
        };
        Sonuc taslak = await isKurallari.OnBasvuruDenetimiKaydetAsync(karar, kullanici, false);
        if (!taslak.basarili) throw new TestVeriHatasi("Karar kaydedilemedi", taslak.hatalar);
        Sonuc sonuc = await isKurallari.OnBasvuruDenetimiKaydetAsync(karar, kullanici, true);
        if (!sonuc.basarili) throw new TestVeriHatasi("İnceleme sonuçlandırılamadı", sonuc.hatalar);
        return sonuc.mesaj;
    }

    /// <summary>
    /// Her soruya "Kabul" sonucunu veren cevap verilir. Uzmanın birim türü yetkisi dışında kalan sorular da
    /// doldurulsun diye liste iş kuralını atlayıp doğrudan tabloya yazılır (yalnızca test için).
    /// </summary>
    private async Task TamlikUygunlukKaydetAsync(int basvuruId)
    {
        await using SqlConnection connection = new(_baglanti);
        await connection.OpenAsync();
        List<UygunlukSorusu> sorular = await new TABUygunlukSorusu(connection).ListeleAsync(true);
        List<UzmanSonucSozlukMaddesi> maddeler = sorular.Select(soru =>
        {
            string cevap = soru.evetSonucu == "Kabul" ? "Evet" : soru.hayirSonucu == "Kabul" ? "Hayır" : "Evet";
            string sonuc = cevap == "Evet" ? soru.evetSonucu : soru.hayirSonucu;
            return new UzmanSonucSozlukMaddesi
            {
                soruId = soru.id, no = soru.siraNo, konu = soru.konu, soru = soru.soru, kaynak = soru.kaynak, birimTuru = soru.birimTuru,
                evetSonucu = soru.evetSonucu, hayirSonucu = soru.hayirSonucu, cevap = cevap, sonuc = sonuc,
                duzeltilecekBilgi = sonuc == "Düzeltme" ? "Test verisi: düzeltme gerekmiyor." : ""
            };
        }).ToList();
        if (maddeler.Count == 0) throw new TestVeriHatasi("Tamlık-Uygunluk Kontrolü", ["Tanımlarda aktif uygunluk sorusu bulunmuyor."]);
        if (!await new TABBasvuru(connection).DenetimListesiKaydetAsync(basvuruId, JsonSerializer.Serialize(maddeler), false))
            throw new TestVeriHatasi("Tamlık-Uygunluk Kontrolü", ["Başvuru inceleme aşamasında değil veya liste kaydedilemedi."]);
    }
}
