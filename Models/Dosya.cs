namespace TarimDonusum.Models
{
    public class DosyaAnahtari
    {
        public string ModulKod { get; set; } = "";
        public string FormAd { get; set; } = "";
        public string FormAnahtar { get; set; } = "";
        public int DosyaNo { get; set; }
    }

    public class DosyaBilgisi : DosyaAnahtari
    {
        public int Id { get; set; }
        public string DosyaAdi { get; set; } = "";
        public long Buyukluk { get; set; }
        public DateTime IlkYuklemeTarihi { get; set; }
        public DateTime STarihi { get; set; }
        public string? Aciklama { get; set; }
    }

    public class Dosya : DosyaBilgisi
    {
        public byte[] Icerik { get; set; } = [];
    }

    public class DosyaKaydetModel : DosyaAnahtari
    {
        public string DosyaAdi { get; set; } = "";
        public byte[] Icerik { get; set; } = [];
        public string? Aciklama { get; set; }
    }

    public class DosyaLog
    {
        public int Id { get; set; }
        public int? DosyaId { get; set; }
        public string ModulKod { get; set; } = "";
        public string FormAd { get; set; } = "";
        public string FormAnahtar { get; set; } = "";
        public int DosyaNo { get; set; }
        public DateTime IslemTarihi { get; set; } = DateTime.Now;
        public string Islem { get; set; } = "";
        public string JsonText { get; set; } = "";
    }
}
