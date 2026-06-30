using System.ComponentModel.DataAnnotations;
using TarimDonusum.Models;

namespace TarimDonusum.ViewModels.Home
{
    public class YeniKullaniciViewModel
    {
        public Kullanici Kullanici { get; set; } = new();
        
        [Display(Name = "Parola Tekrar")]
        public string ParolaTekrar { get; set; } = "";

        [Display(Name = "Güvenlik Kodu")]
        public string GuvenlikKodu { get; set; } = "";

        public bool SozlesmeKabulEdildi { get; set; }
    }
}
