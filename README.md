# PlaceKit

A Word add-in for documents you write again and again with different details. Mark the parts that change as `{{Client name}}`, `{{Order}}` and so on, click **Create**, and fill them all in from one panel.

## How it works

1. Write the document and mark the parts that change: `Dear {{Client name}}, ...`
2. **Create** turns the markers into content controls. Save the document as your template.
3. For each new case, **Fill** in the values. A name used in five places is typed once.
4. **Finalize** turns the controls back into plain text when the document is ready.

Click **Guide** on the PlaceKit tab for a sample warranty claim to try it on.

## Features

- **Create** works in the body, tables, headers, footers, text boxes and notes. Each control gets the name as its title and `PlaceKit-{name}` as its tag.
- **Detect** finds the placeholders in a finished document for you. Claude or ChatGPT reads the document (or just the selection) and suggests the parts that change, like names, dates and amounts. You can add a note on what to look for. You pick and rename the suggestions, and each one becomes a placeholder everywhere it appears. Suggestions are checked against the document, so made-up text never gets through. Markers already in the text, like `{{Customer}}`, are listed too and become empty placeholders, as with Create.
- **Fill** opens an editor in a side panel or in its own window. Each placeholder is listed once, with a search box and a jump to its place in the document. **Save and finalize** does both in one go.
- **Export** and **Import** save the values to a JSON file and load them into another document. Names match ignoring case, and nothing changes in the document until you click Save.

  ```json
  {
    "Client name": "Contoso Ltd.",
    "Address": "221B Baker Street\nLondon"
  }
  ```

- **From text** fills the editor from a pasted email or notes, using Claude or ChatGPT. Only the text and the placeholder names are sent, and you check the values before saving.
- **Finalize** asks what to do with placeholders left empty: turn them back into markers or delete them. Deleting also cleans up the space around them, so `Dear {{Name}},` becomes `Dear,`.
- **Settings**: the marker style (`[[name]]`, `<<name>>`, ...), how the editor opens after Create, what Finalize does with empty placeholders, and the AI service with your own API key ([Claude](https://console.anthropic.com/settings/keys) or [OpenAI](https://platform.openai.com/api-keys); a ChatGPT subscription doesn't cover the API). Test connection checks a key for free. Keys are encrypted with Windows DPAPI, and everything is stored under `HKEY_CURRENT_USER\Software\BoringJob\PlaceKit`.
- **Updates**: when Word starts, PlaceKit asks boringjob.work for the latest version, and an **Update** button appears on the tab when there is a newer one. The Updates page in Settings turns this off or checks right away, and so does Check for updates in About. Only the version number is asked for; nothing from your documents is sent.

Each step is a single Ctrl+Z (Save and finalize counts as two), and long ones can be canceled. With Track Changes on, the values Fill writes are tracked; creating and finalizing placeholders aren't.

## Install

Download `PlaceKit.msi` from [boringjob.work/placekit](https://boringjob.work/placekit/) or [Releases](../../releases) and run it. It doesn't need admin rights. Start Word and the PlaceKit tab is there.

- Requires Windows 10 or 11, Word 2013 or later (Microsoft 365 included), the .NET Framework 4.8 and the VSTO runtime. Current Windows and Office already have the last two, and the installer tells you if one is missing.
- The installer isn't code-signed yet, so SmartScreen may warn about an unknown publisher. Click **More info**, then **Run anyway**.
- To update, close Word and run the new `PlaceKit.msi`. It replaces the old version, and your settings and API keys stay.
- Silent install, for company deployments: `msiexec /i PlaceKit.msi /qn`
- Uninstall from Settings > Apps. PlaceKit's settings and saved API keys stay in the registry, so remove the keys in PlaceKit's Settings first.

## License

PlaceKit is free for personal and other noncommercial use under the [PolyForm Noncommercial License 1.0.0](LICENSE.md). Charities, schools, public research and government institutions may use it too, however they are funded.

Using it in a business, for example for a company's contracts, offers or letters, needs a commercial license. [Write to me](https://boringjob.work/contact/) about a license for your team, adapting PlaceKit to your templates or a feature you are missing.

The add-in installs `License.txt` and `ThirdPartyNotices.txt` (the licenses of the libraries it uses, [THIRD-PARTY-NOTICES.txt](THIRD-PARTY-NOTICES.txt) here). About opens both.

## Project

- `PlaceKit`: the VSTO add-in (.NET Framework 4.8, WPF, MVVM). The ribbon only calls `RibbonViewModel` commands, and only services such as `PlaceholderService` touch the Word object model. Dependency injection is Microsoft.Extensions.DependencyInjection (`CompositionRoot`), and PropertyChanged.Fody implements `INotifyPropertyChanged`. The AI features use the official Anthropic and OpenAI SDKs with strict JSON schema output (`ValueRequest`, `DetectRequest`, `AiService`). The SDKs need binding redirects on .NET Framework, so the build writes `PlaceKit.dll.config`, and the add-in turns on TLS 1.2, which Office leaves off. The update check reads a small JSON file from the website in the background and hands the answer back to Word's thread (`WebsiteReleaseSource`, `UpdateService`).
- `PlaceKit.IconRenderer`: draws the ribbon icons (PNG strips for each Office theme and scale), the installer icon and the setup wizard pictures. Run it after changing an icon; a test fails until you do.
- `PlaceKit.Installer`: a per-user MSI built with WiX 5. It installs to `%LocalAppData%\Programs\PlaceKit` and trusts the key the manifests are signed with, so Word loads the add-in without asking. The file list comes from the add-in's manifest.
- `PlaceKit.Tests`: MSTest. Tests in the `Word` category drive a hidden Word instance; the rest don't need Office.

## Building

You need Visual Studio 2022 or later with the Office/SharePoint development workload, and Word 2013 or later.

1. Open `PlaceKit.slnx`.
2. Create a signing certificate, since it isn't in the repository: Project properties > Signing > Create Test Certificate.
3. Press F5. Word starts with the PlaceKit tab.

Run the tests from Test Explorer, and filter with `TestCategory!=Word` to skip the ones that need Word. When you add or update a NuGet package, add it to `THIRD-PARTY-NOTICES.txt` as well, or a test fails.

Build the installer from the command line, because Visual Studio only opens a `.wixproj` with the HeatWave extension:

```
msbuild PlaceKit.Installer\PlaceKit.Installer.wixproj /restore /p:Configuration=Release
```

The MSI ends up in `PlaceKit.Installer\bin\Release\en-US\`. Building it leaves the registration F5 uses alone, but installing it on the same computer replaces it. After uninstalling, build the add-in once to register it again.

To release a version, set it in `PlaceKit/Properties/AssemblyInfo.cs` and push a tag such as `v1.0.1`. The Release workflow builds and tests it on GitHub and attaches `PlaceKit.msi` to the release.

Installed copies learn about the release from `https://boringjob.work/placekit/latest.json`, so update it once the installer is online. `page` is where "What's new" leads, and `download` is the installer; without it, Download opens the page too. Both must be https links.

```json
{
  "version": "1.0.1",
  "page": "https://boringjob.work/placekit/",
  "download": "https://boringjob.work/placekit/PlaceKit.msi"
}
```

## Need a .NET developer?

PlaceKit is a sample of my work. I'm Dzmitry Kazhamiakin, a C# and .NET developer with 11 years of experience, and I build software for small and mid-sized businesses:

- desktop applications and internal tools in C#, .NET and WPF
- REST APIs, webhooks and integrations between systems
- AI integration: document summaries, data extraction and assistants built into the software you already use
- SQL Server databases, data processing and reporting
- Office add-ins (VSTO), Excel and VBA automation
- maintenance and step-by-step modernization of legacy .NET software

If your team loses hours to copy-paste and routine work, [describe the problem](https://boringjob.work/contact/) at [boringjob.work](https://boringjob.work). The initial assessment is free. You can also find me on [LinkedIn](https://www.linkedin.com/in/dzmitry-kazhamiakin-55440486/).
