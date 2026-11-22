using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace AppleNotesPortable
{
    internal enum ReloadMode
    {
        Manual,     // reload only on Ctrl+R / F5
        Automatic,  // also reload after wake from sleep / screen-off, and on unlock
        Timed       // Automatic, plus a reload every ReloadIntervalMinutes
    }

    // Reads settings.ini from the application folder (portable: travels next to
    // the .exe). Section headers like [Window] are cosmetic; parsing is on
    // simple key=value lines, so the file is hard to break.
    internal sealed class AppSettings
    {
        // Window size and clip height are in 100%-scale pixels; the app scales
        // them to the monitor's DPI at startup.
        public int WindowWidth { get; private set; } = 450;
        public int WindowHeight { get; private set; } = 700;
        public int ClipHeight { get; private set; } = 52;

        public ReloadMode ReloadMode { get; private set; } = ReloadMode.Automatic;
        public int ReloadIntervalMinutes { get; private set; } = 15;

        public bool SuspendWhenMinimized { get; private set; } = true;
        public bool ReloadOnRestore { get; private set; } = true;

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
            s.ReloadIntervalMinutes = GetInt(map, "ReloadIntervalMinutes", s.ReloadIntervalMinutes);
            s.SuspendWhenMinimized = GetBool(map, "SuspendWhenMinimized", s.SuspendWhenMinimized);
            s.ReloadOnRestore = GetBool(map, "ReloadOnRestore", s.ReloadOnRestore);

            if (map.TryGetValue("ReloadMode", out var mode) &&
                Enum.TryParse(mode, true, out ReloadMode parsed) &&
                Enum.IsDefined(typeof(ReloadMode), parsed))
            {
                s.ReloadMode = parsed;
            }
            else if (map.ContainsKey("ReloadOnFocus") && !GetBool(map, "ReloadOnFocus", true))
            {
                // Older settings.ini files: ReloadOnFocus=false meant "manual only".
                s.ReloadMode = ReloadMode.Manual;
            }

            // Sanity clamps.
            if (s.WindowWidth < 200) s.WindowWidth = 200;
            if (s.WindowHeight < 200) s.WindowHeight = 200;
            if (s.ClipHeight < 0) s.ClipHeight = 0;
            if (s.ReloadIntervalMinutes < 1) s.ReloadIntervalMinutes = 1;

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

        internal const string DefaultIni =
@"; AppleNotesPortable settings
; Edit these values and restart the app.

[Window]
; Size of the app window at 100% display scaling. The app scales it to your
; display's scaling automatically (e.g. 900x1400 real pixels at 200%).
Width=450
Height=700

[WebView]
; Height of iCloud's toolbar to hide, at 100% scaling (also auto-scaled).
; Increase if any toolbar still shows; decrease if it cuts into Notes.
ClipHeight=52

[Sync]
; When to reload the page to pull the latest from iCloud:
;   Manual    - only when you press Ctrl+R or F5
;   Automatic - also after the PC wakes from sleep or the screen turns back
;               on, and when the screen is unlocked
;   Timed     - everything in Automatic, plus a reload every
;               ReloadIntervalMinutes (never while you're typing in the app)
ReloadMode=Automatic
; Used only when ReloadMode=Timed.
ReloadIntervalMinutes=15

[Idle]
; Fully suspend the web view while minimized (deepest power saving).
SuspendWhenMinimized=true
; When restoring from minimized, reload if a reload came due while it was
; minimized (a wake or unlock in Automatic/Timed mode, or the interval
; passing in Timed mode). Set to false to skip it and use the app right away.
ReloadOnRestore=true
";
    }
}
