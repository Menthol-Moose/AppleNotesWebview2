using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace AppleNotesPortable
{
    // Reads settings.ini from the application folder (portable: travels next to
    // the .exe). Section headers like [Window] are cosmetic; parsing is on
    // simple key=value lines, so the file is hard to break.
    internal sealed class AppSettings
    {
        public int WindowWidth { get; private set; } = 450;
        public int WindowHeight { get; private set; } = 700;
        public int ClipHeight { get; private set; } = 52;

        // Reload the page (pull latest from iCloud) when the window regains focus.
        public bool ReloadOnFocus { get; private set; } = true;
        // Don't reload unless the window was idle this many seconds first.
        public int IdleThresholdSeconds { get; private set; } = 2;
        // Suspend the web view entirely while minimized.
        public bool SuspendWhenMinimized { get; private set; } = true;

        public static AppSettings Load()
        {
            var s = new AppSettings();
            string path = Path.Combine(AppContext.BaseDirectory, "settings.ini");

            if (!File.Exists(path))
            {
                try { File.WriteAllText(path, DefaultIni); } catch { /* read-only: use defaults */ }
                return s;
            }

            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                foreach (var raw in File.ReadAllLines(path))
                {
                    string line = raw.Trim();
                    if (line.Length == 0 || line[0] == ';' || line[0] == '#' || line[0] == '[')
                        continue;
                    int eq = line.IndexOf('=');
                    if (eq <= 0) continue;
                    map[line.Substring(0, eq).Trim()] = line.Substring(eq + 1).Trim();
                }
            }
            catch { /* locked/malformed: use whatever parsed */ }

            s.WindowWidth = GetInt(map, "Width", s.WindowWidth);
            s.WindowHeight = GetInt(map, "Height", s.WindowHeight);
            s.ClipHeight = GetInt(map, "ClipHeight", s.ClipHeight);
            s.ReloadOnFocus = GetBool(map, "ReloadOnFocus", s.ReloadOnFocus);
            s.IdleThresholdSeconds = GetInt(map, "IdleThresholdSeconds", s.IdleThresholdSeconds);
            s.SuspendWhenMinimized = GetBool(map, "SuspendWhenMinimized", s.SuspendWhenMinimized);

            // Sanity clamps.
            if (s.WindowWidth < 200) s.WindowWidth = 200;
            if (s.WindowHeight < 200) s.WindowHeight = 200;
            if (s.ClipHeight < 0) s.ClipHeight = 0;
            if (s.IdleThresholdSeconds < 0) s.IdleThresholdSeconds = 0;

            return s;
        }

        private static int GetInt(Dictionary<string, string> map, string key, int fallback)
            => map.TryGetValue(key, out var v) &&
               int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n)
               ? n : fallback;

        private static bool GetBool(Dictionary<string, string> map, string key, bool fallback)
        {
            if (!map.TryGetValue(key, out var v)) return fallback;
            switch (v.Trim().ToLowerInvariant())
            {
                case "true": case "yes": case "1": case "on": return true;
                case "false": case "no": case "0": case "off": return false;
                default: return fallback;
            }
        }

        private const string DefaultIni =
@"; AppleNotesPortable settings
; Edit these values and restart the app.

[Window]
; Size of the app window, in pixels.
Width=450
Height=700

[WebView]
; Pixels clipped off the top of the page to hide iCloud's toolbar.
; Increase if any chrome still shows; decrease if it cuts into content.
ClipHeight=52

[Sync]
; Pull the latest from iCloud by reloading the page when the window regains
; focus. This is the reliable way to see changes made on another device,
; because iCloud's web client does not dependably push incoming changes into
; an already-open page. Trade-off: a reload may reset which note is selected
; (your edits are safe; iCloud auto-saves them). Set to false to keep your
; place and sync only on a manual Ctrl+R.
ReloadOnFocus=true
; Only reload if the window was idle/unfocused at least this many seconds,
; so a quick alt-tab doesn't trigger a reload.
IdleThresholdSeconds=2

[Idle]
; Fully suspend the web view while minimized (deepest power saving). It
; resumes and re-syncs automatically when you restore the window.
SuspendWhenMinimized=true
";
    }
}
