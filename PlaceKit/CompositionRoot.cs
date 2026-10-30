using System;
using System.Collections.Generic;
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
            services.AddSingleton<IPlaceholderValuesFile, PlaceholderValuesFile>();
            services.AddSingleton<IGuideService, GuideService>();
            services.AddSingleton<IApiKeyStore, ProtectedApiKeyStore>();
            services.AddSingleton<IValueFinder, ClaudeValueFinder>();

            services.AddSingleton<RibbonViewModel>();
            services.AddTransient<SettingsViewModel>();
            services.AddTransient<AboutViewModel>();
            services.AddSingleton<Func<IReadOnlyList<string>, FillFromTextViewModel>>(provider => names => new FillFromTextViewModel(
                names,
                provider.GetRequiredService<IValueFinder>(),
                provider.GetRequiredService<IApiKeyStore>(),
                provider.GetRequiredService<IUrlLauncher>(),
                provider.GetRequiredService<IDialogService>()));

            return services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });
        }
    }
}
