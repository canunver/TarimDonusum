using TarimDonusum.Araclar;
using TarimDonusum.Models;

namespace TarimDonusum.Raporlar;

public sealed class RPROB_BilancoGelir(string uygulamaRootPath) : RPROBTemel(uygulamaRootPath)
{
    protected override string SablonAdi => "BilancoGelir.xltx";
    protected override string GeciciDosyaOnEki => "bilanco-gelir";
    protected override string CiktiDosyaOnEki => "Bilanco-Gelir-Analizi";
    protected override void Doldur(Tablo tablo, Basvuru basvuru)
    {
        int yil = basvuru.basvuruFirma.donem?.yil ?? 0;
        for (int c=0;c<3;c++) tablo.HucreDegerYaz(2,c+1,yil>0 ? (yil-(c+1)).ToString() : "");
        Dictionary<string,BasvuruBilancoGelirSatiri> degerler=basvuru.bilancoGelir.satirlar.ToDictionary(x=>x.kod,StringComparer.OrdinalIgnoreCase);
        foreach(var t in BilancoGelirTanimlari.GirisSatirlari)
        {
            degerler.TryGetValue(t.Kod,out var s); decimal?[] y=[s?.yil_1,s?.yil_2,s?.yil_3];
            for(int c=0;c<3;c++) if(y[c].HasValue) tablo.HucreDegerYaz(t.ExcelSatiri-1,c+1,y[c]!.Value);
        }
        for(int c=0;c<3;c++)
        {
            BilancoGelirHesapSonucu h=BilancoGelirHesaplayici.Hesapla(basvuru.bilancoGelir.satirlar,c+1);
            decimal?[] sonuc=[h.AktifToplami,h.PasifToplami,h.NetSatislar,h.BrutSatisKari,h.FaaliyetKari,h.OlaganKar,h.DonemKari,h.NetKar,h.NetIsletmeSermayesi,h.CariOran,h.LikiditeOrani,h.FinansmanOrani,h.FaaliyetKarliligi,h.BilancoKarliligi,h.OzsermayeKarliligi,h.AktifKarliligi,h.Favok];
            int[] satirlar=[9,14,19,21,23,27,30,32,35,36,37,38,39,40,41,42,43];
            for(int i=0;i<satirlar.Length;i++)
                if(sonuc[i].HasValue) tablo.HucreDegerYaz(satirlar[i]-1,c+1,sonuc[i]!.Value);
                else tablo.HucreDegerYaz(satirlar[i]-1,c+1,"");
        }
        tablo.FormulleriSil();
    }
}
