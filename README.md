# PlaceKit

**Fill a Word template once. Update every occurrence.**

PlaceKit is a Microsoft Word add-in for Windows. Turn a warranty claim, proposal, or letter into a reusable template with fields like `{{Customer}}`. Enter the customer name once and PlaceKit fills every occurrence, including in tables, headers, and footers. When you’re ready to send the document, finalize the fields as ordinary Word text.

[**Download for Windows**](https://boringjob.work/placekit/#install) · [See the full walkthrough](https://boringjob.work/placekit/#steps) · [**Custom Word automation by BoringJob**](https://boringjob.work/contact/)

[![PlaceKit in Microsoft Word showing a warranty claim and the Fill placeholders pane](https://boringjob.work/placekit/created.webp)](https://boringjob.work/placekit/#steps)

> **Built by BoringJob.** If your team generates the same documents from Word templates, spreadsheets, or internal systems, [tell me what you’re automating](https://boringjob.work/contact/). I build custom Word add-ins and document workflows.

## Try it with a warranty claim

1. Open **Guide** on the PlaceKit ribbon to get a sample claim. Or add markers such as `{{Customer}}` and `{{Claim number}}` to your own document.
2. Click **Create** to turn the markers into Word content controls. Save a copy to reuse as a template.
3. Open **Fill**, enter each value once, and save. PlaceKit updates every field with the same name.
4. Click **Finalize** to convert the fields to ordinary Word text. Recipients can open the finished document in Word without PlaceKit.

**Windows desktop Word · C# · VSTO · WPF · PolyForm Noncommercial**

## Features

Create, Fill, Finalize, Import, and Export run locally. AI is optional. When you use Detect or From text, PlaceKit sends the relevant text directly to Anthropic or OpenAI using your own API key.

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

Download the current MSI from [BoringJob’s PlaceKit page](https://boringjob.work/placekit/#install). Run it, then open Word and look for the **PlaceKit** tab. The installer runs per user and needs no administrator rights.

- Requires Windows 10 or 11, Word 2013 or later (Microsoft 365 included), the .NET Framework 4.8 and the VSTO runtime. Current Windows and Office already have the last two, and the installer tells you if one is missing.
- The current MSI is unsigned. The [download page](https://boringjob.work/placekit/#install) provides the SHA-256 checksum.
- To update, close Word and run the new `PlaceKit.msi`. It replaces the old version, and your settings and API keys stay.
- Silent install, for company deployments: `msiexec /i PlaceKit.msi /qn`
- Uninstall from Settings > Apps. PlaceKit's settings and saved API keys stay in the registry, so remove the keys in PlaceKit's Settings first.
- When something goes wrong, PlaceKit writes the details to `%LocalAppData%\BoringJob\PlaceKit\PlaceKit.log`. The file stays on your computer; attach it when you report a problem. **Show log file** on the Updates page of Settings opens its folder.

## License

PlaceKit is free for personal and other noncommercial use under the [PolyForm Noncommercial License 1.0.0](LICENSE.md). Charities, schools, public research and government institutions may use it too, however they are funded.

Using it in a business, for example for a company's contracts, offers or letters, needs a commercial license. [Write to me](https://boringjob.work/contact/) about a license for your team, adapting PlaceKit to your templates or a feature you are missing.

The add-in installs `License.txt` and `ThirdPartyNotices.txt` (the licenses of the libraries it uses, [THIRD-PARTY-NOTICES.txt](THIRD-PARTY-NOTICES.txt) here). About opens both.

## Architecture

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

## About BoringJob

I’m Dzmitry Kazhamiakin, the .NET developer behind [BoringJob](https://boringjob.work/). I build Word and Excel add-ins, document automation, and integrations for teams that spend too much time copying data into templates.

Need PlaceKit adapted to your workflow, a commercial license, or an add-in built for your team? [Tell me what you’re trying to automate](https://boringjob.work/contact/). You can also find me on [LinkedIn](https://www.linkedin.com/in/dzmitry-kazhamiakin-55440486/).
