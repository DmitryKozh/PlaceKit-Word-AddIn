using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Office.Tools;
using PlaceKit.Interfaces;
using PlaceKit.Models;
using PlaceKit.Services;
using PlaceKit.ViewModels;
using Word = Microsoft.Office.Interop.Word;

namespace PlaceKit
{
    internal static class CompositionRoot
    {
        public static ServiceProvider Build(Func<Word.Application> application, Func<CustomTaskPaneCollection> taskPanes)
        {
            ServiceCollection services = new ServiceCollection();

            services.AddSingleton<IWordHost>(new WordHost(application));
            services.AddSingleton<ITaskPaneService>(new TaskPaneService(taskPanes));
            services.AddSingleton(ProductInfo.FromAssembly(typeof(CompositionRoot).Assembly));
            services.AddSingleton<ISettingsService, RegistrySettingsService>();
            services.AddSingleton<IPlaceholderService, PlaceholderService>();
            services.AddSingleton<IDialogService, DialogService>();
            services.AddSingleton<IProgressService, ProgressService>();
            services.AddSingleton<IUrlLauncher, UrlLauncher>();
            services.AddSingleton<IEditorService, EditorService>();

            services.AddSingleton<RibbonViewModel>();
            services.AddTransient<SettingsViewModel>();
            services.AddTransient<AboutViewModel>();

            return services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });
        }
    }
}
