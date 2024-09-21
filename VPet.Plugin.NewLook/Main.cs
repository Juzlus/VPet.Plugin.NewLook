using LinePutScript.Localization.WPF;
using System.Windows.Controls;
using System.Windows;
using VPet_Simulator.Windows.Interface;
using System.Reflection;
using System.Xml;
using System.IO;

namespace VPet.Plugin.NewLook;

public class Main : MainPlugin
{
    public string path;
    public winSettings winSettings;
    public ModelMapping modelMapping = new ModelMapping();

    public override string PluginName => nameof(NewLook);

    public Main(IMainWindow mainwin) : base(mainwin)
    {
    }

    public override void LoadPlugin()
    {
        this.Setup();
        this.CreateMenuItem(nameof(NewLook));
    }

    private void Setup()
    {
        this.path = Directory.GetParent(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)).FullName;
    }

    private void CreateMenuItem(string buttonName)
    {
        MenuItem menuModConfig = this.MW.Main.ToolBar.MenuMODConfig;
        menuModConfig.Visibility = Visibility.Visible;

        MenuItem menuItem = new MenuItem()
        {
            Header = buttonName.Translate(),
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        menuItem.Click += (RoutedEventHandler)((s, e) => this.Setting());
        menuModConfig.Items.Add((object)menuItem);
    }

    public override void Setting()
    {
        if (this.winSettings == null)
        {
            this.winSettings = new winSettings(this);
            this.winSettings.Show();
        }
        else
            this.winSettings.Topmost = true;
    }

    public void SaveToConfig(string key, string value)
    {
        string configPath = Path.Combine(this.path, "config.cfg");
        XmlDocument xmlDoc = new XmlDocument();
        if (File.Exists(configPath))
            xmlDoc.Load(configPath);
        else
        {
            XmlDeclaration xmlDeclaration = xmlDoc.CreateXmlDeclaration("1.0", "UTF-8", null);
            xmlDoc.AppendChild(xmlDeclaration);

            XmlElement root = xmlDoc.CreateElement("config.cfg");
            xmlDoc.AppendChild(root);
        }

        XmlElement node = xmlDoc.SelectSingleNode($"//{key}") as XmlElement;
        if (node is null)
        {
            node = xmlDoc.CreateElement(key);
            xmlDoc.DocumentElement?.AppendChild(node);
        }
        node.InnerText = value;
        try {
            xmlDoc.Save(configPath);
        } catch { }
    }

    public string GetFromConfig(string key, string alt = null)
    {
        string configPath = Path.Combine(this.path, "config.cfg");
        XmlDocument xmlDoc = new XmlDocument();
        if (!File.Exists(configPath))
            return alt;

        xmlDoc.Load(configPath);
        XmlNode node = xmlDoc.SelectSingleNode($"//{key}");
        if (node != null)
            return node.InnerText;

        return alt;
    }
}
