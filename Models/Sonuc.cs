namespace TarimDonusum.Models
{
    public class Sonuc
    {
        public bool Basarili
        {
            get
            {
                return Hatalar.Count == 0;
            }
        }

        public List<string> Hatalar { get; set; } = new List<string>();

        public void HataEkle(string hata)
        {
            if (!string.IsNullOrWhiteSpace(hata))
                Hatalar.Add(hata);
        }
    }

    public class Sonuc<T> : Sonuc
    {
        public T? Nesne { get; set; }
    }
}
