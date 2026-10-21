using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Localization;
using TarimDonusum.Models;

namespace TarimDonusum.Tablolar;

/// <summary>Yerinde Kontrol (DB 1201) başlık ve bölüm tabloları.</summary>
public class TABYerindeKontrol : TABTablo
{
    public TABYerindeKontrol(SqlConnection connection, IStringLocalizer<SharedResource>? localizer = null, SqlTransaction? transaction = null)
        : base(connection, localizer, transaction) { }

    private const string BaslikKolonlari = @"Id,BasvuruId,SiraNo,SeriNo,Durum,BaslangicTarihi,BitisTarihi,HazirBulunanlarJson,Uzman1KullaniciId,Uzman1Adi,Uzman2Adi,BirimAmiriAdi,
        YapiYorumu,MakineYorumu,DigerBulgular,CalismaKagidiSayisi,CalismaDokumaniNotu,BildirimYapildiMi,BildirimTarihi,OzetBulgularJson,BasvuruSahibiKatiliyorMu,BasvuruSahibiYorumu,
        OlusturanKullaniciId,OlusturmaTarihi,GuncellemeTarihi";

    private static List<T> JsonOku<T>(SqlDataReader r, int i)
    {
        if (r.IsDBNull(i)) return [];
        try { return JsonSerializer.Deserialize<List<T>>(r.GetString(i)) ?? []; }
        catch (JsonException) { return []; }
    }

    private static string Metin(SqlDataReader r, int i) => r.IsDBNull(i) ? "" : r.GetString(i);
    private static decimal? Ondalik(SqlDataReader r, int i) => r.IsDBNull(i) ? null : r.GetDecimal(i);
    private static bool? Mantiksal(SqlDataReader r, int i) => r.IsDBNull(i) ? null : r.GetBoolean(i);
    private static DateTime? Tarih(SqlDataReader r, int i) => r.IsDBNull(i) ? null : r.GetDateTime(i);
    private static object Db(object? deger) => deger ?? DBNull.Value;

    private static YerindeKontrol BaslikOku(SqlDataReader r) => new()
    {
        id = r.GetInt32(0), basvuruId = r.GetInt32(1), siraNo = r.GetInt32(2), seriNo = Metin(r, 3), durum = (enumYerindeKontrolDurum)r.GetInt32(4),
        baslangicTarihi = Tarih(r, 5), bitisTarihi = Tarih(r, 6), hazirBulunanlar = JsonOku<YerindeKontrolHazirBulunan>(r, 7),
        uzman1KullaniciId = r.IsDBNull(8) ? null : r.GetInt32(8), uzman1Adi = Metin(r, 9), uzman2Adi = Metin(r, 10), birimAmiriAdi = Metin(r, 11),
        yapiYorumu = Metin(r, 12), makineYorumu = Metin(r, 13), digerBulgular = Metin(r, 14), calismaKagidiSayisi = r.IsDBNull(15) ? null : r.GetInt32(15),
        calismaDokumaniNotu = Metin(r, 16), bildirimYapildiMi = Mantiksal(r, 17), bildirimTarihi = Tarih(r, 18), ozetBulgular = JsonOku<string>(r, 19),
        basvuruSahibiKatiliyorMu = Mantiksal(r, 20), basvuruSahibiYorumu = Metin(r, 21),
        olusturanKullaniciId = r.GetInt32(22), olusturmaTarihi = r.GetDateTime(23), guncellemeTarihi = Tarih(r, 24)
    };

    /// <summary>Tüm yerinde kontrollerin başlık bilgileri (liste ekranı için; bölüm satırları okunmaz).</summary>
    public async Task<List<YerindeKontrol>> ListeleAsync()
    {
        await using SqlCommand c = KomutOlustur($"SELECT {BaslikKolonlari} FROM dbo.YerindeKontrol ORDER BY BasvuruId, SiraNo;");
        await using SqlDataReader r = await c.ExecuteReaderAsync();
        List<YerindeKontrol> liste = [];
        while (await r.ReadAsync()) liste.Add(BaslikOku(r));
        return liste;
    }

    public async Task<YerindeKontrol?> OkuAsync(int id)
    {
        string sql = $@"SELECT {BaslikKolonlari} FROM dbo.YerindeKontrol WHERE Id=@Id;
            SELECT Id,UygulamaAdresiId,SiraNo,AdresAyniMi,DegerZinciriAsamalari,Notlar FROM dbo.YerindeKontrolAdres WHERE YerindeKontrolId=@Id ORDER BY SiraNo,Id;
            SELECT Id,BinaId,SiraNo,YapiAdi,MevcutDurum,KullanimAmaci,UygunHarcamaId,UzunlukBulgu,GenislikBulgu,AlanBeyan,AlanBulgu,EkBulgular,FotografNo,BilgilerDogruMu
                FROM dbo.YerindeKontrolYapi WHERE YerindeKontrolId=@Id ORDER BY SiraNo,Id;
            SELECT Id,MakineId,SiraNo,MevcutDurum,RiskliMevcutVarlik,ProjedeKullanilacak,MakineAdi,UygunHarcamaId,MiktarBeyan,MarkaModelBeyan,
                    SeriNoBeyan,KullanimAmaciBeyan,TeknikOzelliklerBeyan,FotografNo,BilgilerDogruMu
                FROM dbo.YerindeKontrolMakine WHERE YerindeKontrolId=@Id ORDER BY SiraNo,Id;
            SELECT Id,SoruKodu,Cevap,Bulgu,FotografNo,DogruMu,Referans,Yorum FROM dbo.YerindeKontrolSoru WHERE YerindeKontrolId=@Id ORDER BY SoruKodu;
            SELECT Id,SiraNo,VerilisTarihi,GecerlilikTarihi,BelgeNo,Konu,DosyaId,DosyaAdi FROM dbo.YerindeKontrolBelge WHERE YerindeKontrolId=@Id ORDER BY SiraNo,Id;
            SELECT Id,KayitNo,NeyeAit,Notlar,DosyaId,DosyaAdi FROM dbo.YerindeKontrolFotograf WHERE YerindeKontrolId=@Id ORDER BY Id;
            SELECT k.Id,k.YerindeKontrolAdresId,k.AdresKonumId,k.KonumDogruMu,k.KoordinatTuru,k.DilimOrtaMeridyen,k.OlculenEnDusukY,k.OlculenEnDusukX,k.OlculenEnYuksekY,k.OlculenEnYuksekX,k.Notlar
                FROM dbo.YerindeKontrolKonum k INNER JOIN dbo.YerindeKontrolAdres a ON a.Id=k.YerindeKontrolAdresId WHERE a.YerindeKontrolId=@Id ORDER BY k.Id;
            SELECT f.Id,f.YerindeKontrolMakineId,f.AlanKodu,f.AlanAdi,f.BeyanDeger,f.Karar,f.YeniDeger,f.Aciklama
                FROM dbo.YerindeKontrolMakineAlan f INNER JOIN dbo.YerindeKontrolMakine m ON m.Id=f.YerindeKontrolMakineId WHERE m.YerindeKontrolId=@Id ORDER BY f.Id;";
        await using SqlCommand c = KomutOlustur(sql);
        c.Parameters.AddWithValue("@Id", id);
        await using SqlDataReader r = await c.ExecuteReaderAsync();
        if (!await r.ReadAsync()) return null;
        YerindeKontrol k = BaslikOku(r);
        await r.NextResultAsync();
        while (await r.ReadAsync())
            k.adresler.Add(new()
            {
                id = r.GetInt32(0), yerindeKontrolId = id, uygulamaAdresiId = r.IsDBNull(1) ? null : r.GetInt32(1), siraNo = r.GetInt32(2), adresAyniMi = Mantiksal(r, 3),
                degerZinciriAsamalari = Metin(r, 4), notlar = Metin(r, 5)
            });
        await r.NextResultAsync();
        while (await r.ReadAsync())
            k.yapilar.Add(new()
            {
                id = r.GetInt32(0), yerindeKontrolId = id, binaId = r.IsDBNull(1) ? null : r.GetInt32(1), siraNo = r.GetInt32(2), yapiAdi = Metin(r, 3), mevcutDurum = Metin(r, 4),
                kullanimAmaci = Metin(r, 5), uygunHarcamaId = r.IsDBNull(6) ? null : r.GetInt32(6), uzunlukBulgu = Ondalik(r, 7), genislikBulgu = Ondalik(r, 8),
                alanBeyan = Ondalik(r, 9), alanBulgu = Ondalik(r, 10), ekBulgular = Metin(r, 11), fotografNo = Metin(r, 12), bilgilerDogruMu = Mantiksal(r, 13)
            });
        await r.NextResultAsync();
        while (await r.ReadAsync())
            k.makineler.Add(new()
            {
                id = r.GetInt32(0), yerindeKontrolId = id, makineId = r.IsDBNull(1) ? null : r.GetInt32(1), siraNo = r.GetInt32(2), mevcutDurum = Metin(r, 3),
                riskliMevcutVarlik = r.GetBoolean(4), projedeKullanilacak = r.GetBoolean(5), makineAdi = Metin(r, 6), uygunHarcamaId = r.IsDBNull(7) ? null : r.GetInt32(7),
                miktarBeyan = Ondalik(r, 8), markaModelBeyan = Metin(r, 9), seriNoBeyan = Metin(r, 10), kullanimAmaciBeyan = Metin(r, 11),
                teknikOzelliklerBeyan = Metin(r, 12), fotografNo = Metin(r, 13), bilgilerDogruMu = Mantiksal(r, 14)
            });
        await r.NextResultAsync();
        while (await r.ReadAsync())
            k.sorular.Add(new()
            {
                id = r.GetInt32(0), yerindeKontrolId = id, soruKodu = Metin(r, 1), cevap = Metin(r, 2), bulgu = Metin(r, 3), fotografNo = Metin(r, 4),
                dogruMu = Mantiksal(r, 5), referans = Metin(r, 6), yorum = Metin(r, 7)
            });
        await r.NextResultAsync();
        while (await r.ReadAsync())
            k.belgeler.Add(new()
            {
                id = r.GetInt32(0), yerindeKontrolId = id, siraNo = r.GetInt32(1), verilisTarihi = Tarih(r, 2), gecerlilikTarihi = Tarih(r, 3), belgeNo = Metin(r, 4),
                konu = Metin(r, 5), dosyaId = r.IsDBNull(6) ? null : r.GetInt32(6), dosyaAdi = Metin(r, 7)
            });
        await r.NextResultAsync();
        while (await r.ReadAsync())
            k.fotograflar.Add(new()
            {
                id = r.GetInt32(0), yerindeKontrolId = id, kayitNo = Metin(r, 1), neyeAit = Metin(r, 2), notlar = Metin(r, 3),
                dosyaId = r.IsDBNull(4) ? null : r.GetInt32(4), dosyaAdi = Metin(r, 5)
            });
        await r.NextResultAsync();
        while (await r.ReadAsync())
        {
            YerindeKontrolKonum konum = new()
            {
                id = r.GetInt32(0), yerindeKontrolAdresId = r.GetInt32(1), adresKonumId = r.IsDBNull(2) ? null : r.GetInt32(2), konumDogruMu = Mantiksal(r, 3),
                koordinatTuru = (enumKoordinatTuru)r.GetInt32(4), dilimOrtaMeridyen = r.IsDBNull(5) ? null : r.GetInt32(5),
                olculenEnDusukY = Ondalik(r, 6), olculenEnDusukX = Ondalik(r, 7), olculenEnYuksekY = Ondalik(r, 8), olculenEnYuksekX = Ondalik(r, 9), notlar = Metin(r, 10)
            };
            k.adresler.FirstOrDefault(x => x.id == konum.yerindeKontrolAdresId)?.konumlar.Add(konum);
        }
        await r.NextResultAsync();
        while (await r.ReadAsync())
        {
            YerindeKontrolMakineAlan alan = new()
            {
                id = r.GetInt32(0), yerindeKontrolMakineId = r.GetInt32(1), alanKodu = Metin(r, 2), alanAdi = Metin(r, 3), beyanDeger = Metin(r, 4),
                karar = (enumYerindeKontrolAlanKarar)r.GetByte(5), yeniDeger = Metin(r, 6), aciklama = Metin(r, 7)
            };
            k.makineler.FirstOrDefault(x => x.id == alan.yerindeKontrolMakineId)?.alanlar.Add(alan);
        }
        return k;
    }

    /// <summary>Yeni yerinde kontrolü (başvurudan doldurulmuş adres, yapı, makine ve soru satırlarıyla) ekler; sıra ve seri numarası burada verilir.</summary>
    public async Task<int> EkleAsync(YerindeKontrol k, string seriNoOnEki)
    {
        const string baslik = @"DECLARE @Sira INT = ISNULL((SELECT MAX(SiraNo) FROM dbo.YerindeKontrol WITH (UPDLOCK, HOLDLOCK) WHERE BasvuruId=@BasvuruId), 0) + 1;
            INSERT dbo.YerindeKontrol(BasvuruId,SiraNo,Durum,Uzman1KullaniciId,Uzman1Adi,OlusturanKullaniciId,HazirBulunanlarJson,OzetBulgularJson)
            VALUES(@BasvuruId,@Sira,1,@Uzman1KullaniciId,@Uzman1Adi,@OlusturanKullaniciId,N'[]',N'[]');
            DECLARE @Id INT = CONVERT(INT, SCOPE_IDENTITY());
            UPDATE dbo.YerindeKontrol SET SeriNo = @SeriNoOnEki + RIGHT('0000' + CONVERT(NVARCHAR(10), @Id), 4) WHERE Id=@Id;
            SELECT @Id;";
        await using SqlCommand c = KomutOlustur(baslik);
        c.Parameters.AddWithValue("@BasvuruId", k.basvuruId);
        c.Parameters.AddWithValue("@Uzman1KullaniciId", Db(k.uzman1KullaniciId));
        c.Parameters.AddWithValue("@Uzman1Adi", k.uzman1Adi ?? "");
        c.Parameters.AddWithValue("@OlusturanKullaniciId", k.olusturanKullaniciId);
        c.Parameters.AddWithValue("@SeriNoOnEki", seriNoOnEki);
        k.id = Convert.ToInt32(await c.ExecuteScalarAsync());

        foreach (YerindeKontrolAdres a in k.adresler)
        {
            await using SqlCommand ac = KomutOlustur(@"INSERT dbo.YerindeKontrolAdres(YerindeKontrolId,UygulamaAdresiId,SiraNo,DegerZinciriAsamalari)
                VALUES(@KontrolId,@AdresId,@SiraNo,@Asamalar);");
            ac.Parameters.AddWithValue("@KontrolId", k.id); ac.Parameters.AddWithValue("@AdresId", Db(a.uygulamaAdresiId)); ac.Parameters.AddWithValue("@SiraNo", a.siraNo);
            ac.Parameters.AddWithValue("@Asamalar", a.degerZinciriAsamalari ?? "");
            await ac.ExecuteNonQueryAsync();
        }
        foreach (YerindeKontrolYapi y in k.yapilar)
        {
            await using SqlCommand yc = KomutOlustur(@"INSERT dbo.YerindeKontrolYapi(YerindeKontrolId,BinaId,SiraNo,YapiAdi,MevcutDurum,KullanimAmaci,UygunHarcamaId,AlanBeyan)
                VALUES(@KontrolId,@BinaId,@SiraNo,@YapiAdi,@MevcutDurum,@KullanimAmaci,@UygunHarcamaId,@AlanBeyan);");
            yc.Parameters.AddWithValue("@KontrolId", k.id); yc.Parameters.AddWithValue("@BinaId", Db(y.binaId)); yc.Parameters.AddWithValue("@SiraNo", y.siraNo);
            yc.Parameters.AddWithValue("@YapiAdi", y.yapiAdi ?? ""); yc.Parameters.AddWithValue("@MevcutDurum", y.mevcutDurum ?? ""); yc.Parameters.AddWithValue("@KullanimAmaci", y.kullanimAmaci ?? "");
            yc.Parameters.AddWithValue("@UygunHarcamaId", Db(y.uygunHarcamaId)); yc.Parameters.AddWithValue("@AlanBeyan", Db(y.alanBeyan));
            await yc.ExecuteNonQueryAsync();
        }
        foreach (YerindeKontrolMakine m in k.makineler)
        {
            await using SqlCommand mc = KomutOlustur(@"INSERT dbo.YerindeKontrolMakine(YerindeKontrolId,MakineId,SiraNo,MevcutDurum,ProjedeKullanilacak,MakineAdi,UygunHarcamaId,
                    MiktarBeyan,MarkaModelBeyan,KullanimAmaciBeyan,TeknikOzelliklerBeyan)
                VALUES(@KontrolId,@MakineId,@SiraNo,@MevcutDurum,@Projede,@MakineAdi,@UygunHarcamaId,@MiktarBeyan,@MarkaModelBeyan,@KullanimAmaciBeyan,@TeknikBeyan);");
            mc.Parameters.AddWithValue("@KontrolId", k.id); mc.Parameters.AddWithValue("@MakineId", Db(m.makineId)); mc.Parameters.AddWithValue("@SiraNo", m.siraNo);
            mc.Parameters.AddWithValue("@MevcutDurum", m.mevcutDurum ?? ""); mc.Parameters.AddWithValue("@Projede", m.projedeKullanilacak); mc.Parameters.AddWithValue("@MakineAdi", m.makineAdi ?? "");
            mc.Parameters.AddWithValue("@UygunHarcamaId", Db(m.uygunHarcamaId)); mc.Parameters.AddWithValue("@MiktarBeyan", Db(m.miktarBeyan));
            mc.Parameters.AddWithValue("@MarkaModelBeyan", Kisalt(m.markaModelBeyan, 300)); mc.Parameters.AddWithValue("@KullanimAmaciBeyan", Kisalt(m.kullanimAmaciBeyan, 2000));
            mc.Parameters.AddWithValue("@TeknikBeyan", Kisalt(m.teknikOzelliklerBeyan, 2000));
            await mc.ExecuteNonQueryAsync();
        }
        foreach (YerindeKontrolSoru s in k.sorular)
        {
            await using SqlCommand sc = KomutOlustur("INSERT dbo.YerindeKontrolSoru(YerindeKontrolId,SoruKodu,Referans) VALUES(@KontrolId,@Kod,@Referans);");
            sc.Parameters.AddWithValue("@KontrolId", k.id); sc.Parameters.AddWithValue("@Kod", s.soruKodu); sc.Parameters.AddWithValue("@Referans", s.referans ?? "");
            await sc.ExecuteNonQueryAsync();
        }
        return k.id;
    }

    private static string Kisalt(string? metin, int uzunluk) => (metin ?? "").Length > uzunluk ? metin![..uzunluk] : metin ?? "";

    /// <summary>GB - Genel Bölüm alanlarını kaydeder; yalnızca taslak durumundaki kontrol güncellenir.</summary>
    public async Task<bool> GenelBilgilerGuncelleAsync(YerindeKontrol k)
    {
        const string sql = @"UPDATE dbo.YerindeKontrol SET BaslangicTarihi=@Baslangic, BitisTarihi=@Bitis, HazirBulunanlarJson=@Hazir,
                Uzman1Adi=@Uzman1Adi, Uzman2Adi=@Uzman2Adi, BirimAmiriAdi=@BirimAmiri, GuncellemeTarihi=SYSDATETIME()
            WHERE Id=@Id AND Durum=@Taslak;";
        await using SqlCommand c = KomutOlustur(sql);
        c.Parameters.AddWithValue("@Id", k.id);
        c.Parameters.AddWithValue("@Baslangic", Db(k.baslangicTarihi?.Date));
        c.Parameters.AddWithValue("@Bitis", Db(k.bitisTarihi?.Date));
        c.Parameters.AddWithValue("@Hazir", JsonSerializer.Serialize(k.hazirBulunanlar));
        c.Parameters.AddWithValue("@Uzman1Adi", k.uzman1Adi ?? "");
        c.Parameters.AddWithValue("@Uzman2Adi", k.uzman2Adi ?? "");
        c.Parameters.AddWithValue("@BirimAmiri", k.birimAmiriAdi ?? "");
        c.Parameters.AddWithValue("@Taslak", (int)enumYerindeKontrolDurum.Taslak);
        return await c.ExecuteNonQueryAsync() == 1;
    }

    // ---- Doğrulama bölümleri (1, 1.1, 1.2, 2): satırlar yalnızca bu kontrole aitse ve kontrol taslaksa güncellenir.
    private const string TaslakKosulu = "EXISTS(SELECT 1 FROM dbo.YerindeKontrol WHERE Id=@KontrolId AND Durum=1)";

    /// <summary>Adres cevabı ve notları güncellenir; konum satırları (adres + başvuru konumu) yoksa eklenir, varsa güncellenir.</summary>
    public async Task AdreslerGuncelleAsync(int kontrolId, IEnumerable<YerindeKontrolAdres> adresler)
    {
        foreach (YerindeKontrolAdres a in adresler)
        {
            await using SqlCommand c = KomutOlustur($@"UPDATE dbo.YerindeKontrolAdres SET AdresAyniMi=@AyniMi, Notlar=@Notlar
                WHERE Id=@Id AND YerindeKontrolId=@KontrolId AND {TaslakKosulu};");
            c.Parameters.AddWithValue("@Id", a.id); c.Parameters.AddWithValue("@KontrolId", kontrolId);
            c.Parameters.AddWithValue("@AyniMi", Db(a.adresAyniMi)); c.Parameters.AddWithValue("@Notlar", Kisalt(a.notlar, 4000));
            if (await c.ExecuteNonQueryAsync() != 1) continue;
            foreach (YerindeKontrolKonum k in a.konumlar)
            {
                await using SqlCommand kc = KomutOlustur(@"UPDATE dbo.YerindeKontrolKonum SET KonumDogruMu=@DogruMu, KoordinatTuru=@KoordinatTuru, DilimOrtaMeridyen=@Meridyen,
                        OlculenEnDusukY=@DusukY, OlculenEnDusukX=@DusukX, OlculenEnYuksekY=@YuksekY, OlculenEnYuksekX=@YuksekX, Notlar=@Notlar
                    WHERE YerindeKontrolAdresId=@AdresId AND ((@AdresKonumId IS NULL AND AdresKonumId IS NULL) OR AdresKonumId=@AdresKonumId);
                    IF @@ROWCOUNT = 0
                        INSERT dbo.YerindeKontrolKonum(YerindeKontrolAdresId,AdresKonumId,KonumDogruMu,KoordinatTuru,DilimOrtaMeridyen,OlculenEnDusukY,OlculenEnDusukX,OlculenEnYuksekY,OlculenEnYuksekX,Notlar)
                        VALUES(@AdresId,@AdresKonumId,@DogruMu,@KoordinatTuru,@Meridyen,@DusukY,@DusukX,@YuksekY,@YuksekX,@Notlar);");
                kc.Parameters.AddWithValue("@AdresId", a.id); kc.Parameters.AddWithValue("@AdresKonumId", Db(k.adresKonumId));
                kc.Parameters.AddWithValue("@DogruMu", Db(k.konumDogruMu)); kc.Parameters.AddWithValue("@KoordinatTuru", (int)k.koordinatTuru);
                kc.Parameters.AddWithValue("@Meridyen", Db(k.dilimOrtaMeridyen));
                kc.Parameters.AddWithValue("@DusukY", Db(k.olculenEnDusukY)); kc.Parameters.AddWithValue("@DusukX", Db(k.olculenEnDusukX));
                kc.Parameters.AddWithValue("@YuksekY", Db(k.olculenEnYuksekY)); kc.Parameters.AddWithValue("@YuksekX", Db(k.olculenEnYuksekX));
                kc.Parameters.AddWithValue("@Notlar", Kisalt(k.notlar, 4000));
                await kc.ExecuteNonQueryAsync();
            }
        }
    }

    public async Task YapilarGuncelleAsync(int kontrolId, IEnumerable<YerindeKontrolYapi> yapilar, string yorum)
    {
        foreach (YerindeKontrolYapi y in yapilar)
        {
            await using SqlCommand c = KomutOlustur($@"UPDATE dbo.YerindeKontrolYapi SET BilgilerDogruMu=@DogruMu, UzunlukBulgu=@Uzunluk, GenislikBulgu=@Genislik,
                    AlanBulgu=@Alan, EkBulgular=@EkBulgular, FotografNo=@Foto
                WHERE Id=@Id AND YerindeKontrolId=@KontrolId AND {TaslakKosulu};");
            c.Parameters.AddWithValue("@Id", y.id); c.Parameters.AddWithValue("@KontrolId", kontrolId);
            c.Parameters.AddWithValue("@DogruMu", Db(y.bilgilerDogruMu)); c.Parameters.AddWithValue("@Uzunluk", Db(y.uzunlukBulgu));
            c.Parameters.AddWithValue("@Genislik", Db(y.genislikBulgu)); c.Parameters.AddWithValue("@Alan", Db(y.alanBulgu));
            c.Parameters.AddWithValue("@EkBulgular", Kisalt(y.ekBulgular, 2000)); c.Parameters.AddWithValue("@Foto", Kisalt(y.fotografNo, 100));
            await c.ExecuteNonQueryAsync();
        }
        await YorumGuncelleAsync(kontrolId, "YapiYorumu", yorum);
    }

    public async Task MakinelerGuncelleAsync(int kontrolId, IEnumerable<YerindeKontrolMakine> makineler, string yorum)
    {
        foreach (YerindeKontrolMakine m in makineler)
        {
            await using SqlCommand c = KomutOlustur($@"UPDATE dbo.YerindeKontrolMakine SET BilgilerDogruMu=@DogruMu, RiskliMevcutVarlik=@Riskli, FotografNo=@Foto
                WHERE Id=@Id AND YerindeKontrolId=@KontrolId AND {TaslakKosulu};
                IF @@ROWCOUNT = 1 DELETE FROM dbo.YerindeKontrolMakineAlan WHERE YerindeKontrolMakineId=@Id;
                SELECT CONVERT(BIT, CASE WHEN EXISTS(SELECT 1 FROM dbo.YerindeKontrolMakine WHERE Id=@Id AND YerindeKontrolId=@KontrolId) AND {TaslakKosulu} THEN 1 ELSE 0 END);");
            c.Parameters.AddWithValue("@Id", m.id); c.Parameters.AddWithValue("@KontrolId", kontrolId);
            c.Parameters.AddWithValue("@DogruMu", Db(m.bilgilerDogruMu)); c.Parameters.AddWithValue("@Riskli", m.riskliMevcutVarlik);
            c.Parameters.AddWithValue("@Foto", Kisalt(m.fotografNo, 100));
            if (!Convert.ToBoolean(await c.ExecuteScalarAsync())) continue;

            // Alan kararları her kayıtta yeniden yazılır (yalnızca "Hayır" cevabında gelir).
            foreach (YerindeKontrolMakineAlan a in m.alanlar)
            {
                await using SqlCommand ac = KomutOlustur(@"INSERT dbo.YerindeKontrolMakineAlan(YerindeKontrolMakineId,AlanKodu,AlanAdi,BeyanDeger,Karar,YeniDeger,Aciklama)
                    VALUES(@MakineId,@Kod,@Ad,@Beyan,@Karar,@Yeni,@Aciklama);");
                ac.Parameters.AddWithValue("@MakineId", m.id); ac.Parameters.AddWithValue("@Kod", Kisalt(a.alanKodu, 50)); ac.Parameters.AddWithValue("@Ad", Kisalt(a.alanAdi, 300));
                ac.Parameters.AddWithValue("@Beyan", Kisalt(a.beyanDeger, 2000)); ac.Parameters.AddWithValue("@Karar", (byte)a.karar);
                ac.Parameters.AddWithValue("@Yeni", Kisalt(a.yeniDeger, 2000)); ac.Parameters.AddWithValue("@Aciklama", Kisalt(a.aciklama, 2000));
                await ac.ExecuteNonQueryAsync();
            }
        }
        await YorumGuncelleAsync(kontrolId, "MakineYorumu", yorum);
    }

    public async Task SorularGuncelleAsync(int kontrolId, IEnumerable<YerindeKontrolSoru> sorular)
    {
        foreach (YerindeKontrolSoru s in sorular)
        {
            await using SqlCommand c = KomutOlustur($@"UPDATE dbo.YerindeKontrolSoru SET Cevap=@Cevap, Bulgu=@Bulgu, FotografNo=@Foto, DogruMu=@DogruMu, Referans=@Referans, Yorum=@Yorum
                WHERE YerindeKontrolId=@KontrolId AND SoruKodu=@Kod AND {TaslakKosulu};");
            c.Parameters.AddWithValue("@KontrolId", kontrolId); c.Parameters.AddWithValue("@Kod", s.soruKodu);
            c.Parameters.AddWithValue("@Cevap", Kisalt(s.cevap, 20)); c.Parameters.AddWithValue("@Bulgu", Kisalt(s.bulgu, 4000));
            c.Parameters.AddWithValue("@Foto", Kisalt(s.fotografNo, 100)); c.Parameters.AddWithValue("@DogruMu", Db(s.dogruMu));
            c.Parameters.AddWithValue("@Referans", Kisalt(s.referans, 500)); c.Parameters.AddWithValue("@Yorum", Kisalt(s.yorum, 4000));
            await c.ExecuteNonQueryAsync();
        }
        await using SqlCommand g = KomutOlustur($"UPDATE dbo.YerindeKontrol SET GuncellemeTarihi=SYSDATETIME() WHERE Id=@KontrolId AND {TaslakKosulu};");
        g.Parameters.AddWithValue("@KontrolId", kontrolId);
        await g.ExecuteNonQueryAsync();
    }

    // ---- Metin bölümleri (3 diğer bulgular, 6 çalışma dokümanı)
    public async Task DigerBulgularGuncelleAsync(int kontrolId, string metin)
    {
        await using SqlCommand c = KomutOlustur($"UPDATE dbo.YerindeKontrol SET DigerBulgular=@Metin, GuncellemeTarihi=SYSDATETIME() WHERE Id=@KontrolId AND {TaslakKosulu};");
        c.Parameters.AddWithValue("@KontrolId", kontrolId); c.Parameters.AddWithValue("@Metin", metin ?? "");
        await c.ExecuteNonQueryAsync();
    }

    public async Task CalismaDokumaniGuncelleAsync(int kontrolId, int? sayi, string metin)
    {
        await using SqlCommand c = KomutOlustur($@"UPDATE dbo.YerindeKontrol SET CalismaKagidiSayisi=@Sayi, CalismaDokumaniNotu=@Metin, GuncellemeTarihi=SYSDATETIME()
            WHERE Id=@KontrolId AND {TaslakKosulu};");
        c.Parameters.AddWithValue("@KontrolId", kontrolId); c.Parameters.AddWithValue("@Sayi", Db(sayi)); c.Parameters.AddWithValue("@Metin", Kisalt(metin, 4000));
        await c.ExecuteNonQueryAsync();
    }

    // ---- Dosyalı bölümler (4 ek dokümanlar, 5 fotoğraf / video): satır önce eklenir, dosya kaydedilince dosya bilgisi yazılır.
    /// <summary>Yeni ek doküman satırı; sıra no kontrol içindeki en büyük sıra + 1. Kontrol taslak değilse 0 döner.</summary>
    public async Task<int> BelgeEkleAsync(int kontrolId)
    {
        await using SqlCommand c = KomutOlustur($@"IF {TaslakKosulu}
            BEGIN
                DECLARE @Sira INT = ISNULL((SELECT MAX(SiraNo) FROM dbo.YerindeKontrolBelge WITH (UPDLOCK, HOLDLOCK) WHERE YerindeKontrolId=@KontrolId), 0) + 1;
                INSERT dbo.YerindeKontrolBelge(YerindeKontrolId,SiraNo) VALUES(@KontrolId,@Sira);
                SELECT CONVERT(INT, SCOPE_IDENTITY());
            END
            ELSE SELECT 0;");
        c.Parameters.AddWithValue("@KontrolId", kontrolId);
        return Convert.ToInt32(await c.ExecuteScalarAsync());
    }

    /// <summary>Yeni fotoğraf / video satırı; kayıt no kullanılmayan ilk sayı olarak verilir. Kontrol taslak değilse 0 döner.</summary>
    public async Task<int> FotografEkleAsync(int kontrolId)
    {
        await using SqlCommand c = KomutOlustur($@"IF {TaslakKosulu}
            BEGIN
                DECLARE @No INT = (SELECT COUNT(*) FROM dbo.YerindeKontrolFotograf WITH (UPDLOCK, HOLDLOCK) WHERE YerindeKontrolId=@KontrolId) + 1;
                WHILE EXISTS(SELECT 1 FROM dbo.YerindeKontrolFotograf WHERE YerindeKontrolId=@KontrolId AND KayitNo=CONVERT(NVARCHAR(50), @No)) SET @No += 1;
                INSERT dbo.YerindeKontrolFotograf(YerindeKontrolId,KayitNo) VALUES(@KontrolId, CONVERT(NVARCHAR(50), @No));
                SELECT CONVERT(INT, SCOPE_IDENTITY());
            END
            ELSE SELECT 0;");
        c.Parameters.AddWithValue("@KontrolId", kontrolId);
        return Convert.ToInt32(await c.ExecuteScalarAsync());
    }

    private static string DosyaTablosu(string tur) => tur == YerindeKontrolDosyaTurleri.Belge ? "dbo.YerindeKontrolBelge" : "dbo.YerindeKontrolFotograf";

    public async Task DosyaBilgisiYazAsync(string tur, int kontrolId, int satirId, int dosyaId, string dosyaAdi)
    {
        await using SqlCommand c = KomutOlustur($"UPDATE {DosyaTablosu(tur)} SET DosyaId=@DosyaId, DosyaAdi=@DosyaAdi WHERE Id=@Id AND YerindeKontrolId=@KontrolId;");
        c.Parameters.AddWithValue("@Id", satirId); c.Parameters.AddWithValue("@KontrolId", kontrolId);
        c.Parameters.AddWithValue("@DosyaId", dosyaId); c.Parameters.AddWithValue("@DosyaAdi", Kisalt(dosyaAdi, 500));
        await c.ExecuteNonQueryAsync();
    }

    /// <summary>Dosyalı satırı siler; yalnızca taslak kontrolde çalışır, silinen satır sayısını döner.</summary>
    public async Task<int> DosyaliSatirSilAsync(string tur, int kontrolId, int satirId)
    {
        await using SqlCommand c = KomutOlustur($"DELETE FROM {DosyaTablosu(tur)} WHERE Id=@Id AND YerindeKontrolId=@KontrolId AND {TaslakKosulu};");
        c.Parameters.AddWithValue("@Id", satirId); c.Parameters.AddWithValue("@KontrolId", kontrolId);
        int silinen = await c.ExecuteNonQueryAsync();
        if (silinen == 1 && tur == YerindeKontrolDosyaTurleri.Belge)
        {
            // Excel'deki "No" sütunu boşluksuz kalsın diye kalan dokümanlar yeniden numaralandırılır.
            await using SqlCommand s = KomutOlustur(@"WITH n AS (SELECT SiraNo, ROW_NUMBER() OVER (ORDER BY SiraNo, Id) AS Yeni FROM dbo.YerindeKontrolBelge WHERE YerindeKontrolId=@KontrolId)
                UPDATE n SET SiraNo = Yeni WHERE SiraNo <> Yeni;");
            s.Parameters.AddWithValue("@KontrolId", kontrolId);
            await s.ExecuteNonQueryAsync();
        }
        return silinen;
    }

    public async Task BelgelerGuncelleAsync(int kontrolId, IEnumerable<YerindeKontrolBelge> belgeler)
    {
        foreach (YerindeKontrolBelge b in belgeler)
        {
            await using SqlCommand c = KomutOlustur($@"UPDATE dbo.YerindeKontrolBelge SET VerilisTarihi=@Verilis, GecerlilikTarihi=@Gecerlilik, BelgeNo=@BelgeNo, Konu=@Konu
                WHERE Id=@Id AND YerindeKontrolId=@KontrolId AND {TaslakKosulu};");
            c.Parameters.AddWithValue("@Id", b.id); c.Parameters.AddWithValue("@KontrolId", kontrolId);
            c.Parameters.AddWithValue("@Verilis", Db(b.verilisTarihi?.Date)); c.Parameters.AddWithValue("@Gecerlilik", Db(b.gecerlilikTarihi?.Date));
            c.Parameters.AddWithValue("@BelgeNo", Kisalt(b.belgeNo, 150)); c.Parameters.AddWithValue("@Konu", Kisalt(b.konu, 1000));
            await c.ExecuteNonQueryAsync();
        }
        await GuncellemeTarihiYazAsync(kontrolId);
    }

    public async Task FotograflarGuncelleAsync(int kontrolId, IEnumerable<YerindeKontrolFotograf> fotograflar)
    {
        foreach (YerindeKontrolFotograf f in fotograflar)
        {
            await using SqlCommand c = KomutOlustur($@"UPDATE dbo.YerindeKontrolFotograf SET KayitNo=@KayitNo, NeyeAit=@NeyeAit, Notlar=@Notlar
                WHERE Id=@Id AND YerindeKontrolId=@KontrolId AND {TaslakKosulu};");
            c.Parameters.AddWithValue("@Id", f.id); c.Parameters.AddWithValue("@KontrolId", kontrolId);
            c.Parameters.AddWithValue("@KayitNo", Kisalt(f.kayitNo, 50)); c.Parameters.AddWithValue("@NeyeAit", Kisalt(f.neyeAit, 1000));
            c.Parameters.AddWithValue("@Notlar", Kisalt(f.notlar, 2000));
            await c.ExecuteNonQueryAsync();
        }
        await GuncellemeTarihiYazAsync(kontrolId);
    }

    private async Task GuncellemeTarihiYazAsync(int kontrolId)
    {
        await using SqlCommand g = KomutOlustur($"UPDATE dbo.YerindeKontrol SET GuncellemeTarihi=SYSDATETIME() WHERE Id=@KontrolId AND {TaslakKosulu};");
        g.Parameters.AddWithValue("@KontrolId", kontrolId);
        await g.ExecuteNonQueryAsync();
    }

    private async Task YorumGuncelleAsync(int kontrolId, string kolon, string yorum)
    {
        await using SqlCommand c = KomutOlustur($"UPDATE dbo.YerindeKontrol SET {kolon}=@Yorum, GuncellemeTarihi=SYSDATETIME() WHERE Id=@KontrolId AND {TaslakKosulu};");
        c.Parameters.AddWithValue("@KontrolId", kontrolId); c.Parameters.AddWithValue("@Yorum", Kisalt(yorum, 4000));
        await c.ExecuteNonQueryAsync();
    }
}
