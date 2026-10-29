using System;
using System.Linq;
using System.Reflection;

namespace PlaceKit.Models
{
    public sealed class ProductInfo
    {
        public ProductInfo(string name, string version, string description, string developer, string copyright, Uri website, Uri contactPage)
        {
            Name = name;
            Version = version;
            Description = description;
            Developer = developer;
            Copyright = copyright;
            Website = website;
            ContactPage = contactPage;
        }

        public string Name { get; }

        public string Version { get; }

        public string Description { get; }

        public string Developer { get; }

        public string Copyright { get; }

        public Uri Website { get; }

        public Uri ContactPage { get; }

        // Links from the add-in to the developer's website are tagged, so the website's statistics show where visitors came from.
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
                new Uri(GetMetadata(assembly, "ContactPage")));
        }

        static string GetMetadata(Assembly assembly, string key) =>
            assembly.GetCustomAttributes<AssemblyMetadataAttribute>().FirstOrDefault(attribute => attribute.Key == key)?.Value;
    }
}
