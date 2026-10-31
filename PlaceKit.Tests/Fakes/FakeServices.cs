using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using PlaceKit.Interfaces;
using PlaceKit.Models;
using PlaceKit.ViewModels;
using Word = Microsoft.Office.Interop.Word;

namespace PlaceKit.Tests.Fakes
{
    sealed class MemorySettings : ISettingsService
    {
        public PlaceKitSettings Current { get; } = new PlaceKitSettings();

        public PlaceKitSettings Saved { get; private set; }

        public string Location => @"HKEY_CURRENT_USER\Software\BoringJob\PlaceKit";

        public PlaceKitSettings Load() => Copy(Current, new PlaceKitSettings());

        // Like the registry: what is saved is what the next Load returns.
        public void Save(PlaceKitSettings settings)
        {
            Saved = settings;
            Copy(settings, Current);
        }

        static PlaceKitSettings Copy(PlaceKitSettings from, PlaceKitSettings to)
        {
            to.OpeningMarker = from.OpeningMarker;
            to.ClosingMarker = from.ClosingMarker;
            to.ShowCreateSummary = from.ShowCreateSummary;
            to.OpenEditorAfterCreate = from.OpenEditorAfterCreate;
            to.EditorPlacement = from.EditorPlacement;
            to.AskAboutEmptyPlaceholders = from.AskAboutEmptyPlaceholders;
            to.EmptyPlaceholderAction = from.EmptyPlaceholderAction;
            to.AiProvider = from.AiProvider;
            to.DetectNote = from.DetectNote;
            to.CheckForUpdates = from.CheckForUpdates;
            return to;
        }
    }

    sealed class InlineProgress : IProgressService
    {
        public T Run<T>(string title, Func<IProgress<ProgressInfo>, CancellationToken, T> operation) =>
            operation(new ProgressRecorder(), CancellationToken.None);
    }

    sealed class FakePanes : ITaskPaneService
    {
        public List<string> Calls { get; } = new List<string>();

        public EditorViewModel Shown { get; private set; }

        public void Show(EditorViewModel editor, Word.Window window)
        {
            Calls.Add(window == null ? "show without window" : "show");
            Shown = editor;
        }

        public void Hide(EditorViewModel editor) => Calls.Add("hide");

        public void Remove(EditorViewModel editor) => Calls.Add("remove");
    }

    sealed class FakeValuesFile : IPlaceholderValuesFile
    {
        public Dictionary<string, List<KeyValuePair<string, string>>> Files { get; } = new Dictionary<string, List<KeyValuePair<string, string>>>();

        public Exception ReadError { get; set; }

        public IReadOnlyDictionary<string, string> Read(string path)
        {
            if (ReadError != null)
            {
                throw ReadError;
            }

            return Files[path].ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);
        }

        public void Write(string path, IEnumerable<KeyValuePair<string, string>> values) => Files[path] = values.ToList();
    }

    sealed class FakeUrlLauncher : IUrlLauncher
    {
        public List<Uri> Opened { get; } = new List<Uri>();

        public void Open(Uri url) => Opened.Add(url);
    }

    sealed class FakeEditors : IEditorService
    {
        public int Refreshes { get; private set; }

        public void Open(Word.Document document)
        {
        }

        public void Refresh(Word.Document document) => Refreshes++;

        public void FinalizeDocument(Word.Document document)
        {
        }
    }

    sealed class FakeGuide : IGuideService
    {
        public List<PlaceholderPattern> Patterns { get; } = new List<PlaceholderPattern>();

        public Word.Document CreateGuide(PlaceholderPattern pattern)
        {
            Patterns.Add(pattern);
            return null;
        }
    }

    sealed class MemoryApiKeyStore : IApiKeyStore
    {
        readonly Dictionary<AiProvider, string> _keys = new Dictionary<AiProvider, string>();

        public string Claude
        {
            get => Load(AiProvider.Claude);
            set => _keys[AiProvider.Claude] = value;
        }

        public string OpenAI
        {
            get => Load(AiProvider.OpenAI);
            set => _keys[AiProvider.OpenAI] = value;
        }

        public bool HasKey(AiProvider provider) => Load(provider) != null;

        public string Load(AiProvider provider) => _keys.TryGetValue(provider, out string key) ? key : null;

        public void Save(AiProvider provider, string apiKey) => _keys[provider] = apiKey;

        public void Delete(AiProvider provider) => _keys.Remove(provider);
    }

    sealed class FakeAiService : IAiService
    {
        public AiProvider Provider { get; set; } = AiProvider.Claude;

        public bool HasApiKey { get; set; } = true;

        public List<string> EnsuredFor { get; } = new List<string>();

        // What the user does when asked to set up a key; by default nothing, so the answer is HasApiKey.
        public Action OnSetUpKey { get; set; } = () => { };

        public int SettingsOpened { get; private set; }

        public Action OnOpenSettings { get; set; } = () => { };

        public List<KeyValuePair<AiProvider, string>> TestedKeys { get; } = new List<KeyValuePair<AiProvider, string>>();

        public Exception TestError { get; set; }

        public string Send(AiRequest request, string progressTitle) => throw new NotSupportedException("The view model tests use fake finders.");

        public void TestKey(AiProvider provider, string apiKey)
        {
            TestedKeys.Add(new KeyValuePair<AiProvider, string>(provider, apiKey));
            if (TestError != null)
            {
                throw TestError;
            }
        }

        public bool EnsureApiKey(string feature)
        {
            EnsuredFor.Add(feature);
            if (!HasApiKey)
            {
                OnSetUpKey();
            }

            return HasApiKey;
        }

        public void OpenSettings()
        {
            SettingsOpened++;
            OnOpenSettings();
        }
    }

    sealed class FakeDetector : IPlaceholderDetector
    {
        public List<DetectedPlaceholder> Found { get; } = new List<DetectedPlaceholder>();

        public Exception Error { get; set; }

        public int Calls { get; private set; }

        public string LastText { get; private set; }

        public string LastDocumentText { get; private set; }

        public string LastNote { get; private set; }

        public IReadOnlyList<string> LastExistingNames { get; private set; }

        public PlaceholderPattern LastMarkers { get; private set; }

        public IReadOnlyList<DetectedPlaceholder> Detect(string text, string documentText, string note, IReadOnlyList<string> existingNames, PlaceholderPattern markers)
        {
            Calls++;
            LastText = text;
            LastDocumentText = documentText;
            LastNote = note;
            LastExistingNames = existingNames;
            LastMarkers = markers;
            if (Error != null)
            {
                throw Error;
            }

            return Found;
        }
    }

    // A service that answers with Answer and remembers the key and request it got.
    sealed class FakeAiClient : IAiClient
    {
        public FakeAiClient(AiProvider provider)
        {
            Provider = provider;
        }

        public AiProvider Provider { get; }

        public string Answer { get; set; } = "{}";

        public string LastApiKey { get; private set; }

        public AiRequest LastRequest { get; private set; }

        public System.Threading.Tasks.Task<string> SendAsync(string apiKey, AiRequest request, CancellationToken token)
        {
            LastApiKey = apiKey;
            LastRequest = request;
            return System.Threading.Tasks.Task.FromResult(Answer);
        }

        public System.Threading.Tasks.Task TestKeyAsync(string apiKey, CancellationToken token)
        {
            LastApiKey = apiKey;
            return System.Threading.Tasks.Task.CompletedTask;
        }
    }

    sealed class FakeValueFinder : IValueFinder
    {
        public Dictionary<string, string> Values { get; } = new Dictionary<string, string>(StringComparer.Ordinal);

        public Exception Error { get; set; }

        public int Calls { get; private set; }

        public string LastText { get; private set; }

        public IReadOnlyList<string> LastNames { get; private set; }

        public IReadOnlyDictionary<string, string> FindValues(string text, IReadOnlyList<string> names)
        {
            Calls++;
            LastText = text;
            LastNames = names;
            if (Error != null)
            {
                throw Error;
            }

            return Values;
        }
    }

    static class TestProduct
    {
        public static ProductInfo Create(string version = "1.2.3") => new ProductInfo("PlaceKit", version, "Description", "Developer", "Copyright",
            new Uri("https://boringjob.work"), new Uri("https://boringjob.work/contact/"), new Uri("https://www.linkedin.com/in/developer/"),
            new Uri("https://boringjob.work/placekit/latest.json"), @"C:\PlaceKit");
    }

    static class TestRelease
    {
        public static ReleaseInfo Create(string version, bool withInstaller = true) => new ReleaseInfo(
            Version.Parse(version),
            new Uri("https://boringjob.work/placekit/"),
            withInstaller ? new Uri($"https://boringjob.work/placekit/PlaceKit-{version}.msi") : null);
    }

    sealed class FakeReleaseSource : IReleaseSource
    {
        public ReleaseInfo Latest { get; set; }

        public Exception Error { get; set; }

        public int Calls { get; private set; }

        public System.Threading.Tasks.Task<ReleaseInfo> GetLatestAsync(CancellationToken token)
        {
            Calls++;
            return Error != null ? System.Threading.Tasks.Task.FromException<ReleaseInfo>(Error) : System.Threading.Tasks.Task.FromResult(Latest);
        }
    }

    sealed class FakeUpdateService : IUpdateService
    {
        public ReleaseInfo AvailableUpdate { get; set; }

        // What Check finds; null means this is the latest version.
        public ReleaseInfo Found { get; set; }

        public Exception Error { get; set; }

        public int Checks { get; private set; }

        public event EventHandler AvailableUpdateChanged;

        public ReleaseInfo Check()
        {
            Checks++;
            if (Error != null)
            {
                throw Error;
            }

            Announce(Found);
            return Found;
        }

        public System.Threading.Tasks.Task CheckInBackgroundAsync() => System.Threading.Tasks.Task.CompletedTask;

        public void Announce(ReleaseInfo update)
        {
            AvailableUpdate = update;
            AvailableUpdateChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    // Runs what is posted at once and counts it, in place of Word's thread.
    sealed class InlineSynchronizationContext : SynchronizationContext
    {
        public int Posts { get; private set; }

        public override void Post(SendOrPostCallback callback, object state)
        {
            Posts++;
            callback(state);
        }
    }
}
