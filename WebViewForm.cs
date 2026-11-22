using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;

namespace AppleNotesPortable
{
    public partial class WebViewForm : Form
    {
        private const string NotesUrl = "https://www.icloud.com/notes";

        // A timed reload never fires while you're using the app: the window is
        // focused (or under the mouse) and there was input in the last minute.
        private static readonly TimeSpan TypingGrace = TimeSpan.FromSeconds(60);
        // Wake, screen-on and unlock often arrive together; one reload covers them.
        private static readonly TimeSpan TriggerMergeWindow = TimeSpan.FromSeconds(15);
        // After a wake, give Wi-Fi a moment to reconnect before reloading,
        // and retry a few times if the network still isn't back.
        private const int ReloadDelayMs = 3000;
        // Restoring from minimized isn't a wake, so the network is already up.
        private const int RestoreReloadDelayMs = 100;
        private const int RetryDelayMs = 5000;
        private const int MaxRetries = 6;

        private readonly AppSettings _settings = AppSettings.Load();
        private readonly Timer _reloadDelayTimer = new Timer();
        private readonly Timer _intervalTimer = new Timer { Interval = 30_000 };

        private bool _firstLoadComplete;
        private bool _isMinimized;
        private bool _sessionLocked;
        private bool _pendingReload;   // a trigger arrived while minimized or locked
        private int _retriesLeft;
        private DateTime _lastAutoReload = DateTime.MinValue;
        private DateTime _lastNavigationStart = DateTime.UtcNow;

        private IntPtr _consoleDisplayNotify = IntPtr.Zero;
        private IntPtr _sessionDisplayNotify = IntPtr.Zero;
        private bool _sessionNotifyRegistered;
        private bool _consoleDisplayWasOff;
        private bool _sessionDisplayWasOff;

        public WebViewForm()
        {
            InitializeComponent();

            _reloadDelayTimer.Tick += (s, e) => { _reloadDelayTimer.Stop(); DoReload(); };
            _intervalTimer.Tick += IntervalTimer_Tick;
            this.Resize += Form_Resize;

            // Moving to a monitor with different scaling: Windows resizes the
            // window first, then we re-apply exact sizes for the new scale.
            this.DpiChanged += (s, e) => BeginInvoke(new Action(ApplyLayout));
        }

        private async void WebViewForm_Load(object sender, EventArgs e)
        {
            ApplyLayout();

            // Pin the User Data Folder to a stable location in %LocalAppData%
            // so cookies (and therefore the iCloud sign-in) survive app
            // rebuilds, moves, and WebView2 Runtime updates.
            string udf = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "AppleNotesPortable", "WebView2");

            try
            {
                var env = await CoreWebView2Environment.CreateAsync(null, udf);
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

            // Ctrl+R / F5 always work as a manual sync.
            core.Settings.AreBrowserAcceleratorKeysEnabled = true;

            core.NavigationStarting += (s, a) => _lastNavigationStart = DateTime.UtcNow;
            core.NavigationCompleted += Core_NavigationCompleted;

            // Stay inside this one window.
            core.NewWindowRequested += (s, a) => a.Handled = true;

            if (_settings.ReloadMode == ReloadMode.Timed)
                _intervalTimer.Start();

            // Retry the first load too, in case the app starts before Wi-Fi is up.
            _retriesLeft = MaxRetries;
            core.Navigate(NotesUrl);
        }

        // Window size and clip height in settings.ini are at 100% scaling;
        // convert them to real pixels for the monitor the window is on.
        private void ApplyLayout()
        {
            if (WindowState == FormWindowState.Minimized)
                return;

            float scale = DeviceDpi / 96f;
            var client = new Size(
                (int)Math.Round(_settings.WindowWidth * scale),
                (int)Math.Round(_settings.WindowHeight * scale));
            int clip = (int)Math.Round(_settings.ClipHeight * scale);

            ClientSize = client;
            // Shift the page up by the clip height so iCloud's toolbar sits
            // above the visible area: hidden and unclickable.
            webView21.Bounds = new Rectangle(0, -clip, client.Width, client.Height + clip);
        }

        private void Core_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs a)
        {
            if (a.IsSuccess)
            {
                _firstLoadComplete = true;
                _retriesLeft = 0;
                return;
            }

            // A load was replaced by another one; nothing to retry.
            if (a.WebErrorStatus == CoreWebView2WebErrorStatus.OperationCanceled)
                return;

            // Usually means the network isn't back yet after a wake.
            if (_retriesLeft > 0)
            {
                _retriesLeft--;
                ScheduleReload(RetryDelayMs);
            }
        }

        // ---- Triggers ----------------------------------------------------------

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            var consoleGuid = NativeMethods.GUID_CONSOLE_DISPLAY_STATE;
            var sessionGuid = NativeMethods.GUID_SESSION_DISPLAY_STATUS;
            _consoleDisplayNotify = NativeMethods.RegisterPowerSettingNotification(
                Handle, ref consoleGuid, NativeMethods.DEVICE_NOTIFY_WINDOW_HANDLE);
            _sessionDisplayNotify = NativeMethods.RegisterPowerSettingNotification(
                Handle, ref sessionGuid, NativeMethods.DEVICE_NOTIFY_WINDOW_HANDLE);
            _sessionNotifyRegistered = NativeMethods.WTSRegisterSessionNotification(
                Handle, NativeMethods.NOTIFY_FOR_THIS_SESSION);
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            if (_consoleDisplayNotify != IntPtr.Zero)
                NativeMethods.UnregisterPowerSettingNotification(_consoleDisplayNotify);
            if (_sessionDisplayNotify != IntPtr.Zero)
                NativeMethods.UnregisterPowerSettingNotification(_sessionDisplayNotify);
            if (_sessionNotifyRegistered)
                NativeMethods.WTSUnRegisterSessionNotification(Handle);

            _consoleDisplayNotify = _sessionDisplayNotify = IntPtr.Zero;
            _sessionNotifyRegistered = false;
            base.OnHandleDestroyed(e);
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            if (m.Msg == NativeMethods.WM_POWERBROADCAST)
            {
                long evt = m.WParam.ToInt64();
                if (evt == NativeMethods.PBT_APMRESUMEAUTOMATIC)
                    RequestAutoReload();   // woke from classic sleep or hibernation
                else if (evt == NativeMethods.PBT_POWERSETTINGCHANGE && m.LParam != IntPtr.Zero)
                    OnPowerSettingChange(m.LParam);
            }
            else if (m.Msg == NativeMethods.WM_WTSSESSION_CHANGE)
            {
                long reason = m.WParam.ToInt64();
                if (reason == NativeMethods.WTS_SESSION_LOCK)
                {
                    _sessionLocked = true;
                }
                else if (reason == NativeMethods.WTS_SESSION_UNLOCK)
                {
                    _sessionLocked = false;
                    RequestAutoReload();   // unlocked, with or without a password
                }
            }
        }

        // POWERBROADCAST_SETTING: GUID (16 bytes), DWORD DataLength, then the data.
        private void OnPowerSettingChange(IntPtr lParam)
        {
            Guid setting = Marshal.PtrToStructure<Guid>(lParam);
            int state = Marshal.ReadInt32(lParam, 20);   // 0 off, 1 on, 2 dimmed

            if (setting == NativeMethods.GUID_CONSOLE_DISPLAY_STATE)
                TrackDisplay(ref _consoleDisplayWasOff, state);
            else if (setting == NativeMethods.GUID_SESSION_DISPLAY_STATUS)
                TrackDisplay(ref _sessionDisplayWasOff, state);
        }

        // Display went fully off (screen timeout, sleep, Modern Standby) and has
        // now come back on. A brief dim without turning off doesn't count.
        private void TrackDisplay(ref bool wasOff, int state)
        {
            if (state == 0)
            {
                wasOff = true;
            }
            else if (state == 1 && wasOff)
            {
                wasOff = false;
                RequestAutoReload();
            }
        }

        private void IntervalTimer_Tick(object sender, EventArgs e)
        {
            if (_settings.ReloadMode != ReloadMode.Timed || _isMinimized || _sessionLocked)
                return;
            if (webView21?.CoreWebView2 == null)
                return;
            if (DateTime.UtcNow - _lastNavigationStart < TimeSpan.FromMinutes(_settings.ReloadIntervalMinutes))
                return;
            if (UserIsUsingApp())
                return;   // check again on the next tick

            RequestAutoReload();
        }

        private bool UserIsUsingApp()
        {
            bool engaged = Form.ActiveForm == this || Bounds.Contains(Cursor.Position);
            return engaged && NativeMethods.GetIdleTime() < TypingGrace;
        }

        // ---- Reloading ---------------------------------------------------------

        private void RequestAutoReload(int delayMs = ReloadDelayMs)
        {
            if (_settings.ReloadMode == ReloadMode.Manual)
                return;
            if (webView21?.CoreWebView2 == null)
                return;

            // Nothing useful to do while hidden; reload when the user can see it.
            if (_isMinimized || _sessionLocked)
            {
                _pendingReload = true;
                return;
            }

            if (DateTime.UtcNow - _lastAutoReload < TriggerMergeWindow)
                return;
            if (_reloadDelayTimer.Enabled)
                return;   // one is already on its way

            _retriesLeft = MaxRetries;
            ScheduleReload(delayMs);
        }

        private void ScheduleReload(int delayMs)
        {
            _reloadDelayTimer.Stop();
            _reloadDelayTimer.Interval = delayMs;
            _reloadDelayTimer.Start();
        }

        private void DoReload()
        {
            var core = webView21?.CoreWebView2;
            if (core == null)
                return;

            if (_isMinimized || _sessionLocked)
            {
                _pendingReload = true;
                return;
            }

            _pendingReload = false;
            _lastAutoReload = DateTime.UtcNow;

            // Reload keeps the current page; if nothing has loaded yet (offline at
            // startup), go to Notes instead.
            if (_firstLoadComplete)
                core.Reload();
            else
                core.Navigate(NotesUrl);
        }

        // ---- Minimize / restore ------------------------------------------------

        private void Form_Resize(object sender, EventArgs e)
        {
            bool minimized = WindowState == FormWindowState.Minimized;
            if (minimized == _isMinimized)
                return;

            _isMinimized = minimized;
            if (minimized)
                GoIdle();
            else
                WakeUp();
        }

        // Minimized: suspend the web view entirely for the deepest power saving.
        private async void GoIdle()
        {
            var core = webView21?.CoreWebView2;
            if (core == null || !_settings.SuspendWhenMinimized)
                return;

            webView21.Visible = false;   // suspend is only allowed while hidden
            try { await core.TrySuspendAsync(); }
            catch { /* not supported or already suspended */ }

            // Restored before the suspend finished: undo it.
            if (!_isMinimized)
                WakeUp();
        }

        private void WakeUp()
        {
            var core = webView21?.CoreWebView2;
            if (core == null)
                return;

            webView21.Visible = true;
            try { core.Resume(); }
            catch { /* wasn't suspended */ }

            bool overdue = _settings.ReloadMode == ReloadMode.Timed &&
                DateTime.UtcNow - _lastNavigationStart >= TimeSpan.FromMinutes(_settings.ReloadIntervalMinutes);

            if (_pendingReload || overdue)
            {
                _pendingReload = false;
                if (_settings.ReloadOnRestore)
                    RequestAutoReload(RestoreReloadDelayMs);
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _reloadDelayTimer.Stop();
            _intervalTimer.Stop();
            base.OnFormClosed(e);
        }
    }
}
