using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Security;
using System.Text;

namespace PlaceKit.Tools
{
    // Warnings and errors from Trace go to a file on the user's computer, to send along with a problem report.
    // Another Word may write to it too, so the file is opened for each line instead of being kept open.
    public sealed class FileLog : TraceListener
    {
        internal const long MaxSize = 1024 * 1024;

        static readonly string Version = typeof(FileLog).Assembly.GetName().Version.ToString(3);

        public FileLog(string path)
        {
            FilePath = path;
        }

        public static string DefaultPath { get; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BoringJob", "PlaceKit", "PlaceKit.log");

        public string FilePath { get; }

        // When it outgrows MaxSize, the log becomes PlaceKit.old.log and a new one starts.
        public string OldFilePath => Path.ChangeExtension(FilePath, ".old.log");

        public static void Start() => Trace.Listeners.Add(new FileLog(DefaultPath));

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string message) =>
            Append(eventType, message);

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string format, params object[] args) =>
            Append(eventType, Format(format, args));

        // Only the events above; plain Trace.Write output from libraries is left out.
        public override void Write(string message)
        {
        }

        public override void WriteLine(string message)
        {
        }

        static string Format(string format, object[] args)
        {
            if (args == null || args.Length == 0)
            {
                return format;
            }

            try
            {
                return string.Format(CultureInfo.InvariantCulture, format, args);
            }
            catch (FormatException)
            {
                return format + " " + string.Join(" | ", args);
            }
        }

        void Append(TraceEventType eventType, string message)
        {
            if (eventType > TraceEventType.Warning)
            {
                return;
            }

            string line = string.Format(CultureInfo.InvariantCulture, "{0:yyyy-MM-dd HH:mm:ss} {1} {2} {3}{4}", DateTime.Now, Version, eventType, message, Environment.NewLine);
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
                FileInfo file = new FileInfo(FilePath);
                if (file.Exists && file.Length > MaxSize)
                {
                    File.Copy(FilePath, OldFilePath, overwrite: true);
                    File.Delete(FilePath);
                }

                using (FileStream stream = new FileStream(FilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete))
                using (StreamWriter writer = new StreamWriter(stream, new UTF8Encoding(false)))
                {
                    writer.Write(line);
                }
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is SecurityException)
            {
                // Logging must never break what it reports on.
            }
        }
    }
}
