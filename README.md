# AppleNotesPortable

A small Windows app that shows iCloud Notes (icloud.com/notes) in its own window, using Microsoft Edge WebView2. It works like a dedicated Notes app rather than a browser tab.

This is an unofficial personal project and is not affiliated with or endorsed by Apple.

## What it does

- Opens straight to iCloud Notes in a fixed-size window.
- Hides iCloud's top toolbar by shifting the page up out of view, so the window shows only Notes and not links to Mail, Calendar, and other iCloud apps.
- Renders sharply on high-DPI displays and when moved between monitors with different scaling.
- Stays signed in across restarts. Browser data is kept in `%LocalAppData%\AppleNotesPortable\WebView2`, so moving or rebuilding the app doesn't sign you out.
- Saves power: it suspends while minimized.
- Reloads to pick up changes made on other devices, on a schedule you choose (see `ReloadMode` below).

## Download

Get the latest `AppleNotesPortable.exe` from the [Releases page](../../releases). It's a single self-contained file: put it in any folder and run it.

The app needs the Microsoft Edge WebView2 Runtime, which is already installed on current versions of Windows. It does not need .NET installed.

Builds of every commit to `main` are also available from the [Actions tab](../../actions) as the `AppleNotesPortable-win-x64` artifact (a GitHub login is required).

## Settings

`settings.ini` sits next to the `.exe`. If it's missing, the app creates one with default values on first launch. Restart the app after editing it.

| Setting | Default | Meaning |
|---|---|---|
| `Width`, `Height` | 450, 700 | Window size at 100% display scaling; scaled automatically for your display |
| `ClipHeight` | 52 | Height of iCloud's toolbar to hide, at 100% scaling. Raise it if part of the toolbar still shows; lower it if Notes content is cut off. |
| `ReloadMode` | Automatic | `Manual`: reload only with Ctrl+R or F5. `Automatic`: also reload after the PC wakes from sleep or the screen turns back on, and when the screen is unlocked. `Timed`: everything in Automatic, plus a reload every `ReloadIntervalMinutes`. |
| `ReloadIntervalMinutes` | 15 | Used only with `ReloadMode=Timed`. A timed reload waits until you've stopped typing or clicking in the app for a minute. |
| `SuspendWhenMinimized` | true | Fully suspend the web view while minimized. Reloads that come due while minimized happen when you restore the window. |

## Building

Requires the .NET 9 SDK on Windows.

```
dotnet run                  # build and run for testing
dotnet publish -c Release   # portable single-file exe in bin\Release\net9.0-windows\win-x64\publish\
```

## Releasing a new version

Tag the commit and push the tag. GitHub Actions builds the exe with that version number stamped in and publishes a Release.

```
git tag v1.2.0
git push origin v1.2.0
```

Tags with a suffix, like `v1.3.0-beta1`, are published as pre-releases.

## Credits

App icon: <a href="https://www.flaticon.com/free-icons/essay" title="essay icons">Essay icons created by Magnific - Flaticon</a>
