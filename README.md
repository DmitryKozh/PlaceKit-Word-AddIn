# PlaceKit

Word add-in for documents with placeholders. Mark the variable parts of a document as `{{Client name}}`, `{{Order}}` and so on, click Create, and every marker becomes a content control. Then fill them all in from one editor instead of hunting through the document.

## Features

- Create turns `{{name}}` markers into plain-text content controls. Title is the name, tag is `PlaceKit-{name}`. Works in the body, tables, headers, footers, text boxes and notes.
- Fill opens the editor in a side panel next to the document, or in a separate window (the button in its top-left corner switches between the two). It lists every placeholder once, even if it is used several times, has a search box and can jump to a placeholder in the document.
- Export and Import in the editor save the filled-in values to a JSON file and fill them in from one, so the same data (company name, address, ...) can be reused in other documents. Names are matched ignoring case, and imported values reach the document only when you click Save:

  ```json
  {
    "Client name": "Contoso Ltd.",
    "Address": "221B Baker Street\nLondon"
  }
  ```

- Clear turns the controls back into text. Placeholders that were never filled in become `{{name}}` markers again.
- Settings: the markers (`[[name]]`, `<<name>>`, ...), whether to ask about opening the editor after Create, and where the editor opens. Stored in `HKEY_CURRENT_USER\Software\BoringJob\PlaceKit`.

Long operations show a progress window with Cancel. Every operation can be undone with a single Ctrl+Z.

## Project

- `PlaceKit` - the VSTO add-in (.NET Framework 4.8, WPF, MVVM). The ribbon only calls commands of `RibbonViewModel`; all Word object model code is in `PlaceholderService`. Services are wired up with Microsoft.Extensions.DependencyInjection in `CompositionRoot`, and PropertyChanged.Fody implements `INotifyPropertyChanged`.
- `PlaceKit.Tests` - MSTest. Tests in the `Word` category start a hidden Word instance and work on real documents; the rest run without Office.

## Building

You need Visual Studio 2022 or later with the Office/SharePoint development workload and Word 2013 or later.

1. Open `PlaceKit.slnx`.
2. The signing certificate is not in the repository: Project properties > Signing > Create Test Certificate.
3. F5 starts Word with the add-in loaded (PlaceKit tab).

Run the tests from Test Explorer. To skip the ones that need Word, filter with `TestCategory!=Word`.

## Author

Dzmitry Kazhamiakin - [boringjob.work](https://boringjob.work)
