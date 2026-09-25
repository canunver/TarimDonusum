namespace TarimDonusum.Models;

public enum GirdiGiderTuru
{
    IlkMaddeKullanimi = 1,
    YardimciMadde = 2,
    IsletmeMalzemesiKullanimi = 3
}

public static class GirdiGiderTurleri
{
    public static string Ad(GirdiGiderTuru tur) => tur switch
    {
        GirdiGiderTuru.IlkMaddeKullanimi => "İlk Madde Kullanımı",
        GirdiGiderTuru.YardimciMadde => "Yardımcı Madde",
        GirdiGiderTuru.IsletmeMalzemesiKullanimi => "İşletme Malzemesi Kullanımı",
        _ => ""
    };

    public static string Anahtar(GirdiGiderTuru tur) => tur switch
    {
        GirdiGiderTuru.IlkMaddeKullanimi => "ilkMadde",
        GirdiGiderTuru.YardimciMadde => "yardimciMadde",
        GirdiGiderTuru.IsletmeMalzemesiKullanimi => "isletmeMalzemesi",
        _ => ""
    };
}
