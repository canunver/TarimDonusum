using System.Globalization;
using TarimDonusum.Araclar;
using TarimDonusum.Models;

namespace TarimDonusum.Raporlar;

/// <summary>
/// Yerinde Kontrol (DB 1201): seçilen bölümün sayfasını şablondaki biçimiyle doldurup tek sayfalık Excel olarak verir.
/// Şablon: Sablonlar/YerindeKontrol1201.xlsx (DB 1201 V1.0.00 kontrol listesinin makrosuz kopyası).
/// </summary>
public sealed class RPYK_YerindeKontrol(string uygulamaRootPath)
{
    private const string SablonAdi = "YerindeKontrol1201.xlsx";
    /// <summary>Şablondaki sayfa sırası; bölüm kodları sayfa adlarıyla aynıdır.</summary>
    private static readonly string[] Sayfalar = ["GB", "1", "1.1", "1.2", "2", "3", "4", "5", "6", "7"];
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");

    private Tablo _t = null!;

    public RaporDosyasi Olustur(YerindeKontrolFormModel model, string bolum)
    {
        string sablon = Path.Combine(uygulamaRootPath, "Sablonlar", SablonAdi);
        if (!File.Exists(sablon)) throw new FileNotFoundException($"Rapor şablonu bulunamadı: {SablonAdi}", sablon);
        int sayfa = Array.IndexOf(Sayfalar, bolum);
        if (sayfa < 0) throw new ArgumentException("Geçersiz yerinde kontrol bölümü.", nameof(bolum));

        string gecici = Path.Combine(Path.GetTempPath(), $"yerinde-kontrol-{Guid.NewGuid():N}.xlsx");
        try
        {
            _t = OrtakFonksiyonlar.NewTablo();
            _t.DosyaAc(sablon, gecici);
            for (int i = _t.SheetSayisi() - 1; i >= 0; i--)
                if (i != sayfa) _t.SheetSil(i);
            _t.AktifSheetDegistir(0);
            BaslikYaz(model, bolum);
            switch (bolum)
            {
                case "GB": GenelBolum(model); break;
                case "1": Adresler(model); break;
                case "1.1": Yapilar(model.kontrol); break;
                case "1.2": Makineler(model.kontrol); break;
                case "2": Sorular(model.kontrol); break;
                case "3": DigerBulgular(model.kontrol); break;
                case "4": Belgeler(model.kontrol); break;
                case "5": Fotograflar(model.kontrol); break;
                case "6": CalismaDokumani(model.kontrol); break;
                case "7": Ozet(model); break;
            }
            _t.DosyaSaklaTamYol();
            _t.DosyaKapat();
            string seri = string.Concat(model.kontrol.seriNo.Select(c => char.IsLetterOrDigit(c) ? c : '-'));
            return new RaporDosyasi(File.ReadAllBytes(gecici), $"YerindeKontrol-{seri}-{bolum}.xlsx");
        }
        finally
        {
            if (File.Exists(gecici)) File.Delete(gecici);
        }
    }

    // ---- Yardımcılar: "AE9" gibi Excel adresleriyle yazım (Tablo 0 tabanlı satır/sütun kullanır).
    private static (int satir, int sutun) Adres(string adres)
    {
        int i = 0, sutun = 0;
        while (i < adres.Length && char.IsLetter(adres[i])) sutun = sutun * 26 + (char.ToUpperInvariant(adres[i++]) - 'A' + 1);
        return (int.Parse(adres[i..]) - 1, sutun - 1);
    }

    private void Yaz(string adres, string? deger, int satirKaydir = 0)
    {
        (int s, int c) = Adres(adres);
        _t.HucreDegerYaz(s + satirKaydir, c, deger ?? "");
    }

    private void Yaz(string adres, decimal? deger, int satirKaydir = 0)
    {
        (int s, int c) = Adres(adres);
        if (deger.HasValue) _t.HucreDegerYaz(s + satirKaydir, c, deger.Value);
        else _t.HucreDegerYaz(s + satirKaydir, c, "");
    }

    private void Isaret(string adres, bool isaretli, int satirKaydir = 0) => Yaz(adres, isaretli ? "X" : "", satirKaydir);
    private static string T(DateTime? tarih) => tarih?.ToString("dd.MM.yyyy", Tr) ?? "…/…/…";

    /// <summary>
    /// Tablo şablondaki satır sayısından uzunsa son veri satırı (biçimi ve birleşik hücreleriyle) altına kopyalanarak satır eklenir;
    /// eklenen satır sayısı döner.
    /// </summary>
    private int SatirAyir(int ilkSatir, int sablonSatirSayisi, int kayitSayisi, int sonSutun)
    {
        int ek = Math.Max(0, kayitSayisi - sablonSatirSayisi);
        int son = ilkSatir + sablonSatirSayisi - 1;
        for (int i = 1; i <= ek; i++)
        {
            _t.SatirAc(0, son + i, 1);
            _t.HucreKopyala(0, son, 0, son, sonSutun, 0, son + i, 0);
        }
        return ek;
    }

    private static string DegerZinciri(Basvuru b) => b.yatirim.degerZinciriAsamalari.Concat(b.YatirimAdresleri.SelectMany(x => x.degerZinciriAsamalari))
        .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.dz.ad))?.dz.ad ?? (b.basvuruFirma.degerZinciriId?.ToString() ?? "");

    private static string AltDegerZinciri(Basvuru b) => string.Join(", ", b.YatirimAdresleri.SelectMany(x => x.degerZinciriAsamalari).Select(x => x.ad)
        .Where(x => !string.IsNullOrWhiteSpace(x)).Distinct());

    private static string AdresMetni(BasvuruUygulamaAdresi a) => $"{a.ilAdi} / {a.ilceAdi} - {a.tamAdres}";

    private void BaslikYaz(YerindeKontrolFormModel m, string bolum)
    {
        Basvuru b = m.basvuru;
        string il = b.basvuruFirma.il.kod > 0 ? b.basvuruFirma.il.kod.ToString("00") : "", dz = DegerZinciri(b), alt = AltDegerZinciri(b);
        string ayYil = (m.kontrol.baslangicTarihi ?? DateTime.Today).ToString("MM'/'yyyy", Tr);
        // İl kodu, değer zinciri, alt değer zinciri, seri no, ay/yıl hücreleri ve "1201/..." üst bilgisi (sayfaya göre).
        (string[] hucreler, string ust) = bolum switch
        {
            "GB" => (new[] { "A5", "E5", "K5", "O5", "S5" }, "N2"),
            "1" => (new[] { "A5", "D5", "H5", "M5", "R5" }, "A2"),
            "1.1" => (new[] { "A5", "B5", "C5", "E5", "R5" }, "A2"),
            "1.2" => (new[] { "A5", "D5", "G5", "S5", "AK5" }, "A2"),
            "2" => (new[] { "A5", "G5", "P5", "W5", "AC5" }, "S2"),
            _ => (new[] { "A5", "E5", "K5", "O5", "S5" }, "M2")
        };
        string[] degerler = [il, dz, alt, m.kontrol.seriNo, ayYil];
        for (int i = 0; i < hucreler.Length; i++) Yaz(hucreler[i], degerler[i]);
        Yaz(ust, ust is "A2" ? $"{YerindeKontrol.FormKodu}/{dz}" : dz);
    }

    private void GenelBolum(YerindeKontrolFormModel m)
    {
        Basvuru b = m.basvuru; YerindeKontrol k = m.kontrol;
        BasvuruAdliSicilKisi? yetkili = b.AdliSicilKisileri.FirstOrDefault();
        Yaz("J8", DegerZinciri(b));
        Yaz("J9", AltDegerZinciri(b));
        Yaz("H10", b.yatirim.yatirimAdi);
        Yaz("P11", b.basvuruFirma.firma.ticaretUnvani);
        Yaz("P12", b.basvuruFirma.firma.vergiKimlikNo);
        Yaz("P13", yetkili != null ? $"{yetkili.ad} {yetkili.soyad}" : b.irtibat.kisi);
        Yaz("P14", yetkili?.tckn);
        Yaz("H15", b.irtibat.adres);
        Yaz("H16", string.Join("; ", b.YatirimAdresleri.OrderBy(x => x.siraNo).Select(x => $"{x.siraNo}. {AdresMetni(x)}")));
        Yaz("H17", b.irtibat.telefon);
        Yaz("H18", string.Join(", ", k.hazirBulunanlar.Select(x => x.adSoyad)));
        Yaz("P18", "Telefon Numaraları: " + string.Join(", ", k.hazirBulunanlar.Select(x => x.telefon).Where(x => !string.IsNullOrWhiteSpace(x))));
        Yaz("H19", $"Başlangıç: {T(k.baslangicTarihi)}        Bitiş: {T(k.bitisTarihi)}");
        Yaz("A21", k.uzman1Adi);
        Yaz("H21", k.hazirBulunanlar.FirstOrDefault()?.adSoyad);
        Yaz("Q21", k.uzman2Adi);
    }

    private void Adresler(YerindeKontrolFormModel m)
    {
        // Tablo 1 bir bloktur (9.-18. satırlar); her adresin her konumu (ada/parsel) için blok kopyalanır.
        // Başvuruda konumu olmayan adres için tek blok açılır.
        const int blokBasi = 8, blokBoyu = 10;
        List<(YerindeKontrolAdres adres, BasvuruUygulamaAdresi? basvuruAdresi, BasvuruUygulamaAdresiKonum? konum, int konumNo, int konumSayisi)> bloklar = [];
        foreach (YerindeKontrolAdres a in m.kontrol.adresler.OrderBy(x => x.siraNo))
        {
            BasvuruUygulamaAdresi? adres = m.basvuru.YatirimAdresleri.FirstOrDefault(x => x.id == a.uygulamaAdresiId);
            List<BasvuruUygulamaAdresiKonum> konumlar = adres?.konumlar ?? [];
            if (konumlar.Count == 0) bloklar.Add((a, adres, null, 0, 0));
            for (int i = 0; i < konumlar.Count; i++) bloklar.Add((a, adres, konumlar[i], i + 1, konumlar.Count));
        }
        for (int i = 1; i < bloklar.Count; i++)
        {
            _t.SatirAc(0, blokBasi + i * blokBoyu, blokBoyu);
            _t.HucreKopyala(0, blokBasi, 0, blokBasi + blokBoyu - 1, 19, 0, blokBasi + i * blokBoyu, 0);
        }
        for (int i = 0; i < bloklar.Count; i++)
        {
            (YerindeKontrolAdres a, BasvuruUygulamaAdresi? adres, BasvuruUygulamaAdresiKonum? bk, int konumNo, int konumSayisi) = bloklar[i];
            int o = i * blokBoyu;
            YerindeKontrolKonum? kk = a.konumlar.FirstOrDefault(x => x.adresKonumId == bk?.id);
            bool konumYanlis = kk?.konumDogruMu == false;
            enumKoordinatTuru tur = konumYanlis ? kk!.koordinatTuru : bk?.koordinatTuru ?? enumKoordinatTuru.ITRF96;
            string konumMetni = bk == null ? "" : $" – {konumNo}/{konumSayisi}. konum: {bk.mahalle} {bk.ada}/{bk.parsel}";
            Isaret("R10", a.adresAyniMi == true, o);
            Isaret("T10", a.adresAyniMi == false, o);
            Yaz("A11", "Adresteki Değer Zinciri Aşaması: " + (string.IsNullOrWhiteSpace(a.degerZinciriAsamalari) ? "-" : a.degerZinciriAsamalari), o);
            Yaz("M11", (tur == enumKoordinatTuru.ITRF96 ? "X " : "") + "ITRF", o);
            Yaz("O11", (tur == enumKoordinatTuru.ED50 ? "X " : "") + "ED50", o);
            Yaz("Q11", $"Kullanılan Dilim Orta Meridyen: {(konumYanlis ? kk!.dilimOrtaMeridyen : null)}", o);
            Yaz("A12", $"Yatırımın Uygulama Adresi: {a.siraNo}. {(adres == null ? "" : AdresMetni(adres))}{konumMetni}", o);
            // Başvuruda konumlar enlem/boylam (derece) olarak girilir: Y = boylam, X = enlem.
            Yaz("G14", bk?.minBoylam, o);
            Yaz("J14", bk?.minEnlem, o);
            Yaz("G16", bk?.maxBoylam, o);
            Yaz("J16", bk?.maxEnlem, o);
            Yaz("M15", konumYanlis ? kk!.olculenEnDusukY : null, o);
            Yaz("Q15", konumYanlis ? kk!.olculenEnDusukX : null, o);
            Yaz("M17", konumYanlis ? kk!.olculenEnYuksekY : null, o);
            Yaz("Q17", konumYanlis ? kk!.olculenEnYuksekX : null, o);
            string notlar = string.Join(" ", new[] { a.notlar, kk?.konumDogruMu == true ? "GPS koordinatları doğru." : kk?.notlar }.Where(x => !string.IsNullOrWhiteSpace(x)));
            Yaz("A18", "NOTLAR: (Yerinde yapılan denetimin adresi, yatırımın gerçekleştirildiği adresle farklıysa, gözlemlenen adresi yazınız) " + notlar, o);
        }
    }

    private void Yapilar(YerindeKontrol k)
    {
        // Tablo 2.1: 11.-16. satırlar (6 satır).
        List<YerindeKontrolYapi> liste = k.yapilar.OrderBy(x => x.siraNo).ToList();
        int ek = SatirAyir(10, 6, liste.Count, 27);
        for (int i = 0; i < liste.Count; i++)
        {
            YerindeKontrolYapi y = liste[i];
            bool mevcut = string.Equals(y.mevcutDurum, "Mevcut", StringComparison.OrdinalIgnoreCase);
            Yaz("A11", mevcut ? y.yapiAdi : "", i);
            Yaz("B11", y.kullanimAmaci, i);
            Yaz("C11", mevcut ? "" : y.yapiAdi, i);
            Yaz("D11", UygunHarcamaTanimSaglayici.Etiket(y.uygunHarcamaId), i);
            Yaz("E11", y.uzunlukBulgu, i);
            Yaz("I11", y.genislikBulgu, i);
            Yaz("M11", y.alanBeyan, i);
            // Uzman bilgileri doğru bulduysa bulgu sütunlarında beyan edilen değer gösterilir.
            bool dogru = y.bilgilerDogruMu == true;
            Yaz("Q11", dogru ? y.alanBeyan : y.alanBulgu, i);
            Yaz("U11", dogru ? "Bilgiler doğru" : y.ekBulgular, i);
            Yaz("Y11", y.fotografNo, i);
        }
        Yaz("A17", "YORUMLAR: " + k.yapiYorumu, ek);
    }

    private void Makineler(YerindeKontrol k)
    {
        // Tablo 2.2: 10.-14. satırlar (5 satır); her alan için beyan ve yerinde kontrol bulgusu.
        List<YerindeKontrolMakine> liste = k.makineler.OrderBy(x => x.siraNo).ToList();
        int ek = SatirAyir(9, 5, liste.Count, 37);
        for (int i = 0; i < liste.Count; i++)
        {
            YerindeKontrolMakine m = liste[i];
            bool mevcut = string.Equals(m.mevcutDurum, "Mevcut", StringComparison.OrdinalIgnoreCase);
            Yaz("A10", mevcut ? m.makineAdi : "", i);
            Isaret("B10", m.riskliMevcutVarlik, i);
            Isaret("C10", m.projedeKullanilacak, i);
            Yaz("D10", mevcut ? "" : m.makineAdi, i);
            Yaz("E10", UygunHarcamaTanimSaglayici.Etiket(m.uygunHarcamaId), i);
            Yaz("F10", m.riskliMevcutVarlik ? m.teknikOzelliklerBeyan : "", i);
            // Uzman bilgileri doğru bulduysa bulgu sütunlarında beyan edilen değer gösterilir.
            bool dogru = m.bilgilerDogruMu == true;
            Yaz("G10", m.miktarBeyan, i); Yaz("J10", dogru ? m.miktarBeyan : m.miktarBulgu, i);
            Yaz("M10", m.markaModelBeyan, i); Yaz("P10", dogru ? m.markaModelBeyan : m.markaModelBulgu, i);
            Yaz("S10", m.seriNoBeyan, i); Yaz("V10", dogru ? m.seriNoBeyan : m.seriNoBulgu, i);
            Yaz("Y10", m.kullanimAmaciBeyan, i); Yaz("AB10", dogru ? m.kullanimAmaciBeyan : m.kullanimAmaciBulgu, i);
            Yaz("AE10", m.teknikOzelliklerBeyan, i); Yaz("AH10", dogru ? m.teknikOzelliklerBeyan : m.teknikOzelliklerBulgu, i);
            Yaz("AK10", m.fotografNo, i);
        }
        Yaz("A15", "YORUMLAR: " + k.makineYorumu, ek);
    }

    private void Sorular(YerindeKontrol k)
    {
        // 3.1: kutular 9. satır, bulgular 14. satır, yorum 25. satır; 3.2: kutular 28., bulgular 33., yorum 39. satır.
        (string kod, int kutu, int bulgu, int yorum)[] yerler = [("3.1", 9, 14, 25), ("3.2", 28, 33, 39)];
        foreach ((string kod, int kutu, int bulgu, int yorum) in yerler)
        {
            YerindeKontrolSoru? s = k.sorular.FirstOrDefault(x => x.soruKodu == kod);
            if (s == null) continue;
            Isaret($"AE{kutu}", s.cevap == "Evet");
            Isaret($"AH{kutu}", s.cevap == "Hayır");
            Yaz($"Q{bulgu}", s.bulgu);
            Yaz($"X{bulgu}", s.fotografNo);
            Isaret($"AA{bulgu}", s.dogruMu == true);
            Isaret($"AD{bulgu}", s.dogruMu == false);
            if (!string.IsNullOrWhiteSpace(s.referans)) Yaz($"AG{bulgu}", s.referans);
            Yaz($"A{yorum}", "YORUMLAR: " + s.yorum);
        }
    }

    private void DigerBulgular(YerindeKontrol k)
    {
        _t.HucreBirlestir(6, 0, 21, 22);
        Yaz("A7", k.digerBulgular);
        _t.HucreMetniKaydir(6, 0, true);
        Yaz("I24", k.uzman1Adi); Yaz("T24", T(k.bitisTarihi ?? k.baslangicTarihi));
        Yaz("I25", k.uzman2Adi); Yaz("T25", T(k.bitisTarihi ?? k.baslangicTarihi));
        Yaz("P28", k.birimAmiriAdi);
    }

    private void Belgeler(YerindeKontrol k)
    {
        // Kısım 4: 8.-21. satırlar (14 satır).
        List<YerindeKontrolBelge> liste = k.belgeler.OrderBy(x => x.siraNo).ToList();
        SatirAyir(7, 14, liste.Count, 22);
        for (int i = 0; i < liste.Count; i++)
        {
            YerindeKontrolBelge d = liste[i];
            Yaz("A8", d.siraNo.ToString(), i); Yaz("C8", d.verilisTarihi?.ToString("dd.MM.yyyy", Tr), i); Yaz("F8", d.gecerlilikTarihi?.ToString("dd.MM.yyyy", Tr), i);
            Yaz("K8", d.belgeNo, i); Yaz("Q8", d.konu, i);
        }
    }

    private void Fotograflar(YerindeKontrol k)
    {
        // Kısım 5: 8.-21. satırlar (14 satır); koordinat varsa notlara eklenir.
        List<YerindeKontrolFotograf> liste = k.fotograflar.ToList();
        SatirAyir(7, 14, liste.Count, 22);
        for (int i = 0; i < liste.Count; i++)
        {
            YerindeKontrolFotograf f = liste[i];
            string koordinat = f.koordinatY.HasValue || f.koordinatX.HasValue ? $" (Y: {f.koordinatY?.ToString(Tr)}, X: {f.koordinatX?.ToString(Tr)})" : "";
            Yaz("A8", f.kayitNo, i); Yaz("E8", f.neyeAit, i); Yaz("K8", f.notlar + koordinat, i);
        }
    }

    private void CalismaDokumani(YerindeKontrol k)
    {
        Yaz("A7", $"Çalışma Kağıdı Sayısı: {k.calismaKagidiSayisi}\n*Gerekli olması halinde sayfa çoğaltılabilir.\n\n{k.calismaDokumaniNotu}");
        _t.HucreMetniKaydir(6, 0, true);
    }

    private void Ozet(YerindeKontrolFormModel m)
    {
        Basvuru b = m.basvuru; YerindeKontrol k = m.kontrol;
        BasvuruAdliSicilKisi? yetkili = b.AdliSicilKisileri.FirstOrDefault();
        List<BasvuruUygulamaAdresi> adresler = b.YatirimAdresleri.OrderBy(x => x.siraNo).ToList();
        for (int i = 0; i < Math.Min(5, adresler.Count); i++)
            Yaz("G10", i == 4 && adresler.Count > 5 ? string.Join("; ", adresler.Skip(4).Select(x => $"{x.siraNo}. {AdresMetni(x)}")) : $"{adresler[i].siraNo}. {AdresMetni(adresler[i])}", i);
        Yaz("P15", b.basvuruFirma.firma.ticaretUnvani);
        Yaz("P16", b.basvuruFirma.firma.vergiKimlikNo);
        Yaz("P17", b.irtibat.telefon);
        Yaz("P18", yetkili != null ? $"{yetkili.ad} {yetkili.soyad}" : b.irtibat.kisi);
        Yaz("P19", yetkili?.tckn);
        Yaz("K20", $"{T(k.baslangicTarihi)}   ,   {T(k.bitisTarihi)}");
        Yaz("A22", (k.bildirimYapildiMi == true ? "X " : "") + YerindeKontrolTanimlari.BildirimMetni);
        for (int i = 0; i < Math.Min(7, k.ozetBulgular.Count); i++) Yaz("C24", k.ozetBulgular[i], i);
        Isaret("E33", k.basvuruSahibiKatiliyorMu == true);
        Isaret("E38", k.basvuruSahibiKatiliyorMu == false);
        Yaz("G32", "Yorumlar:\n" + k.basvuruSahibiYorumu);
        _t.HucreMetniKaydir(31, 6, true);
        Yaz("A46", k.uzman1Adi);
        Yaz("I46", k.hazirBulunanlar.FirstOrDefault()?.adSoyad);
        Yaz("P46", k.uzman2Adi);
    }
}
