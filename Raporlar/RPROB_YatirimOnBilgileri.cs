using TarimDonusum.Araclar;
using TarimDonusum.Models;

namespace TarimDonusum.Raporlar;

public sealed class RPROB_YatirimOnBilgileri(string uygulamaRootPath, int? uygulamaAdresiId = null) : RPROBTemel(uygulamaRootPath)
{
    protected override string SablonAdi => "YatirimOnBilgileri.xltx";
    protected override string GeciciDosyaOnEki => "yatirim-on-bilgileri";
    protected override string CiktiDosyaOnEki => "YatirimOnBilgileri";

    protected override void Doldur(Tablo tablo, Basvuru basvuru)
    {
        Yaz(tablo,3,2,basvuru.yatirim.yatirimAdi);
        Yaz(tablo,4,2,basvuru.basvuruFirma.firma.ticaretUnvani);
        Yaz(tablo,5,2,basvuru.yatirim.yatiriminAmaci ?? basvuru.finans.yatiriminAmaci);
        if(uygulamaAdresiId.HasValue)
        {
            BasvuruUygulamaAdresi? adres=basvuru.YatirimAdresleri.FirstOrDefault(x=>x.id==uygulamaAdresiId.Value);
            if(adres!=null) Yaz(tablo,6,1,$"Yatırım adresi: {adres.siraNo}. {adres.ilAdi} / {adres.ilceAdi} - {adres.tamAdres}");
        }

        List<BasvuruYatirimOnBilgi> tum=(basvuru.YatirimOnBilgileri??[])
            .Where(x=>!uygulamaAdresiId.HasValue || x.tur is enumYatirimOnBilgiTuru.EnerjiKullanimi or enumYatirimOnBilgiTuru.KuruluGuc || x.uygulamaAdresiId==uygulamaAdresiId.Value)
            .OrderBy(x=>x.siraNo).ThenBy(x=>x.id).ToList();
        DegiskenBirimliBolumYaz(tablo,26,tum.Where(x=>x.tur==enumYatirimOnBilgiTuru.KuruluGuc).ToList());
        EnerjiBolumuYaz(tablo,22,tum.Where(x=>x.tur==enumYatirimOnBilgiTuru.EnerjiKullanimi).ToList());
        DegiskenBirimliBolumYaz(tablo,18,tum.Where(x=>x.tur==enumYatirimOnBilgiTuru.Girdi).ToList());
        DegiskenBirimliBolumYaz(tablo,14,tum.Where(x=>x.tur==enumYatirimOnBilgiTuru.UretilecekUrun).ToList());
        DegiskenBirimliBolumYaz(tablo,10,tum.Where(x=>x.tur==enumYatirimOnBilgiTuru.MevcutUrun).ToList());
    }

    private static void DegiskenBirimliBolumYaz(Tablo t,int ilkSatir,List<BasvuruYatirimOnBilgi> liste)
    {
        int ilk=ilkSatir-1,ek=Math.Max(0,liste.Count-1);SatirlariGenislet(t,ilk,ek,true);
        for(int i=0;i<liste.Count;i++){BasvuruYatirimOnBilgi x=liste[i];int r=ilkSatir+i;Yaz(t,r,1,x.ad);Yaz(t,r,2,x.miktar);Yaz(t,r,3,x.birim);}
    }

    private static void EnerjiBolumuYaz(Tablo t,int ilkSatir,List<BasvuruYatirimOnBilgi> liste)
    {
        int ilk=ilkSatir-1,ek=Math.Max(0,liste.Count-1);SatirlariGenislet(t,ilk,ek,false);
        for(int i=0;i<liste.Count;i++){BasvuruYatirimOnBilgi x=liste[i];int r=ilkSatir+i;Yaz(t,r,1,x.ad);Yaz(t,r,2,x.miktar);Yaz(t,r,3,x.tekPanelGucu);Yaz(t,r,4,x.toplamGuc);}
    }

    private static void SatirlariGenislet(Tablo t,int ilk,int ek,bool birimSutunu)
    {
        if(ek<=0)return;double yukseklik=t.SatirGercekYukseklikAl(ilk);t.SatirAc(ilk+1,ek);
        for(int i=0;i<ek;i++){int hedef=ilk+1+i;t.HucreKopyala(ilk,0,ilk,3,hedef,0);if(birimSutunu)t.HucreBirlestir(hedef,2,hedef,3);t.SatirGercekYukseklikAyarla(hedef,hedef,yukseklik);}
    }

    private static void Yaz(Tablo t,int r,int c,string? v)=>t.HucreDegerYaz(r-1,c-1,v??"");
    private static void Yaz(Tablo t,int r,int c,decimal? v){if(v.HasValue)t.HucreDegerYaz(r-1,c-1,Convert.ToDouble(v.Value));else t.HucreDegerYaz(r-1,c-1,"");}
}
