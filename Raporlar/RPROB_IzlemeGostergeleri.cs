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
        for(int i=0;i<IzlemeGostergesiTanimlari.Tum.Count;i++)
        {
            IzlemeGostergesiTanimi tanim=IzlemeGostergesiTanimlari.Tum[i];int r=9+i;
            Yaz(t,r,1,tanim.Ad);Yaz(t,r,4,tanim.Birim);Yaz(t,r,7,tanim.VeriKaynagi);Yaz(t,r,8,tanim.Siklik);
            if(!kayitlar.TryGetValue(tanim.Kod,out BasvuruIzlemeGostergesi? x))continue;
            Yaz(t,r,2,x.baslangicDegeri);Yaz(t,r,3,x.hedefDeger);Yaz(t,r,5,x.kadinKirilimi);Yaz(t,r,6,x.gencKirilimi);Yaz(t,r,9,x.aciklama);
        }
    }
    private static void Yaz(Tablo t,int r,int c,string? v)=>t.HucreDegerYaz(r-1,c-1,v??"");
}
