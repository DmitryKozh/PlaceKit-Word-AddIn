using Microsoft.Win32;

namespace PlaceKit.Tools
{
    // The theme chosen in File > Account > Office Theme. Office 2016 and later keep it under the 16.0 key.
    internal static class OfficeTheme
    {
        const string OfficeKey = @"HKEY_CURRENT_USER\Software\Microsoft\Office\16.0\Common";
        const string WindowsKey = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";

        const int DarkGray = 3;
        const int Black = 4;
        const int SystemSetting = 6;

        public static bool IsDark() =>
            IsDark(Registry.GetValue(OfficeKey, "UI Theme", null) as int?, Registry.GetValue(WindowsKey, "AppsUseLightTheme", null) as int?);

        // Without a stored theme, Office 2016 and 2019 show Colorful; light icons are the safe guess.
        internal static bool IsDark(int? officeTheme, int? appsUseLightTheme) =>
            officeTheme == DarkGray || officeTheme == Black || (officeTheme == SystemSetting && appsUseLightTheme == 0);
    }
}
