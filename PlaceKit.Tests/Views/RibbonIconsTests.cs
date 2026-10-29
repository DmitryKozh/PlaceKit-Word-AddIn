using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlaceKit.IconRenderer;

namespace PlaceKit.Tests.Views
{
    // The vector shapes PlaceKit.IconRenderer draws the ribbon images from.
    [TestClass]
    public class RibbonIconsTests
    {
        [DataTestMethod]
        [DataRow(1.0, 32)]
        [DataRow(1.25, 40)]
        [DataRow(1.5, 48)]
        [DataRow(2.0, 64)]
        public void Icons_AreDrawnForTheScreenScale(double scale, int size)
        {
            foreach (Bitmap icon in All(new RibbonIcons(scale, darkTheme: true)))
            {
                Assert.AreEqual(new Size(size, size), icon.Size);
                Assert.AreEqual(0, icon.GetPixel(0, 0).A, "the background is transparent");
                Assert.IsTrue(Pixels(icon).Count(pixel => pixel.A > 128) > size, "the shapes are drawn");
            }
        }

        [TestMethod]
        public void Lines_HaveTheColourOfTheTheme()
        {
            Assert.AreEqual(Color.FromArgb(0xD4, 0xD4, 0xD4).ToArgb(), LineColour(new RibbonIcons(2, darkTheme: true).Render(RibbonIcon.Settings)));
            Assert.AreEqual(Color.FromArgb(0x42, 0x42, 0x42).ToArgb(), LineColour(new RibbonIcons(2, darkTheme: false).Render(RibbonIcon.Settings)));
        }

        static IEnumerable<Bitmap> All(RibbonIcons icons) =>
            Enum.GetValues(typeof(RibbonIcon)).Cast<RibbonIcon>().Select(icons.Render);

        internal static IEnumerable<Color> Pixels(Bitmap bitmap) =>
            Enumerable.Range(0, bitmap.Width * bitmap.Height).Select(i => bitmap.GetPixel(i % bitmap.Width, i / bitmap.Width));

        // The gear is drawn in lines only, at 200 % two pixels wide.
        static int LineColour(Bitmap icon) =>
            Pixels(icon).Where(pixel => pixel.A == 255).GroupBy(pixel => pixel.ToArgb()).OrderByDescending(group => group.Count()).First().Key;
    }
}
