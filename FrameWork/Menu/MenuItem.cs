using System.Text.Json.Serialization;

using TarimDonusum.Models;

namespace TarimDonusum.FrameWork.Menu
{
    public enum MenuItemType
    {
        Menu = 0,   // Alt menü
        Url = 1,    // Sayfa
        Html = 2,   // İçerik
        Link = 3    // Url linki
    }

    public class MenuItem
    {
        public string Id { get; set; } = "";
        public string Text { get; set; } = "";
        public string? Url { get; set; } = null;
        public string? Icon { get; set; }
        public string? Target { get; set; }
        public MenuItemType? Type { get; set; }
        [JsonIgnore] public string? Data { get; set; }
        [JsonIgnore] public List<KullaniciRol> Roller { get; set; } = new();
        public List<MenuItem> Children { get; set; } = new();
    }
}
