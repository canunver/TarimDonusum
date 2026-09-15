namespace TarimDonusum.Models
{
    public enum enumMetrajKategori
    {
        Temel = 1,
        TasiyiciSistem = 2,
        DuvarImalatlari = 3,
        CatiKaplamasi = 4,
        DigerImalatlar = 5,
        CevreDuzenlemeImalatlari = 6
    }

    public static class MetrajKategoriTanimlari
    {
        public static string Ad(enumMetrajKategori kategori) => kategori switch
        {
            enumMetrajKategori.Temel => "Temel",
            enumMetrajKategori.TasiyiciSistem => "Taşıyıcı Sistem",
            enumMetrajKategori.DuvarImalatlari => "Duvar İmalatları",
            enumMetrajKategori.CatiKaplamasi => "Çatı Kaplaması",
            enumMetrajKategori.DigerImalatlar => "Diğer İmalatlar",
            enumMetrajKategori.CevreDuzenlemeImalatlari => "Çevre Düzenleme İmalatları",
            _ => ""
        };
    }

    public class BasvuruMetrajVerisi
    {
        public int basvuruId { get; set; }
        public List<BasvuruMetrajBina> binalar { get; set; } = new();
        public List<PozDonemFiyat> pozlar { get; set; } = new();
        public decimal toplamInsaatBedeli => binalar.Sum(x => x.maliyet);
    }

    public class BasvuruMetrajBina
    {
        public int id { get; set; }
        public int siraNo { get; set; }
        public string ad { get; set; } = "";
        public List<BasvuruMetrajBolum> bolumler { get; set; } = new();
        public decimal maliyet => bolumler.Sum(x => x.maliyet);
    }

    public class BasvuruMetrajBolum
    {
        public int id { get; set; }
        public int basvuruId { get; set; }
        public int binaId { get; set; }
        public int siraNo { get; set; }
        public enumMetrajKategori kategori { get; set; }
        public string ad { get; set; } = "";
        public List<BasvuruMetrajPoz> pozlar { get; set; } = new();
        public decimal maliyet => pozlar.Sum(x => x.maliyet);
    }

    public class BasvuruMetrajPoz
    {
        public int id { get; set; }
        public int basvuruId { get; set; }
        public int bolumId { get; set; }
        public int pozId { get; set; }
        public int siraNo { get; set; }
        public decimal birimFiyat { get; set; }
        public string pozNo { get; set; } = "";
        public string pozAdi { get; set; } = "";
        public string birim { get; set; } = "";
        public int hesaplamaTuru { get; set; }
        public List<BasvuruMetrajDetay> detaylar { get; set; } = new();
        public decimal miktar => detaylar.Sum(x => x.Miktar(hesaplamaTuru, birim));
        public decimal maliyet => Math.Round(miktar * birimFiyat, 2);
    }

    public class BasvuruMetrajDetay
    {
        public int id { get; set; }
        public int siraNo { get; set; }
        public string aciklama { get; set; } = "";
        public string urunCinsi { get; set; } = "";
        public decimal? birimAgirlik { get; set; }
        public decimal benzerSayisi { get; set; } = 1;
        public decimal? adet { get; set; }
        public decimal? boy { get; set; }
        public decimal? en { get; set; }
        public decimal? yukseklik { get; set; }
        public decimal miktar
        {
            get
            {
                decimal?[] degerler = [adet, boy, en, yukseklik];
                return degerler.All(x => !x.HasValue) ? 0 : benzerSayisi * degerler.Where(x => x.HasValue).Aggregate(1m, (t, x) => t * x!.Value);
            }
        }

        public decimal Miktar(int hesaplamaTuru, string birim)
        {
            decimal hacim = miktar;
            bool agirlikMetraji = hesaplamaTuru == (int)enumPozHesaplamaTuru.Agirlik ||
                string.Equals(birim?.Trim(), "Kg", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(birim?.Trim(), "Ton", StringComparison.OrdinalIgnoreCase);
            if (!agirlikMetraji)
                return hacim;

            return hacim * (birimAgirlik ?? 0);
        }
    }
}
