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
        var surecler=urun==null?[]:b.UrunSurecleri.Where(x=>x.urunId==urun.id).OrderBy(x=>x.siraNo).ThenBy(x=>x.id).ToList();
        int veriBaslangic=4,sablonSatirSayisi=14,sablonToplamSatiri=18,formatSatiri=12;
        foreach(int r in new[]{4,8,15})t.HucreBirlestirme(r,0);
        t.HucreBirlestirme(sablonToplamSatiri,0);
        int gerekenSatir=Math.Max(1,surecler.Sum(x=>Math.Max(1,x.makineler.Count)));
        if(gerekenSatir>sablonSatirSayisi)t.SatirAc(sablonToplamSatiri,gerekenSatir-sablonSatirSayisi);
        double yukseklik=t.SatirGercekYukseklikAl(formatSatiri);
        for(int i=0;i<gerekenSatir;i++){int r=veriBaslangic+i;t.HucreKopyala(formatSatiri,0,formatSatiri,8,r,0);for(int c=0;c<=8;c++)t.HucreDegerYaz(r,c,"");t.SatirGercekYukseklikAyarla(r,r,yukseklik);}
        if(gerekenSatir<sablonSatirSayisi)t.SatirSil(veriBaslangic+gerekenSatir,veriBaslangic+sablonSatirSayisi-1);

        int satir=veriBaslangic;
        foreach(BasvuruUrunSurec surec in surecler)
        {
            List<BasvuruUrunSurecMakine?> makineler=surec.makineler.Count==0?[null]:surec.makineler.OrderBy(x=>x.siraNo).ThenBy(x=>x.id).Cast<BasvuruUrunSurecMakine?>().ToList();
            int ilk=satir;
            foreach(BasvuruUrunSurecMakine? sm in makineler)
            {
                t.HucreDegerYaz(satir,0,surec.surecAdi);
                if(sm!=null){BasvuruMakine? m=b.Makineler.FirstOrDefault(x=>x.id==sm.makineId);t.HucreDegerYaz(satir,1,m?.ad??"");t.HucreDegerYaz(satir,2,sm.adet);t.HucreDegerYaz(satir,3,sm.yerlesimPlaniNo);t.HucreDegerYaz(satir,4,sm.girdilerMiktarlar);t.HucreDegerYaz(satir,5,sm.ciktilarMiktarlar);t.HucreDegerYaz(satir,6,sm.islemeKapasitesi);if(sm.gunlukCalismaSuresi.HasValue)t.HucreDegerYaz(satir,7,SureMetni(sm.gunlukCalismaSuresi.Value,sm.gunlukCalismaSuresiBirimi));t.HucreDegerYaz(satir,8,sm.aciklama);}satir++;
            }
            if(satir-1>ilk)t.HucreBirlestir(ilk,0,satir-1,0);
        }
        int toplamSatiri=veriBaslangic+gerekenSatir;t.HucreBirlestir(toplamSatiri,0,toplamSatiri,7);t.HucreDegerYaz(toplamSatiri,0,"ÜRÜN ÜRETİMİNİN/İŞLENMESİNİN GERÇEKLEŞMESİ İÇİN GEÇEN SÜRE TOPLAMI:");
        decimal toplamDakika=surecler.SelectMany(x=>x.makineler).Sum(x=>x.gunlukCalismaSuresi is decimal sure?(x.gunlukCalismaSuresiBirimi=="Dakika"?sure:sure*60):0);t.HucreDegerYaz(toplamSatiri,8,ToplamSureMetni(toplamDakika));
    }
    private static string SureMetni(decimal sure,string birim)=>$"{sure:0.##} {(birim=="Dakika"?"dakika":"saat")}";
    private static string ToplamSureMetni(decimal dakika){decimal saat=Math.Floor(dakika/60),kalan=dakika%60;if(saat>0&&kalan>0)return $"{saat:0} saat {kalan:0.##} dakika";return saat>0?$"{saat:0.##} saat":$"{kalan:0.##} dakika";}
    private static string GuvenliSayfaAdi(string ad,int no){string s=new string(ad.Select(c=>"[]:*?/\\".Contains(c)?'_':c).ToArray()).Trim();if(string.IsNullOrWhiteSpace(s))s=$"Ürün {no+1}";return s.Length>31?s[..31]:s;}
}
