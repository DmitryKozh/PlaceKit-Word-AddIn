using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace PlaceKit.IconRenderer
{
    // Writes PlaceKit\Resources\RibbonIcons: for each Office theme and common screen scale, a strip with the icons
    // side by side in RibbonIcon order, named like Dark48.png. Also writes the installer's PlaceKit.ico.
    static class Program
    {
        // 100, 125, 150, 175, 200, 250 and 300 %, as PlaceKit.Views.RibbonImages expects them.
        static readonly int[] Sizes = { 32, 40, 48, 56, 64, 80, 96 };

        [STAThread]
        static int Main(string[] args)
        {
            string root = args.Length > 0 ? args[0] : RepositoryFolder();
            string folder = Path.Combine(root, "PlaceKit", "Resources", "RibbonIcons");
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

            string installer = Path.Combine(root, "PlaceKit.Installer");
            string icon = Path.Combine(installer, "PlaceKit.ico");
            WriteIcon(icon);
            Console.WriteLine(icon);

            foreach (string image in WriteWizardImages(installer))
            {
                Console.WriteLine(image);
            }

            return 0;
        }

        // The pictures of the setup wizard: Dialog.bmp behind its first and last pages, with the PlaceKit braces on a band
        // on the left, and Banner.bmp at the top of the other pages, with the About tile on the right. Windows Installer
        // stretches them to the page, so they are drawn for 200 % and get smaller on smaller scales.
        static string[] WriteWizardImages(string folder)
        {
            const double scale = 2;
            // The sizes WixUI's own pictures have at 100 %, and the width of the band its Dialog.bmp has.
            int width = (int)(493 * scale);
            int dialogHeight = (int)(312 * scale);
            int bannerHeight = (int)(58 * scale);
            int band = (int)(164 * scale);

            string dialog = Path.Combine(folder, "Dialog.bmp");
            using (Bitmap image = new Bitmap(width, dialogHeight, PixelFormat.Format24bppRgb))
            using (Graphics graphics = Graphics.FromImage(image))
            using (Bitmap braces = new RibbonIcons(band * 0.9 / 32, darkTheme: false).Render(RibbonIcon.About))
            using (SolidBrush tile = new SolidBrush(ColorTranslator.FromHtml(RibbonIcons.TileColor)))
            {
                graphics.Clear(Color.White);
                graphics.FillRectangle(tile, 0, 0, band, dialogHeight);
                // The tile of the icon has the colour of the band, so only its braces show.
                graphics.DrawImage(braces, (band - braces.Width) / 2, (dialogHeight - braces.Height) / 2, braces.Width, braces.Height);
                SaveIndexed(image, dialog);
            }

            string banner = Path.Combine(folder, "Banner.bmp");
            using (Bitmap image = new Bitmap(width, bannerHeight, PixelFormat.Format24bppRgb))
            using (Graphics graphics = Graphics.FromImage(image))
            using (Bitmap tile = new RibbonIcons(bannerHeight * 0.75 / 32, darkTheme: false).Render(RibbonIcon.About))
            {
                graphics.Clear(Color.White);
                // The icon has a 4-unit margin around its tile; the tile keeps the same space from the right as from the top.
                int margin = (bannerHeight - tile.Height * 24 / 32) / 2;
                graphics.DrawImage(tile, width - margin - tile.Width * 28 / 32, (bannerHeight - tile.Height) / 2, tile.Width, tile.Height);
                SaveIndexed(image, banner);
            }

            return new[] { dialog, banner };
        }

        // As an 8-bit bitmap: the package keeps the pictures uncompressed, and with only the band, white and the blends
        // between them they have far fewer than 256 colours, so a palette holds them all.
        static void SaveIndexed(Bitmap image, string path)
        {
            BitmapData source = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            int[] pixels = new int[image.Width * image.Height];
            try
            {
                for (int y = 0; y < image.Height; y++)
                {
                    Marshal.Copy(source.Scan0 + y * source.Stride, pixels, y * image.Width, image.Width);
                }
            }
            finally
            {
                image.UnlockBits(source);
            }

            int[] colours = pixels.Distinct().ToArray();
            if (colours.Length > 256)
            {
                throw new InvalidOperationException(Path.GetFileName(path) + " has " + colours.Length + " colours, more than an 8-bit bitmap holds.");
            }

            var indexOf = colours.Select((colour, index) => new { colour, index }).ToDictionary(entry => entry.colour, entry => (byte)entry.index);
            using (Bitmap indexed = new Bitmap(image.Width, image.Height, PixelFormat.Format8bppIndexed))
            {
                ColorPalette palette = indexed.Palette;
                for (int i = 0; i < palette.Entries.Length; i++)
                {
                    palette.Entries[i] = i < colours.Length ? Color.FromArgb(colours[i]) : Color.Black;
                }

                indexed.Palette = palette;
                BitmapData target = indexed.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);
                try
                {
                    byte[] row = new byte[image.Width];
                    for (int y = 0; y < image.Height; y++)
                    {
                        for (int x = 0; x < image.Width; x++)
                        {
                            row[x] = indexOf[pixels[y * image.Width + x]];
                        }

                        Marshal.Copy(row, 0, target.Scan0 + y * target.Stride, row.Length);
                    }
                }
                finally
                {
                    indexed.UnlockBits(target);
                }

                indexed.Save(path, ImageFormat.Bmp);
            }
        }

        // The About tile, at the sizes Windows shows in Programs and Features and Explorer. The images are PNG,
        // which .ico files may hold since Windows Vista.
        static void WriteIcon(string path)
        {
            int[] sizes = { 16, 20, 24, 32, 40, 48, 64, 256 };
            byte[][] images = sizes.Select(size =>
            {
                using (Bitmap image = new RibbonIcons(size / 32.0, darkTheme: false).Render(RibbonIcon.About))
                using (MemoryStream png = new MemoryStream())
                {
                    image.Save(png, ImageFormat.Png);
                    return png.ToArray();
                }
            }).ToArray();

            using (BinaryWriter writer = new BinaryWriter(File.Create(path)))
            {
                // Header: reserved, type 1 (icon), image count; then one 16-byte entry per image.
                writer.Write((short)0);
                writer.Write((short)1);
                writer.Write((short)sizes.Length);
                int offset = 6 + 16 * sizes.Length;
                for (int i = 0; i < sizes.Length; i++)
                {
                    // Width and height; 0 means 256.
                    writer.Write((byte)(sizes[i] % 256));
                    writer.Write((byte)(sizes[i] % 256));
                    writer.Write((byte)0);
                    writer.Write((byte)0);
                    writer.Write((short)1);
                    writer.Write((short)32);
                    writer.Write(images[i].Length);
                    writer.Write(offset);
                    offset += images[i].Length;
                }

                foreach (byte[] image in images)
                {
                    writer.Write(image);
                }
            }
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

        // Found from the build output.
        static string RepositoryFolder()
        {
            for (DirectoryInfo folder = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory); folder != null; folder = folder.Parent)
            {
                if (File.Exists(Path.Combine(folder.FullName, "PlaceKit.slnx")))
                {
                    return folder.FullName;
                }
            }

            throw new DirectoryNotFoundException("PlaceKit.slnx not found. Pass the repository folder as the first argument.");
        }
    }
}
