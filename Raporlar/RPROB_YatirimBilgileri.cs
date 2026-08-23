using TarimDonusum.Araclar;
using TarimDonusum.Models;

namespace TarimDonusum.Raporlar;

public sealed class RPROB_YatirimBilgileri(string uygulamaRootPath) : RPROBTemel(uygulamaRootPath)
{
    protected override string SablonAdi => "YatirimBilgileri.xltx";
    protected override string GeciciDosyaOnEki => "yatirim-bilgileri";
    protected override string CiktiDosyaOnEki => "YatirimBilgileri";

    protected override void Doldur(Tablo tablo, Basvuru basvuru)
    {
        BasvuruYatirim y = basvuru.yatirim;
        List<BasvuruUygulamaAdresi> adresler = (basvuru.YatirimAdresleri ?? []).OrderBy(x => x.siraNo).ToList();
        Yaz(tablo, 5, 2, y.yatirimAdi); Yaz(tablo, 5, 6, string.Join(", ", y.yatirimTurleri.Select(YatirimTuruAdi)));
        Yaz(tablo, 6, 2, adresler.Count); Yaz(tablo, 6, 6, y.basvuruKonusuTesis);
        Yaz(tablo, 7, 2, string.Join(", ", y.harcamaTurleri.Select(HarcamaTuruAdi))); Yaz(tablo, 7, 6, y.organizeAlanTuru);
        Yaz(tablo, 8, 2, y.planlananBaslangicTarihi); Yaz(tablo, 8, 6, y.planlananTamamlanmaTarihi);
        Yaz(tablo, 10, 2, string.Join(Environment.NewLine, new[] { y.yatiriminAmaci, y.yatirimFaaliyetleri, y.yatirimGirdileri, y.yatirimCiktilari }.Where(x => !string.IsNullOrWhiteSpace(x))));

        int ekAdres = Math.Max(0, adresler.Count - 3);
        if (ekAdres > 0) { tablo.SatirAc(16, ekAdres); for (int i=0;i<ekAdres;i++) tablo.HucreKopyala(15,0,15,7,16+i,0); }
        for (int i=0;i<adresler.Count;i++)
        {
            BasvuruUygulamaAdresi a=adresler[i]; int r=14+i;
            Yaz(tablo,r,1,a.siraNo); Yaz(tablo,r,2,a.ilAdi); Yaz(tablo,r,3,a.ilceAdi); Yaz(tablo,r,4,a.segeKademesi);
            Yaz(tablo,r,5,a.tamAdres); Yaz(tablo,r,6,a.koordinat); Yaz(tablo,r,7,a.adaParsel); Yaz(tablo,r,8,a.adresBelgeDosyaAdi);
        }

        int hakBaslangic=20+ekAdres;
        int ekHak=Math.Max(0,adresler.Count-1);
        if(ekHak>0){int ekle=hakBaslangic;tablo.SatirAc(ekle,ekHak);for(int i=0;i<ekHak;i++)tablo.HucreKopyala(ekle-1,0,ekle-1,7,ekle+i,0);}
        for(int i=0;i<adresler.Count;i++)
        {
            BasvuruUygulamaAdresi a=adresler[i];int r=hakBaslangic+i;
            Yaz(tablo,r,1,YatirimYeriStatusuAdi(a.yatirimYeriStatusu)); Yaz(tablo,r,2,a.kullanimHakkiBaslangicTarihi); Yaz(tablo,r,3,a.kiraTahsisBitisTarihi);
            Yaz(tablo,r,4,a.donemleriKapsiyorMu.HasValue?(a.donemleriKapsiyorMu.Value?"Evet":"Hayır"):""); Yaz(tablo,r,5,a.kullanimHakkiDosyaAdi);
            Yaz(tablo,r,6,YapiRuhsatiAdi(a.yapiRuhsatiDurumu)); Yaz(tablo,r,7,a.izinTakvimAciklama); Yaz(tablo,r,8,a.kanitDosyaAdi);
        }
    }

    private static string YatirimTuruAdi(int v)=>((enumYatirimTuru)v) switch { enumYatirimTuru.Yeni=>"Yeni",enumYatirimTuru.KapasiteArtirimi=>"Kapasite Artırımı",enumYatirimTuru.Modernizasyon=>"Modernizasyon",enumYatirimTuru.TeknolojiYenileme=>"Teknoloji Yenileme",_=>""};
    private static string HarcamaTuruAdi(int v)=>((enumHarcamaTuru)v) switch {enumHarcamaTuru.YapimIsleri=>"Yapım İşleri",enumHarcamaTuru.MakineEkipman=>"Makine Ekipman",enumHarcamaTuru.Danismanlik=>"Danışmanlık",enumHarcamaTuru.TedarikciGelistirmeHarcamalari=>"Tedarikçi Geliştirme Harcamaları",enumHarcamaTuru.YazilimDonanım=>"Yazılım Donanım",_=>""};
    private static string YatirimYeriStatusuAdi(enumUygulamaAdresiYatirimYeriStatusu v)=>v switch {enumUygulamaAdresiYatirimYeriStatusu.Mulkiyet=>"Mülkiyet",enumUygulamaAdresiYatirimYeriStatusu.Kira=>"Kira",enumUygulamaAdresiYatirimYeriStatusu.Tahsis=>"Tahsis",enumUygulamaAdresiYatirimYeriStatusu.IrtifakHakki=>"İrtifak Hakkı",enumUygulamaAdresiYatirimYeriStatusu.OrganizeSanayi_IhtisasAlaniTahsisi=>"Organize Sanayi / İhtisas Alanı Tahsisi",enumUygulamaAdresiYatirimYeriStatusu.Diger=>"Diğer",_=>""};
    private static string YapiRuhsatiAdi(enumUygulamaAdresiYapiRuhsatiDurumu v)=>v switch {enumUygulamaAdresiYapiRuhsatiDurumu.YapiRuhsatiMevcut=>"Yapı ruhsatı mevcut",enumUygulamaAdresiYapiRuhsatiDurumu.YapiRuhsatiBasvurusuYapildi=>"Yapı ruhsatı başvurusu yapıldı",enumUygulamaAdresiYapiRuhsatiDurumu.RuhsatGerekmedigineDairYaziMevcut=>"Ruhsat gerekmediğine dair yazı mevcut",enumUygulamaAdresiYapiRuhsatiDurumu.HenuzTeminEdilmedi=>"Henüz temin edilmedi",enumUygulamaAdresiYapiRuhsatiDurumu.YapimIsiYok=>"Yapım işi yok",_=>""};
    private static void Yaz(Tablo t,int r,int c,string? v)=>t.HucreDegerYaz(r-1,c-1,v??"");
    private static void Yaz(Tablo t,int r,int c,int v)=>t.HucreDegerYaz(r-1,c-1,v);
    private static void Yaz(Tablo t,int r,int c,DateTime? v){if(v.HasValue)t.HucreDegerYaz(r-1,c-1,v.Value);else t.HucreDegerYaz(r-1,c-1,"");}
}