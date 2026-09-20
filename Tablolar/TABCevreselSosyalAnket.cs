using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Localization;
using TarimDonusum.Araclar;
using TarimDonusum.IsKurallari;
using TarimDonusum.Models;

namespace TarimDonusum.Tablolar;

public sealed class TABCevreselSosyalAnket : TABTablo
{
    public TABCevreselSosyalAnket(SqlConnection connection, IStringLocalizer<SharedResource>? localizer = null, SqlTransaction? transaction = null)
        : base(connection, localizer, transaction) { }

    public async Task<CevreselSosyalAnketSurumu?> YayindakiSurumuOkuAsync()
    {
        const string sql = @"
            SELECT TOP (1) Id,SurumNo,Durum,ISNULL(Aciklama,N''),YayinTarihi
            FROM dbo.CevreselSosyalAnketSurum
            WHERE Durum=1
            ORDER BY SurumNo DESC;";
        await using SqlCommand command = KomutOlustur(sql);
        await using SqlDataReader reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;
        CevreselSosyalAnketSurumu surum = new()
        {
            id = reader.GetInt32(0),
            surumNo = reader.GetInt32(1),
            durum = (enumCevreselSosyalAnketSurumDurumu)OrtakFonksiyonlar.Int32Yap(reader.GetValue(2)),
            aciklama = reader.GetString(3),
            yayinTarihi = reader.IsDBNull(4) ? null : reader.GetDateTime(4)
        };
        await reader.CloseAsync();
        surum.gruplar = await GruplariOkuAsync(surum.id);
        return surum;
    }

    private async Task<List<CevreselSosyalSoruGrubu>> GruplariOkuAsync(int surumId)
    {
        const string sql = @"
            SELECT Id,Kod,Baslik
            FROM dbo.CevreselSosyalAnketBolum
            WHERE AnketSurumId=@SurumId AND Aktif=1
            ORDER BY SiraNo,Id;";
        await using SqlCommand command = KomutOlustur(sql);
        command.Parameters.AddWithValue("@SurumId", surumId);
        await using SqlDataReader reader = await command.ExecuteReaderAsync();
        List<(int Id,string Kod,string Baslik)> bolumler=[];
        while (await reader.ReadAsync()) bolumler.Add((reader.GetInt32(0),reader.GetString(1),reader.GetString(2)));
        await reader.CloseAsync();

        List<CevreselSosyalSoruGrubu> sonuc=[];
        foreach (var bolum in bolumler)
            sonuc.Add(new(bolum.Kod,bolum.Baslik,await SorulariOkuAsync(bolum.Id),await AciklamalariOkuAsync(bolum.Id)));
        return sonuc;
    }

    private async Task<IReadOnlyList<CevreselSosyalSoru>> SorulariOkuAsync(int bolumId)
    {
        const string sql = @"
            SELECT Id,Anahtar,GorunumKodu,Baslik,Metin,CevapTuru,CevapBaglami,YapimIsindeGoster,GuncellemeIsindeGoster,
                   ZorunluMu,MaksimumUzunluk,NotMetni,BilgiMetni,YerTutucu,KapsamDisiBirakirMi,HerZamanAciklamaIste,OtomatikKaynakKodu
            FROM dbo.CevreselSosyalAnketSoru
            WHERE BolumId=@BolumId AND Aktif=1
            ORDER BY SiraNo,Id;";
        await using SqlCommand command = KomutOlustur(sql);
        command.Parameters.AddWithValue("@BolumId", bolumId);
        await using SqlDataReader reader = await command.ExecuteReaderAsync();
        List<(int Id,string Anahtar,string Kod,string Baslik,string Metin,string Tur,int Baglam,bool Yapim,bool Guncelleme,bool Zorunlu,int? Maksimum,string? Not,string? Bilgi,string? YerTutucu,bool KapsamDisi,bool HerZaman,string? OtomatikKaynak)> satirlar=[];
        while (await reader.ReadAsync()) satirlar.Add((reader.GetInt32(0),reader.GetString(1),reader.GetString(2),reader.GetString(3),reader.GetString(4),reader.GetString(5),reader.GetInt32(6),reader.GetBoolean(7),reader.GetBoolean(8),reader.GetBoolean(9),reader.IsDBNull(10)?null:reader.GetInt32(10),reader.IsDBNull(11)?null:reader.GetString(11),reader.IsDBNull(12)?null:reader.GetString(12),reader.IsDBNull(13)?null:reader.GetString(13),reader.GetBoolean(14),reader.GetBoolean(15),reader.IsDBNull(16)?null:reader.GetString(16)));
        await reader.CloseAsync();

        List<CevreselSosyalSoru> sonuc=[];
        foreach (var x in satirlar)
        {
            (IReadOnlyList<string> secenekler,IReadOnlyList<string> aciklama,IReadOnlyList<string> dosya)=await SoruDetaylariniOkuAsync(x.Id);
            IReadOnlyList<string>? baglamlar=x.Baglam==(int)enumCevreselSosyalCevapBaglami.Ortak?null:
                x.Yapim&&x.Guncelleme?["existing","planned"]:x.Yapim?["planned"]:["existing"];
            sonuc.Add(new(x.Anahtar,x.Baslik,x.Metin,x.Tur,secenekler,false,x.Zorunlu,x.Maksimum,
                x.Baglam==(int)enumCevreselSosyalCevapBaglami.Ortak?"global":null,baglamlar,x.Not,x.Bilgi,x.YerTutucu,
                x.KapsamDisi,aciklama,dosya,x.HerZaman,x.OtomatikKaynak,x.Kod));
        }
        return sonuc;
    }

    private async Task<(IReadOnlyList<string>,IReadOnlyList<string>,IReadOnlyList<string>)> SoruDetaylariniOkuAsync(int soruId)
    {
        const string sql = @"
            SELECT Deger FROM dbo.CevreselSosyalAnketSoruSecenek WHERE SoruId=@SoruId AND Aktif=1 ORDER BY SiraNo,Id;
            SELECT SecenekDegeri,AciklamaIstensinMi,DosyaIstensinMi FROM dbo.CevreselSosyalAnketSoruKosul WHERE SoruId=@SoruId ORDER BY SiraNo,Id;";
        await using SqlCommand command = KomutOlustur(sql); command.Parameters.AddWithValue("@SoruId",soruId);
        await using SqlDataReader reader=await command.ExecuteReaderAsync(); List<string> secenekler=[]; List<string> aciklama=[]; List<string> dosya=[];
        while(await reader.ReadAsync()) secenekler.Add(reader.GetString(0));
        await reader.NextResultAsync();
        while(await reader.ReadAsync()){string deger=reader.GetString(0);if(reader.GetBoolean(1))aciklama.Add(deger);if(reader.GetBoolean(2))dosya.Add(deger);}
        return (secenekler,aciklama,dosya);
    }

    private async Task<IReadOnlyList<CevreselSosyalAciklama>> AciklamalariOkuAsync(int bolumId)
    {
        const string sql="SELECT Tur,Baslik,Metin,MaddelerJson FROM dbo.CevreselSosyalAnketBilgi WHERE BolumId=@BolumId AND Aktif=1 ORDER BY SiraNo,Id;";
        await using SqlCommand command=KomutOlustur(sql);command.Parameters.AddWithValue("@BolumId",bolumId);await using SqlDataReader reader=await command.ExecuteReaderAsync();List<CevreselSosyalAciklama> sonuc=[];
        while(await reader.ReadAsync()) sonuc.Add(new(reader.GetString(0),reader.IsDBNull(2)?null:reader.GetString(2),reader.IsDBNull(1)?null:reader.GetString(1),reader.IsDBNull(3)?null:JsonSerializer.Deserialize<List<string>>(reader.GetString(3))));
        return sonuc;
    }
}
