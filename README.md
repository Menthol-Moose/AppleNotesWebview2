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

Get the latest `AppleNotesPortable.exe` from the [Releases page](../../releases). It's a single self-contained file: put it in any folder and run it.

The app needs the Microsoft Edge WebView2 Runtime, which is already installed on current versions of Windows. It does not need .NET installed.

Builds of every commit to `main` are also available from the [Actions tab](../../actions) as the `AppleNotesPortable-win-x64` artifact (a GitHub login is required).

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

## Releasing a new version

Tag the commit and push the tag. GitHub Actions builds the exe with that version number stamped in and publishes a Release.

```
git tag v1.2.0
git push origin v1.2.0
```

Tags with a suffix, like `v1.3.0-beta1`, are published as pre-releases.

## Credits

App icon: <a href="https://www.flaticon.com/free-icons/essay" title="essay icons">Essay icons created by Magnific - Flaticon</a>
