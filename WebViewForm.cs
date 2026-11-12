using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;

namespace AppleNotesPortable
{
    public partial class WebViewForm : Form
    {
        private const string NotesUrl = "https://www.icloud.com/notes";

        private readonly AppSettings _settings = AppSettings.Load();

        // Sync/idle state tracking.
        private DateTime _hiddenSince = DateTime.MinValue;
        private bool _pendingResync;     // window went idle; a refocus should pull
        private bool _firstLoadComplete; // never reload before the first load/sign-in
        private bool _waking;            // re-entrancy guard for WakeAndSync

        public WebViewForm()
        {
            InitializeComponent();
            this.ClientSize = new Size(_settings.WindowWidth, _settings.WindowHeight);

            // Drive sync off the OS window's focus/visibility, because WebView2
            // does NOT propagate window state to the page on its own.
            this.Activated += (s, e) => WakeAndSync();
            this.Deactivate += (s, e) => MarkIdle();
            this.Resize += Form_Resize;
        }

        private async void WebViewForm_Load(object sender, EventArgs e)
        {
            int clip = _settings.ClipHeight;
            webView21.Bounds = new Rectangle(
                0, -clip, contentHost.ClientSize.Width, contentHost.ClientSize.Height + clip);

            // Pin the User Data Folder to a stable location in %LocalAppData%
            // so cookies (and therefore the iCloud sign-in) survive app
            // rebuilds, moves, and WebView2 Runtime updates. The default
            // location is next to the .exe, which is fragile: rename/move the
            // exe and you get a fresh UDF with no cookies and have to sign in
            // again. %LocalAppData%\AppleNotesPortable\WebView2 never changes.
            string udf = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "AppleNotesPortable", "WebView2");

            CoreWebView2Environment env;
            try
            {
                env = await CoreWebView2Environment.CreateAsync(null, udf);
                await webView21.EnsureCoreWebView2Async(env);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Couldn't start the WebView2 engine. Make sure the Microsoft Edge " +
                    "WebView2 Runtime is installed.\n\n" + ex.Message,
                    "Apple Notes", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var core = webView21.CoreWebView2;

            // Keep reload (Ctrl+R / F5) available as a manual sync as well. A
            // previous build disabling these would explain "Ctrl+R did nothing."
            core.Settings.AreBrowserAcceleratorKeysEnabled = true;

            // Record that the page has loaded at least once so we never reload
            // mid sign-in.
            core.NavigationCompleted += (s, a) => { if (a.IsSuccess) _firstLoadComplete = true; };

            // Stay inside this one window.
            core.NewWindowRequested += (s, a) => a.Handled = true;

            core.Navigate(NotesUrl);
        }

        // Window lost focus (possibly still on screen). Record the moment so a
        // later refocus can decide whether it was idle long enough to warrant a
        // cloud pull. We deliberately do NOT suspend here: the window may still
        // be visible, and Chromium throttles an unfocused renderer on its own,
        // which is the idle saving we want without blanking the window.
        private void MarkIdle()
        {
            if (!_pendingResync)
            {
                _pendingResync = true;
                _hiddenSince = DateTime.UtcNow;
            }
        }

        private void Form_Resize(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
                GoIdleMinimized();
            else
                WakeAndSync();
        }

        // Minimized: nothing is on screen to keep live, so take the deepest
        // power saving by suspending the web view entirely.
        private async void GoIdleMinimized()
        {
            MarkIdle();
            var core = webView21?.CoreWebView2;
            if (core == null) return;

            if (_settings.SuspendWhenMinimized)
            {
                // Suspend is only allowed when the web view is hidden; while the
                // window is minimized this isn't visible to the user anyway.
                webView21.Visible = false;
                try { await core.TrySuspendAsync(); }
                catch { /* not supported / already suspended */ }
            }
        }

        // Window returned to the foreground: wake the engine and, if it was idle
        // long enough, force a fresh pull from iCloud by reloading. The reload
        // is the reliable cloud sync, since iCloud's web client doesn't
        // dependably push incoming changes into an open page.
        private void WakeAndSync()
        {
            if (_waking) return;
            _waking = true;
            try
            {
                var core = webView21?.CoreWebView2;
                if (core == null) return;

                if (!webView21.Visible)
                    webView21.Visible = true;

                try { core.Resume(); }
                catch { /* wasn't suspended */ }

                if (_pendingResync)
                {
                    bool idleEnough =
                        (DateTime.UtcNow - _hiddenSince).TotalSeconds >= _settings.IdleThresholdSeconds;

                    if (_settings.ReloadOnFocus && _firstLoadComplete && idleEnough)
                        core.Reload();

                    _pendingResync = false;
                }
            }
            finally { _waking = false; }
        }
    }
}
