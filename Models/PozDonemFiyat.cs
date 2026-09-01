namespace TarimDonusum.Models
{
    public class PozDonemFiyat
    {
        public int id { get; set; }
        public int donemId { get; set; }
        public int pozId { get; set; }
        public decimal? birimFiyat { get; set; }
        public string pozNo { get; set; } = "";
        public string pozAdi { get; set; } = "";
        public string birim { get; set; } = "";
    }

    public class PozDonemFiyatKayit
    {
        public int donemId { get; set; }
        public List<PozDonemFiyat> fiyatlar { get; set; } = new();
    }
}
