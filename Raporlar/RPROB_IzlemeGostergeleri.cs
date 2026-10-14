using TarimDonusum.Araclar;
using TarimDonusum.Models;

namespace TarimDonusum.Raporlar;

public sealed class RPROB_IzlemeGostergeleri(string uygulamaRootPath) : RPROBTemel(uygulamaRootPath)
{
    protected override string SablonAdi=>"IzlemeGostergeleri.xltx";
    protected override string GeciciDosyaOnEki=>"izleme-gostergeleri";
    protected override string CiktiDosyaOnEki=>"IzlemeGostergeleri";
    protected override void Doldur(Tablo t,Basvuru b)
    {
        int yil=(b.basvuruFirma.donem.yil>0?b.basvuruFirma.donem.yil:DateTime.Today.Year)-1;
        Yaz(t,5,2,yil.ToString());Yaz(t,5,4,b.izlemeUstBilgi.baslangicTarihi?.ToString("dd.MM.yyyy"));Yaz(t,5,6,b.izlemeUstBilgi.hedefTarihi?.ToString("dd.MM.yyyy"));Yaz(t,5,8,b.izlemeUstBilgi.veriSorumlusu);
        Dictionary<string,BasvuruIzlemeGostergesi> kayitlar=b.IzlemeGostergeleri.GroupBy(x=>x.gostergeKodu).ToDictionary(x=>x.Key,x=>x.First());
        IReadOnlyList<IzlemeGostergesiTanim> tanimlar=IzlemeGostergesiTanimSaglayici.BasvuruIcin(b);
        // Şablonda 9-28. satırlarda 20 gösterge satırı hazır; tanım sayısına göre satır eklenir veya silinir.
        int fark=tanimlar.Count-SablonSatirSayisi;
        if(fark>0)
        {
            double yukseklik=t.SatirGercekYukseklikAl(IlkSatir-1);
            int acilacakYer=IlkSatir-1+SablonSatirSayisi;
            t.SatirAc(acilacakYer,fark);
            for(int r=acilacakYer;r<acilacakYer+fark;r++){t.HucreKopyala(IlkSatir-1,0,IlkSatir-1,SonSutun-1,r,0);t.SatirGercekYukseklikAyarla(r,r,yukseklik);}
        }
        else if(fark<0)t.SatirSil(IlkSatir-1+Math.Max(tanimlar.Count,1),IlkSatir-2+SablonSatirSayisi);
        for(int i=0;i<Math.Max(tanimlar.Count,1);i++)
        {
            int r=IlkSatir+i;
            IzlemeGostergesiTanim? tanim=i<tanimlar.Count?tanimlar[i]:null;
            BasvuruIzlemeGostergesi? x=tanim!=null&&kayitlar.TryGetValue(tanim.kod,out BasvuruIzlemeGostergesi? k)?k:null;
            Yaz(t,r,1,tanim?.ad);Yaz(t,r,2,x?.baslangicDegeri);Yaz(t,r,3,x?.hedefDeger);Yaz(t,r,4,tanim?.birim);Yaz(t,r,5,x?.kadinKirilimi);
            Yaz(t,r,6,x?.gencKirilimi);Yaz(t,r,7,tanim?.veriKaynagi);Yaz(t,r,8,tanim?.siklik);Yaz(t,r,9,x?.aciklama);
        }
    }
    private const int IlkSatir=9,SablonSatirSayisi=20,SonSutun=9;
    private static void Yaz(Tablo t,int r,int c,string? v)=>t.HucreDegerYaz(r-1,c-1,v??"");
}
