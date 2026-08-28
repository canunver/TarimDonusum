using TarimDonusum.Araclar;
using TarimDonusum.Models;

namespace TarimDonusum.Raporlar;

public sealed class RPROB_YatirimMakineEkipmanListesi(string uygulamaRootPath) : RPROBTemel(uygulamaRootPath)
{
    protected override string SablonAdi => "MakineEkipmanListesi.xltx";
    protected override string GeciciDosyaOnEki => "yatirim-makine-ekipman-listesi";
    protected override string CiktiDosyaOnEki => "MakineEkipmanListesi";

    protected override void Doldur(Tablo tablo, Basvuru basvuru)
    {
        List<BasvuruMakine> makineler=(basvuru.Makineler??[]).OrderBy(x=>x.siraNo).ThenBy(x=>x.id).ToList();
        const int ornekSatir=5;
        if(makineler.Count>1)
        {
            double yukseklik=tablo.SatirGercekYukseklikAl(ornekSatir);
            tablo.SatirAc(ornekSatir+1,makineler.Count-1);
            for(int i=1;i<makineler.Count;i++)
            {
                int hedef=ornekSatir+i;
                tablo.HucreKopyala(ornekSatir,0,ornekSatir,13,hedef,0);
                tablo.SatirGercekYukseklikAyarla(hedef,hedef,yukseklik);
            }
        }

        for(int i=0;i<makineler.Count;i++) SatirYaz(tablo,ornekSatir+i,i+1,makineler[i]);
    }

    private static void SatirYaz(Tablo t,int r,int sira,BasvuruMakine m)
    {
        BasvuruMakineTeklif? esas=(m.teklifler??[]).FirstOrDefault(x=>x.basvuruyaEsas);
        bool yeni=m.durum.StartsWith("YENİ",StringComparison.Ordinal);
        string markaModel=yeni
            ? Birlestir(esas?.marka,esas?.model)
            : Birlestir(m.marka,m.model);
        string kapasite=!string.IsNullOrWhiteSpace(m.kapasiteOzellikleri)
            ? m.kapasiteOzellikleri
            : string.Join(Environment.NewLine,(m.teknikOzellikler??[]).OrderBy(x=>x.siraNo).Select(x=>Birlestir(x.baslik,x.aciklamaAsgariGereklilik,": ")).Where(x=>x.Length>0));

        Yaz(t,r,0,sira);
        Yaz(t,r,1,m.ad);
        Yaz(t,r,2,markaModel);
        Yaz(t,r,3,kapasite);
        Yaz(t,r,4,m.miktar);
        Yaz(t,r,5,m.yerlesimPlaniSiraNo);
        Yaz(t,r,6,m.kullanimAmaci);
        Yaz(t,r,7,m.durum.StartsWith("MEVCUT",StringComparison.Ordinal)?"X":"");
        Yaz(t,r,8,yeni?"X":"");
        Yaz(t,r,9,m.durum=="MEVCUT KULLANILACAK"?"X":"");
        Yaz(t,r,10,m.durum=="MEVCUT KULLANILMAYACAK"?"X":"");
        Yaz(t,r,11,m.durum=="YENİ DESTEK İSTENİYOR"?"X":"");
        Yaz(t,r,12,m.durum=="YENİ DESTEK İSTEMİYOR"?"X":"");
        Yaz(t,r,13,m.kapasiteSecimGerekcesi);
        t.SatirYukseklikAyarla(r,r,-1,3,22);
    }

    private static string Birlestir(string? a,string? b,string ayirici=" ") =>
        string.Join(ayirici,new[]{a,b}.Where(x=>!string.IsNullOrWhiteSpace(x)));
    private static void Yaz(Tablo t,int r,int c,string? v)=>t.HucreDegerYaz(r,c,v??"");
    private static void Yaz(Tablo t,int r,int c,int v)=>t.HucreDegerYaz(r,c,v);
    private static void Yaz(Tablo t,int r,int c,int? v){if(v.HasValue)t.HucreDegerYaz(r,c,v.Value);else t.HucreDegerYaz(r,c,"");}
    private static void Yaz(Tablo t,int r,int c,decimal v)=>t.HucreDegerYaz(r,c,Convert.ToDouble(v));
}
