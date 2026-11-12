# AppleNotesPortable

A small Windows app that shows iCloud Notes (icloud.com/notes) in its own window, using Microsoft Edge WebView2. It works like a dedicated Notes app rather than a browser tab.

This is an unofficial personal project and is not affiliated with or endorsed by Apple.

## What it does

- Opens straight to iCloud Notes in a fixed-size window.
- Hides iCloud's top toolbar by shifting the page up out of view, so the window shows only Notes and not links to Mail, Calendar, and other iCloud apps.
- Stays signed in across restarts. Browser data is kept in `%LocalAppData%\AppleNotesPortable\WebView2`, so moving or rebuilding the app doesn't sign you out.
- Saves power when idle: it suspends when minimized and lets the browser engine throttle when the window loses focus.
- Reloads the page when the window regains focus, so changes made on other devices show up.

## Download

Each push to `main` builds a self-contained, single-file `AppleNotesPortable.exe`. Open the [Actions tab](../../actions), pick the latest successful run, and download the `AppleNotesPortable-win-x64` artifact (a GitHub login is required).

The app needs the Microsoft Edge WebView2 Runtime, which is already installed on current versions of Windows. It does not need .NET installed.

## Settings

`settings.ini` sits next to the `.exe`. If it's missing, the app creates one with default values on first launch. Restart the app after editing it.

| Setting | Default | Meaning |
|---|---|---|
| `Width`, `Height` | 450, 700 | Window size in pixels |
| `ClipHeight` | 52 | Pixels hidden at the top of the page. Raise it if part of iCloud's toolbar still shows; lower it if Notes content is cut off. |
| `ReloadOnFocus` | true | Reload when the window regains focus, to pick up changes from other devices. Set to false to keep your place and refresh manually with Ctrl+R. |
| `IdleThresholdSeconds` | 2 | How long the window must be unfocused before refocusing triggers a reload |
| `SuspendWhenMinimized` | true | Fully suspend the web view while minimized |

## Building

Requires the .NET 9 SDK on Windows.

```
dotnet run                  # build and run for testing
dotnet publish -c Release   # portable single-file exe in bin\Release\net9.0-windows\win-x64\publish\
```

## Credits

App icon: <a href="https://www.flaticon.com/free-icons/essay" title="essay icons">Essay icons created by Magnific - Flaticon</a>
