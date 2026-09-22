using TarimDonusum.IsKurallari;

namespace TarimDonusum.Models;

public enum enumCevreselSosyalAnketSurumDurumu
{
    Taslak = 0,
    Yayinda = 1,
    Arsiv = 2
}

public enum enumCevreselSosyalCevapBaglami
{
    Ortak = 1,
    IsTuruBazli = 2
}

public sealed class CevreselSosyalAnketSurumu
{
    public int id { get; set; }
    public int surumNo { get; set; }
    public enumCevreselSosyalAnketSurumDurumu durum { get; set; }
    public string aciklama { get; set; } = "";
    public string kapsamDisiFaaliyetlerBaslik { get; set; } = "Kapsam Dışı Faaliyetler Listesi - Bilgilendirme";
    public string kapsamDisiFaaliyetlerHtml { get; set; } = "";
    public DateTime? yayinTarihi { get; set; }
    public List<CevreselSosyalSoruGrubu> gruplar { get; set; } = [];
}

public sealed class CevreselSosyalAnketDuzenlemeModeli
{
    public int id { get; set; }
    public int surumNo { get; set; }
    public string aciklama { get; set; } = "";
    public string kapsamDisiFaaliyetlerBaslik { get; set; } = "Kapsam Dışı Faaliyetler Listesi - Bilgilendirme";
    public string kapsamDisiFaaliyetlerHtml { get; set; } = "";
    public int durum { get; set; }
    public List<CevreselSosyalBolumDuzenlemeModeli> bolumler { get; set; } = [];
}

public sealed class CevreselSosyalBolumDuzenlemeModeli
{
    public int id { get; set; }
    public string kod { get; set; } = "";
    public string baslik { get; set; } = "";
    public int siraNo { get; set; }
    public List<CevreselSosyalBilgiDuzenlemeModeli> bilgiler { get; set; } = [];
    public List<CevreselSosyalSoruDuzenlemeModeli> sorular { get; set; } = [];
}

public sealed class CevreselSosyalBilgiDuzenlemeModeli
{
    public int id { get; set; }
    public string tur { get; set; } = "info";
    public string? baslik { get; set; }
    public string? metin { get; set; }
    public List<string> maddeler { get; set; } = [];
}

public sealed class CevreselSosyalSoruDuzenlemeModeli
{
    public int id { get; set; }
    public int bolumId { get; set; }
    public string anahtar { get; set; } = "";
    public string gorunumKodu { get; set; } = "";
    public string baslik { get; set; } = "";
    public string metin { get; set; } = "";
    public string cevapTuru { get; set; } = "text";
    public bool ortak { get; set; }
    public bool yapim { get; set; } = true;
    public bool guncelleme { get; set; } = true;
    public bool zorunlu { get; set; }
    public int? maksimumUzunluk { get; set; }
    public string? notMetni { get; set; }
    public string? bilgiMetni { get; set; }
    public string? yerTutucu { get; set; }
    public bool kapsamDisiBirakir { get; set; }
    public bool kapsamDisiFaaliyetlerListesiniGoster { get; set; }
    public bool herZamanAciklamaIste { get; set; }
    public string? otomatikKaynakKodu { get; set; }
    public bool aktif { get; set; } = true;
    public int siraNo { get; set; }
    public List<string> secenekler { get; set; } = [];
    public List<string> aciklamaKosullari { get; set; } = [];
    public List<string> dosyaKosullari { get; set; } = [];
}

public sealed class CevreselSosyalAnketAdiModeli
{
    public int id { get; set; }
    public string aciklama { get; set; } = "";
}

public sealed class CevreselSosyalKapsamDisiModeli
{
    public int id { get; set; }
    public string baslik { get; set; } = "";
    public string html { get; set; } = "";
}

public sealed class CevreselSosyalBolumKayitModeli
{
    public int id { get; set; }
    public int anketSurumId { get; set; }
    public string kod { get; set; } = "";
    public string baslik { get; set; } = "";
}

public sealed class CevreselSosyalSoruKayitModeli
{
    public int anketSurumId { get; set; }
    public int bolumId { get; set; }
    public CevreselSosyalSoruDuzenlemeModeli soru { get; set; } = new();
}
