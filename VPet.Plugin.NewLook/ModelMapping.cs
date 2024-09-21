using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TrayNotify;

namespace VPet.Plugin.NewLook;

public class ColorMapping
{
    public string Pattern { get; set; }
    public Color TargetColor { get; set; }
    public int Tolerance { get; set; }

    public ColorMapping(string pattern, Color targetColor, int tolerance)
    {
        Pattern = pattern;
        TargetColor = targetColor;
        Tolerance = tolerance;
    }
}

public class ModelMapping
{
    public async Task ChangeColor(string path, List<ColorMapping> colorMappings, Main main = null)
    {
        using (Bitmap colorsImage = new Bitmap(Path.Combine(path, "colors.png")),
                      masksImage = new Bitmap(Path.Combine(path, "masks.png")),
                      lineartsImage = new Bitmap(Path.Combine(path, "linearts.png")),
                      outputImage = new Bitmap(colorsImage.Width, colorsImage.Height))
        {
            string[] names = File.ReadAllLines(Path.Combine(path, "names.txt"));

            for (int x = 0; x < colorsImage.Width; x++)
                for (int y = 0; y < colorsImage.Height; y++)
                {
                    Color colorPixel = colorsImage.GetPixel(x, y);
                    Color lineartsPixel = lineartsImage.GetPixel(x, y);
                    bool colorMapped = false;

                    foreach (ColorMapping mapping in colorMappings)
                    {
                        string pattern = mapping.Pattern;
                        Color targetColor = mapping.TargetColor;
                        int tolerance = mapping.Tolerance;

                        if (IsColorWithinTolerance(colorPixel, ColorTranslator.FromHtml("#" + pattern), tolerance))
                        {
                            Color blendedColor = BlendColors(masksImage.GetPixel(x, y), targetColor, 0.5f);
                            outputImage.SetPixel(x, y, blendedColor);
                            colorMapped = true;
                            break;
                        }
                    }

                    if (!colorMapped)
                        outputImage.SetPixel(x, y, colorPixel);

                    if (lineartsPixel.A != 0)
                    {
                        Color blendedColor = AddLineart(lineartsPixel, outputImage.GetPixel(x, y));
                        outputImage.SetPixel(x, y, blendedColor);
                    }

                    if (x > 0 && x % 999 == 0 && y == 999)
                    {
                        int col = (x / 1000);
                        Rectangle spriteRectangle = new Rectangle(col * 1000, 0, 1000, 1000);
                        Bitmap sprite = outputImage.Clone(spriteRectangle, outputImage.PixelFormat);
                        sprite.Save(Path.Combine(path.Replace("models", Path.Combine("pet", "newLook")), names[col] + ".png"), ImageFormat.Png);
                        sprite.Dispose();
                        if (main != null)
                            main.winSettings.ChangeOutputText();
                        await Task.Delay(100);
                    }
                }
        }
    }

    bool IsColorWithinTolerance(Color color1, Color color2, int tolerance)
    {
        int rDiff = Math.Abs(color1.R - color2.R);
        int gDiff = Math.Abs(color1.G - color2.G);
        int bDiff = Math.Abs(color1.B - color2.B);
        return rDiff <= tolerance && gDiff <= tolerance && bDiff <= tolerance;
    }

    Color BlendColors(Color color1, Color color2, float amount)
    {
        float amount1 = 1.0f - amount;
        float amount2 = 1.0f - amount1 + 0.2f;
        int r = (int)(color1.R * amount1 + color2.R * amount2);
        int g = (int)(color1.G * amount1 + color2.G * amount2);
        int b = (int)(color1.B * amount1 + color2.B * amount2);
        return Color.FromArgb(r > 255 ? 255 : r, g > 255 ? 255 :g , b > 255 ? 255 : b);
    }

    Color AddLineart(Color lineart, Color output)
    {
        float alphaLineart = lineart.A / 255f;
        float alphaOutput = output.A / 255f;

        int r = (int)((lineart.R * alphaLineart) + (output.R * alphaOutput * (1 - alphaLineart)));
        int g = (int)((lineart.G * alphaLineart) + (output.G * alphaOutput * (1 - alphaLineart)));
        int b = (int)((lineart.B * alphaLineart) + (output.B * alphaOutput * (1 - alphaLineart)));

        float alpha = alphaLineart + alphaOutput * (1 - alphaLineart);
        int a = (int)(alpha * 255);
        return Color.FromArgb(a, r, g, b);
    }
}
