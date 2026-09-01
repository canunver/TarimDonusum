namespace TarimDonusum.Models
{
    public class Poz
    {
        public int id { get; set; }
        public string pozNo { get; set; } = "";
        public string ad { get; set; } = "";
        public string birim { get; set; } = "";
        public enumPozHesaplamaTuru hesaplamaTuru { get; set; } = enumPozHesaplamaTuru.Hacim;
        public bool aktif { get; set; } = true;
    }

    public enum enumPozHesaplamaTuru
    {
        Adet = 1,
        Uzunluk = 2,
        Alan = 3,
        Hacim = 4,
        Agirlik = 5
    }
}
