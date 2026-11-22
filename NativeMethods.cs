using System;
using System.Runtime.InteropServices;

namespace AppleNotesPortable
{
    // Windows API calls used to detect wake, screen-on, unlock, and user idle time.
    internal static class NativeMethods
    {
        internal const int WM_POWERBROADCAST = 0x0218;
        internal const int PBT_APMRESUMEAUTOMATIC = 0x0012;   // resume from classic sleep / hibernate
        internal const int PBT_POWERSETTINGCHANGE = 0x8013;   // a registered power setting changed

        internal const int WM_WTSSESSION_CHANGE = 0x02B1;
        internal const int WTS_SESSION_LOCK = 0x7;
        internal const int WTS_SESSION_UNLOCK = 0x8;

        internal const int DEVICE_NOTIFY_WINDOW_HANDLE = 0;
        internal const int NOTIFY_FOR_THIS_SESSION = 0;

        // Display on/off/dimmed. On Modern Standby PCs (most current laptops,
        // including Surface) the classic resume event doesn't fire, so "the
        // display came back on" is the reliable wake signal. Values: 0 off,
        // 1 on, 2 dimmed. We register for both the console and session
        // variants; duplicates are merged by the caller.
        internal static readonly Guid GUID_CONSOLE_DISPLAY_STATE =
            new Guid("6FE69556-704A-47A0-8F24-C28D936FDA47");
        internal static readonly Guid GUID_SESSION_DISPLAY_STATUS =
            new Guid("2B84C20E-AD23-4DDF-93DB-05FFBD7EFCA5");

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern IntPtr RegisterPowerSettingNotification(
            IntPtr hRecipient, ref Guid powerSettingGuid, int flags);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool UnregisterPowerSettingNotification(IntPtr handle);

        [DllImport("wtsapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool WTSRegisterSessionNotification(IntPtr hWnd, int dwFlags);

        [DllImport("wtsapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool WTSUnRegisterSessionNotification(IntPtr hWnd);

        [StructLayout(LayoutKind.Sequential)]
        internal struct LASTINPUTINFO
        {
            public uint cbSize;
            public uint dwTime;
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

        // Time since the last keyboard or mouse input anywhere in the session.
        internal static TimeSpan GetIdleTime()
        {
            var info = new LASTINPUTINFO { cbSize = (uint)Marshal.SizeOf<LASTINPUTINFO>() };
            if (!GetLastInputInfo(ref info))
                return TimeSpan.MaxValue;
            uint idleMs = unchecked((uint)Environment.TickCount - info.dwTime);
            return TimeSpan.FromMilliseconds(idleMs);
        }
    }
}
