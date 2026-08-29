using System.Text.Json;
using TarimDonusum.Araclar;
using TarimDonusum.Models;
namespace TarimDonusum.Raporlar;

public sealed class RPROB_BinaBolumListesi(string uygulamaRootPath):RPROBTemel(uygulamaRootPath)
{
    protected override string SablonAdi=>"BinaBolumListesi.xltx";
    protected override string GeciciDosyaOnEki=>"bina-bolum-listesi";
    protected override string CiktiDosyaOnEki=>"BinaBolumListesi";
    protected override void Doldur(Tablo t,Basvuru b)
    {
        List<BasvuruBina> binalar=Oku(b);int bas=3,sayi=binalar.Sum(x=>Math.Max(1,x.mahaller.Count));if(sayi>1)t.SatirAc(bas+1,sayi-1);double yuk=t.SatirGercekYukseklikAl(bas);
        for(int i=0;i<Math.Max(1,sayi);i++){int r=bas+i;if(i>0)t.HucreKopyala(bas,0,bas,9,r,0);for(int c=0;c<10;c++)t.HucreDegerYaz(r,c,"");t.SatirGercekYukseklikAyarla(r,r,yuk);}
        int satir=bas;foreach(BasvuruBina bina in binalar.OrderBy(x=>x.siraNo).ThenBy(x=>x.id))
        {
            List<BasvuruBinaMahal?> ms=bina.mahaller.Count==0?[null]:bina.mahaller.OrderBy(x=>x.siraNo).ThenBy(x=>x.id).Cast<BasvuruBinaMahal?>().ToList();int ilk=satir,son=satir+ms.Count-1;
            t.HucreDegerYaz(ilk,0,bina.ad);t.HucreDegerYaz(ilk,1,bina.vaziyetPlaniNo);t.HucreDegerYaz(ilk,4,X(bina.mevcutYeni,"Mevcut"));t.HucreDegerYaz(ilk,5,X(bina.mevcutYeni,"Yeni"));t.HucreDegerYaz(ilk,6,X(bina.yatirimSekli,"Değişiklik yok / Kullanılacak"));t.HucreDegerYaz(ilk,7,X(bina.yatirimSekli,"Genişletme / Modernizasyon"));t.HucreDegerYaz(ilk,8,X(bina.destekTalebi,"Evet"));t.HucreDegerYaz(ilk,9,X(bina.destekTalebi,"Hayır"));
            foreach(BasvuruBinaMahal? m in ms){if(m!=null){t.HucreDegerYaz(satir,2,m.mahalAdi);t.HucreDegerYaz(satir,3,m.alanM2);}satir++;}if(son>ilk)foreach(int c in new[]{0,1,4,5,6,7,8,9})t.HucreBirlestir(ilk,c,son,c);
        }
    }
    private static string X(string a,string b)=>string.Equals(a,b,StringComparison.OrdinalIgnoreCase)?"X":"";
    private static List<BasvuruBina> Oku(Basvuru b)
    {
        if(b.Binalar.Count>0)return b.Binalar;List<BasvuruBina> r=[];
        if(Dizi(b.dbCtpTeknikProje.dbCtpTeknikProjeJson,"buildingRows",out JsonDocument? d,out JsonElement a)){using(d)foreach(JsonElement x in a.EnumerateArray())r.Add(new(){siraNo=r.Count+1,ad=RaporJson.DegerMetni(x,"name"),mevcutYeni=RaporJson.DegerMetni(x,"assetStatus"),yatirimSekli=RaporJson.DegerMetni(x,"investmentType"),destekTalebi=RaporJson.DegerMetni(x,"supportRequested")});}
        if(r.Count==0&&Dizi(b.uygunHarcama.pikkListesiJson,"constructionRows",out d,out a)){using(d)foreach(JsonElement x in a.EnumerateArray())r.Add(new(){siraNo=r.Count+1,ad=RaporJson.DegerMetni(x,"name"),yatirimSekli=RaporJson.DegerMetni(x,"purpose")});}return r.Where(x=>!string.IsNullOrWhiteSpace(x.ad)).ToList();
    }
    private static bool Dizi(string? json,string alan,out JsonDocument? d,out JsonElement a){d=null;a=default;if(string.IsNullOrWhiteSpace(json))return false;try{d=JsonDocument.Parse(json);if(d.RootElement.TryGetProperty(alan,out a)&&a.ValueKind==JsonValueKind.Array)return true;d.Dispose();d=null;return false;}catch{return false;}}
}
