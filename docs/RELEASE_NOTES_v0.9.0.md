# v0.9.0 preview — clearer homepage and changelog

KSA UI Languages is a multi-language UI mod for Kitten Space Agency (KSA), a hardcore spaceflight simulator grounded in real physics. v0.9.0 reworks the project homepage and adds a changelog; the plugin's features are the same as v0.7.0 and v0.8.0.

Supports **Windows x64 with KSA 2026.10.7.5541 and 2026.10.10.5554**. The translation covers part of the UI; screens and entries without a translation stay in English. Guides: [English](https://github.com/LiX-Works/KsaUiLanguages/blob/v0.9.0/README.md) · [简体中文](https://github.com/LiX-Works/KsaUiLanguages/blob/v0.9.0/README.zh-CN.md) · [日本語](https://github.com/LiX-Works/KsaUiLanguages/blob/v0.9.0/README.ja.md)

## Install or update

- Download **KsaUiLanguages-0.9.0-installer6-win-x64.zip**, extract all files, close the game, run **Install.cmd**, and select the folder containing **KSA.dll**.
- Launch from the desktop shortcut **KSA UI Languages (5554)** or **KSA UI Languages (5541)**.
- Switch languages with **Language → English / 日本語 / 简体中文**. Your choice is remembered for the next launch.
- Updating within the same game build keeps settings, saves, and the startup language. [Installation, update, and removal guide](https://github.com/LiX-Works/KsaUiLanguages/blob/v0.9.0/docs/INSTALLER.en.md)

## What's new

- **Clearer homepage in three languages:** each README now opens with what the mod does and a short description of the game, shows the screenshot first, and lists plain-language highlights, known limits, and how to report a translation problem.
- **Changelog:** version history moved to [CHANGELOG.md](https://github.com/LiX-Works/KsaUiLanguages/blob/v0.9.0/CHANGELOG.md).
- **Bundled guides:** the installation guides inside the packages now point to v0.9.0.

## Checks and known limits

The rebuilt release plugin passed **60 language and UI checks** on each supported build. The installer passed **25 isolated install, update, and uninstall checks** with 5541 as the primary build and **23** with 5554.

Known limits:

- Some screens and labels are still in English.
- Long labels and long save names can be cut off.
- Japanese uses a shared Chinese font, so some kanji show Chinese-style shapes.
- Not every in-game action or long play session has been tested.

## Files

- **KsaUiLanguages-0.9.0-installer6-win-x64.zip** — recommended full offline installer.
- **KsaUiLanguages-0.9.0-build5541-5554.zip** — plugin only, for players who already set up StarMap 0.4.7.
- **KsaUiLanguages-0.9.0-source.zip** — source and translations for this release.
- **SHA256SUMS.txt / INSTALLER-SHA256.txt** — checksums.

The dictionaries are unchanged from v0.6.0; the review dictionary remains available as [KsaUiLanguages-0.6.0-dictionary.zip](https://github.com/LiX-Works/KsaUiLanguages/releases/tag/v0.6.0).

This is an unofficial community mod and includes neither the KSA game nor the Starship mod. Original code and translations use MIT; the font uses SIL OFL; third-party components keep their own licenses.
