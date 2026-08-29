using TarimDonusum.Araclar;
using TarimDonusum.Models;

namespace TarimDonusum.Raporlar;

public sealed class RPROB_Istihdam(string uygulamaRootPath):RPROBTemel(uygulamaRootPath)
{
    protected override string SablonAdi=>"TamZamanliIstihdam.xltx";
    protected override string GeciciDosyaOnEki=>"tam-zamanli-istihdam";
    protected override string CiktiDosyaOnEki=>"TamZamanliIstihdam";
    protected override void Doldur(Tablo t,Basvuru b)
    {
        BasvuruIstihdam i=b.istihdam;List<BasvuruIstihdamSatir> satirlar=i.satirlar.OrderBy(x=>x.siraNo).ThenBy(x=>x.id).ToList();
        t.HucreDegerYaz(3,2,b.basvuruFirma.firma.ticaretUnvani);t.HucreDegerYaz(3,8,KimlikBilgisi(b));
        BasvuruUygulamaAdresi? adres=b.YatirimAdresleri.OrderBy(x=>x.siraNo).FirstOrDefault();t.HucreDegerYaz(4,2,adres?.ilAdi??b.basvuruFirma.il.ad);t.HucreDegerYaz(4,6,adres?.ilceAdi??"");t.HucreDegerYaz(4,10,b.finans.talepEdilenDestekTutari.GetValueOrDefault());
        int basvuruYili=b.basvuruFirma.donem.yil;
        t.HucreDegerYaz(8,0,basvuruYili-2);t.HucreDegerYaz(8,1,i.oncekiYilKadin);t.HucreDegerYaz(8,2,i.oncekiYilErkek);t.HucreFormulYaz(8,3,"IF(COUNT(B9:C9)=0,\"\",SUM(B9:C9))");t.HucreFormulYaz(8,4,"IF(D9=\"\",\"\",IF(AND(D9>20,B9>C9),\"EVET\",\"HAYIR\"))");
        t.HucreDegerYaz(9,0,basvuruYili-1);t.HucreDegerYaz(9,1,i.sonYilKadin);t.HucreDegerYaz(9,2,i.sonYilErkek);t.HucreFormulYaz(9,3,"IF(COUNT(B10:C10)=0,\"\",SUM(B10:C10))");t.HucreFormulYaz(9,4,"IF(D10=\"\",\"\",IF(AND(D10>20,B10>C10),\"EVET\",\"HAYIR\"))");
        t.HucreFormulYaz(10,1,"IF(COUNT(B9:C10)<4,\"\",IF(AND(AVERAGE(D9:D10)>20,SUM(B9:B10)>SUM(C9:C10)),\"SAĞLANIYOR\",\"SAĞLANMIYOR\"))");t.HucreFormulYaz(10,5,"IF(B11=\"\",\"\",IF(B11=\"SAĞLANIYOR\",2.5,0))");
        const int bas=18,sablon=2;int sayi=Math.Max(1,satirlar.Count);if(sayi>sablon)t.SatirAc(bas+sablon,sayi-sablon);else if(sayi<sablon)t.SatirSil(bas+sayi,bas+sablon-1);double yuk=t.SatirGercekYukseklikAl(bas);
        for(int n=0;n<sayi;n++){int r=bas+n;t.HucreKopyala(bas,0,bas,13,r,0);for(int c=0;c<14;c++)t.HucreDegerYaz(r,c,"");t.SatirGercekYukseklikAyarla(r,r,yuk);if(n<satirlar.Count){BasvuruIstihdamSatir x=satirlar[n];t.HucreDegerYaz(r,0,x.birimUnite);t.HucreDegerYaz(r,1,x.gorevUretimHatti);t.HucreDegerYaz(r,2,x.cinsiyet);t.HucreDegerYaz(r,3,x.yasDurumu);t.HucreDegerYaz(r,4,x.mevcutCalisan);t.HucreDegerYaz(r,5,x.netCalisanArtisi);t.HucreDegerYaz(r,7,x.bazAylikBrutUcret);t.HucreDegerYaz(r,8,x.hedefAylikBrutUcret);}int er=r+1;t.HucreFormulYaz(r,6,$"IF(COUNT(E{er}:F{er})=0,\"\",SUM(E{er}:F{er}))");t.HucreFormulYaz(r,9,$"IF(OR(H{er}=\"\",H{er}=0,I{er}=\"\"),\"\",(I{er}-H{er})/H{er})");t.HucreFormulYaz(r,10,$"IF(OR(G{er}=\"\",H{er}=\"\",I{er}=\"\"),\"\",G{er}*I{er}-E{er}*H{er})");t.HucreFormulYaz(r,11,$"IF(OR(K{er}=\"\",H{er}=\"\",H{er}=0),\"\",K{er}/H{er})");}
        IstihdamHesapSonucu h=IstihdamHesaplayici.Hesapla(i,b.finans.talepEdilenDestekTutari.GetValueOrDefault());
        t.HucreDegerYaz(13,1,h.mevcutToplamTzi);t.HucreDegerYaz(13,3,h.netEkTzi);if(h.yuzBinUsdBasinaNetEkTzi.HasValue)t.HucreDegerYaz(13,5,h.yuzBinUsdBasinaNetEkTzi.Value);else t.HucreDegerYaz(13,5,"");t.HucreDegerYaz(13,7,h.ekKadinTzi);t.HucreDegerYaz(13,9,h.ekGencTzi);t.HucreDegerYaz(13,11,h.padDfdiIsEsdegeri);t.HucreDegerYaz(13,13,h.yillikEmekGeliriDegisimiTl);
        t.HucreDegerYaz(14,1,h.asgariEsikDurumu);t.HucreDegerYaz(14,3,h.istihdamPuani);t.HucreDegerYaz(14,5,h.kadinIstihdamPuani);t.HucreDegerYaz(14,7,h.buFormdanGelenPuan);t.HucreDegerYaz(14,9,h.kadinDfdiIsEsdegeri);t.HucreDegerYaz(14,11,h.gencDfdiIsEsdegeri);t.HucreDegerYaz(14,13,h.padRaporlamaSikligi);
        int gerekceBaslik=20+(sayi-sablon);t.HucreDegerYaz(gerekceBaslik+1,0,i.gerekceVarsayimlarDogrulamaYaklasimi);
    }
    private static string KimlikBilgisi(Basvuru b){string v=b.basvuruFirma.firma.vergiKimlikNo?.Trim()??"",m=b.basvuruFirma.firma.mersisNo?.Trim()??"";return string.Join(" / ",new[]{v,m}.Where(x=>!string.IsNullOrWhiteSpace(x)));}
}
