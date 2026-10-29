using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlaceKit.Interfaces;
using PlaceKit.Services;
using PlaceKit.ViewModels;

namespace PlaceKit.Tests
{
    [TestClass]
    public class CompositionRootTests
    {
        [TestMethod]
        public void ResolvesEverythingTheRibbonNeeds()
        {
            using (var services = Build())
            {
                Assert.IsNotNull(services.GetService(typeof(RibbonViewModel)));
                Assert.IsNotNull(services.GetService(typeof(SettingsViewModel)));
                Assert.IsNotNull(services.GetService(typeof(AboutViewModel)));
                Assert.IsInstanceOfType(services.GetService(typeof(IEditorService)), typeof(EditorService));
                Assert.IsInstanceOfType(services.GetService(typeof(ISettingsService)), typeof(RegistrySettingsService));
            }
        }

        // Build takes lazy accessors for Office objects. They are created by reflection, returning null,
        // so the tests don't need to reference the VSTO assemblies.
        static DisposableProvider Build()
        {
            MethodInfo build = typeof(CompositionRoot).GetMethod("Build");
            object[] accessors = build.GetParameters()
                .Select(p => (object)Expression.Lambda(p.ParameterType, Expression.Constant(null, p.ParameterType.GetGenericArguments()[0])).Compile())
                .ToArray();
            return new DisposableProvider((IServiceProvider)build.Invoke(null, accessors));
        }

        sealed class DisposableProvider : IServiceProvider, IDisposable
        {
            readonly IServiceProvider _provider;

            public DisposableProvider(IServiceProvider provider)
            {
                _provider = provider;
            }

            public object GetService(Type serviceType) => _provider.GetService(serviceType);

            public void Dispose() => (_provider as IDisposable)?.Dispose();
        }
    }
}
