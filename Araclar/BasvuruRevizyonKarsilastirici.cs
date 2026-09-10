using System.Globalization;
using System.Reflection;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using TarimDonusum.Models;

namespace TarimDonusum.Araclar;

public sealed record BasvuruAlanDegisikligi(string Bolum, string Alan, string OncekiDeger, string YeniDeger);

public static class BasvuruRevizyonKarsilastirici
{
    public static readonly string[] Bolumler = ["1. Başvuru Sahibi ve İletişim","2. Ortaklık Yapısı","3. Yatırım Bilgileri ve Adresler","4. Değer Zinciri","5. Ürünler","6. Bütçe ve Giderler","7. Finansman","8. Malzemeler ve Enerji Kullanımı","9. Makine ve Ekipman","10. Bina Listesi","11. Belgeler","12. Çevresel ve Sosyal Anket"];

    public static List<BasvuruAlanDegisikligi> Karsilastir(Basvuru e, Basvuru y)
    {
        List<BasvuruAlanDegisikligi> d=[]; BasvuruSahibi(e,y,d); Ortaklik(e,y,d); YatirimAdres(e,y,d); DegerZinciri(e,y,d);
        Kalemler(Bolumler[4],"Ürün",e,y,[enumYatirimOnBilgiTuru.MevcutUrun,enumYatirimOnBilgiTuru.UretilecekUrun],d);
        Butce(e,y,d); Finans(e,y,d); Kalemler(Bolumler[7],"",e,y,[enumYatirimOnBilgiTuru.Girdi,enumYatirimOnBilgiTuru.EnerjiKullanimi,enumYatirimOnBilgiTuru.KuruluGuc],d);
        Makineler(e,y,d); Binalar(e,y,d); Belgeler(e,y,d); Anket(e,y,d); return d;
    }

    private static void BasvuruSahibi(Basvuru e,Basvuru y,List<BasvuruAlanDegisikligi>d){string b=Bolumler[0];
        Alanlar(b,"Firma",e.basvuruFirma.firma,y.basvuruFirma.firma,d,("ticaretUnvani","Ticaret unvanı"),("ticaretSicilNo","Ticaret sicil no"),("kurulusTarihi","Kuruluş tarihi"),("mersisNo","MERSİS no"),("naceKodu","NACE kodu"),("webSitesi","Web sitesi"),("telefon","Firma telefonu"),("kepAdresi","KEP adresi"),("eposta","Firma iletişim e-postası"),("faaliyetKonusu","Faaliyet konusu"),("adres","Firma iletişim adresi"));
        Alanlar(b,"İrtibat",e.irtibat,y.irtibat,d,("kisi","İletişim kişisi"),("unvan","Unvan"),("telefon","Telefon"),("ePosta","E-posta"),("adres","İletişim adresi"),("yetkiliKisiler","Yetkili kişiler"));
        Eslesmis(b,"Adli sicil kişisi",e.AdliSicilKisileri,y.AdliSicilKisileri,x=>Key(x.tckn,x.ad,x.soyad),x=>$"{x.ad} {x.soyad}".Trim(),d,("ad","Ad"),("soyad","Soyad"),("gorev","Görev"),("yetkiKapsami","Yetki kapsamı"),("aciklama","Açıklama"));}

    private static void Ortaklik(Basvuru e,Basvuru y,List<BasvuruAlanDegisikligi>d)=>Eslesmis(Bolumler[1],"Ortak",e.ortaklik.ortaklar,y.ortaklik.ortaklar,x=>Key(x.tcknVkn,x.adUnvan),x=>x.adUnvan??"Ortak",d,("adUnvan","Ad/unvan"),("kisiTuru","Kişi türü"),("payOrani","Pay oranı"),("hesabaDahilOran","Hesaba dahil oran"),("ozelKamuNiteligi","Özel/kamu niteliği"),("dogumTarihi","Doğum tarihi"),("cinsiyet","Cinsiyet"),("sahiplikNiteligi","Sahiplik niteliği"),("nihaiFaydalaniciBilgisi","Nihai faydalanıcı"),("oncekiYilNetSatis","Önceki yıl net satış"),("sonYilNetSatis","Son yıl net satış"),("oncekiYilAktifToplami","Önceki yıl aktif toplamı"),("sonYilAktifToplami","Son yıl aktif toplamı"),("iliskiTuru","İlişki türü"));

    private static void YatirimAdres(Basvuru e,Basvuru y,List<BasvuruAlanDegisikligi>d){string b=Bolumler[2];
        Alanlar(b,"Yatırım",e.yatirim,y.yatirim,d,("yatirimAdi","Yatırım adı"),("yatiriminAmaci","Yatırımın amacı"),("basvuruKonusuTesis","Başvuru konusu/tesis"),("organizeAlanTuru","Organize alan türü"),("planlananBaslangicTarihi","Planlanan başlangıç"),("planlananTamamlanmaTarihi","Planlanan tamamlanma"),("yatirimAlaniTipolojisi","Yatırım alanı tipolojisi"));
        var ea=e.YatirimAdresleri.GroupBy(x=>x.siraNo).ToDictionary(x=>x.Key,x=>x.First());var ya=y.YatirimAdresleri.GroupBy(x=>x.siraNo).ToDictionary(x=>x.Key,x=>x.First());
        foreach(int no in ea.Keys.Union(ya.Keys).OrderBy(x=>x)){if(!ea.TryGetValue(no,out var ev)){Add(d,b,$"Yatırım adresi {no}",null,"Adres eklendi");continue;}if(!ya.TryGetValue(no,out var yv)){Add(d,b,$"Yatırım adresi {no}","Adres silindi",null);continue;}
            Alanlar(b,$"Adres {no}",ev,yv,d,("ilAdi","İl"),("ilceAdi","İlçe"),("tamAdres","Açık adres"),("yatirimYeriStatusu","Yatırım yeri statüsü"),("kiraVeyaTahsisSuresi","Kira/tahsis süresi"),("kiraTahsisBitisTarihi","Kira/tahsis bitiş tarihi"),("koordinat","Koordinat"),("ada","Ada"),("parsel","Parsel"),("enlem","Enlem"),("boylam","Boylam"),("kullanimHakkiBaslangicTarihi","Kullanım hakkı başlangıcı"),("donemleriKapsiyorMu","Dönemleri kapsıyor mu"),("izinTakvimAciklama","İzin/takvim açıklaması"),("yapiRuhsatiDurumu","Yapı ruhsatı durumu"),("yatirimFaaliyetleri","Faaliyetler"),("yatirimGirdileri","Girdiler"),("yatirimCiktilari","Çıktılar"));
            Diff(d,b,$"Adres {no} / Yatırım türleri",string.Join(", ",ev.yatirimTurleri.OrderBy(x=>x)),string.Join(", ",yv.yatirimTurleri.OrderBy(x=>x)));Diff(d,b,$"Adres {no} / Harcama türleri",string.Join(", ",ev.harcamaTurleri.OrderBy(x=>x)),string.Join(", ",yv.harcamaTurleri.OrderBy(x=>x)));}}

    private static void DegerZinciri(Basvuru e,Basvuru y,List<BasvuruAlanDegisikligi>d){string b=Bolumler[3];var ea=e.yatirim.degerZinciriAsamalari.Where(x=>x.secili).GroupBy(x=>Key(x.ad)).ToDictionary(x=>x.Key,x=>x.First());var ya=y.yatirim.degerZinciriAsamalari.Where(x=>x.secili).GroupBy(x=>Key(x.ad)).ToDictionary(x=>x.Key,x=>x.First());foreach(string k in ea.Keys.Union(ya.Keys)){if(!ea.TryGetValue(k,out var ev)){Add(d,b,"Değer zinciri aşaması",null,$"{ya[k].ad} eklendi");continue;}if(!ya.TryGetValue(k,out var yv)){Add(d,b,"Değer zinciri aşaması",$"{ev.ad} kaldırıldı",null);continue;}Diff(d,b,$"{ev.ad} / Yapılacak faaliyetler",ev.yapilacakFaaliyetler,yv.yapilacakFaaliyetler);}}

    private static void Kalemler(string b,string varsayilanTur,Basvuru e,Basvuru y,enumYatirimOnBilgiTuru[] turler,List<BasvuruAlanDegisikligi>d)
    {
        var ea=e.YatirimOnBilgileri.Where(x=>turler.Contains(x.tur)).GroupBy(x=>$"{x.tur}:{Key(x.ad)}").ToDictionary(x=>x.Key,x=>x.First());
        var ya=y.YatirimOnBilgileri.Where(x=>turler.Contains(x.tur)).GroupBy(x=>$"{x.tur}:{Key(x.ad)}").ToDictionary(x=>x.Key,x=>x.First());
        foreach(string k in ea.Keys.Union(ya.Keys))
        {
            if(!ea.TryGetValue(k,out var ev)){var n=ya[k];Add(d,b,$"{KalemTuru(n,varsayilanTur)} / {n.ad}",null,$"Eklendi: {KalemMetni(n)}");continue;}
            if(!ya.TryGetValue(k,out var yv)){Add(d,b,$"{KalemTuru(ev,varsayilanTur)} / {ev.ad}",$"Kaldırıldı: {KalemMetni(ev)}",null);continue;}
            string on=$"{KalemTuru(ev,varsayilanTur)} / {ev.ad}";
            MiktarFarki(d,b,$"{on} / Miktar",ev.miktar,ev.birim,yv.miktar,yv.birim);
            MiktarFarki(d,b,$"{on} / Mevcut kapasite",ev.mevcutKapasite,ev.birim,yv.mevcutKapasite,yv.birim);
            MiktarFarki(d,b,$"{on} / Birinci yıl kapasite",ev.birinciYilKapasite,ev.birim,yv.birinciYilKapasite,yv.birim);
            MiktarFarki(d,b,$"{on} / Kurulu güç",ev.toplamGuc,ev.toplamGucBirim,yv.toplamGuc,yv.toplamGucBirim);
            MiktarFarki(d,b,$"{on} / Tek panel gücü",ev.tekPanelGucu,ev.tekPanelGucuBirim,yv.tekPanelGucu,yv.tekPanelGucuBirim);
        }
    }
    private static string KalemTuru(BasvuruYatirimOnBilgi x,string varsayilan)=>x.tur switch{enumYatirimOnBilgiTuru.Girdi=>"Girdi/Hammadde",enumYatirimOnBilgiTuru.EnerjiKullanimi=>"Enerji Kullanımı",enumYatirimOnBilgiTuru.KuruluGuc=>"Kurulu Güç",enumYatirimOnBilgiTuru.MevcutUrun=>"Mevcut Ürün",enumYatirimOnBilgiTuru.UretilecekUrun=>"Planlanan Ürün",_=>varsayilan};
    private static void MiktarFarki(List<BasvuruAlanDegisikligi>d,string b,string alan,decimal? eski,string? eskiBirim,decimal? yeni,string? yeniBirim)=>Diff(d,b,alan,eski.HasValue?$"{Text(eski)} {eskiBirim}".Trim():null,yeni.HasValue?$"{Text(yeni)} {yeniBirim}".Trim():null);
    private static string KalemMetni(BasvuruYatirimOnBilgi x)=>string.Join("; ",new[]{x.miktar.HasValue?$"Miktar {Text(x.miktar)} {x.birim}":"",x.mevcutKapasite.HasValue?$"Mevcut kapasite {Text(x.mevcutKapasite)}":"",x.birinciYilKapasite.HasValue?$"1. yıl kapasite {Text(x.birinciYilKapasite)}":"",x.toplamGuc.HasValue?$"Toplam güç {Text(x.toplamGuc)} {x.toplamGucBirim}":"",x.tekPanelGucu.HasValue?$"Tek panel gücü {Text(x.tekPanelGucu)} {x.tekPanelGucuBirim}":""}.Where(x=>x.Length>0));

    private static void Butce(Basvuru e,Basvuru y,List<BasvuruAlanDegisikligi>d){Diff(d,Bolumler[5],"Toplam yatırım bütçesi",e.yatirimOzeti.toplamYatirimButcesiTl,y.yatirimOzeti.toplamYatirimButcesiTl);Diff(d,Bolumler[5],"Toplam işletme gideri",Gider(e.yatirimOzeti.yatirimOzetiJson),Gider(y.yatirimOzeti.yatirimOzetiJson));}
    private static void Finans(Basvuru e,Basvuru y,List<BasvuruAlanDegisikligi>d)=>Alanlar(Bolumler[6],"Finansman",e.finans,y.finans,d,("talepEdilenFinansmanOrani","Talep edilen finansman oranı"),("talepEdilenVadeSuresiAy","Talep edilen vade (ay)"));

    private static void Makineler(Basvuru e,Basvuru y,List<BasvuruAlanDegisikligi>d){var em=AdresMap(e);var ym=AdresMap(y);foreach(int no in em.Values.Union(ym.Values).Distinct().OrderBy(x=>x))Diff(d,Bolumler[8],$"Adres {no} / Makine listesi",MakineMetni(e.Makineler.Where(x=>AdresNo(x.uygulamaAdresiId,em)==no)),MakineMetni(y.Makineler.Where(x=>AdresNo(x.uygulamaAdresiId,ym)==no)));}
    private static void Binalar(Basvuru e,Basvuru y,List<BasvuruAlanDegisikligi>d){var em=AdresMap(e);var ym=AdresMap(y);foreach(int no in em.Values.Union(ym.Values).Distinct().OrderBy(x=>x))Diff(d,Bolumler[9],$"Adres {no} / Bina listesi",BinaMetni(e.Binalar.Where(x=>AdresNo(x.uygulamaAdresiId,em)==no)),BinaMetni(y.Binalar.Where(x=>AdresNo(x.uygulamaAdresiId,ym)==no)));}
    private static Dictionary<int,int> AdresMap(Basvuru b)=>b.YatirimAdresleri.GroupBy(x=>x.id).ToDictionary(x=>x.Key,x=>x.First().siraNo);
    private static int AdresNo(int? id,Dictionary<int,int> m)=>id.HasValue&&m.TryGetValue(id.Value,out int no)?no:0;
    private static string MakineMetni(IEnumerable<BasvuruMakine> l)=>string.Join("\n",l.OrderBy(x=>x.siraNo).Select(x=>$"{x.ad} | {Text(x.miktar)} {x.birim} | {x.durum} | {x.marka} {x.model} | {x.kapasiteOzellikleri} | Amaç: {x.kullanimAmaci}"));
    private static string BinaMetni(IEnumerable<BasvuruBina> l)=>string.Join("\n",l.OrderBy(x=>x.siraNo).Select(x=>$"{x.ad} | {x.mevcutYeni} | {x.yatirimSekli} | Destek: {x.destekTalebi} | Plan: {x.vaziyetPlaniNo}"));

    private static void Belgeler(Basvuru e,Basvuru y,List<BasvuruAlanDegisikligi>d){var ed=DosyaMap(e);var yd=DosyaMap(y);foreach(string k in ed.Keys.Union(yd.Keys).OrderBy(x=>x))if(!Text(ed.GetValueOrDefault(k)).Equals(Text(yd.GetValueOrDefault(k)),StringComparison.OrdinalIgnoreCase))Add(d,Bolumler[10],$"{k} (Dosya değişti)",ed.GetValueOrDefault(k),yd.GetValueOrDefault(k));}
    private static Dictionary<string,string> DosyaMap(Basvuru b){Dictionary<string,string>d=new(StringComparer.OrdinalIgnoreCase){{"Belge paketi",b.BelgePaketiDosyaAdi},{"Taahhüt belgesi",b.TaahhutDosyaAdi},{"Bağımsız denetim belgesi",b.mali.denetimDosyaAdi}};foreach(var a in b.YatirimAdresleri){d[$"Adres {a.siraNo} / Adres belgesi"]=a.adresBelgeDosyaAdi??"";d[$"Adres {a.siraNo} / Kullanım hakkı belgesi"]=a.kullanimHakkiDosyaAdi??"";d[$"Adres {a.siraNo} / Kanıt belgesi"]=a.kanitDosyaAdi??"";}foreach(var k in b.AdliSicilKisileri){string ad=$"{k.ad} {k.soyad}".Trim();d[$"Adli sicil / {ad}"]=k.dosyaAdi??"";d[$"İmza-yetki / {ad}"]=k.imzaYetkiDosyaAdi??"";}foreach(var o in b.ortaklik.ortaklar){d[$"UBO/KYC / {o.adUnvan}"]=o.uboKycBelgeAdi??"";foreach(var f in o.zorunluBelgeler)d[$"Ortak / {o.adUnvan} / {f.dosyaTuru} {f.dosyaNo}"]=f.dosyaAdi;}foreach(var f in b.ZorunluBelgeler)d[$"Zorunlu belge / {f.dosyaTuru} {f.dosyaNo}"]=f.dosyaAdi;foreach(var m in b.Makineler)foreach(var t in m.teklifler)d[$"Makine teklifi / {m.ad} / {t.siraNo}"]=t.teklifBelgesiDosyaAdi??"";return d;}

    private static void Anket(Basvuru e,Basvuru y,List<BasvuruAlanDegisikligi>d){JsonDiff("Genel anket",e.cevreselSosyal.cevreselSosyalJson,y.cevreselSosyal.cevreselSosyalJson,d);var ea=e.YatirimAdresleri.GroupBy(x=>x.siraNo).ToDictionary(x=>x.Key,x=>x.First());var ya=y.YatirimAdresleri.GroupBy(x=>x.siraNo).ToDictionary(x=>x.Key,x=>x.First());foreach(int no in ea.Keys.Intersect(ya.Keys).OrderBy(x=>x))JsonDiff($"Adres {no}",ea[no].cevreselSosyalJson,ya[no].cevreselSosyalJson,d);}
    private static void JsonDiff(string on,string? e,string? y,List<BasvuruAlanDegisikligi>d){var em=JsonLeaves(e);var ym=JsonLeaves(y);foreach(string k in em.Keys.Union(ym.Keys).OrderBy(x=>x))Diff(d,Bolumler[11],$"{on} / {k}",em.GetValueOrDefault(k),ym.GetValueOrDefault(k));}
    private static Dictionary<string,string> JsonLeaves(string? json){Dictionary<string,string>d=[];try{Walk(JsonNode.Parse(json??""),"",d);}catch{}return d;}
    private static void Walk(JsonNode? n,string p,Dictionary<string,string>d){if(n is JsonObject o){foreach(var x in o)Walk(x.Value,Path(p,x.Key),d);return;}if(n is JsonArray a){for(int i=0;i<a.Count;i++)Walk(a[i],Path(p,$"Satır {i+1}"),d);return;}if(p.Length>0)d[p]=n?.ToString()??"";}
    private static string Path(string p,string n){string s=Regex.Replace(n.Replace('_',' '),"(?<=[a-zçğıöşü0-9])([A-ZÇĞİÖŞÜ])"," $1");return p.Length==0?s:$"{p} / {s}";}

    private static void Eslesmis<T>(string b,string tur,IEnumerable<T> e,IEnumerable<T> y,Func<T,string> key,Func<T,string> ad,List<BasvuruAlanDegisikligi>d,params (string,string)[] alan){var em=e.GroupBy(key).ToDictionary(x=>x.Key,x=>x.First());var ym=y.GroupBy(key).ToDictionary(x=>x.Key,x=>x.First());foreach(string k in em.Keys.Union(ym.Keys)){if(!em.TryGetValue(k,out T? ev)){Add(d,b,tur,null,$"{ad(ym[k])} eklendi");continue;}if(!ym.TryGetValue(k,out T? yv)){Add(d,b,tur,$"{ad(ev)} kaldırıldı",null);continue;}Alanlar(b,$"{tur}: {ad(ev)}",ev!,yv!,d,alan);}}
    private static void Alanlar(string b,string on,object e,object y,List<BasvuruAlanDegisikligi>d,params (string Alan,string Etiket)[] alanlar){Type t=e.GetType();foreach(var a in alanlar){PropertyInfo? p=t.GetProperty(a.Alan,BindingFlags.Public|BindingFlags.Instance|BindingFlags.IgnoreCase);if(p!=null)Diff(d,b,$"{on} / {a.Etiket}",p.GetValue(e),p.GetValue(y));}}
    private static void Diff(List<BasvuruAlanDegisikligi>d,string b,string a,object? e,object? y){string em=Text(e),ym=Text(y);if(!em.Equals(ym,StringComparison.Ordinal))d.Add(new(b,a,em,ym));}
    private static void Add(List<BasvuruAlanDegisikligi>d,string b,string a,object? e,object? y)=>d.Add(new(b,a,Text(e),Text(y)));
    private static string Text(object? v)=>v switch{null=>"—",string s=>string.IsNullOrWhiteSpace(s)?"—":s.Trim(),bool b=>b?"Evet":"Hayır",DateTime t=>t.ToString("dd.MM.yyyy"),decimal n=>n.ToString("N2",CultureInfo.GetCultureInfo("tr-TR")),Enum e=>e.ToString(),_=>v.ToString()??"—"};
    private static string Key(params string?[] v)=>string.Join("|",v.Select(x=>(x??"").Trim().ToUpperInvariant()));
    private static decimal Gider(string? json){try{JsonArray? a=JsonNode.Parse(json??"")?["operatingExpenseRows"] as JsonArray;return a?.Sum(x=>Num(x?["qty"])*Num(x?["unitPrice"]))??0;}catch{return 0;}}
    private static decimal Num(JsonNode? n){string s=(n?.ToString()??"").Trim();if(decimal.TryParse(s,NumberStyles.Number,CultureInfo.InvariantCulture,out decimal v))return v;s=s.Replace(".","").Replace(',','.');return decimal.TryParse(s,NumberStyles.Number,CultureInfo.InvariantCulture,out v)?v:0;}
}
