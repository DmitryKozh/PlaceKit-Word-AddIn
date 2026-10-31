using System;

namespace PlaceKit.Models
{
    public sealed class ReleaseInfo
    {
        public ReleaseInfo(Version version, Uri page, Uri installer)
        {
            Version = Normalize(version);
            Page = page;
            Installer = installer;
        }

        public Version Version { get; }

        public string VersionText => Version.ToString(3);

        // On the website, with what's new.
        public Uri Page { get; }

        // null when only the page offers the download
        public Uri Installer { get; }

        public bool IsNewerThan(string version) => TryParseVersion(version, out Version other) && Version > other;

        // "v1.2", "1.2.0" and "1.2.0.0" are all 1.2.0.
        public static bool TryParseVersion(string text, out Version version)
        {
            version = null;
            if (text == null || !Version.TryParse(text.Trim().TrimStart('v', 'V'), out Version parsed))
            {
                return false;
            }

            version = Normalize(parsed);
            return true;
        }

        static Version Normalize(Version version) => new Version(version.Major, version.Minor, Math.Max(version.Build, 0));
    }
}
