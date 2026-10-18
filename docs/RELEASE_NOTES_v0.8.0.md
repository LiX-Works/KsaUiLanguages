# v0.8.0 preview — English installation guide

KSA UI Languages is a multi-language UI mod for Kitten Space Agency (KSA), a space simulation game still in development. v0.8.0 adds a full English installation guide; the plugin's features are the same as v0.7.0.

Supports **Windows x64 with KSA 2026.10.7.5541 and 2026.10.10.5554**. The translation covers part of the UI; screens and entries without a translation stay in English. Guides: [English](https://github.com/LiX-Works/KsaUiLanguages/blob/v0.8.0/README.md) · [简体中文](https://github.com/LiX-Works/KsaUiLanguages/blob/v0.8.0/README.zh-CN.md) · [日本語](https://github.com/LiX-Works/KsaUiLanguages/blob/v0.8.0/README.ja.md)

## Install or update

- Download **KsaUiLanguages-0.8.0-installer6-win-x64.zip**, extract all files, close the game, run **Install.cmd**, and select the folder containing **KSA.dll**.
- Launch from the desktop shortcut **KSA UI Languages (5554)** or **KSA UI Languages (5541)**.
- Switch languages with **Language → English / 日本語 / 简体中文**. Your choice is remembered for the next launch.
- Updating within the same game build keeps settings, saves, and the startup language. [Installation, update, and removal guide](https://github.com/LiX-Works/KsaUiLanguages/blob/v0.8.0/docs/INSTALLER.en.md)

## What's new

- **English installation guide:** [docs/INSTALLER.en.md](https://github.com/LiX-Works/KsaUiLanguages/blob/v0.8.0/docs/INSTALLER.en.md) covers installation, updates, moving between game builds, configuration, removal, and what was checked. The English and Japanese READMEs now link to it, and the Chinese and English guides link to each other.
- **Repository topics:** added `japanese`, `csharp`, and `game-localization`.

The v0.7.0 changes remain: the Language menu choice is remembered, the installer is in English, the desktop shortcut is named “KSA UI Languages (build)”, and a new installation picks its startup language from the Windows display language. See the [v0.7.0 notes](https://github.com/LiX-Works/KsaUiLanguages/blob/v0.8.0/docs/RELEASE_NOTES_v0.7.0.md).

## Checks and known limits

The rebuilt release plugin passed **60 language and UI checks** on each supported build. The installer passed **25 isolated install, update, and uninstall checks** with 5541 as the primary build and **23** with 5554 (the extra two cover migration from older installers). The automated installer checks use isolated folders and do not create desktop shortcuts or start the game.

Known limits:

- Long resource labels and long save names can clip.
- Version history, some short HUD labels, and debug and deeper screens remain in English.
- Japanese uses the shared Noto Sans SC font, so some kanji show Chinese glyph shapes.
- Some reference documents (dictionary review notes, part-name review, versioning) are still Chinese only.
- Full operation, long sessions, other PCs, old-save migration, and other game builds are not fully verified.

## Files

- **KsaUiLanguages-0.8.0-installer6-win-x64.zip** — recommended full offline installer.
- **KsaUiLanguages-0.8.0-build5541-5554.zip** — plugin only, for players who already set up StarMap 0.4.7.
- **KsaUiLanguages-0.8.0-source.zip** — source and translations for this release.
- **SHA256SUMS.txt / INSTALLER-SHA256.txt** — checksums.

The dictionaries are unchanged from v0.6.0; the review dictionary remains available as [KsaUiLanguages-0.6.0-dictionary.zip](https://github.com/LiX-Works/KsaUiLanguages/releases/tag/v0.6.0).

This is an unofficial community mod and includes neither the KSA game nor the Starship mod. Original code and translations use MIT; the font uses SIL OFL; third-party components keep their own licenses.
