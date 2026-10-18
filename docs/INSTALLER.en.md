# Offline installation and updates

**English** · [简体中文](INSTALLER.md)

[Download v0.8.0](https://github.com/LiX-Works/KsaUiLanguages/releases/tag/v0.8.0) and choose **KsaUiLanguages-0.8.0-installer6-win-x64.zip**. The package contains the language plugin, StarMap 0.4.7, and a private .NET 10.0.12 runtime. It does not contain the game.

## Install

1. Install the Windows x64 game, **KSA 2026.10.7.5541 or 2026.10.10.5554**, and check that it runs.
2. Extract the whole ZIP, close the game, double-click **Install.cmd**, and select the game folder containing KSA.dll.
3. Start the game from the desktop shortcut **KSA UI Languages (5554)** or **KSA UI Languages (5541)**.

No administrator rights, SDK, or system-wide .NET are needed, and installation works offline. Double-clicking a .ps1 file may only open it in Notepad, so use the .cmd entry and do not paste the script into a command prompt.

Installer prompts are in English, and the package includes Chinese, Japanese, and English guides. A new installation picks the startup language from the Windows display language (Simplified Chinese on Chinese Windows, Japanese on Japanese Windows, English otherwise); an update keeps the existing language. Scripted installs can pass `-Locale zh-CN|ja-JP|en-US`. In the game, switch with **Language → English / 日本語 / 简体中文**; the choice is remembered for the next launch.

## Updating and moving between game builds

To update the language pack for the same game build, run the new Install.cmd and select the original game folder. The separate settings, saves, and startup language are kept, and the configuration and previous plugin are backed up. The installer does not back up saves automatically. When updating from v0.6.0 or earlier, the old “KSA 中文版” desktop shortcut is replaced with the new name after the installation completes.

The installer chooses a separate root folder for each game build:

| Game build | Default folder | Desktop shortcut |
| --- | --- | --- |
| 2026.10.7.5541 | %LOCALAPPDATA%\KsaUiLanguages\Build5541 | KSA UI Languages (5541) |
| 2026.10.10.5554 | %LOCALAPPDATA%\KsaUiLanguages\Build5554 | KSA UI Languages (5554) |

Both builds can keep their own shortcut, settings, and saves. An existing 5541 instance is never reused by a 5554 installation, and a custom folder cannot be overwritten across builds. 5541 instances from installers 1–4 can be upgraded for the same build.

The language installer does not install or update KSA itself. To keep an older game build, keep its game folder and prepare a separate folder for the new build. Instances do not import your original player settings or saves; migrating older saves has not been verified, so back them up first.

## Configuration and removal

In the installation root, **instance** holds the separate settings, mods, and saves, and **backups** holds reinstall backups. The startup language is stored in instance/mods/KsaUiLanguages/language.json and is updated when you switch with the Language menu.

To remove an installation, open its root folder and run **Uninstall.cmd**. It removes the shortcut and deployment files that belong to that installation and keeps instance and backups; back up your saves before deleting those yourself.

To move to another PC, copy the original ZIP and install again. Deployed shortcuts and configuration contain paths specific to this PC.

## What was checked

The installation flow is checked in Windows PowerShell 5.1 with a Chinese and space-containing path: reinstall, backups, refusing the other game build and wrong versions, reparse points, shortcut ownership, and keeping saves on uninstall, plus `-Locale`, keeping the in-game language across a reinstall, refusing unsupported languages, and installing the three guides. 5541 passes 25 checks and 5554 passes 23; the two extra 5541 checks cover migration from older installers. The checks use temporary instances inside the project, never touch the real desktop, and do not start the game.

The same release plugin passes 60 language and UI checks on both builds, including saving the menu language choice. This does not mean the installer GUI, another PC, or every game operation has been verified.

## Packaging (developers)

After building the plugin and recording the real plugin ZIP hash in installer/dependencies.json, run:

~~~powershell
.\tools\make_installer.ps1
.\tools\check_installer.ps1 -GameDir 'C:\Games\KSA-5554' -OtherGameDir 'C:\Games\KSA-5541'
~~~

The output is dist/KsaUiLanguages-0.8.0-installer6-win-x64.zip. The script reuses the verified local plugin and dependency cache; the loader and runtime come from pinned official URLs and are checked with SHA-256, and the runtime also with SHA-512. Packaged .ps1 files are UTF-8 with BOM and .cmd files use Windows line endings, for compatibility with the built-in Windows PowerShell 5.1.
