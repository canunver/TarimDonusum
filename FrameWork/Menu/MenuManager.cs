using Microsoft.Extensions.Localization;
using System.Xml.Linq;

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
                Type = item.Type,
                Data = item.Data,
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

        private static readonly Lazy<List<MenuItem>> _menuGiris =
            new(() => Load());

        public static List<MenuItem> MenuGiris => _menuGiris.Value;

        public static string ContentRootPath { get; private set; } = "";

        public static void Initialize(string contentRootPath)
        {
            ContentRootPath = contentRootPath;
        }

        private static List<MenuItem> Load()
        {
            string fileName = Path.Combine(
                ContentRootPath,
                "App_Data",
                "MenuGiris.xml");

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
                Type = ReadType(e),
                Data = e.Element("Data")?.Value,
                Children = e.Elements("Item")
                            .Select(ReadItem)
                            .ToList()
            };
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