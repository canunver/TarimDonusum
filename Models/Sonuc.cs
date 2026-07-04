namespace TarimDonusum.Models
{
    public class Sonuc
    {
        public bool basarili
        {
            get
            {
                return hatalar.Count == 0;
            }
        }

        public List<string> hatalar { get; set; } = new List<string>();

        public void HataEkle(string hata)
        {
            if (!string.IsNullOrWhiteSpace(hata))
                hatalar.Add(hata);
        }

        public string hataStr {
            get {
                return String.Join(';', hatalar);
            }
        }

        public string mesaj { get; set; } = "";
    }

    public class Sonuc<T> : Sonuc 
    {
        public T nesne { get; set; } = default!;

        public Sonuc()
        {
            // Eğer T bir referans tipiyse (yani null olabiliyorsa) 
            // ve parametresiz bir constructor'ı varsa otomatik new'le
            if (typeof(T).IsClass && typeof(T).GetConstructor(Type.EmptyTypes) != null)
            {
                nesne = (T)Activator.CreateInstance(typeof(T))!;
            }
        }
    }
}
