# PlaceKit

Word add-in for documents with placeholders. Mark the variable parts of a document as `{{Client name}}`, `{{Order}}` and so on, click Create, and every marker becomes a content control. Then fill them all in from one editor instead of hunting through the document.

## What it is for

Claims, offers, contracts, letters: many documents are written again and again, and only the details change. With PlaceKit you prepare such a document once and then reuse it:

1. Write the document and mark the parts that change: `Dear {{Client name}}, ...`.
2. **Create** turns the markers into fields. Save the document as your template.
3. For each new case, **Fill** in the values in one list. A name used in five places is typed once. **Export** saves the values to a file, and **Import** fills them into the next document.
4. **Finalize** turns the fields into plain text when the document is ready.

To try it, click **Guide** on the PlaceKit tab. It opens a sample warranty claim with these steps, with placeholders in the text, a table and the footer.

## Features

- Create turns `{{name}}` markers into plain-text content controls. Title is the name, tag is `PlaceKit-{name}`. Works in the body, tables, headers, footers, text boxes and notes.
- Fill opens the editor in a side panel next to the document, or in a separate window (the button in its top-left corner switches between the two). It lists every placeholder once, even if it is used several times, has a search box and can jump to a placeholder in the document. Save and finalize writes the values and then finalizes the document; the first Ctrl+Z undoes the finalizing, the second the values.
- Export and Import in the editor save the filled-in values to a JSON file and fill them in from one, so the same data (company name, address, ...) can be reused in other documents. Names are matched ignoring case, and imported values reach the document only when you click Save:

  ```json
  {
    "Client name": "Contoso Ltd.",
    "Address": "221B Baker Street\nLondon"
  }
  ```

- From text in the editor finds the values in a text you paste, such as a customer's email, with Claude by Anthropic and your own [Claude API key](https://console.anthropic.com/settings/keys). Only the text and the placeholder names are sent. The answer has to follow a JSON schema that allows only these names, and the values fill in the editor like an import, so you check them before saving. The key is entered in the same window and saved encrypted with Windows DPAPI for your account; Remove deletes it.
- Finalize turns the controls back into text. For placeholders that were never filled in it asks whether to turn them back into `{{name}}` markers or delete them; with "Don't ask again" the choice is remembered and can be changed in Settings. Deleting tidies the space left behind, so `Dear {{Name}},` becomes `Dear,`.
- Settings: the markers (`[[name]]`, `<<name>>`, ...), whether to ask about opening the editor after Create, where the editor opens, and what Finalize does with empty placeholders. Stored in `HKEY_CURRENT_USER\Software\BoringJob\PlaceKit`.

Long operations show a progress window with Cancel. Every operation can be undone with a single Ctrl+Z. With Track Changes on, the values that Fill writes are tracked; creating and finalizing placeholders are not.

## Install

Download `PlaceKit.msi` from [boringjob.work/placekit](https://boringjob.work/placekit/) or the [releases](../../releases) and run it. It installs for the current user without administrator rights; start Word and the PlaceKit tab is there.

- Needs Windows 10 or 11, Word 2013 or later (Microsoft 365 included), the .NET Framework 4.8 and the Visual Studio Tools for Office runtime. Current Windows and Office come with the last two; the installer says so if one is missing.
- The installer is not code-signed yet, so Windows SmartScreen may warn about an unknown publisher: click More info, then Run anyway.
- To install it silently, for example from a company's software deployment: `msiexec /i PlaceKit.msi /qn`.
- Uninstall it in Settings > Apps. The PlaceKit settings stay in the registry, and so does a saved Claude API key: remove it first in the From text window.

## Project

- `PlaceKit` - the VSTO add-in (.NET Framework 4.8, WPF, MVVM). The ribbon only calls commands of `RibbonViewModel`; the Word object model is used only by services such as `PlaceholderService` and `GuideService`. Services are wired up with Microsoft.Extensions.DependencyInjection in `CompositionRoot`, and PropertyChanged.Fody implements `INotifyPropertyChanged`. `ClaudeValueFinder` calls the Claude API through the official Anthropic SDK with structured output; its packages need binding redirects on the .NET Framework, so the build writes `PlaceKit.dll.config`, which Word loads with the add-in. The ribbon images are PNG strips for the light and dark Office themes at 100 to 300 % scaling; the add-in loads the one that matches when Word starts.
- `PlaceKit.IconRenderer` - draws those strips from the vector shapes in `RibbonIcons`, and the installer's icon and setup wizard pictures from the About tile. Run it after changing an icon; a test fails while the strips and the shapes differ.
- `PlaceKit.Installer` - the MSI (WiX Toolset 5). It installs into `%LocalAppData%\Programs\PlaceKit`, registers the add-in in Word and adds the key the manifests are signed with to Office's trusted add-ins, so Word loads PlaceKit without asking. The list of files comes from the add-in's manifest at build time. The setup wizard uses the WixUI pages with PlaceKit's pictures and its own last page, which says where to find PlaceKit and can start Word.
- `PlaceKit.Tests` - MSTest. Tests in the `Word` category start a hidden Word instance and work on real documents; the rest run without Office.

## Building

You need Visual Studio 2022 or later with the Office/SharePoint development workload and Word 2013 or later.

1. Open `PlaceKit.slnx`.
2. The signing certificate is not in the repository: Project properties > Signing > Create Test Certificate.
3. F5 starts Word with the add-in loaded (PlaceKit tab).

Run the tests from Test Explorer. To skip the ones that need Word, filter with `TestCategory!=Word`.

The installer is built from the command line, since Visual Studio opens a `.wixproj` only with the HeatWave extension:

```
msbuild PlaceKit.Installer\PlaceKit.Installer.wixproj /restore /p:Configuration=Release
```

The package is `PlaceKit.Installer\bin\Release\en-US\PlaceKit.msi`. It builds the add-in in Release without registering it in Word, so the registration F5 uses stays as it is. Installing the MSI on the same computer replaces that registration; after uninstalling it, build the add-in once to register it again.

To release a version, set it in `PlaceKit/Properties/AssemblyInfo.cs` and push a tag such as `v1.0.1`. The Release workflow builds and tests it on GitHub and attaches `PlaceKit.msi` to the release.

## Need a .NET developer?

PlaceKit is a sample of my work. I'm Dzmitry Kazhamiakin, a C# and .NET developer with 11 years of experience, and I build software for small and mid-sized businesses:

- desktop applications and internal tools in C#, .NET and WPF
- REST APIs, webhooks and integrations between systems
- AI integration: document summaries, data extraction and assistants built into the software you already use
- SQL Server databases, data processing and reporting
- Office add-ins (VSTO), Excel and VBA automation
- maintenance and step-by-step modernization of legacy .NET software

If your team loses hours to copy-paste and routine work, [describe the problem](https://boringjob.work/contact/) at [boringjob.work](https://boringjob.work). The initial assessment is free.
