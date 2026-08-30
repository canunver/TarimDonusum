using System.Text.Json;
using TarimDonusum.Araclar;
using TarimDonusum.Models;

namespace TarimDonusum.Raporlar;

public sealed class RPROB_BasvuruOzeti(string uygulamaRootPath) : RPROBTemel(uygulamaRootPath)
{
    public bool UzmanCiktisi { get; set; }
    protected override string SablonAdi => "BasvuruOzeti.xltx";
    protected override string GeciciDosyaOnEki => "basvuru-ozeti";
    protected override string CiktiDosyaOnEki => "BasvuruOzeti";

    protected override void Doldur(Tablo t,Basvuru b)
    {
        BasvuruOzetiHesapSonucu o=BasvuruOzetiHesaplayici.Hesapla(b);
        Yaz(t,5,2,b.basvuruFirma.firma.ticaretUnvani);Yaz(t,5,6,b.yatirim.yatirimAdi);
        Yaz(t,6,2,o.YatirimYeri);Yaz(t,6,6,o.DegerZinciri);
        Yaz(t,7,2,b.finans.talepEdilenDestekTutari);Yaz(t,7,6,o.OnSiralamaPuani);
        Yaz(t,8,2,o.BelgeTamamlanmaOrani);Yaz(t,8,6,o.GenelDurum);
        Dictionary<string,Dictionary<string,string>> tamlik=Oku(b.basvuruOzetiKurum.kurumJson,"tamlik");
        Dictionary<string,Dictionary<string,string>> uygunluk=Oku(b.basvuruOzetiKurum.kurumJson,"uygunluk");
        for(int i=0;i<Math.Min(13,o.Tamlik.Count);i++)
        {
            BasvuruOzetiTamlikSatiri x=o.Tamlik[i];int r=12+i;
            Yaz(t,r,3,x.Dolu);Yaz(t,r,4,x.Durum);
            if(UzmanCiktisi&&tamlik.TryGetValue(x.Kod,out var k)){Yaz(t,r,7,k.GetValueOrDefault("teyit"));Yaz(t,r,8,k.GetValueOrDefault("aciklama"));}
            else{Yaz(t,r,7,"");Yaz(t,r,8,"");}
        }
        for(int i=0;i<Math.Min(6,o.Uygunluk.Count);i++)
        {
            BasvuruOzetiUygunlukSatiri x=o.Uygunluk[i];int r=28+i;
            Yaz(t,r,2,x.BasvuruDegeri);Yaz(t,r,3,x.OnSonuc);
            if(UzmanCiktisi&&uygunluk.TryGetValue(x.Kod,out var k)){Yaz(t,r,6,k.GetValueOrDefault("sonuc"));Yaz(t,r,7,k.GetValueOrDefault("not"));Yaz(t,r,8,k.GetValueOrDefault("nihai"));}
            else{Yaz(t,r,6,"");Yaz(t,r,7,"");Yaz(t,r,8,"");}
        }
    }

    private static Dictionary<string,Dictionary<string,string>> Oku(string? json,string alan)
    {
        Dictionary<string,Dictionary<string,string>> sonuc=[];if(string.IsNullOrWhiteSpace(json))return sonuc;
        try{using JsonDocument d=JsonDocument.Parse(json);if(!d.RootElement.TryGetProperty(alan,out JsonElement kok)||kok.ValueKind!=JsonValueKind.Object)return sonuc;foreach(JsonProperty p in kok.EnumerateObject()){Dictionary<string,string> deger=[];if(p.Value.ValueKind==JsonValueKind.Object)foreach(JsonProperty q in p.Value.EnumerateObject())deger[q.Name]=q.Value.GetString()??"";sonuc[p.Name]=deger;}}catch{}return sonuc;
    }
    private static void Yaz(Tablo t,int r,int c,string? v)=>t.HucreDegerYaz(r-1,c-1,v??"");
    private static void Yaz(Tablo t,int r,int c,int v)=>t.HucreDegerYaz(r-1,c-1,v);
    private static void Yaz(Tablo t,int r,int c,decimal? v){if(v.HasValue)t.HucreDegerYaz(r-1,c-1,v.Value);else Yaz(t,r,c,"");}
}
