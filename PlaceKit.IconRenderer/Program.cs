using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace PlaceKit.IconRenderer
{
    // Writes PlaceKit\Resources\RibbonIcons: for each Office theme and common screen scale, a strip with the icons
    // side by side in RibbonIcon order, named like Dark48.png.
    static class Program
    {
        // 100, 125, 150, 175, 200, 250 and 300 %, as PlaceKit.Views.RibbonImages expects them.
        static readonly int[] Sizes = { 32, 40, 48, 56, 64, 80, 96 };

        [STAThread]
        static int Main(string[] args)
        {
            string folder = args.Length > 0 ? args[0] : DefaultFolder();
            Directory.CreateDirectory(folder);

            foreach (bool dark in new[] { false, true })
            {
                foreach (int size in Sizes)
                {
                    string path = Path.Combine(folder, (dark ? "Dark" : "Light") + size + ".png");
                    using (Bitmap strip = DrawStrip(size, dark))
                    {
                        strip.Save(path, ImageFormat.Png);
                    }

                    Console.WriteLine(path);
                }
            }

            return 0;
        }

        static Bitmap DrawStrip(int size, bool dark)
        {
            RibbonIcons icons = new RibbonIcons(size / 32.0, dark);
            RibbonIcon[] all = Enum.GetValues(typeof(RibbonIcon)).Cast<RibbonIcon>().ToArray();
            Bitmap strip = new Bitmap(size * all.Length, size, PixelFormat.Format32bppArgb);
            foreach (RibbonIcon icon in all)
            {
                using (Bitmap image = icons.Render(icon))
                {
                    CopyInto(strip, image, (int)icon * size);
                }
            }

            return strip;
        }

        // Pixel for pixel: drawing through GDI+ would round the colour of the half-transparent edges.
        static void CopyInto(Bitmap strip, Bitmap image, int left)
        {
            BitmapData source = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            BitmapData target = strip.LockBits(new Rectangle(left, 0, image.Width, image.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            try
            {
                byte[] row = new byte[image.Width * 4];
                for (int y = 0; y < image.Height; y++)
                {
                    Marshal.Copy(source.Scan0 + y * source.Stride, row, 0, row.Length);
                    Marshal.Copy(row, 0, target.Scan0 + y * target.Stride, row.Length);
                }
            }
            finally
            {
                image.UnlockBits(source);
                strip.UnlockBits(target);
            }
        }

        // The add-in's folder in the repository, found from the build output.
        static string DefaultFolder()
        {
            for (DirectoryInfo folder = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory); folder != null; folder = folder.Parent)
            {
                if (File.Exists(Path.Combine(folder.FullName, "PlaceKit.slnx")))
                {
                    return Path.Combine(folder.FullName, "PlaceKit", "Resources", "RibbonIcons");
                }
            }

            throw new DirectoryNotFoundException("PlaceKit.slnx not found. Pass the output folder as the first argument.");
        }
    }
}
