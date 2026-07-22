using System.ComponentModel.DataAnnotations;

namespace TarimDonusum.Models
{
    public class ParolaBelirleViewModel
    {
        [Required] public string Token { get; set; } = "";
        [Required] public string Parola { get; set; } = "";
        [Required] public string ParolaTekrar { get; set; } = "";
    }
}
