namespace TarimDonusum.Models;

public sealed class DonemTahminAyari
{
    public int donemId { get; set; }
    public int donemYili { get; set; }
    public List<DonemTahminSatiri> tahminler { get; set; } = [];
}

public sealed class DonemTahminSatiri
{
    public int yil { get; set; }
    public decimal? kurTahminiTL { get; set; }
    public decimal? enflasyonTahminiYuzde { get; set; }
}
