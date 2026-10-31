using System;
using Microsoft.Win32;
using PlaceKit.Interfaces;
using PlaceKit.Models;

namespace PlaceKit.Services
{
    public sealed class RegistrySettingsService : ISettingsService
    {
        const string DefaultKeyPath = @"Software\BoringJob\PlaceKit";
        const string OpeningMarkerValue = "OpeningMarker";
        const string ClosingMarkerValue = "ClosingMarker";
        const string ShowCreateSummaryValue = "ShowCreateSummary";
        const string OpenEditorAfterCreateValue = "OpenEditorAfterCreate";
        const string EditorPlacementValue = "EditorPlacement";
        const string AskAboutEmptyPlaceholdersValue = "AskAboutEmptyPlaceholders";
        const string EmptyPlaceholderActionValue = "EmptyPlaceholderAction";
        const string AiProviderValue = "AiProvider";
        const string DetectNoteValue = "DetectNote";

        readonly string _keyPath;

        public RegistrySettingsService()
            : this(DefaultKeyPath)
        {
        }

        internal RegistrySettingsService(string keyPath)
        {
            _keyPath = keyPath;
        }

        public string Location => @"HKEY_CURRENT_USER\" + _keyPath;

        public PlaceKitSettings Load()
        {
            PlaceKitSettings settings = new PlaceKitSettings();
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(_keyPath))
            {
                if (key == null)
                {
                    return settings;
                }

                settings.OpeningMarker = ReadMarker(key, OpeningMarkerValue, settings.OpeningMarker);
                settings.ClosingMarker = ReadMarker(key, ClosingMarkerValue, settings.ClosingMarker);
                settings.ShowCreateSummary = ReadFlag(key, ShowCreateSummaryValue, settings.ShowCreateSummary);
                settings.OpenEditorAfterCreate = ReadFlag(key, OpenEditorAfterCreateValue, settings.OpenEditorAfterCreate);
                settings.EditorPlacement = ReadEnum(key, EditorPlacementValue, settings.EditorPlacement);
                settings.AskAboutEmptyPlaceholders = ReadFlag(key, AskAboutEmptyPlaceholdersValue, settings.AskAboutEmptyPlaceholders);
                settings.EmptyPlaceholderAction = ReadEnum(key, EmptyPlaceholderActionValue, settings.EmptyPlaceholderAction);
                settings.AiProvider = ReadEnum(key, AiProviderValue, settings.AiProvider);
                settings.DetectNote = key.GetValue(DetectNoteValue) as string ?? settings.DetectNote;
            }

            return settings;
        }

        public void Save(PlaceKitSettings settings)
        {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(_keyPath))
            {
                key.SetValue(OpeningMarkerValue, settings.OpeningMarker, RegistryValueKind.String);
                key.SetValue(ClosingMarkerValue, settings.ClosingMarker, RegistryValueKind.String);
                key.SetValue(ShowCreateSummaryValue, settings.ShowCreateSummary ? 1 : 0, RegistryValueKind.DWord);
                key.SetValue(OpenEditorAfterCreateValue, settings.OpenEditorAfterCreate ? 1 : 0, RegistryValueKind.DWord);
                key.SetValue(EditorPlacementValue, settings.EditorPlacement.ToString(), RegistryValueKind.String);
                key.SetValue(AskAboutEmptyPlaceholdersValue, settings.AskAboutEmptyPlaceholders ? 1 : 0, RegistryValueKind.DWord);
                key.SetValue(EmptyPlaceholderActionValue, settings.EmptyPlaceholderAction.ToString(), RegistryValueKind.String);
                key.SetValue(AiProviderValue, settings.AiProvider.ToString(), RegistryValueKind.String);
                key.SetValue(DetectNoteValue, settings.DetectNote ?? string.Empty, RegistryValueKind.String);
            }
        }

        static string ReadMarker(RegistryKey key, string name, string fallback) =>
            key.GetValue(name) is string marker && PlaceholderPattern.ValidateMarker(marker) == null ? marker : fallback;

        static bool ReadFlag(RegistryKey key, string name, bool fallback) =>
            key.GetValue(name) is int flag ? flag != 0 : fallback;

        // TryParse also accepts numbers, hence the IsDefined check.
        static TEnum ReadEnum<TEnum>(RegistryKey key, string name, TEnum fallback) where TEnum : struct =>
            key.GetValue(name) is string text && Enum.TryParse(text, out TEnum value) && Enum.IsDefined(typeof(TEnum), value) ? value : fallback;
    }
}
