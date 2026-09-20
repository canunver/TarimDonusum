using System.Text.Json;
using Microsoft.Data.SqlClient;
using TarimDonusum.Models;
using TarimDonusum.Tablolar;

namespace TarimDonusum.IsKurallari;

public sealed class CevreselSosyalAnketYonetimIsKurallari(IConfiguration configuration, ILogger<CevreselSosyalAnketYonetimIsKurallari> logger)
{
    private readonly string _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";

    public async Task<Sonuc<object>> SayfaVerisiAsync(Kullanici? kullanici, int? surumId = null)
    {
        Sonuc<object> sonuc = new();
        if (!YetkiliMi(kullanici, sonuc)) return sonuc;
        try
        {
            await using SqlConnection connection = new(_connectionString); await connection.OpenAsync();
            List<object> surumler = [];
            await using (SqlCommand command = new("SELECT Id,SurumNo,Durum,Aciklama,YayinTarihi FROM dbo.CevreselSosyalAnketSurum ORDER BY SurumNo DESC", connection))
            await using (SqlDataReader reader = await command.ExecuteReaderAsync())
                while (await reader.ReadAsync()) surumler.Add(new { id=reader.GetInt32(0), surumNo=reader.GetInt32(1), durum=reader.GetInt32(2), aciklama=reader.GetString(3), yayinTarihi=reader.IsDBNull(4)?null:(DateTime?)reader.GetDateTime(4) });

            int seciliId = surumId ?? await SecilecekSurumIdAsync(connection);
            CevreselSosyalAnketDuzenlemeModeli? anket = seciliId > 0 ? await OkuAsync(connection, seciliId) : null;
            sonuc.nesne = new { surumler, anket };
        }
        catch (Exception ex) { logger.LogError(ex,"Çevresel-sosyal anket yönetim verisi okunamadı."); sonuc.HataEkle("Anket bilgileri okunamadı."); }
        return sonuc;
    }

    public async Task<Sonuc<int>> TaslakOlusturAsync(Kullanici? kullanici)
    {
        Sonuc<int> sonuc = new(); if (!YetkiliMi(kullanici, sonuc)) return sonuc;
        try
        {
            await using SqlConnection connection = new(_connectionString); await connection.OpenAsync();
            int mevcutTaslak = await ScalarIntAsync(connection,"SELECT ISNULL(MAX(Id),0) FROM dbo.CevreselSosyalAnketSurum WHERE Durum=0");
            if (mevcutTaslak > 0) { sonuc.nesne=mevcutTaslak; sonuc.mesaj="Mevcut taslak açıldı."; return sonuc; }
            int kaynakId = await ScalarIntAsync(connection,"SELECT ISNULL(MAX(Id),0) FROM dbo.CevreselSosyalAnketSurum WHERE Durum=1");
            if (kaynakId <= 0) { sonuc.HataEkle("Kopyalanacak yayınlanmış anket bulunamadı."); return sonuc; }
            CevreselSosyalAnketDuzenlemeModeli model = (await OkuAsync(connection,kaynakId))!;
            model.id=0; model.durum=0; model.surumNo=await ScalarIntAsync(connection,"SELECT ISNULL(MAX(SurumNo),0)+1 FROM dbo.CevreselSosyalAnketSurum");
            model.aciklama=$"Sürüm {model.surumNo} taslağı";
            sonuc.nesne=await KaydetYeniAsync(connection,model); sonuc.mesaj="Taslak oluşturuldu.";
        }
        catch(Exception ex){logger.LogError(ex,"Anket taslağı oluşturulamadı.");sonuc.HataEkle("Anket taslağı oluşturulamadı.");}
        return sonuc;
    }

    public async Task<Sonuc> KaydetAsync(CevreselSosyalAnketDuzenlemeModeli model, Kullanici? kullanici)
    {
        Sonuc sonuc=new(); if(!YetkiliMi(kullanici,sonuc))return sonuc;
        foreach(var soru in model.bolumler.SelectMany(x=>x.sorular))
            if(string.IsNullOrWhiteSpace(soru.anahtar))soru.anahtar=$"soru_{Guid.NewGuid():N}";
        Dogrula(model,sonuc); if(!sonuc.basarili)return sonuc;
        try
        {
            await using SqlConnection connection=new(_connectionString);await connection.OpenAsync();
            int durum=await ScalarIntAsync(connection,"SELECT ISNULL(MAX(Durum),-1) FROM dbo.CevreselSosyalAnketSurum WHERE Id=@Id",("@Id",model.id));
            if(durum is not 0 and not 1){sonuc.HataEkle("Yalnızca taslak veya yayındaki anket düzenlenebilir.");return sonuc;}
            if(durum==1)
            {
                HashSet<string> mevcutAnahtarlar=[];
                await using SqlCommand anahtarKomutu=new("SELECT S.Anahtar FROM dbo.CevreselSosyalAnketSoru S INNER JOIN dbo.CevreselSosyalAnketBolum B ON B.Id=S.BolumId WHERE B.AnketSurumId=@Id",connection);
                anahtarKomutu.Parameters.AddWithValue("@Id",model.id);
                await using SqlDataReader reader=await anahtarKomutu.ExecuteReaderAsync();while(await reader.ReadAsync())mevcutAnahtarlar.Add(reader.GetString(0));
                HashSet<string> gelenAnahtarlar=model.bolumler.SelectMany(x=>x.sorular).Select(x=>x.anahtar.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);
                if(!mevcutAnahtarlar.IsSubsetOf(gelenAnahtarlar)){sonuc.HataEkle("Yayındaki soruların anahtarı değiştirilemez veya soru kaldırılamaz. Soruyu kullanım dışı bırakmak için yeni bir sürüm oluşturun.");return sonuc;}
            }
            await using SqlTransaction tr=(SqlTransaction)await connection.BeginTransactionAsync();
            try
            {
                await CalistirAsync(connection,tr,"UPDATE dbo.CevreselSosyalAnketSurum SET Aciklama=@Aciklama WHERE Id=@Id",("@Aciklama",model.aciklama.Trim()),("@Id",model.id));
                await IcerigiEsitleAsync(connection,tr,model.id,model,durum==0);
                await tr.CommitAsync();
                if(durum==1)
                {
                    CevreselSosyalAnketSurumu? surum=await new TABCevreselSosyalAnket(connection).YayindakiSurumuOkuAsync();
                    if(surum==null)throw new InvalidOperationException("Güncellenen anket okunamadı.");
                    CevreselSosyalAnketTanimSaglayici.Guncelle(surum);
                }
                sonuc.mesaj=durum==1?"Yayındaki anket güncellendi.":"Anket taslağı kaydedildi.";
            } catch {await tr.RollbackAsync();throw;}
        }
        catch(Exception ex){logger.LogError(ex,"Anket taslağı kaydedilemedi. SurumId: {SurumId}",model.id);sonuc.HataEkle("Anket taslağı kaydedilemedi.");}
        return sonuc;
    }

    public async Task<Sonuc> YayinlaAsync(int id, Kullanici? kullanici)
    {
        Sonuc sonuc=new();if(!YetkiliMi(kullanici,sonuc))return sonuc;
        try
        {
            await using SqlConnection connection=new(_connectionString);await connection.OpenAsync();
            if(await ScalarIntAsync(connection,"SELECT COUNT(*) FROM dbo.CevreselSosyalAnketSurum WHERE Id=@Id AND Durum=0",("@Id",id))==0){sonuc.HataEkle("Yayınlanacak taslak bulunamadı.");return sonuc;}
            await using SqlTransaction tr=(SqlTransaction)await connection.BeginTransactionAsync();
            try{await CalistirAsync(connection,tr,"UPDATE dbo.CevreselSosyalAnketSurum SET Durum=2 WHERE Durum=1; UPDATE dbo.CevreselSosyalAnketSurum SET Durum=1,YayinTarihi=SYSDATETIME() WHERE Id=@Id",("@Id",id));await tr.CommitAsync();}catch{await tr.RollbackAsync();throw;}
            CevreselSosyalAnketSurumu? surum=await new TABCevreselSosyalAnket(connection).YayindakiSurumuOkuAsync();
            if(surum==null)throw new InvalidOperationException("Yayınlanan anket okunamadı.");
            CevreselSosyalAnketTanimSaglayici.Guncelle(surum); sonuc.mesaj="Anket yayınlandı.";
        }
        catch(Exception ex){logger.LogError(ex,"Anket yayınlanamadı. SurumId: {SurumId}",id);sonuc.HataEkle("Anket yayınlanamadı.");}
        return sonuc;
    }

    private static async Task<CevreselSosyalAnketDuzenlemeModeli?> OkuAsync(SqlConnection c,int id)
    {
        CevreselSosyalAnketDuzenlemeModeli? m=null;
        await using(SqlCommand q=new("SELECT Id,SurumNo,Durum,Aciklama FROM dbo.CevreselSosyalAnketSurum WHERE Id=@Id",c)){q.Parameters.AddWithValue("@Id",id);await using SqlDataReader r=await q.ExecuteReaderAsync();if(await r.ReadAsync())m=new(){id=r.GetInt32(0),surumNo=r.GetInt32(1),durum=r.GetInt32(2),aciklama=r.GetString(3)};}
        if(m==null)return null;
        List<(int Id,CevreselSosyalBolumDuzenlemeModeli Model)> bolumler=[];
        await using(SqlCommand q=new("SELECT Id,Kod,Baslik,SiraNo FROM dbo.CevreselSosyalAnketBolum WHERE AnketSurumId=@Id AND Aktif=1 ORDER BY SiraNo,Id",c)){q.Parameters.AddWithValue("@Id",id);await using SqlDataReader r=await q.ExecuteReaderAsync();while(await r.ReadAsync()){int bid=r.GetInt32(0);bolumler.Add((bid,new(){id=bid,kod=r.GetString(1),baslik=r.GetString(2),siraNo=r.GetInt32(3)}));}}
        foreach(var b in bolumler){await BilgileriOkuAsync(c,b.Id,b.Model);await SorulariOkuAsync(c,b.Id,b.Model);m.bolumler.Add(b.Model);} return m;
    }

    private static async Task BilgileriOkuAsync(SqlConnection c,int bolumId,CevreselSosyalBolumDuzenlemeModeli b)
    {await using SqlCommand q=new("SELECT Id,Tur,Baslik,Metin,MaddelerJson FROM dbo.CevreselSosyalAnketBilgi WHERE BolumId=@Id AND Aktif=1 ORDER BY SiraNo,Id",c);q.Parameters.AddWithValue("@Id",bolumId);await using SqlDataReader r=await q.ExecuteReaderAsync();while(await r.ReadAsync())b.bilgiler.Add(new(){id=r.GetInt32(0),tur=r.GetString(1),baslik=r.IsDBNull(2)?null:r.GetString(2),metin=r.IsDBNull(3)?null:r.GetString(3),maddeler=r.IsDBNull(4)?[]:JsonSerializer.Deserialize<List<string>>(r.GetString(4))??[]});}

    private static async Task SorulariOkuAsync(SqlConnection c,int bolumId,CevreselSosyalBolumDuzenlemeModeli b)
    {
        List<(int Id,CevreselSosyalSoruDuzenlemeModeli Model)> sorular=[];
        await using(SqlCommand q=new(@"SELECT Id,Anahtar,GorunumKodu,Baslik,Metin,CevapTuru,CevapBaglami,YapimIsindeGoster,GuncellemeIsindeGoster,ZorunluMu,MaksimumUzunluk,NotMetni,BilgiMetni,YerTutucu,KapsamDisiBirakirMi,HerZamanAciklamaIste,SiraNo,OtomatikKaynakKodu,Aktif FROM dbo.CevreselSosyalAnketSoru WHERE BolumId=@Id ORDER BY SiraNo,Id",c)){q.Parameters.AddWithValue("@Id",bolumId);await using SqlDataReader r=await q.ExecuteReaderAsync();while(await r.ReadAsync()){int sid=r.GetInt32(0);sorular.Add((sid,new(){id=sid,anahtar=r.GetString(1),gorunumKodu=r.GetString(2),baslik=r.GetString(3),metin=r.GetString(4),cevapTuru=r.GetString(5),ortak=r.GetInt32(6)==1,yapim=r.GetBoolean(7),guncelleme=r.GetBoolean(8),zorunlu=r.GetBoolean(9),maksimumUzunluk=r.IsDBNull(10)?null:r.GetInt32(10),notMetni=r.IsDBNull(11)?null:r.GetString(11),bilgiMetni=r.IsDBNull(12)?null:r.GetString(12),yerTutucu=r.IsDBNull(13)?null:r.GetString(13),kapsamDisiBirakir=r.GetBoolean(14),herZamanAciklamaIste=r.GetBoolean(15),siraNo=r.GetInt32(16),otomatikKaynakKodu=r.IsDBNull(17)?null:r.GetString(17),aktif=r.GetBoolean(18)}));}}
        foreach(var x in sorular){await using SqlCommand q=new(@"SELECT Deger,0,0,SiraNo FROM dbo.CevreselSosyalAnketSoruSecenek WHERE SoruId=@Id AND Aktif=1 UNION ALL SELECT SecenekDegeri,CASE WHEN AciklamaIstensinMi=1 THEN 1 ELSE 0 END,CASE WHEN DosyaIstensinMi=1 THEN 1 ELSE 0 END,10000+SiraNo FROM dbo.CevreselSosyalAnketSoruKosul WHERE SoruId=@Id ORDER BY SiraNo",c);q.Parameters.AddWithValue("@Id",x.Id);await using SqlDataReader r=await q.ExecuteReaderAsync();while(await r.ReadAsync()){string d=r.GetString(0);bool a=r.GetInt32(1)==1, f=r.GetInt32(2)==1;if(!a&&!f)x.Model.secenekler.Add(d);if(a)x.Model.aciklamaKosullari.Add(d);if(f)x.Model.dosyaKosullari.Add(d);}b.sorular.Add(x.Model);}
    }

    private static async Task<int> KaydetYeniAsync(SqlConnection c,CevreselSosyalAnketDuzenlemeModeli m){await using SqlTransaction tr=(SqlTransaction)await c.BeginTransactionAsync();try{await using SqlCommand q=new("INSERT dbo.CevreselSosyalAnketSurum(SurumNo,Durum,Aciklama) OUTPUT INSERTED.Id VALUES(@No,0,@Aciklama)",c,tr);q.Parameters.AddWithValue("@No",m.surumNo);q.Parameters.AddWithValue("@Aciklama",m.aciklama);int id=Convert.ToInt32(await q.ExecuteScalarAsync());await IcerigiEkleAsync(c,tr,id,m);await tr.CommitAsync();return id;}catch{await tr.RollbackAsync();throw;}}

    private static async Task IcerigiEsitleAsync(SqlConnection c,SqlTransaction tr,int surumId,CevreselSosyalAnketDuzenlemeModeli m,bool silmeyeIzinVer)
    {
        HashSet<int> mevcutBolumler=await IdleriOkuAsync(c,tr,"SELECT Id FROM dbo.CevreselSosyalAnketBolum WHERE AnketSurumId=@Id",surumId);
        HashSet<int> mevcutSorular=await IdleriOkuAsync(c,tr,"SELECT S.Id FROM dbo.CevreselSosyalAnketSoru S INNER JOIN dbo.CevreselSosyalAnketBolum B ON B.Id=S.BolumId WHERE B.AnketSurumId=@Id",surumId);
        HashSet<int> gelenBolumler=[];HashSet<int> gelenSorular=[];int bs=0;
        foreach(var b in m.bolumler.OrderBy(x=>KodSiraAnahtari(x.kod),StringComparer.OrdinalIgnoreCase))
        {
            int bolumId=b.id;
            if(bolumId>0)
            {
                if(!mevcutBolumler.Contains(bolumId))throw new InvalidOperationException("Bölüm seçili anket sürümüne ait değil.");
                await CalistirAsync(c,tr,"UPDATE dbo.CevreselSosyalAnketBolum SET Kod=@Kod,Baslik=@Baslik,SiraNo=@Sira,Aktif=1 WHERE Id=@Id",("@Kod",b.kod.Trim()),("@Baslik",b.baslik.Trim()),("@Sira",++bs),("@Id",bolumId));
            }
            else bolumId=await InsertIdAsync(c,tr,"INSERT dbo.CevreselSosyalAnketBolum(AnketSurumId,Kod,Baslik,SiraNo,Aktif) OUTPUT INSERTED.Id VALUES(@Surum,@Kod,@Baslik,@Sira,1)",("@Surum",surumId),("@Kod",b.kod.Trim()),("@Baslik",b.baslik.Trim()),("@Sira",++bs));
            gelenBolumler.Add(bolumId);
            await CalistirAsync(c,tr,"DELETE FROM dbo.CevreselSosyalAnketBilgi WHERE BolumId=@Id",("@Id",bolumId));int bi=0;
            foreach(var i in b.bilgiler)await CalistirAsync(c,tr,"INSERT dbo.CevreselSosyalAnketBilgi(BolumId,Tur,Baslik,Metin,MaddelerJson,SiraNo,Aktif) VALUES(@Bolum,@Tur,@Baslik,@Metin,@Maddeler,@Sira,1)",("@Bolum",bolumId),("@Tur",i.tur),("@Baslik",(object?)i.baslik??DBNull.Value),("@Metin",(object?)i.metin??DBNull.Value),("@Maddeler",i.maddeler.Count==0?DBNull.Value:JsonSerializer.Serialize(i.maddeler)),("@Sira",++bi));
            int ss=0;
            foreach(var s in b.sorular.OrderBy(x=>KodSiraAnahtari(x.gorunumKodu),StringComparer.OrdinalIgnoreCase))
            {
                int soruId=s.id;
                object? maks=s.maksimumUzunluk, not=s.notMetni, bilgi=s.bilgiMetni, yer=s.yerTutucu, kaynak=s.otomatikKaynakKodu;
                if(soruId>0)
                {
                    if(!mevcutSorular.Contains(soruId))throw new InvalidOperationException("Soru seçili anket sürümüne ait değil.");
                    await CalistirAsync(c,tr,@"UPDATE dbo.CevreselSosyalAnketSoru SET BolumId=@Bolum,AnketSurumId=@Surum,Anahtar=@Anahtar,GorunumKodu=@Kod,Baslik=@Baslik,Metin=@Metin,CevapTuru=@Tur,CevapBaglami=@Baglam,YapimIsindeGoster=@Yapim,GuncellemeIsindeGoster=@Guncelleme,ZorunluMu=@Zorunlu,MaksimumUzunluk=@Maks,NotMetni=@Not,BilgiMetni=@Bilgi,YerTutucu=@Yer,KapsamDisiBirakirMi=@Kapsam,HerZamanAciklamaIste=@Aciklama,OtomatikKaynakKodu=@Kaynak,SiraNo=@Sira,Aktif=@Aktif WHERE Id=@Id",("@Bolum",bolumId),("@Surum",surumId),("@Anahtar",s.anahtar.Trim()),("@Kod",s.gorunumKodu.Trim()),("@Baslik",s.baslik??""),("@Metin",s.metin.Trim()),("@Tur",s.cevapTuru),("@Baglam",s.ortak?1:2),("@Yapim",s.ortak||s.yapim),("@Guncelleme",s.ortak||s.guncelleme),("@Zorunlu",s.zorunlu),("@Maks",maks??DBNull.Value),("@Not",not??DBNull.Value),("@Bilgi",bilgi??DBNull.Value),("@Yer",yer??DBNull.Value),("@Kapsam",s.kapsamDisiBirakir),("@Aciklama",s.herZamanAciklamaIste),("@Kaynak",kaynak??DBNull.Value),("@Sira",++ss),("@Aktif",s.aktif),("@Id",soruId));
                }
                else soruId=await InsertIdAsync(c,tr,@"INSERT dbo.CevreselSosyalAnketSoru(BolumId,AnketSurumId,Anahtar,GorunumKodu,Baslik,Metin,CevapTuru,CevapBaglami,YapimIsindeGoster,GuncellemeIsindeGoster,ZorunluMu,MaksimumUzunluk,NotMetni,BilgiMetni,YerTutucu,KapsamDisiBirakirMi,HerZamanAciklamaIste,OtomatikKaynakKodu,SiraNo,Aktif) OUTPUT INSERTED.Id VALUES(@Bolum,@Surum,@Anahtar,@Kod,@Baslik,@Metin,@Tur,@Baglam,@Yapim,@Guncelleme,@Zorunlu,@Maks,@Not,@Bilgi,@Yer,@Kapsam,@Aciklama,@Kaynak,@Sira,@Aktif)",("@Bolum",bolumId),("@Surum",surumId),("@Anahtar",s.anahtar.Trim()),("@Kod",s.gorunumKodu.Trim()),("@Baslik",s.baslik??""),("@Metin",s.metin.Trim()),("@Tur",s.cevapTuru),("@Baglam",s.ortak?1:2),("@Yapim",s.ortak||s.yapim),("@Guncelleme",s.ortak||s.guncelleme),("@Zorunlu",s.zorunlu),("@Maks",maks??DBNull.Value),("@Not",not??DBNull.Value),("@Bilgi",bilgi??DBNull.Value),("@Yer",yer??DBNull.Value),("@Kapsam",s.kapsamDisiBirakir),("@Aciklama",s.herZamanAciklamaIste),("@Kaynak",kaynak??DBNull.Value),("@Sira",++ss),("@Aktif",s.aktif));
                gelenSorular.Add(soruId);await CalistirAsync(c,tr,"DELETE FROM dbo.CevreselSosyalAnketSoruSecenek WHERE SoruId=@Id; DELETE FROM dbo.CevreselSosyalAnketSoruKosul WHERE SoruId=@Id",("@Id",soruId));int n=0;
                foreach(string o in s.secenekler.Where(x=>!string.IsNullOrWhiteSpace(x)).Distinct())await CalistirAsync(c,tr,"INSERT dbo.CevreselSosyalAnketSoruSecenek(SoruId,Deger,Metin,SiraNo,Aktif) VALUES(@Soru,@Deger,@Deger,@Sira,1)",("@Soru",soruId),("@Deger",o.Trim()),("@Sira",++n));n=0;
                foreach(string d in s.aciklamaKosullari.Concat(s.dosyaKosullari).Where(x=>!string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase))await CalistirAsync(c,tr,"INSERT dbo.CevreselSosyalAnketSoruKosul(SoruId,SecenekDegeri,AciklamaIstensinMi,DosyaIstensinMi,SiraNo) VALUES(@Soru,@Deger,@Aciklama,@Dosya,@Sira)",("@Soru",soruId),("@Deger",d.Trim()),("@Aciklama",s.aciklamaKosullari.Contains(d,StringComparer.OrdinalIgnoreCase)),("@Dosya",s.dosyaKosullari.Contains(d,StringComparer.OrdinalIgnoreCase)),("@Sira",++n));
            }
        }
        if(silmeyeIzinVer){foreach(int id in mevcutSorular.Except(gelenSorular))await CalistirAsync(c,tr,"DELETE FROM dbo.CevreselSosyalAnketSoru WHERE Id=@Id",("@Id",id));foreach(int id in mevcutBolumler.Except(gelenBolumler))await CalistirAsync(c,tr,"DELETE FROM dbo.CevreselSosyalAnketBolum WHERE Id=@Id",("@Id",id));}
    }

    private static async Task<HashSet<int>> IdleriOkuAsync(SqlConnection c,SqlTransaction tr,string sql,int id){await using SqlCommand q=new(sql,c,tr);q.Parameters.AddWithValue("@Id",id);await using SqlDataReader r=await q.ExecuteReaderAsync();HashSet<int> sonuc=[];while(await r.ReadAsync())sonuc.Add(r.GetInt32(0));return sonuc;}

    private static async Task IcerigiEkleAsync(SqlConnection c,SqlTransaction tr,int surumId,CevreselSosyalAnketDuzenlemeModeli m)
    {int bs=0;foreach(var b in m.bolumler.OrderBy(x=>KodSiraAnahtari(x.kod),StringComparer.OrdinalIgnoreCase)){int bid=await InsertIdAsync(c,tr,"INSERT dbo.CevreselSosyalAnketBolum(AnketSurumId,Kod,Baslik,SiraNo,Aktif) OUTPUT INSERTED.Id VALUES(@Surum,@Kod,@Baslik,@Sira,1)",("@Surum",surumId),("@Kod",b.kod.Trim()),("@Baslik",b.baslik.Trim()),("@Sira",++bs));int bi=0;foreach(var i in b.bilgiler)await CalistirAsync(c,tr,"INSERT dbo.CevreselSosyalAnketBilgi(BolumId,Tur,Baslik,Metin,MaddelerJson,SiraNo,Aktif) VALUES(@Bolum,@Tur,@Baslik,@Metin,@Maddeler,@Sira,1)",("@Bolum",bid),("@Tur",i.tur),("@Baslik",(object?)i.baslik??DBNull.Value),("@Metin",(object?)i.metin??DBNull.Value),("@Maddeler",i.maddeler.Count==0?DBNull.Value:JsonSerializer.Serialize(i.maddeler)),("@Sira",++bi));int ss=0;foreach(var s in b.sorular.OrderBy(x=>KodSiraAnahtari(x.gorunumKodu),StringComparer.OrdinalIgnoreCase)){int sid=await InsertIdAsync(c,tr,@"INSERT dbo.CevreselSosyalAnketSoru(BolumId,AnketSurumId,Anahtar,GorunumKodu,Baslik,Metin,CevapTuru,CevapBaglami,YapimIsindeGoster,GuncellemeIsindeGoster,ZorunluMu,MaksimumUzunluk,NotMetni,BilgiMetni,YerTutucu,KapsamDisiBirakirMi,HerZamanAciklamaIste,OtomatikKaynakKodu,SiraNo,Aktif) OUTPUT INSERTED.Id VALUES(@Bolum,@Surum,@Anahtar,@Kod,@Baslik,@Metin,@Tur,@Baglam,@Yapim,@Guncelleme,@Zorunlu,@Maks,@Not,@Bilgi,@Yer,@Kapsam,@Aciklama,@Kaynak,@Sira,@Aktif)",("@Bolum",bid),("@Surum",surumId),("@Anahtar",s.anahtar.Trim()),("@Kod",s.gorunumKodu.Trim()),("@Baslik",s.baslik??""),("@Metin",s.metin.Trim()),("@Tur",s.cevapTuru),("@Baglam",s.ortak?1:2),("@Yapim",s.ortak||s.yapim),("@Guncelleme",s.ortak||s.guncelleme),("@Zorunlu",s.zorunlu),("@Maks",(object?)s.maksimumUzunluk??DBNull.Value),("@Not",(object?)s.notMetni??DBNull.Value),("@Bilgi",(object?)s.bilgiMetni??DBNull.Value),("@Yer",(object?)s.yerTutucu??DBNull.Value),("@Kapsam",s.kapsamDisiBirakir),("@Aciklama",s.herZamanAciklamaIste),("@Kaynak",(object?)s.otomatikKaynakKodu??DBNull.Value),("@Sira",++ss),("@Aktif",s.aktif));int n=0;foreach(string o in s.secenekler.Where(x=>!string.IsNullOrWhiteSpace(x)).Distinct())await CalistirAsync(c,tr,"INSERT dbo.CevreselSosyalAnketSoruSecenek(SoruId,Deger,Metin,SiraNo,Aktif) VALUES(@Soru,@Deger,@Deger,@Sira,1)",("@Soru",sid),("@Deger",o.Trim()),("@Sira",++n));n=0;foreach(string d in s.aciklamaKosullari.Concat(s.dosyaKosullari).Where(x=>!string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase))await CalistirAsync(c,tr,"INSERT dbo.CevreselSosyalAnketSoruKosul(SoruId,SecenekDegeri,AciklamaIstensinMi,DosyaIstensinMi,SiraNo) VALUES(@Soru,@Deger,@Aciklama,@Dosya,@Sira)",("@Soru",sid),("@Deger",d.Trim()),("@Aciklama",s.aciklamaKosullari.Contains(d,StringComparer.OrdinalIgnoreCase)),("@Dosya",s.dosyaKosullari.Contains(d,StringComparer.OrdinalIgnoreCase)),("@Sira",++n));}}}

    private static void Dogrula(CevreselSosyalAnketDuzenlemeModeli m,Sonuc s){if(m.id<=0)s.HataEkle("Anket sürümü seçilmelidir.");if(m.bolumler.Count==0)s.HataEkle("En az bir bölüm bulunmalıdır.");var bolumKodlari=m.bolumler.Select(x=>x.kod?.Trim()).ToList();if(bolumKodlari.Any(string.IsNullOrWhiteSpace))s.HataEkle("Her bölümün numarası olmalıdır.");if(bolumKodlari.Where(x=>!string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).Count()!=bolumKodlari.Count)s.HataEkle("Bölüm numaraları benzersiz olmalıdır.");var keys=m.bolumler.SelectMany(x=>x.sorular).Select(x=>x.anahtar?.Trim()).ToList();if(keys.Any(string.IsNullOrWhiteSpace))s.HataEkle("Her sorunun tekil anahtarı olmalıdır.");if(keys.Where(x=>!string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).Count()!=keys.Count)s.HataEkle("Soru anahtarları benzersiz olmalıdır.");var kodlar=m.bolumler.SelectMany(x=>x.sorular).Select(x=>x.gorunumKodu?.Trim()).ToList();if(kodlar.Any(string.IsNullOrWhiteSpace))s.HataEkle("Her sorunun numarası olmalıdır.");if(kodlar.Where(x=>!string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).Count()!=kodlar.Count)s.HataEkle("Soru numaraları benzersiz olmalıdır.");if(m.bolumler.Any(b=>b.sorular.Any(q=>!string.IsNullOrWhiteSpace(q.gorunumKodu)&&!q.gorunumKodu.Trim().StartsWith(b.kod.Trim()+".",StringComparison.OrdinalIgnoreCase))))s.HataEkle("Soru numarası ait olduğu bölüm numarasıyla başlamalıdır (örnek: 1.4). ");if(m.bolumler.SelectMany(x=>x.sorular).Any(x=>string.IsNullOrWhiteSpace(x.metin)))s.HataEkle("Soru metni boş olamaz.");if(m.bolumler.SelectMany(x=>x.sorular).Any(x=>!x.ortak&&!x.yapim&&!x.guncelleme))s.HataEkle("Her soru en az bir yatırım bağlamında gösterilmelidir.");}
    private static string KodSiraAnahtari(string? kod)=>string.Join(".",(kod??"").Trim().Split('.',StringSplitOptions.RemoveEmptyEntries|StringSplitOptions.TrimEntries).Select(x=>int.TryParse(x,out int n)?n.ToString("D10"):x));
    private static bool YetkiliMi(Kullanici? k,Sonuc s){if(k?.Yetkiler.Any(x=>x.Rol==KullaniciRol.SistemYoneticisi)==true)return true;s.HataEkle("Bu işlem için sistem yöneticisi yetkisi gereklidir.");return false;}
    private static async Task<int> SecilecekSurumIdAsync(SqlConnection c){int x=await ScalarIntAsync(c,"SELECT ISNULL(MAX(Id),0) FROM dbo.CevreselSosyalAnketSurum WHERE Durum=0");return x>0?x:await ScalarIntAsync(c,"SELECT ISNULL(MAX(Id),0) FROM dbo.CevreselSosyalAnketSurum WHERE Durum=1");}
    private static async Task<int> ScalarIntAsync(SqlConnection c,string sql,params (string,object)[] p){await using SqlCommand q=new(sql,c);foreach(var x in p)q.Parameters.AddWithValue(x.Item1,x.Item2);return Convert.ToInt32(await q.ExecuteScalarAsync());}
    private static async Task<int> InsertIdAsync(SqlConnection c,SqlTransaction tr,string sql,params (string,object)[] p){await using SqlCommand q=new(sql,c,tr);foreach(var x in p)q.Parameters.AddWithValue(x.Item1,x.Item2??DBNull.Value);return Convert.ToInt32(await q.ExecuteScalarAsync());}
    private static async Task CalistirAsync(SqlConnection c,SqlTransaction tr,string sql,params (string,object)[] p){await using SqlCommand q=new(sql,c,tr);foreach(var x in p)q.Parameters.AddWithValue(x.Item1,x.Item2??DBNull.Value);await q.ExecuteNonQueryAsync();}
}
