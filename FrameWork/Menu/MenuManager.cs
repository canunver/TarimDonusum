using Microsoft.Extensions.Localization;
using System.Xml.Linq;

using TarimDonusum.Models;

namespace TarimDonusum.FrameWork.Menu
{
    public static class MenuManager
    {

        private static MenuItem CloneItem(MenuItem item)
        {
            return new MenuItem
            {
                Id = item.Id,
                Text = item.Text,
                Url = item.Url,
                Icon = item.Icon,
                Target = item.Target,
                Type = item.Type,
                Data = item.Data,
                Roller = item.Roller.ToList(),
                Children = item.Children
                    .Select(CloneItem)
                    .ToList()
            };
        }

        private static List<MenuItem> CloneMenu(List<MenuItem> source)
        {
            return source.Select(CloneItem).ToList();
        }


        private static void MenuLocalize(string menuOnEk, List<MenuItem> items, IStringLocalizer<SharedResource> L)
        {
            foreach (var item in items)
            {
                item.Text = L[$"{menuOnEk}.{item.Id}"];

                if (item.Children?.Count > 0)
                    MenuLocalize(menuOnEk, item.Children, L);
            }
        }


        public static List<MenuItem> GetMenuGiris(IStringLocalizer<SharedResource> L)
        {
            var menu = CloneMenu(_menuGiris.Value);
            MenuLocalize("MenuGiris", menu, L);
            return menu;
        }

        public static List<MenuItem> GetMenuBasvuru(IStringLocalizer<SharedResource> L)
        {
            var menu = CloneMenu(_menuBasvuru.Value);
            MenuLocalize("MenuBasvuru", menu, L);
            return menu;
        }

        public static List<MenuItem> GetMenuBasvuru(IStringLocalizer<SharedResource> L, IEnumerable<KullaniciRol> kullaniciRolleri)
        {
            var roller = kullaniciRolleri.ToHashSet();
            var menu = YetkiyeGoreFiltrele(_menuBasvuru.Value, roller);
            MenuLocalize("MenuBasvuru", menu, L);
            return menu;
        }

        private static readonly Lazy<List<MenuItem>> _menuGiris =
            new(() => Load("MenuGiris.xml"));

        private static readonly Lazy<List<MenuItem>> _menuBasvuru =
            new(() => Load("MenuBasvuru.xml"));

        public static List<MenuItem> MenuGiris => _menuGiris.Value;

        public static string ContentRootPath { get; private set; } = "";

        public static void Initialize(string contentRootPath)
        {
            ContentRootPath = contentRootPath;
        }

        private static List<MenuItem> Load(string xmlFileName)
        {
            string fileName = Path.Combine(
                ContentRootPath,
                "App_Data",
                xmlFileName);

            var doc = XDocument.Load(fileName);

            List<MenuItem> menuList = doc.Root!
                      .Elements("Item")
                      .Select(ReadItem)
                      .ToList();

            return menuList;
        }

        private static MenuItem ReadItem(XElement e)
        {
            return new MenuItem
            {
                Id = (string?)e.Attribute("id") ?? "",
                Url = (string?)e.Attribute("url"),
                Icon = (string?)e.Attribute("icon"),
                Target = (string?)e.Attribute("target"),
                Type = ReadType(e),
                Data = e.Element("Data")?.Value,
                Roller = ReadRoller(e),
                Children = e.Elements("Item")
                            .Select(ReadItem)
                            .ToList()
            };
        }

        private static List<MenuItem> YetkiyeGoreFiltrele(List<MenuItem> menu, HashSet<KullaniciRol> kullaniciRolleri)
        {
            return menu
                .Select(item => YetkiyeGoreFiltrele(item, kullaniciRolleri))
                .Where(item => item != null)
                .Select(item => item!)
                .ToList();
        }

        private static MenuItem? YetkiyeGoreFiltrele(MenuItem item, HashSet<KullaniciRol> kullaniciRolleri)
        {
            if (!MenuYetkiliMi(item, kullaniciRolleri))
                return null;

            var kopya = CloneItem(item);
            kopya.Children = YetkiyeGoreFiltrele(kopya.Children, kullaniciRolleri);

            if (kopya.Type == MenuItemType.Menu &&
                kopya.Children.Count == 0 &&
                string.IsNullOrWhiteSpace(kopya.Url) &&
                string.IsNullOrWhiteSpace(kopya.Data))
                return null;

            return kopya;
        }

        private static bool MenuYetkiliMi(MenuItem item, HashSet<KullaniciRol> kullaniciRolleri)
        {
            return item.Roller.Count == 0 || item.Roller.Any(kullaniciRolleri.Contains);
        }

        private static List<KullaniciRol> ReadRoller(XElement e)
        {
            string? roller = (string?)e.Attribute("roles") ??
                             (string?)e.Attribute("roller") ??
                             (string?)e.Attribute("rol") ??
                             (string?)e.Attribute("kullaniciTipleri") ??
                             (string?)e.Attribute("kullaniciTipi");

            if (string.IsNullOrWhiteSpace(roller))
                return new List<KullaniciRol>();

            char[] ayiraclar = new[] { ',', ';', '|', ' ' };
            return roller
                .Split(ayiraclar, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(ReadRol)
                .Where(rol => rol.HasValue)
                .Select(rol => rol!.Value)
                .Distinct()
                .ToList();
        }

        private static KullaniciRol? ReadRol(string rol)
        {
            if (int.TryParse(rol, out int rolNo) &&
                Enum.IsDefined(typeof(KullaniciRol), rolNo))
                return (KullaniciRol)rolNo;

            if (Enum.TryParse<KullaniciRol>(rol, true, out var enumRol))
                return enumRol;

            return null;
        }


        private static MenuItemType ReadType(XElement e)
        {
            var typeText = (string?)e.Attribute("type");

            if (Enum.TryParse<MenuItemType>(typeText, true, out var type))
                return type;

            return MenuItemType.Url;
        }
    }
}
