using LinePutScript;
using LinePutScript.Localization.WPF;
using Microsoft.VisualBasic;
using Panuon.WPF.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using VPet_Simulator.Windows.Interface;
using Path = System.IO.Path;

namespace VPet.Plugin.NewLook
{
    public partial class winSettings : WindowX
    {
        private Main main;
        public int maxAnimations = 0;
        private int animationsCount = 0;
        private string newVpetName = "newLook";

        public winSettings(Main main)
        {
            this.main = main;

            this.InitializeComponent();
            this.LoadConfig();
        }

        private void LoadConfig()
        {
            this.ColorPicker0.SelectedColor = this.GetColor("Hair", "#C7C5C5");
            this.ColorPicker1.SelectedColor = this.GetColor("Skin", "#FFEEE1");
            this.ColorPicker2.SelectedColor = this.GetColor("EyeAndL", "#F5CE86");
            this.ColorPicker3.SelectedColor = this.GetColor("Shirt", "#F5F5F5");
            this.ColorPicker4.SelectedColor = this.GetColor("Accessories", "#CB7070");
            this.ColorPicker5.SelectedColor = this.GetColor("Socks", "#F5F5F5");
            this.ColorPicker6.SelectedColor = this.GetColor("Boots", "#99A4C4");
            this.ChangePreviewImage(false);
        }

        public void ChangeOutputText()
        {
            this.Output.Text = "Loaded animations".Translate() + $": {++this.animationsCount}/{this.maxAnimations}";
        }

        private System.Windows.Media.Color GetColor(string name, string alt)
        {
            System.Drawing.Color drawingColor = System.Drawing.ColorTranslator.FromHtml(this.main.GetFromConfig(name, alt));
            return System.Windows.Media.Color.FromArgb(drawingColor.A, drawingColor.R, drawingColor.G, drawingColor.B);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            this.main.winSettings = (winSettings)null;
        }

        private async void ChangeColor(object sender, RoutedEventArgs e)
        {
            ColorPicker colorPicker = sender as ColorPicker;
            this.main.SaveToConfig(colorPicker.Tag.ToString(), colorPicker.SelectedColor.Value.ToString());

            this.ChangeFocus(false);
            await this.ChangePreviewImage();
            this.ChangeFocus(true);
        }

        private void ChangeFocus(bool isEnabled)
        {
            this.ColorPicker0.IsEnabled = isEnabled;
            this.ColorPicker1.IsEnabled = isEnabled;
            this.ColorPicker2.IsEnabled = isEnabled;
            this.ColorPicker3.IsEnabled = isEnabled;
            this.ColorPicker4.IsEnabled = isEnabled;
            this.ColorPicker5.IsEnabled = isEnabled;
            this.ColorPicker6.IsEnabled = isEnabled;
            this.CreateNewLolis.IsEnabled = isEnabled;
        }

        private async Task ChangePreviewImage(bool changeColors = true)
        {
            if (changeColors)
                await this.main.modelMapping.ChangeColor(Path.Combine(this.main.path, "models", "Preview"), this.GetColorMap());

            string imagePath = Path.Combine(this.main.path, "pet", "newLook", "Preview", "preview.png");
            using (FileStream stream = new FileStream(imagePath, FileMode.Open, FileAccess.Read))
            {
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = stream;
                bitmap.EndInit();
                this.ImagePreview.Source = bitmap;
            }
        }

        private List<ColorMapping> GetColorMap()
        {
            List<ColorMapping> colorMappings = new List<ColorMapping>
            {
                new ColorMapping("ff0000", ColorTranslator.FromHtml(this.ColorPicker0.SelectedColor.Value.ToString()), 0),  // Hair
                new ColorMapping("8f00e4", ColorTranslator.FromHtml(this.ColorPicker1.SelectedColor.Value.ToString()), 0),  // Skin
                new ColorMapping("fffb00", ColorTranslator.FromHtml(this.ColorPicker2.SelectedColor.Value.ToString()), 0),  // Eye and L
                new ColorMapping("ff12d8", ColorTranslator.FromHtml(this.ColorPicker3.SelectedColor.Value.ToString()), 0),  // Shirt
                new ColorMapping("20ff12", ColorTranslator.FromHtml(this.ColorPicker4.SelectedColor.Value.ToString()), 0),  // Shirt Accessories
                new ColorMapping("6270ff", ColorTranslator.FromHtml(this.ColorPicker5.SelectedColor.Value.ToString()), 0),  // Socks
                new ColorMapping("1623aa", ColorTranslator.FromHtml(this.ColorPicker6.SelectedColor.Value.ToString()), 0),  // Boots
            };
            return colorMappings;
        }

        private void CreateNewVpet(object sender, RoutedEventArgs e)
        {
            this.ChangeFocus(false);
            this.maxAnimations = GetAnimationsLenght();
            this.Output.Visibility = Visibility.Visible;

            Task.Run(() => this.FindAllAnimations());
        }

        private int GetAnimationsLenght()
        {
            int count = 0;
            this.animationsCount = 0;
            string[] files = Directory.GetFiles(Path.Combine(this.main.path, "models"), "colors.png", SearchOption.AllDirectories);
            foreach (string file in files)
            {
                Bitmap image = new Bitmap(file);
                if (image == null) continue;
                count += (image.Width / 1000);
                image.Dispose();
            }
            return count;
        }

        private async Task FindAllAnimations()
        {
            if (this.main.winSettings.Dispatcher.CheckAccess())
            {
                int i = 0;
                string[] files = Directory.GetFiles(this.main.path, "colors.png", SearchOption.AllDirectories);
                foreach (string file in files)
                    await this.main.modelMapping.ChangeColor(file.Substring(0, file.Length - "colors.png".Length), this.GetColorMap(), this.main);

                //this.newVpetName = $"newLook{new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds()}";
                this.ChangeVpet();

                this.ChangeFocus(true);
                this.Output.Visibility = Visibility.Hidden;
            }
            else
                this.main.winSettings.Dispatcher.Invoke(this.FindAllAnimations);
        }

        private void ChangePetAnimationsPath()
        {
            string lpsFile = Path.Combine(this.main.path, "pet", "newLook.lps");
            if (!File.Exists(lpsFile)) return;
            string content = File.ReadAllText(lpsFile);
            content = $"pet#{this.newVpetName}:|{content.Substring(content.IndexOf(":|"))}";
            File.WriteAllText(lpsFile, content);
        }

        private void ChangeVpet()
        {
            this.main.MW.Set["gameconfig"].SetString("petgraph", this.newVpetName);
            this.main.MW.Set.LastCacheDate = DateTime.MinValue;
            this.main.MW.Restart();
        }
    }
}