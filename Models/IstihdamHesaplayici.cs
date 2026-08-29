namespace TarimDonusum.Models;

public sealed class IstihdamHesapSonucu
{
    public decimal mevcutToplamTzi { get; set; }
    public decimal netEkTzi { get; set; }
    public decimal? yuzBinUsdBasinaNetEkTzi { get; set; }
    public decimal ekKadinTzi { get; set; }
    public decimal ekGencTzi { get; set; }
    public decimal padDfdiIsEsdegeri { get; set; }
    public decimal yillikEmekGeliriDegisimiTl { get; set; }
    public string asgariEsikDurumu { get; set; } = "";
    public decimal istihdamPuani { get; set; }
    public decimal kadinIstihdamPuani { get; set; }
    public decimal buFormdanGelenPuan { get; set; }
    public decimal kadinDfdiIsEsdegeri { get; set; }
    public decimal gencDfdiIsEsdegeri { get; set; }
    public string padRaporlamaSikligi { get; set; } = "Yıllık";
}

public static class IstihdamHesaplayici
{
    public static IstihdamHesapSonucu Hesapla(BasvuruIstihdam istihdam, decimal talepEdilenDestekUsd)
    {
        List<BasvuruIstihdamSatir> satirlar=istihdam.satirlar??[];
        IstihdamHesapSonucu sonuc=new()
        {
            mevcutToplamTzi=satirlar.Sum(x=>x.mevcutCalisan),
            netEkTzi=satirlar.Sum(x=>x.netCalisanArtisi),
            ekKadinTzi=satirlar.Where(x=>x.cinsiyet=="Kadın").Sum(x=>x.netCalisanArtisi),
            ekGencTzi=satirlar.Where(x=>x.yasDurumu=="Genç").Sum(x=>x.netCalisanArtisi)
        };
        sonuc.yuzBinUsdBasinaNetEkTzi=talepEdilenDestekUsd>0?sonuc.netEkTzi/(talepEdilenDestekUsd/100000m):null;
        foreach(BasvuruIstihdamSatir x in satirlar)
        {
            decimal yatirimSonrasi=x.mevcutCalisan+x.netCalisanArtisi;
            decimal aylikGelirDegisimi=yatirimSonrasi*x.hedefAylikBrutUcret-x.mevcutCalisan*x.bazAylikBrutUcret;
            decimal dfdi=x.bazAylikBrutUcret!=0?aylikGelirDegisimi/x.bazAylikBrutUcret:0;
            sonuc.padDfdiIsEsdegeri+=dfdi;sonuc.yillikEmekGeliriDegisimiTl+=aylikGelirDegisimi*12m;
            if(x.cinsiyet=="Kadın")sonuc.kadinDfdiIsEsdegeri+=dfdi;
            if(x.yasDurumu=="Genç")sonuc.gencDfdiIsEsdegeri+=dfdi;
        }
        decimal? oran=sonuc.yuzBinUsdBasinaNetEkTzi;
        sonuc.asgariEsikDurumu=oran.HasValue?(oran.Value>=0.25m?"SAĞLANIYOR":"SAĞLANMIYOR"):"";
        sonuc.istihdamPuani=!oran.HasValue||oran.Value<0.25m?0:oran.Value<0.5m?5:oran.Value<0.75m?10:oran.Value<1m?15:20;
        bool kadinKriteri=(istihdam.oncekiYilKadin+istihdam.oncekiYilErkek+istihdam.sonYilKadin+istihdam.sonYilErkek)>0
            && ((istihdam.oncekiYilKadin+istihdam.oncekiYilErkek)+(istihdam.sonYilKadin+istihdam.sonYilErkek))/2m>20m
            && istihdam.oncekiYilKadin+istihdam.sonYilKadin>istihdam.oncekiYilErkek+istihdam.sonYilErkek;
        sonuc.kadinIstihdamPuani=kadinKriteri?2.5m:0;sonuc.buFormdanGelenPuan=sonuc.istihdamPuani+sonuc.kadinIstihdamPuani;
        return sonuc;
    }
}
