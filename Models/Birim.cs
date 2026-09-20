namespace TarimDonusum.Models
{
    public class Birim
    {
        public int id { get; set; }
        public string birimAdi { get; set; } = "";
        public enumBirimTuru birimTuru { get; set; } = enumBirimTuru.Merkez;
        public List<int> ilKodlari { get; set; } = new();
        public List<Il> iller { get; set; } = new();
        public string ilKodlariMetni => string.Join(", ", ilKodlari.OrderBy(x => x));
        public string ilAdlariMetni => string.Join(", ", iller.OrderBy(x => x.ad).Select(x => x.ad));
        public int siraNo { get; set; }
        public bool aktif { get; set; } = true;
    }

    public enum enumBirimTuru
    {
        Merkez = 1,
        Tasra = 2
    }
}
