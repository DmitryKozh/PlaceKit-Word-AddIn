using System;
using System.Linq;
using System.Reflection;

namespace PlaceKit.Models
{
    public sealed class ProductInfo
    {
        public ProductInfo(string name, string version, string description, string developer, string copyright, Uri website)
        {
            Name = name;
            Version = version;
            Description = description;
            Developer = developer;
            Copyright = copyright;
            Website = website;
        }

        public string Name { get; }

        public string Version { get; }

        public string Description { get; }

        public string Developer { get; }

        public string Copyright { get; }

        public Uri Website { get; }

        public static ProductInfo FromAssembly(Assembly assembly)
        {
            AssemblyName assemblyName = assembly.GetName();
            return new ProductInfo(
                assembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product ?? assemblyName.Name,
                assemblyName.Version.ToString(3),
                assembly.GetCustomAttribute<AssemblyDescriptionAttribute>()?.Description,
                GetMetadata(assembly, "Developer"),
                assembly.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright,
                new Uri(GetMetadata(assembly, "Website")));
        }

        static string GetMetadata(Assembly assembly, string key) =>
            assembly.GetCustomAttributes<AssemblyMetadataAttribute>().FirstOrDefault(attribute => attribute.Key == key)?.Value;
    }
}
