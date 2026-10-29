using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlaceKit.Views;
using Drawn = PlaceKit.IconRenderer;

namespace PlaceKit.Tests.Views
{
    // The image strips the add-in ships, drawn by PlaceKit.IconRenderer.
    [TestClass]
    public class RibbonImagesTests
    {
        [DataTestMethod]
        [DataRow(1.0, 32)]
        [DataRow(1.25, 40)]
        [DataRow(1.5, 48)]
        [DataRow(1.75, 56)]
        [DataRow(2.0, 64)]
        [DataRow(2.25, 80)]
        [DataRow(3.0, 96)]
        [DataRow(4.0, 96)]
        public void SizeFor_IsTheScaleOrTheNextLargerStrip(double scale, int size)
        {
            Assert.AreEqual(size, RibbonImages.SizeFor(scale));
        }

        // Fails after a shape changed until PlaceKit.IconRenderer is run again.
        [TestMethod]
        public void Strips_MatchTheDrawings()
        {
            foreach (bool dark in new[] { false, true })
            {
                foreach (int size in RibbonImages.Sizes)
                {
                    IReadOnlyDictionary<RibbonIcon, Bitmap> images = RibbonImages.Load(size, dark);
                    Drawn.RibbonIcons drawings = new Drawn.RibbonIcons(size / 32.0, dark);

                    foreach (RibbonIcon icon in Enum.GetValues(typeof(RibbonIcon)))
                    {
                        Drawn.RibbonIcon same = (Drawn.RibbonIcon)Enum.Parse(typeof(Drawn.RibbonIcon), icon.ToString());
                        using (Bitmap drawn = drawings.Render(same))
                        {
                            CollectionAssert.AreEqual(Argb(drawn), Argb(images[icon]), $"{icon}, {size} px, {(dark ? "dark" : "light")}: run PlaceKit.IconRenderer");
                        }
                    }
                }
            }
        }

        static List<int> Argb(Bitmap bitmap) => RibbonIconsTests.Pixels(bitmap).Select(pixel => pixel.ToArgb()).ToList();
    }
}
