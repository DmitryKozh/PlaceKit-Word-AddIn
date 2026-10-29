using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace PlaceKit.Views
{
    // The ribbon images, drawn by the PlaceKit.IconRenderer tool: for each Office theme and common screen scale
    // a strip with the icons side by side in RibbonIcon order. Loading one is much cheaper at startup than drawing.
    internal static class RibbonImages
    {
        // 100, 125, 150, 175, 200, 250 and 300 %
        internal static readonly int[] Sizes = { 32, 40, 48, 56, 64, 80, 96 };

        public static IReadOnlyDictionary<RibbonIcon, Bitmap> Load(double scale, bool darkTheme) => Load(SizeFor(scale), darkTheme);

        // The size for the scale, or the next larger one, which Office scales down.
        internal static int SizeFor(double scale)
        {
            int wanted = (int)Math.Round(32 * scale);
            return Sizes.Where(size => size >= wanted).DefaultIfEmpty(Sizes.Last()).First();
        }

        internal static IReadOnlyDictionary<RibbonIcon, Bitmap> Load(int size, bool darkTheme)
        {
            string name = $"PlaceKit.Resources.RibbonIcons.{(darkTheme ? "Dark" : "Light")}{size}.png";
            using (Stream stream = typeof(RibbonImages).Assembly.GetManifestResourceStream(name) ?? throw new FileNotFoundException("Ribbon images not found.", name))
            using (Bitmap strip = new Bitmap(stream))
            {
                return Enum.GetValues(typeof(RibbonIcon)).Cast<RibbonIcon>().ToDictionary(
                    icon => icon,
                    icon => strip.Clone(new Rectangle((int)icon * size, 0, size, size), PixelFormat.Format32bppArgb));
            }
        }
    }
}
