using TarimDonusum.Araclar;
using TarimDonusum.Models;

namespace TarimDonusum.Raporlar;

public sealed class RPROB_UrunSurecMakine(string uygulamaRootPath):RPROBTemel(uygulamaRootPath)
{
    protected override string SablonAdi=>"UrunSurecMakine.xltx";
    protected override string GeciciDosyaOnEki=>"urun-surec-makine";
    protected override string CiktiDosyaOnEki=>"UrunSurecMakine";
    protected override void Doldur(Tablo t,Basvuru b)
    {
        var urunler=b.YatirimOnBilgileri.Where(x=>x.tur==enumYatirimOnBilgiTuru.UretilecekUrun&&b.UrunSurecleri.Any(s=>s.urunId==x.id)).OrderBy(x=>x.siraNo).ThenBy(x=>x.id).ToList();
        if(urunler.Count==0){HazirlaVeYaz(t,b,null,0);return;}
        for(int i=1;i<urunler.Count;i++)t.YeniSheetEkle(0);
        for(int i=0;i<urunler.Count;i++)HazirlaVeYaz(t,b,urunler[i],i);
    }
    private static void HazirlaVeYaz(Tablo t,Basvuru b,BasvuruYatirimOnBilgi? urun,int sheet)
    {
        t.AktifSheetDegistir(sheet);t.SheetAdiVer(sheet,GuvenliSayfaAdi(urun?.ad??"Ürün Süreçleri",sheet));
        t.HucreDegerYaz(0,0,$"TABLO 3 {(urun?.ad??"").ToUpperInvariant()} ÜRÜNÜ İŞLEME AKIŞ ŞEMASI VE ÜRETİMDE KULLANILACAK MAKİNE-EKİPMANLAR İLİŞKİ TABLOSU   (DB TB Versiyon 1.0.00)\n(Bu tablo, tesiste üretimi yapılacak her bir nihai ürün için ayrı sayfalar halinde doldurulur.)");
        t.HucreBirlestirme(4,0);t.HucreBirlestirme(8,0);t.HucreBirlestirme(15,0);
        var surecler=urun==null?[]:b.UrunSurecleri.Where(x=>x.urunId==urun.id).OrderBy(x=>x.siraNo).ThenBy(x=>x.id).ToList();
        var satirlar=surecler.SelectMany(s=>s.makineler.Count==0?[new Satir(s,null)]:s.makineler.OrderBy(x=>x.siraNo).Select(m=>new Satir(s,m))).ToList();
        int sabit=14,ek=Math.Max(0,satirlar.Count-sabit);if(ek>0){t.SatirAc(18,ek);for(int i=0;i<ek;i++){int r=18+i;t.HucreKopyala(12,0,12,8,r,0);t.SatirGercekYukseklikAyarla(r,r,t.SatirGercekYukseklikAl(12));}}
        int toplam=Math.Max(sabit,satirlar.Count);for(int i=0;i<toplam;i++){int r=4+i;for(int c=0;c<=8;c++)t.HucreDegerYaz(r,c,"");if(i>=satirlar.Count)continue;Satir x=satirlar[i];BasvuruMakine? m=x.Makine==null?null:b.Makineler.FirstOrDefault(z=>z.id==x.Makine.makineId);t.HucreDegerYaz(r,0,x.Surec.surecAdi);if(m!=null)t.HucreDegerYaz(r,1,m.ad);if(x.Makine!=null){t.HucreDegerYaz(r,2,x.Makine.adet);t.HucreDegerYaz(r,3,x.Makine.yerlesimPlaniNo);t.HucreDegerYaz(r,4,x.Makine.girdilerMiktarlar);t.HucreDegerYaz(r,5,x.Makine.ciktilarMiktarlar);t.HucreDegerYaz(r,6,x.Makine.islemeKapasitesi);if(x.Makine.gunlukCalismaSuresi.HasValue)t.HucreDegerYaz(r,7,SureMetni(x.Makine.gunlukCalismaSuresi.Value,x.Makine.gunlukCalismaSuresiBirimi));t.HucreDegerYaz(r,8,x.Makine.aciklama);}}
        decimal toplamDakika=satirlar.Sum(x=>x.Makine?.gunlukCalismaSuresi is decimal sure?(x.Makine.gunlukCalismaSuresiBirimi=="Dakika"?sure:sure*60):0);t.HucreDegerYaz(18+ek,8,toplamDakika%60==0?SureMetni(toplamDakika/60,"Saat"):SureMetni(toplamDakika,"Dakika"));
    }
    private static string SureMetni(decimal sure,string birim)=>$"{sure:0.##} {(birim=="Dakika"?"dakika":"saat")}";
    private static string GuvenliSayfaAdi(string ad,int no){string s=new string(ad.Select(c=>"[]:*?/\\".Contains(c)?'_':c).ToArray()).Trim();if(string.IsNullOrWhiteSpace(s))s=$"Ürün {no+1}";return s.Length>31?s[..31]:s;}
    private sealed record Satir(BasvuruUrunSurec Surec,BasvuruUrunSurecMakine? Makine);
}
