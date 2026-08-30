using System.Text.Json;

namespace TarimDonusum.Models;

public sealed record BasvuruOzetiTamlikSatiri(string Kod,string Bolum,int Beklenen,int Dolu,string Kaynak,string KritikNot)
{
    public string Durum => Dolu >= Beklenen ? "Uygun" : "Eksik";
}

public sealed record BasvuruOzetiUygunlukSatiri(string Kod,string Kriter,string BasvuruDegeri,string OnSonuc,string Kaynak,string BelgeTeyit);

public sealed class BasvuruOzetiHesapSonucu
{
    public string YatirimYeri { get; set; } = "";
    public string DegerZinciri { get; set; } = "";
    public decimal? OnSiralamaPuani { get; set; }
    public decimal BelgeTamamlanmaOrani { get; set; }
    public string GenelDurum { get; set; } = "";
    public List<BasvuruOzetiTamlikSatiri> Tamlik { get; set; } = [];
    public List<BasvuruOzetiUygunlukSatiri> Uygunluk { get; set; } = [];
}

public static class BasvuruOzetiHesaplayici
{
    public static BasvuruOzetiHesapSonucu Hesapla(Basvuru b)
    {
        static int Dolu(params bool[] alanlar) => alanlar.Count(x => x);
        static bool Var(string? x) => !string.IsNullOrWhiteSpace(x);
        Firma f=b.basvuruFirma.firma;
        BasvuruUygulamaAdresi? adres=b.YatirimAdresleri.OrderByDescending(x=>int.TryParse(x.segeKademesi,out int s)?s:0).ThenBy(x=>x.siraNo).FirstOrDefault();
        string dz=b.yatirim.degerZinciriAsamalari.FirstOrDefault()?.dz?.ad??"";
        List<BasvuruOzetiTamlikSatiri> t=
        [
            new("sahibi","1 Başvuru Sahibi",12,Dolu(Var(f.ticaretUnvani),Var(f.vergiKimlikNo),Var(f.ticaretSicilNo),Var(f.mersisNo),f.kurulusTarihi.HasValue,Var(f.naceKodu),Var(f.faaliyetKonusu),Var(f.adres),Var(b.irtibat.kisi),Var(b.irtibat.telefon),Var(b.irtibat.ePosta),Var(b.irtibat.unvan)),"Başvuru Sahibi","Kimlik/iletişim"),
            new("mali","2 Mali Veriler",6,Dolu(b.mali.oncekiYilNetSatis>0,b.mali.sonYilNetSatis>0,b.mali.oncekiYilAktifToplami>0,b.mali.sonYilAktifToplami>0,b.mali.oncekiYilCalisanSayisi.HasValue,b.mali.sonYilCalisanSayisi.HasValue),"Mali Veriler","İki yıl ve belgeler"),
            new("ortaklik","3 Ortaklık Yetki",4,Dolu(b.ortaklik.ortaklar.Count>0,b.ortaklik.ortaklar.Sum(x=>x.payOrani.GetValueOrDefault())==100,b.ortaklik.ozelSektorPayi.HasValue,b.ortaklik.ortaklar.All(x=>Var(x.adUnvan)&&Var(x.tcknVkn))),"Ortaklık Yetki","Pay toplamı ve UBO"),
            new("yatirim","4 Yatırım Bilgileri",8,Dolu(Var(b.yatirim.yatirimAdi),b.YatirimAdresleri.Count>0,adres?.ilceId>0,Var(adres?.tamAdres),b.yatirim.yatirimTurleri.Count>0,b.yatirim.harcamaTurleri.Count>0,Var(b.yatirim.yatirimFaaliyetleri),b.yatirim.planlananBaslangicTarihi.HasValue),"Yatırım Bilgileri","İl/ilçe ve yer hakkı"),
            new("deger","5 Değer Zinciri",5,Dolu(b.yatirim.degerZinciriId>0,b.yatirim.degerZinciriAsamalari.Count>0,Var(b.yatirim.ilDegerZinciriEslesmesi),Var(b.yatirim.tarimGidaBaglantiTuru),Var(b.yatirim.degerZinciriUygunlukAciklamasi)),"Değer Zinciri","İl-zincir eşleşmesi"),
            new("harcama","6 Uygun Harcama",1,Dolu(Var(b.uygunHarcama.pikkListesiJson)),"Uygun Harcama","En az bir kalem"),
            new("finans","7 Finansman",5,Dolu(b.finans.toplamYatirimTutari>0,b.finans.uygunHarcamaTutari>0,b.finans.talepEdilenDestekTutari>0,b.finans.basvuruSahibiKatkisi>=0,b.finans.talepEdilenVadeSuresiAy>0),"Finansman","RFF ve katkı"),
            new("ozet","8 Yatırım Özeti",3,Dolu(Var(b.yatirimOzeti.yatirimOzetiJson),b.YatirimOnBilgileri.Any(x=>x.tur==enumYatirimOnBilgiTuru.UretilecekUrun),b.YatirimOnBilgileri.Any(x=>x.tur==enumYatirimOnBilgiTuru.Girdi)),"Yatırım Özeti","Üretim/gider bilgileri"),
            new("teknik","9 Teknik Proje",3,Dolu(b.YatirimOnBilgileri.Count>0,b.Makineler.Count>0,b.UrunSurecleri.Count>0),"Teknik Proje","Ürün/girdi/makine"),
            new("belge","10 Belgeler",1,Dolu(b.TumBasvuruDosyalari.Count>0||b.ZorunluBelgeler.Count>0),"Zorunlu Belgeler","Uygulanabilir belgeler"),
            new("cs","11 Ç&S",1,Dolu(Var(b.cevreselSosyal.cevreselSosyalJson)),"Ç&S Veri Formu","Beyan dahil"),
            new("beyan","12 Beyanlar",21,BeyanKabulSayisi(b.TaahhutBeyanlarJson),"Beyanlar","Tüm zorunlu beyanlar"),
            new("izleme","15 İzleme",3,Dolu(b.IzlemeGostergeleri.Count>0,b.IzlemeGostergeleri.Count>0&&b.IzlemeGostergeleri.All(x=>Var(x.baslangicDegeri)&&Var(x.hedefDeger)),b.IzlemeGostergeleri.Any(x=>Var(x.aciklama))),"İzleme Göstergeleri","Başlangıç/hedef/açıklama")
        ];
        DateTime referans=b.basvuruFirma.donem.basvuruBaslangicTarihi?.Date??DateTime.Today;
        decimal? faaliyetYili=f.kurulusTarihi.HasValue?(decimal)Math.Round((referans-f.kurulusTarihi.Value.Date).TotalDays/365.25,1):null;
        decimal ozelPay=b.ortaklik.ozelSektorPayi??b.basvuruFirma.ozelSektorPayi??0;
        bool maliTam=b.mali.oncekiYilNetSatis>0&&b.mali.sonYilNetSatis>0&&b.mali.oncekiYilAktifToplami>0&&b.mali.sonYilAktifToplami>0;
        decimal talep=b.finans.talepEdilenDestekTutari.GetValueOrDefault(),kumulatif=b.finans.oncekiRffOnayliTutar.GetValueOrDefault()+talep;
        decimal alt=b.basvuruFirma.donem.minimumYatirimTutari.GetValueOrDefault(),ust=b.basvuruFirma.donem.maksimumYatirimTutari.GetValueOrDefault();
        bool rffUygun=talep>0&&(alt<=0||talep>=alt)&&(ust<=0||talep<=ust)&&(ust<=0||kumulatif<=ust);
        string rff=b.finans.talepEdilenDestekTutari.HasValue?$"{talep:N0} USD / {kumulatif:N0} USD":"";
        List<BasvuruOzetiUygunlukSatiri> u=
        [
            new("faaliyet","Faaliyet süresi en az 2 yıl",faaliyetYili.HasValue?$"{faaliyetYili:0.0} yıl":"",faaliyetYili>=2?"Uygun":"Eksik","Başvuru Sahibi","Ticaret sicil"),
            new("ozelPay","Özel sektör payı en az %75",$"%{ozelPay:0.##}",ozelPay>=75?"Uygun":"Eksik","Ortaklık Yetki","Ortaklık belgesi"),
            new("maliOlcek","Mali ölçek bandı",maliTam?"Hesaplanabilir":"Eksik veri",maliTam?"Uygun":"Eksik","Mali Veriler","Mali tablolar"),
            new("ilDz","İl–değer zinciri eşleşmesi",b.yatirim.ilDegerZinciriEslesmesi??"",b.yatirim.ilDegerZinciriEslesmesi=="Evet"?"Uygun":"Eksik","Değer Zinciri","Sistem/liste"),
            new("rff","RFF dönem limiti ve kümülatif tavan",rff,rffUygun?"Uygun":"Eksik","Finansman","Kurum/banka"),
            new("beyanlar","Zorunlu beyanların kabulü",$"{BeyanKabulSayisi(b.TaahhutBeyanlarJson)}/21",BeyanKabulSayisi(b.TaahhutBeyanlarJson)>=21?"Uygun":"Eksik","Beyanlar","İmzalı beyan")
        ];
        int beklenen=t.Sum(x=>x.Beklenen),dolu=t.Sum(x=>Math.Min(x.Dolu,x.Beklenen));
        return new(){YatirimYeri=string.Join(" / ",new[]{adres?.ilAdi??b.basvuruFirma.il.ad,adres?.ilceAdi??""}.Where(Var)),DegerZinciri=dz,Tamlik=t,Uygunluk=u,BelgeTamamlanmaOrani=beklenen>0?(decimal)dolu/beklenen:0,GenelDurum=t.All(x=>x.Durum=="Uygun")&&u.All(x=>x.OnSonuc=="Uygun")?"Ön kontrol tamamlandı":"Eksikler bulunuyor"};
    }

    private static int BeyanKabulSayisi(string? json)
    {
        if(string.IsNullOrWhiteSpace(json))return 0;
        try{using JsonDocument d=JsonDocument.Parse(json);if(!d.RootElement.TryGetProperty("satirlar",out JsonElement s)||s.ValueKind!=JsonValueKind.Array)return 0;return s.EnumerateArray().Count(x=>x.TryGetProperty("kabul",out JsonElement k)&&k.TryGetInt32(out int v)&&v==1);}
        catch{return 0;}
    }
}
