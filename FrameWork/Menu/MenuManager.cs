using System.Xml.Linq;

namespace TarimDonusum.FrameWork.Menu
{
    public static class MenuManager
    {
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

            return doc.Root!
                      .Elements("Item")
                      .Select(ReadItem)
                      .ToList();
        }

        private static MenuItem ReadItem(XElement e)
        {
            return new MenuItem
            {
                Id = (string?)e.Attribute("id") ?? "",
                Text = (string?)e.Attribute("text") ?? "",
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