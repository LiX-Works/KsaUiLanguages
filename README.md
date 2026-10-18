# KSA UI Languages

**English** · [简体中文](README.zh-CN.md) · [日本語](README.ja.md)

**Kitten Space Agency (KSA) is a space simulation game still in development.** This language pack adds Japanese and Simplified Chinese to settings, the vehicle editor, and common flight windows. You can also switch back to English.

**Under the hood:** a runtime localization framework rather than a static text patch. Languages switch instantly in game; each language has a 1,625-entry dictionary with 748 semantic review notes; every supported game build passes 60 automated language and UI checks, and a compatibility probe pre-checks new game builds.

![KSA flight view with the Simplified Chinese UI and the Language menu open, listing English, 日本語 and 简体中文](docs/images/language-menu-zh-CN.webp)

*In flight with the Simplified Chinese UI. The Language menu switches among English, 日本語 and 简体中文.*

**Current release: v0.7.0 preview. Supports Windows x64 and KSA 2026.10.7.5541 / 2026.10.10.5554.** This is a partial UI translation. Screens outside the translation framework and entries without a translation remain in English.

## Download and install

[Download v0.7.0](https://github.com/LiX-Works/KsaUiLanguages/releases/tag/v0.7.0) and choose **KsaUiLanguages-0.7.0-installer6-win-x64.zip**.

1. Install one of the exact supported KSA versions above and check that the original game runs.
2. Extract the entire ZIP, close the game, and run **Install.cmd**. Select the game folder containing **KSA.dll**.
3. Launch **“KSA UI Languages (5554)”** or **“KSA UI Languages (5541)”** from the desktop. The shortcut name depends on the game build you selected.
4. In the game, choose **Language → English / 日本語 / 简体中文**. Your choice is remembered for the next launch.

At the license confirmation, choose **Yes** to accept the bundled licenses and continue. The loader and a private runtime are included, so installation works offline without administrator rights. Use Install.cmd; double-clicking Install.ps1 may only open a text editor.

**Updating the pack for the same game build:** select the original game folder. Settings, saves, and the startup language are preserved; configuration and the previous plugin are backed up. The installer does not automatically back up saves. Updating from v0.6.0 or earlier replaces the old “KSA 中文版” desktop shortcut with the new name.

**Moving from 5541 to 5554:** the installer creates a separate Build5554 profile and preserves Build5541. Keep both game folders. It does not update or back up the game itself, or automatically migrate older saves. [Installation, save locations, and removal guide (Chinese)](docs/INSTALLER.md)

## What's new in v0.7.0

- **Your language choice is remembered:** picking a language from the Language menu also makes it the startup language.
- **English installer:** installer, launcher, and uninstaller messages are in English, and the desktop shortcut is named “KSA UI Languages (build)”.
- **Startup language at install:** a new installation starts in Japanese on Japanese Windows, Simplified Chinese on Chinese Windows, and English otherwise; updates keep the existing language. Scripted installs can pass `-Locale ja-JP`, `zh-CN`, or `en-US`.

## Languages

- **Three languages:** Japanese joins Simplified Chinese and the original English. Each Japanese and Chinese dictionary contains **1,625 entries**, covering the existing framework's settings, editor, common HUD, planning, tracking, save windows, and explanations.
- **Japanese welcome text:** improved wrapping for kana and Japanese punctuation, with the required font characters added.
- **Familiar labels stay recognizable:** common abbreviations such as RCS, EVA, Isp, TWR, HUD, and Δv, along with units, key bindings, part model numbers, and player-defined names, are preserved.

The startup language is stored as `locale` (`ja-JP`, `zh-CN`, or `en-US`) in the installed profile's `instance/mods/KsaUiLanguages/language.json`; the Language menu updates it for you.

## Checks and known limits

The same release plugin passed **60 automated language and UI checks** on each supported build, and the installer passed **25 (5541) and 23 (5554) isolated install, update, and uninstall checks**. Some content remains in English, long labels or save names may clip, and Japanese text uses the shared Noto Sans SC font, so some kanji show Chinese glyph shapes. Not all operations have been tested, and long-session testing is incomplete. [Detailed checks and known issues](docs/RELEASE_NOTES_v0.7.0.md)

This is an unofficial community language pack. **It includes neither the KSA game nor the Starship vehicle mod.** Future public versions advance to v0.8.0, v0.9.0, and so on. [Versioning policy (Chinese)](docs/VERSIONING.md)

## Contributing and licenses

[Review dictionary (Chinese)](docs/DICTIONARY.md) · [Contribute translations](CONTRIBUTING.md) · [Development and builds](docs/DEVELOPMENT.md) · [Part-name review (Chinese)](docs/PART_NAMING_REVIEW.md)

Independently authored code and translations use **MIT**; the font uses **SIL OFL**. The loader and runtime retain their accompanying licenses. [Licenses and attribution](THIRD_PARTY.md)
