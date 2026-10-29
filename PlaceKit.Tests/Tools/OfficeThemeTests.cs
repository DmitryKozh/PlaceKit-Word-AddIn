using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlaceKit.Tools;

namespace PlaceKit.Tests.Tools
{
    [TestClass]
    public class OfficeThemeTests
    {
        [DataTestMethod]
        [DataRow(0, 0, false)] // Colorful
        [DataRow(3, 1, true)] // Dark Gray
        [DataRow(4, 1, true)] // Black
        [DataRow(5, 0, false)] // White
        [DataRow(6, 0, true)] // Use system setting, Windows in dark mode
        [DataRow(6, 1, false)]
        [DataRow(null, 0, false)] // never chosen: Colorful in Office 2016 and 2019
        [DataRow(null, null, false)]
        public void IsDark(int? officeTheme, int? appsUseLightTheme, bool dark)
        {
            Assert.AreEqual(dark, OfficeTheme.IsDark(officeTheme, appsUseLightTheme));
        }
    }
}
