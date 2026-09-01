//using Aspose.Cells.Drawing;

namespace TarimDonusum.Araclar
{

    public enum CellType
    {
        LABEL,
        NUMBER,
        DATE
    }

    public enum LineStyle
    {
        DOUBLE,
        NONE,
        THIN,
        MEDIUM,
        HAIR,
        MEDIUM_DASHED
    }

    public enum TabloRenk
    {
        BLACK,
        GRAY_25,
        RED,
        ROSE,
        LIGHT_TURQUOISE2,
        ICE_BLUE,
        CORAL,
        VERY_LIGHT_YELLOW,
        PALE_BLUE,
        IVORY,
        YELLOW2,
        AQUA,
        ORANGE,
        PERIWINKLE,
        PINK2,
        ALICEBLUE,
        BLUE,
        GREEN,
        LAVENDER,
        DARKBLUE,
        DARKGREEN,
        DARKORANGE,
        DARKRED,
        DARKSALMON,
        DEEPPINK,
        DEEPSKYBLUE,
        DODGERBLUE,
        FIREBRICK,
        FORESTGREEN,
        GOLD,
        OLIVE,
        ORANGERED,
        WHITE
    }

    public enum SayfaYonu
    {
        YATAY, DUSEY
    }

    public interface Tablo
    {
        //tur=1 TIFF, 0/Diğer=JPEG
        void SheetToResim(int horRes, int verRes, string dosyaAd, int tur);
        void YeniSheetEkle(string dosyaYol, string dosyaAd, int index);

        int SheetSayisi();

        int SonDoluSatir(int sheetNo);

        int SonDoluSutun(int sheetNo);

        string SonucDosyaAd();

        void OtomatikYukseklik(int sheetNo, int satir, int sutun1, int sutun2);

        void SutunAcKontrolsuz(int sheetNo, int sutun, int acilacakSutunSayisi);

        void SatirAcKontrolsuz(int sheetNo, int satir, int acilacakSatirSayisi);

        void SatirAc(int sheetNo, int satir, int acilacakSatirSayisi);

        void SatirAc(int satir, int acilacakSatirSayisi);

        void SutunAc(int sheetNo, int sutun, int acilacakSutunSayisi);

        void SutunAc(int sutun, int acilacakSutunSayisi);

        void HucreIcerikAraYaz(string bulDeger, string yazDeger);
        void HucreIcerikAraYaz(string bulDeger, double yazDeger);

        void HucreAdBulYaz(string hucreAd, string deger);

        void HucreAdBulYaz(string hucreAd, double deger);

        void HucreAdBulYaz(string hucreAd, double deger, string paraIsareti, int kurusHane = 2);

        void HucreDegerHtmlYaz(int sheetNo, int satir, int sutun, string HtmlString);

        void HucreDegerYaz(int satir, int sutun, string deger);

        void HucreDegerYaz(int satir, int sutun, double deger);

        void HucreDegerYaz(int satir, int sutun, decimal deger);

        void HucreDegerYaz(int satir, int sutun, int deger);

        void HucreDegerYaz(int satir, int sutun, DateTime deger);

        void HucreDegerYaz(int satir, int sutun, double deger, string paraIsareti, int kurusHane = 2);

        double HucreDegerAlDbl(int sheetNo, int satir, int sutun);

        double HucreDegerAlDbl(int satir, int sutun);

        string HucreDegerAl(int sheetNo, int satir, int sutun, int noktaSay);

        string HucreDegerAl(int satir, int sutun);

        string HucreAdDegerAl(string hucreAd);

        string HucreFormulAl(int satir, int sutun);

        void HucreFormulYaz(int satir, int sutun, string formul);

        void KorumayaAl();

        void KorumayaAl(string sifre);

        void HucreAdAdresCoz(string hucreAd, ref int satir, ref int sutun);

        void HucreAdAdresCoz(string bolgeAd, ref int sheetNo, ref int satir1, ref int sutun1, ref int satir2, ref int sutun2);

        void HucreAdAdresOl(string bolgeAd, int sheetNo, int satir1, int sutun1, int satir2, int sutun2);

        void SatirKopyalaAc(int kaynakSheet, int satir, int kopyalanacakSatirSayisi, int hedefSheet, int hedefSatir);

        void SatirKopyalaAc(int satir, int kopyalanacakSatirSayisi, int hedefSatir);

        void SatirKopyala(int kaynakSheet, int satir, int kopyalanacakSatirSayisi, int hedefSheet, int hedefSatir);

        void SutunKopyalaAc(int kaynakSheet, int sutun, int kopyalanacakSutunSayisi, int hedefSheet, int hedefSutun);

        void SutunKopyalaAc(int sutun, int kopyalanacakSutunSayisi, int hedefSutun);

        void SutunKopyala(int kaynakSheet, int sutun, int kopyalanacakSutunSayisi, int hedefSheet, int hedefSutun);

        void HucreKopyala(int kaynakSheet, int satir1, int sutun1, int satir2, int sutun2, int hedefSheet, int hedefSatir, int hedefSutun);

        void HucreKopyala(int satir1, int sutun1, int satir2, int sutun2, int hedefSatir, int hedefSutun);

        void HucreBirlestir(int satir1, int sutun1, int satir2, int sutun2);

        void HucreBirlestirme(int satir1, int sutun1, int satir2, int sutun2);

        void HucreBirlestirme(int satir, int sutun);

        void SatirYukseklikAyarla(int satir1, int satir2, int yukseklik);

        void SatirYukseklikAyarla(int satir1, int satir2, int yukseklik, int ekle, int minYuks);

        void SatirGercekYukseklikAyarla(int satir1, int satir2, double yukseklik);

        double SatirGercekYukseklikAl(int satir);

        int SatirYukseklikAl(int sheetNo, int satir);

        int SatirYukseklikAl(int satir);

        void SutunGizle(int sutun1, int sutun2, bool gizle);

        void SatirGizle(int satir1, int satir2, bool gizle);

        void SatirSil(int satir1, int satir2);

        void SutunSil(int sutun1, int sutun2);

        void SutunGenislikAyarlaPixel(int sutun1, int sutun2, int genislik);
        void SutunGenislikAyarla(int sutun1, int sutun2, int genislik);

        void SutunGenislikAyarla(int sutun1, int sutun2, int genislik, int ekle, int minGenislik);

        void SutunGenislikAyarla(int sheetNo, int sutun1, int sutun2, int genislik, int ekle, int minGenislik);

        int SutunGenislikAl(int sheetNo, int sutun);

        int SutunGenislikAl(int sutun);

        void DuseyCizgiCiz(int satir1, int satir2, int sutun, LineStyle stil, TabloRenk renk, bool solMu);

        void DuseyCizgiCiz(int satir1, int satir2, int sutun, LineStyle stil, TabloRenk renk);

        void YatayCizgiCiz(int satir, int sutun1, int sutun2, LineStyle stil, TabloRenk renk, bool ustMu);

        void YatayCizgiCiz(int satir, int sutun1, int sutun2, LineStyle stil, TabloRenk renk);

        void CerceveCizgiCiz(int satir1, int satir2, int sutun1, int sutun2, LineStyle stil, TabloRenk renk);

        void CerceveCiz(int satir1, int satir2, int sutun1, int sutun2, LineStyle stil, TabloRenk renk);

        void ZoomSheet(int zf);

        void ZoomYazici(int zf);

        void HucreMetniKaydir(int satir1, int sutun1, int satir2, int sutun2, bool deger);

        void HucreMetniKaydir(int satir, int sutun, bool deger);

        void KoyuYap(int satir1, int sutun1, int satir2, int sutun2, bool deger);

        void KoyuYap(int satir, int sutun, bool deger);

        void YaziTipiAta(int satir1, int sutun1, int satir2, int sutun2, string fontAd);

        void YaziTipBuyuklugu(int satir, int sutun, int deger);

        void YaziTipBuyuklugu(int satir1, int sutun1, int satir2, int sutun2, int deger);

        void DuseyHizala(int satir1, int sutun1, int satir2, int sutun2, int deger);

        void DuseyHizala(int satir, int sutun, int deger);

        void YatayHizala(int satir1, int sutun1, int satir2, int sutun2, int deger);

        void YatayHizala(int satir, int sutun, int deger);

        void ArkaPlanRenk(int satir1, int sutun1, int satir2, int sutun2, System.Drawing.Color renk);

        void ArkaPlanRenk(int satir, int sutun, System.Drawing.Color renk);

        void ArkaPlanRenk(int satir1, int sutun1, int satir2, int sutun2, TabloRenk renk);

        void ArkaPlanRenk(int satir, int sutun, TabloRenk renk);

        void YaziRenk(int satir1, int sutun1, int satir2, int sutun2, TabloRenk renk);

        void YaziRenk(int satir, int sutun, TabloRenk renk);

        void YaziRenk(int satir1, int sutun1, int satir2, int sutun2, System.Drawing.Color renk);

        void YaziRenk(int satir, int sutun, System.Drawing.Color renk);

        void CokluSatirdaYaz(int satir, int sutun, bool deger);

        void AktifSheetDegistir(int sheetNo);

        int AktifSheet();

        int YeniSheetEkle();

        int YeniSheetEkle(int kaynakSheetNo);

        void SheetSil(int sheetNo);

        void SheetAdiVer(int sheetNo, string adi);

        string SheetAdiAl();
        string SheetAdiAl(int sheetNo);

        string CellIndexToName(int row, int column);

        void CellNameToIndex(string name, out int row, out int column);

        void SayfaSonuKoySutun(int sutun);

        void SayfaSonuKoy(int satir);

        void SayfaSonuKoyHucresel(int satir);

        void IlkSayfaNumarasi(int sayfaNo);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="yer">1 ise alt, değil ise üst</param>
        /// <param name="yanasik">1 ise sol, 2 ise sağ, değil ise orta</param>
        /// <param name="deger"></param>
        void HFDegerAta(int yer, int yanasik, string deger);

        void SelectSheet(int sheetNo);

        void SelectSheet();

        void ResimEkle(double x, double y, double width, double height, string dosya);

        void ResimEkle(double x, double y, double width, double height, Stream stream);

        object ResimEkle(double x, double y, double width, double height, Stream stream, double left, double top, int enBoyOran, int yanasiklik);

        void DosyaAc(string dosyaAd, string sonucDosya);

        void DosyaAc(string dosyaAd);

        void BosDosyaAc(string sonucDosya);

        void DosyaOkuAc(string dosyaAd);

        void DosyaOkuAc(System.IO.Stream dosyaStream);

        void DosyaSaklaTamYol();

        void DosyaKapat();

        string UzantiBul();

        void YazdirmaYineleme(int sheetNo, string yineleSatir, string yineleSutun);

        void DosyaSaklamaFormatAta(string uzanti);

        void CalculateFormula();

        void HucreFormatla(int satir1, int sutun1, int satir2, int sutun2, string deger);

        void HucreMetniSigdir(int satir1, int sutun1, int satir2, int sutun2, bool deger);

        void HucreMetniSigdir(int satir, int sutun, bool deger);

        void SayfaYonuAta(SayfaYonu sayfaYonu);

        void TekrarlanacakSatirlar(int satir1, int satir2);

        void TekrarlanacakSutunlar(int sutun1, int sutun2);

        void AltaltaSayfaSayisi(int sayfaSayisi);

        void YanyanaSayfaSayisi(int sayfaSayisi);

        void FormulleriSil();

        void Sirala(int order1, int key1, int satir1, int sutun1, int satir2, int sutun2);
    }

}
