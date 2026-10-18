# v0.7.0 preview — remembered language and English installer

KSA UI Languages is a multi-language UI mod for Kitten Space Agency (KSA), a space simulation game still in development. v0.7.0 focuses on player experience: the language you pick is remembered, and the installer now speaks English.

Supports **Windows x64 with KSA 2026.10.7.5541 and 2026.10.10.5554**. The translation covers part of the UI; screens and entries without a translation stay in English. Guides: [English](https://github.com/LiX-Works/KsaUiLanguages/blob/v0.7.0/README.md) · [简体中文](https://github.com/LiX-Works/KsaUiLanguages/blob/v0.7.0/README.zh-CN.md) · [日本語](https://github.com/LiX-Works/KsaUiLanguages/blob/v0.7.0/README.ja.md)

## Install or update

- Download **KsaUiLanguages-0.7.0-installer6-win-x64.zip**, extract all files, close the game, run **Install.cmd**, and select the folder containing **KSA.dll**.
- Launch from the desktop shortcut **KSA UI Languages (5554)** or **KSA UI Languages (5541)**.
- Switch languages with **Language → English / 日本語 / 简体中文**. Your choice is remembered for the next launch.
- Installation works offline without administrator rights; the loader and a private runtime are included. Updating within the same game build keeps settings, saves, and the startup language, and backs up the configuration and previous plugin (saves are not backed up). Moving to 5554 creates a separate Build5554 profile and keeps 5541. [Install, saves, and removal guide (Chinese)](https://github.com/LiX-Works/KsaUiLanguages/blob/v0.7.0/docs/INSTALLER.md)

## What's new

- **Remembered language:** choosing a language from the Language menu writes it to `language.json`, so the next launch starts in that language. The file is replaced through a temporary file; if saving fails, the game continues and the failure is logged.
- **English installer:** installer, launcher, and uninstaller messages are in English. The desktop shortcut is now **KSA UI Languages (build)**; updating from v0.6.0 or earlier removes the old “KSA 中文版” shortcut after the installation is committed.
- **Startup language at install:** a new installation starts in Japanese on Japanese Windows, Simplified Chinese on Chinese Windows, and English otherwise. Updates keep the existing installation's language, which includes the last in-game choice. Scripted installs can pass `-Locale ja-JP`, `zh-CN`, or `en-US`; other values are refused before anything changes.
- **Guides in three languages:** the installation root now also contains the English and Japanese guides.

## Checks and known limits

The same release plugin passed **60 language and UI checks** on each supported build, including a new check that a menu choice is saved as the next startup language. The installer passed **25 isolated install, update, and uninstall checks** with 5541 as the primary build and **23** with 5554 (the extra two cover migration from older installers). New installer checks cover `-Locale`, keeping the in-game choice across a reinstall, refusing an unsupported locale, and installing all three guides.

Not yet verified on a real desktop: the renamed shortcut replacing an existing “KSA 中文版” shortcut, and the remembered language across an actual game restart. The automated installer checks use isolated folders and do not create desktop shortcuts or start the game.

Known limits:

- Long resource labels and long save names can clip.
- Version history, some short HUD labels, and debug and deeper screens remain in English.
- Japanese uses the shared Noto Sans SC font, so some kanji show Chinese glyph shapes. A dedicated Japanese font needs the font atlas to be rebuilt when switching languages and is postponed.
- Full operation, long sessions, other PCs, old-save migration, and other game builds are not fully verified.

## Files

- **KsaUiLanguages-0.7.0-installer6-win-x64.zip** — recommended full offline installer.
- **KsaUiLanguages-0.7.0-build5541-5554.zip** — plugin only, for players who already set up StarMap 0.4.7.
- **KsaUiLanguages-0.7.0-source.zip** — source and translations for this release.

The dictionaries are unchanged from v0.6.0; the review dictionary remains available as [KsaUiLanguages-0.6.0-dictionary.zip](https://github.com/LiX-Works/KsaUiLanguages/releases/tag/v0.6.0).
- **SHA256SUMS.txt / INSTALLER-SHA256.txt** — checksums.

This is an unofficial community mod and includes neither the KSA game nor the Starship mod. Original code and translations use MIT; the font uses SIL OFL; third-party components keep their own licenses.
