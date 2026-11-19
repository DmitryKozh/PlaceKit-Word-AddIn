using System.Collections.Generic;
using System.Linq;
using PlaceKit.Interfaces;
using PlaceKit.Models;
using Word = Microsoft.Office.Interop.Word;

namespace PlaceKit.Services
{
    // No brackets in the text, so only the placeholders match, whatever the markers.
    public sealed class GuideService : IGuideService
    {
        readonly IWordHost _wordHost;

        public GuideService(IWordHost wordHost)
        {
            _wordHost = wordHost;
        }

        public Word.Document CreateGuide(PlaceholderPattern pattern)
        {
            Word.Document document = _wordHost.NewDocument();
            Write(document, Content(pattern));
            document.Sections[1].Footers[Word.WdHeaderFooterIndex.wdHeaderFooterPrimary].Range.Text =
                $"Warranty claim {pattern.Format("Claim number")} \u00B7 {pattern.Format("Customer")}";

            document.Variables.Add(GuideSamples.DocumentVariable, "1");
            document.UndoClear();
            document.Saved = true;
            return document;
        }

        // The guide until it is saved: a document saved from it is the user's own, maybe a template for real claims.
        public static bool IsGuide(Word.Document document) =>
            string.IsNullOrEmpty(document.Path) &&
            document.Variables.Cast<Word.Variable>().Any(variable => variable.Name == GuideSamples.DocumentVariable);

        static List<(Kind Kind, string Text)> Content(PlaceholderPattern pattern)
        {
            string P(string name) => pattern.Format(name);
            List<(Kind, string)> content = new List<(Kind, string)>
            {
                (Kind.Title, "Try PlaceKit"),
                (Kind.Text, "PlaceKit is for documents you write again and again with different details, like claims, offers, contracts and letters. " +
                    "Mark the parts that change once, then fill them all in from one place instead of searching through the document."),
                (Kind.Text, "The warranty claim on the next page is ready to try. Each marked name in it is a placeholder, and Customer and Claim number are used three times, the footer included."),
                (Kind.Step, "Create. Click Create on the PlaceKit tab. Every marker becomes a field that keeps its name."),
                (Kind.Step, "Fill. Click Fill, type the values and click Save. The editor lists each placeholder once and fills in every place where it is used. " +
                    "Search finds a name, and the eye button shows its place in the document."),
                (Kind.Step, "Finalize. When the letter is ready, click Finalize. The fields turn back into plain text, ready to send. " +
                    "For placeholders left empty, you choose: turn them back into markers or delete them. Save and finalize in the editor does both in one go."),
                (Kind.Subheading, "More to try"),
                (Kind.Feature, "Detect. The order confirmation on the last page has no markers. Click Detect, and Claude or ChatGPT suggests the parts that change, " +
                    "like names, dates and amounts; you choose which of them become placeholders."),
                (Kind.Feature, "From text. After Create, click From text in the editor. A sample email is already filled in; Claude or ChatGPT finds the values in it, " +
                    "and you check them before saving."),
                (Kind.Feature, "Export and Import. In the editor, Export saves the values to a file, and Import loads them into the next claim."),
                (Kind.Feature, "Settings. Choose other markers, where the editor opens and what Finalize does with empty placeholders. " +
                    "Detect and From text need your own API key, which you add here; in this guide they work without one. The Updates page checks for new versions."),
                (Kind.Text, "Each step is a single undo, so Ctrl+Z takes you back. When you are done, close this document without saving."),
                (Kind.ClaimStart, P("Customer")),
                (Kind.Text, P("Customer address")),
                (Kind.Text, P("Date")),
                (Kind.Line, P("Supplier")),
                (Kind.Text, "Attention " + P("Supplier contact")),
                (Kind.Heading, "Warranty claim " + P("Claim number")),
                (Kind.Text, $"Dear {P("Supplier contact")},"),
                (Kind.Text, $"On {P("Delivery date")} we received order {P("Order number")}. The item below does not work as described, and we claim a replacement under the warranty."),
                (Kind.Row, "Item\tQuantity\tProblem"),
                (Kind.Row, $"{P("Item")}\t{P("Quantity")}\t{P("Problem")}"),
                (Kind.Text, $"Please send the replacement within 14 days and quote claim {P("Claim number")} in all correspondence."),
                (Kind.Text, "Kind regards,"),
                (Kind.Line, P("Customer contact")),
                (Kind.Text, P("Customer")),
            };

            // The order confirmation: its heading on a new page, the company line without space below.
            content.AddRange(GuideSamples.OrderConfirmation.Select((text, index) => (index == 0 ? Kind.PageHeading : index == 1 ? Kind.Line : Kind.Text, text)));
            return content;
        }

        static void Write(Word.Document document, List<(Kind Kind, string Text)> content)
        {
            document.Content.Text = string.Join("\r", content.Select(paragraph => paragraph.Text));

            int index = 0;
            Word.Range rows = null;
            int? confirmationStart = null;
            foreach (Word.Paragraph paragraph in document.Paragraphs)
            {
                (Kind kind, string text) = content[index++];
                paragraph.Range.set_Style(StyleOf(kind));
                switch (kind)
                {
                    case Kind.Step:
                    case Kind.Feature:
                        // The name, up to the first full stop.
                        document.Range(paragraph.Range.Start, paragraph.Range.Start + text.IndexOf('.') + 1).Font.Bold = 1;
                        break;
                    case Kind.Line:
                        paragraph.SpaceAfter = 0;
                        break;
                    case Kind.ClaimStart:
                        paragraph.SpaceAfter = 0;
                        paragraph.PageBreakBefore = -1;

                        // No spelling marks under PlaceKit and Ctrl+Z in the steps above.
                        document.Range(0, paragraph.Range.Start).NoProofing = -1;
                        break;
                    case Kind.Row:
                        rows = rows == null ? paragraph.Range : document.Range(rows.Start, paragraph.Range.End);
                        break;
                    case Kind.PageHeading:
                        paragraph.PageBreakBefore = -1;
                        confirmationStart = paragraph.Range.Start;
                        break;
                }
            }

            // Its names would get spelling marks too.
            if (confirmationStart.HasValue)
            {
                document.Range(confirmationStart.Value, document.Content.End).NoProofing = -1;
            }

            if (rows != null)
            {
                Word.Table table = rows.ConvertToTable(Word.WdTableFieldSeparator.wdSeparateByTabs, content.Count(paragraph => paragraph.Kind == Kind.Row));
                table.Borders.Enable = 1;
                table.Rows[1].Range.Font.Bold = 1;
                table.Rows[1].HeadingFormat = -1;

                Word.Range afterTable = table.Range;
                afterTable.Collapse(Word.WdCollapseDirection.wdCollapseEnd);
                afterTable.ParagraphFormat.SpaceBefore = 12;
            }
        }

        static Word.WdBuiltinStyle StyleOf(Kind kind)
        {
            switch (kind)
            {
                case Kind.Title:
                    return Word.WdBuiltinStyle.wdStyleTitle;
                case Kind.Step:
                    return Word.WdBuiltinStyle.wdStyleListNumber;
                case Kind.Feature:
                    return Word.WdBuiltinStyle.wdStyleListBullet;
                case Kind.Subheading:
                    return Word.WdBuiltinStyle.wdStyleHeading2;
                case Kind.Heading:
                case Kind.PageHeading:
                    return Word.WdBuiltinStyle.wdStyleHeading1;
                default:
                    return Word.WdBuiltinStyle.wdStyleNormal;
            }
        }

        enum Kind
        {
            Title,
            Text,
            Step,
            Subheading,
            Feature,

            // An address line, without space below.
            Line,

            // The claim's first line, on a page of its own.
            ClaimStart,
            Heading,

            // Tab-separated columns; consecutive rows become a table.
            Row,

            // The order confirmation's heading, on a page of its own
            PageHeading,
        }
    }
}
