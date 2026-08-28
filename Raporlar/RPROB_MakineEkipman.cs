using TarimDonusum.Araclar;
using TarimDonusum.Models;

namespace TarimDonusum.Raporlar;

public sealed class RPROB_MakineEkipman(string uygulamaRootPath) : RPROBTemel(uygulamaRootPath)
{
    public bool UzmanCiktisi { get; set; }
    protected override string SablonAdi => "MakineEkipmanTeklif.xltx";
    protected override string GeciciDosyaOnEki => "yeni-makine-ekipman";
    protected override string CiktiDosyaOnEki => "YeniMakineEkipman";

    protected override void Doldur(Tablo tablo, Basvuru basvuru)
    {
        List<BasvuruMakine> makineler=basvuru.Makineler.Where(m=>string.Equals(m.durum,"YENİ DESTEK İSTENİYOR",StringComparison.Ordinal)).OrderBy(m=>m.siraNo).ThenBy(m=>m.id).ToList();
        MakineleriYaz(tablo,basvuru.Id,makineler);OzellikleriYaz(tablo,makineler);TeklifleriYaz(tablo,makineler);
        if(!UzmanCiktisi){tablo.SheetSil(5);tablo.SheetSil(4);tablo.SheetSil(3);return;}
        UzmanFiyatlariniYaz(tablo,makineler);UzmanDokumanlariniYaz(tablo,makineler);KontrolOzetiniYaz(tablo,makineler);
    }

    private static void MakineleriYaz(Tablo t,int basvuruId,List<BasvuruMakine> ms){t.AktifSheetDegistir(0);for(int i=0;i<ms.Count;i++){var m=ms[i];var x=m.teklifler.FirstOrDefault(a=>a.basvuruyaEsas);int r=i+1;FormatSatiri(t,1,r,9);S(t,r,0,Kod(m));N(t,r,1,basvuruId);S(t,r,2,m.ad);S(t,r,3,m.birim);D(t,r,4,m.miktar);S(t,r,5,m.aciklama);if(x!=null){N(t,r,6,x.siraNo);S(t,r,7,x.paraBirimi);D(t,r,8,x.birimFiyat);D(t,r,9,Tl(x,m.miktar));}}}
    private static void OzellikleriYaz(Tablo t,List<BasvuruMakine> ms){t.AktifSheetDegistir(1);int r=1;foreach(var m in ms)foreach(var o in m.teknikOzellikler.OrderBy(x=>x.siraNo)){FormatSatiri(t,1,r,4);S(t,r,0,Kod(m));N(t,r,1,o.siraNo);S(t,r,2,o.baslik);S(t,r,3,o.aciklamaAsgariGereklilik);S(t,r,4,o.zorunluMu?"Evet":"Hayır");r++;}}
    private static void TeklifleriYaz(Tablo t,List<BasvuruMakine> ms){t.AktifSheetDegistir(2);int r=1;foreach(var m in ms)foreach(var x in m.teklifler.OrderBy(a=>a.siraNo)){FormatSatiri(t,1,r,14);S(t,r,0,Kod(m));N(t,r,1,x.siraNo);S(t,r,2,x.basvuruyaEsas?"Evet":"Hayır");S(t,r,3,x.tedarikci);S(t,r,4,x.marka);S(t,r,5,x.model);S(t,r,6,x.paraBirimi);D(t,r,7,x.kur);D(t,r,8,x.birimFiyat);D(t,r,9,x.birimFiyat*m.miktar);T(t,r,10,x.teklifTarihi);T(t,r,11,x.gecerlilikTarihi);D(t,r,12,Tl(x,m.miktar));S(t,r,13,x.teklifBelgesiDosyaAdi);S(t,r,14,x.aciklama);r++;}}
    private static void UzmanFiyatlariniYaz(Tablo t,List<BasvuruMakine> ms){t.AktifSheetDegistir(3);for(int i=0;i<ms.Count;i++){var m=ms[i];var esas=m.teklifler.FirstOrDefault(x=>x.basvuruyaEsas);int r=i+1;FormatSatiri(t,1,r,13);decimal kur=m.uzmanKur??0,maks=kur*(m.uzmanMaksimumFiyat??0);S(t,r,0,Kod(m));S(t,r,1,m.ad);S(t,r,2,m.uzmanParaBirimi);D(t,r,3,m.uzmanKur);D(t,r,4,m.uzmanMinimumFiyat);D(t,r,5,m.uzmanMaksimumFiyat);D(t,r,6,kur*m.uzmanMinimumFiyat);D(t,r,7,maks);D(t,r,8,maks*1.05m);if(esas!=null){N(t,r,9,esas.siraNo);D(t,r,10,Tl(esas,m.miktar));}D(t,r,11,m.uzmanOnerilenFiyatTl);S(t,r,12,m.uzmanKontrolSonucu);S(t,r,13,m.uzmanAciklama);}}
    private static void UzmanDokumanlariniYaz(Tablo t,List<BasvuruMakine> ms){t.AktifSheetDegistir(4);t.HucreKopyala(0,6,0,6,0,7);t.HucreDegerYaz(0,7,"Dosya Adı");int r=1;foreach(var m in ms)foreach(var d in m.uzmanDokumanlari.OrderBy(x=>x.siraNo)){FormatSatiri(t,1,r,6);t.HucreKopyala(1,6,1,6,r,7);t.HucreDegerYaz(r,7,"");S(t,r,0,Kod(m));N(t,r,1,d.siraNo);S(t,r,2,d.dokumanAdi);S(t,r,3,d.dokumanTuru);S(t,r,4,d.kaynakTedarikci);T(t,r,5,d.belgeTarihi);S(t,r,6,d.aciklama);S(t,r,7,d.dosyaAdi);r++;}}
    private static void KontrolOzetiniYaz(Tablo t,List<BasvuruMakine> ms){t.AktifSheetDegistir(5);int r=3;foreach(var m in ms){FormatSatiri(t,3,r,5);var esas=m.teklifler.FirstOrDefault(x=>x.basvuruyaEsas);S(t,r,0,Kod(m));S(t,r,1,m.ad);N(t,r,2,m.teklifler.Count);if(esas!=null)D(t,r,3,Tl(esas,m.miktar));D(t,r,4,(m.uzmanKur??0)*(m.uzmanMaksimumFiyat??0)*1.05m);S(t,r,5,m.uzmanKontrolSonucu);r++;}if(ms.Count==0)FormatSatiri(t,3,3,5);}
    private static string Kod(BasvuruMakine m)=>$"M{m.siraNo:000}";
    private static decimal? Tl(BasvuruMakineTeklif x,decimal miktar)=>x.birimFiyat.HasValue?x.birimFiyat.Value*miktar*(x.kur??1):null;
    private static void S(Tablo t,int r,int c,string? v){if(!string.IsNullOrWhiteSpace(v))t.HucreDegerYaz(r,c,v);}
    private static void N(Tablo t,int r,int c,int v)=>t.HucreDegerYaz(r,c,v);
    private static void D(Tablo t,int r,int c,decimal? v){if(v.HasValue)t.HucreDegerYaz(r,c,v.Value);}
    private static void T(Tablo t,int r,int c,DateTime? v){if(v.HasValue)t.HucreDegerYaz(r,c,v.Value);}
    private static void FormatSatiri(Tablo t,int kaynakSatir,int hedefSatir,int sonSutun){int yukseklik=t.SatirYukseklikAl(kaynakSatir);if(hedefSatir!=kaynakSatir)t.HucreKopyala(kaynakSatir,0,kaynakSatir,sonSutun,hedefSatir,0);for(int c=0;c<=sonSutun;c++)t.HucreDegerYaz(hedefSatir,c,"");t.SatirYukseklikAyarla(hedefSatir,hedefSatir,-1,0,yukseklik);}
}
