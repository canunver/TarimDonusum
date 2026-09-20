using System.Text.Json;
using Microsoft.Data.SqlClient;
using TarimDonusum.Models;
using TarimDonusum.Tablolar;

namespace TarimDonusum.IsKurallari;

public static class CevreselSosyalAnketAktarici
{
    public static async Task AktarVeYukleAsync(IConfiguration configuration, ILogger logger)
    {
        string connectionString=configuration.GetConnectionString("DefaultConnection")??"";
        if(string.IsNullOrWhiteSpace(connectionString))return;
        await using SqlConnection connection=new(connectionString);await connection.OpenAsync();
        await IlkSurumuAktarAsync(connection,logger);
        CevreselSosyalAnketSurumu? surum=await new TABCevreselSosyalAnket(connection).YayindakiSurumuOkuAsync();
        if(surum==null)throw new InvalidOperationException("Yayındaki çevresel-sosyal anket sürümü bulunamadı.");
        CevreselSosyalAnketTanimSaglayici.Guncelle(surum);
        logger.LogInformation("Çevresel-sosyal anket modeli yüklendi. SurumId: {SurumId}, SurumNo: {SurumNo}, BolumSayisi: {BolumSayisi}",surum.id,surum.surumNo,surum.gruplar.Count);
    }

    private static async Task IlkSurumuAktarAsync(SqlConnection connection,ILogger logger)
    {
        await using SqlCommand kontrol=new("SELECT COUNT(*) FROM dbo.CevreselSosyalAnketSurum;",connection);
        if(Convert.ToInt32(await kontrol.ExecuteScalarAsync())>0)return;
        await using SqlTransaction transaction=(SqlTransaction)await connection.BeginTransactionAsync();
        try
        {
            await using SqlCommand surumKomutu=new("INSERT dbo.CevreselSosyalAnketSurum(SurumNo,Durum,Aciklama,YayinTarihi) OUTPUT INSERTED.Id VALUES(1,1,N'Çevresel-sosyal anketi ilk sürüm',SYSDATETIME());",connection,transaction);
            int surumId=Convert.ToInt32(await surumKomutu.ExecuteScalarAsync());int bolumSira=0;
            foreach(CevreselSosyalSoruGrubu grup in CevreselSosyalAnketTanimlari.Tum)
            {
                await using SqlCommand bolumKomutu=new("INSERT dbo.CevreselSosyalAnketBolum(AnketSurumId,Kod,Baslik,SiraNo,Aktif) OUTPUT INSERTED.Id VALUES(@SurumId,@Kod,@Baslik,@SiraNo,1);",connection,transaction);
                bolumKomutu.Parameters.AddWithValue("@SurumId",surumId);bolumKomutu.Parameters.AddWithValue("@Kod",grup.Id);bolumKomutu.Parameters.AddWithValue("@Baslik",grup.Title);bolumKomutu.Parameters.AddWithValue("@SiraNo",++bolumSira);
                int bolumId=Convert.ToInt32(await bolumKomutu.ExecuteScalarAsync());int bilgiSira=0;
                foreach(CevreselSosyalAciklama bilgi in grup.Descriptions??[])
                {
                    await using SqlCommand c=new("INSERT dbo.CevreselSosyalAnketBilgi(BolumId,Tur,Baslik,Metin,MaddelerJson,SiraNo,Aktif) VALUES(@BolumId,@Tur,@Baslik,@Metin,@MaddelerJson,@SiraNo,1);",connection,transaction);
                    c.Parameters.AddWithValue("@BolumId",bolumId);c.Parameters.AddWithValue("@Tur",bilgi.Type);c.Parameters.AddWithValue("@Baslik",(object?)bilgi.Title??DBNull.Value);c.Parameters.AddWithValue("@Metin",(object?)bilgi.Text??DBNull.Value);c.Parameters.AddWithValue("@MaddelerJson",bilgi.Items==null?DBNull.Value:JsonSerializer.Serialize(bilgi.Items));c.Parameters.AddWithValue("@SiraNo",++bilgiSira);await c.ExecuteNonQueryAsync();
                }
                int soruSira=0;
                foreach(CevreselSosyalSoru soru in grup.Questions)
                {
                    bool ortak=string.Equals(soru.Scope,"global",StringComparison.OrdinalIgnoreCase);IReadOnlyList<string> baglamlar=soru.Contexts??["existing","planned"];
                    await using SqlCommand c=new(@"INSERT dbo.CevreselSosyalAnketSoru(BolumId,AnketSurumId,Anahtar,GorunumKodu,Baslik,Metin,CevapTuru,CevapBaglami,YapimIsindeGoster,GuncellemeIsindeGoster,ZorunluMu,MaksimumUzunluk,NotMetni,BilgiMetni,YerTutucu,KapsamDisiBirakirMi,HerZamanAciklamaIste,OtomatikKaynakKodu,SiraNo,Aktif)
                        OUTPUT INSERTED.Id VALUES(@BolumId,@SurumId,@Anahtar,@Kod,@Baslik,@Metin,@Tur,@Baglam,@Yapim,@Guncelleme,@Zorunlu,@Maksimum,@Not,@Bilgi,@YerTutucu,@KapsamDisi,@HerZaman,@OtomatikKaynak,@SiraNo,1);",connection,transaction);
                    c.Parameters.AddWithValue("@BolumId",bolumId);c.Parameters.AddWithValue("@SurumId",surumId);c.Parameters.AddWithValue("@Anahtar",soru.Id);c.Parameters.AddWithValue("@Kod",soru.Id);c.Parameters.AddWithValue("@Baslik",soru.Title);c.Parameters.AddWithValue("@Metin",soru.Text);c.Parameters.AddWithValue("@Tur",soru.AnswerType);c.Parameters.AddWithValue("@Baglam",ortak?1:2);c.Parameters.AddWithValue("@Yapim",ortak||baglamlar.Contains("planned")?1:0);c.Parameters.AddWithValue("@Guncelleme",ortak||baglamlar.Contains("existing")?1:0);c.Parameters.AddWithValue("@Zorunlu",soru.Required?1:0);c.Parameters.AddWithValue("@Maksimum",(object?)soru.MaxLength??DBNull.Value);c.Parameters.AddWithValue("@Not",(object?)soru.Note??DBNull.Value);c.Parameters.AddWithValue("@Bilgi",(object?)soru.Info??DBNull.Value);c.Parameters.AddWithValue("@YerTutucu",(object?)soru.Placeholder??DBNull.Value);c.Parameters.AddWithValue("@KapsamDisi",soru.Exclusion?1:0);c.Parameters.AddWithValue("@HerZaman",soru.AlwaysExplain?1:0);c.Parameters.AddWithValue("@OtomatikKaynak",(object?)OtomatikKaynak(soru.Id)??DBNull.Value);c.Parameters.AddWithValue("@SiraNo",++soruSira);
                    int soruId=Convert.ToInt32(await c.ExecuteScalarAsync());int secenekSira=0;
                    foreach(string secenek in soru.Options??[])
                    {await using SqlCommand sc=new("INSERT dbo.CevreselSosyalAnketSoruSecenek(SoruId,Deger,Metin,SiraNo,Aktif) VALUES(@SoruId,@Deger,@Metin,@SiraNo,1);",connection,transaction);sc.Parameters.AddWithValue("@SoruId",soruId);sc.Parameters.AddWithValue("@Deger",secenek);sc.Parameters.AddWithValue("@Metin",secenek);sc.Parameters.AddWithValue("@SiraNo",++secenekSira);await sc.ExecuteNonQueryAsync();}
                    IEnumerable<string> kosulDegerleri=(soru.ExplainOn??[]).Concat(soru.DocOn??[]).Distinct(StringComparer.OrdinalIgnoreCase);int kosulSira=0;
                    foreach(string deger in kosulDegerleri)
                    {await using SqlCommand kc=new("INSERT dbo.CevreselSosyalAnketSoruKosul(SoruId,SecenekDegeri,AciklamaIstensinMi,DosyaIstensinMi,SiraNo) VALUES(@SoruId,@Deger,@Aciklama,@Dosya,@SiraNo);",connection,transaction);kc.Parameters.AddWithValue("@SoruId",soruId);kc.Parameters.AddWithValue("@Deger",deger);kc.Parameters.AddWithValue("@Aciklama",soru.ExplainOn?.Contains(deger,StringComparer.OrdinalIgnoreCase)==true?1:0);kc.Parameters.AddWithValue("@Dosya",soru.DocOn?.Contains(deger,StringComparer.OrdinalIgnoreCase)==true?1:0);kc.Parameters.AddWithValue("@SiraNo",++kosulSira);await kc.ExecuteNonQueryAsync();}
                }
            }
            await transaction.CommitAsync();logger.LogInformation("Kod içi çevresel-sosyal anket soruları veritabanına ilk sürüm olarak aktarıldı.");
        }
        catch{await transaction.RollbackAsync();throw;}
    }

    private static string? OtomatikKaynak(string soruId)=>soruId switch
    {
        "1.1"=>"Firma.TicaretUnvani", "1.2"=>"Yatirim.Adi", "1.3"=>"Yatirim.Adresleri",
        "1.4"=>"Yatirim.Turleri", "1.5"=>"Yatirim.OzetVeGerekce", "2.2"=>"Yatirim.HarcamaTurleri",
        "6.1"=>"Yatirim.KullanimHakkiBelgesiDurumu", "6.2"=>"Yatirim.AraziStatusleri", _=>null
    };
}

public static class CevreselSosyalAnketTanimSaglayici
{
    private static CevreselSosyalAnketSurumu? _yayindakiSurum;
    public static CevreselSosyalAnketSurumu YayindakiSurum => _yayindakiSurum ?? throw new InvalidOperationException("Çevresel-sosyal anket modeli henüz yüklenmedi.");
    public static IReadOnlyList<CevreselSosyalSoruGrubu> Tum => YayindakiSurum.gruplar;
    internal static void Guncelle(CevreselSosyalAnketSurumu surum)=>_yayindakiSurum=surum;
}
