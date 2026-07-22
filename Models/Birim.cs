namespace TarimDonusum.Models
{
    public class Birim
    {
        public int id { get; set; }
        public string birimAdi { get; set; } = "";
        public enumBirimTuru birimTuru { get; set; } = enumBirimTuru.Merkez;
        public int? ilKod { get; set; }
        public string ilAdi { get; set; } = "";
        public int siraNo { get; set; }
        public bool aktif { get; set; } = true;
    }

    public enum enumBirimTuru
    {
        Merkez = 1,
        Tasra = 2
    }
}
