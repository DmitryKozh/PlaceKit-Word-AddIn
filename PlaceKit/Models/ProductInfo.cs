using System;
using System.IO;
using System.Linq;
using System.Reflection;
using PlaceKit.Tools;

namespace PlaceKit.Models
{
    public sealed class ProductInfo
    {
        public ProductInfo(string name, string version, string description, string developer, string copyright, Uri website, Uri contactPage, Uri linkedIn,
            Uri updateFeed, string installFolder, string logFile)
        {
            Name = name;
            Version = version;
            Description = description;
            Developer = developer;
            Copyright = copyright;
            Website = website;
            ContactPage = contactPage;
            LinkedIn = linkedIn;
            UpdateFeed = updateFeed;
            LicenseFile = new Uri(Path.Combine(installFolder, "License.txt"));
            NoticesFile = new Uri(Path.Combine(installFolder, "ThirdPartyNotices.txt"));
            LogFile = logFile;
        }

        public string Name { get; }

        public string Version { get; }

        public string Description { get; }

        public string Developer { get; }

        public string Copyright { get; }

        public Uri Website { get; }

        public Uri ContactPage { get; }

        public Uri LinkedIn { get; }

        // The page with the video walkthrough
        public Uri Walkthrough => new Uri(Website, "placekit/guide/");

        // The JSON file on the website that names the latest version.
        public Uri UpdateFeed { get; }

        public Uri LicenseFile { get; }

        public Uri NoticesFile { get; }

        // Errors and warnings, for a problem report
        public string LogFile { get; }

        // UTM tags, for the website's analytics
        public Uri Link(Uri page, string placement) =>
            new UriBuilder(page) { Query = $"utm_source={Name.ToLowerInvariant()}&utm_medium=add-in&utm_content={placement}" }.Uri;

        public static ProductInfo FromAssembly(Assembly assembly)
        {
            AssemblyName assemblyName = assembly.GetName();
            return new ProductInfo(
                assembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product ?? assemblyName.Name,
                assemblyName.Version.ToString(3),
                assembly.GetCustomAttribute<AssemblyDescriptionAttribute>()?.Description,
                GetMetadata(assembly, "Developer"),
                assembly.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright,
                new Uri(GetMetadata(assembly, "Website")),
                new Uri(GetMetadata(assembly, "ContactPage")),
                new Uri(GetMetadata(assembly, "LinkedIn")),
                new Uri(GetMetadata(assembly, "UpdateFeed")),
                // CodeBase, not Location, which can be a shadow copy
                Path.GetDirectoryName(new Uri(assembly.CodeBase).LocalPath),
                FileLog.DefaultPath);
        }

        static string GetMetadata(Assembly assembly, string key) =>
            assembly.GetCustomAttributes<AssemblyMetadataAttribute>().FirstOrDefault(attribute => attribute.Key == key)?.Value;
    }
}
