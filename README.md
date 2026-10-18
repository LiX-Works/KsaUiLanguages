# KSA UI Languages

[![Release](https://img.shields.io/github/v/release/LiX-Works/KsaUiLanguages?include_prereleases&label=release)](https://github.com/LiX-Works/KsaUiLanguages/releases) ![KSA builds 5541 and 5554](https://img.shields.io/badge/KSA-5541%20%7C%205554-blue) ![Windows x64](https://img.shields.io/badge/platform-Windows%20x64-lightgrey) [![License: MIT](https://img.shields.io/badge/license-MIT-green)](LICENSE)

**English** · [简体中文](README.zh-CN.md) · [日本語](README.ja.md)

**A multi-language UI mod for Kitten Space Agency — play in Simplified Chinese or Japanese, and switch languages any time in game.**

Kitten Space Agency is a hardcore spaceflight simulator grounded in real physics: you design your own rockets and fly them through true orbital mechanics — easy to start, with almost no ceiling for players who love a challenge.

![KSA flight view with the Simplified Chinese UI and the Language menu open, listing English, 日本語 and 简体中文](docs/images/language-menu-zh-CN.webp)

*In flight with the Simplified Chinese UI. The Language menu switches among English, 日本語 and 简体中文.*

## Highlights

- **Switch languages in one click:** choose English, 日本語 or 简体中文 from the in-game menu; your choice is remembered.
- **Easy, offline install:** one installer, no administrator rights, nothing else to download.
- **Your saves stay safe:** updates keep your settings and saves, with automatic backups of the configuration.
- **Careful translation:** 1,625 entries per language, reviewed for meaning; familiar terms such as RCS and Δv stay as players know them.
- **Tested before every release** on each supported game version.

## Download and install

**Latest: v0.9.0 preview** for Windows x64 and KSA 2026.10.7.5541 / 2026.10.10.5554. [Download v0.9.0](https://github.com/LiX-Works/KsaUiLanguages/releases/tag/v0.9.0) and choose **KsaUiLanguages-0.9.0-installer6-win-x64.zip**.

1. Install one of the exact supported KSA versions above and check that the original game runs.
2. Extract the entire ZIP, close the game, and run **Install.cmd**. Select the game folder containing **KSA.dll**.
3. Launch **“KSA UI Languages (5554)”** or **“KSA UI Languages (5541)”** from the desktop. The shortcut name depends on the game build you selected.
4. In the game, choose **Language → English / 日本語 / 简体中文**. Your choice is remembered for the next launch.

At the license confirmation, choose **Yes** to accept the bundled licenses and continue. Use Install.cmd; double-clicking Install.ps1 may only open a text editor.

**Updating:** run the new installer and select the original game folder. Settings, saves, and the startup language are kept; the configuration and previous plugin are backed up, but saves are not. Each game build (5541 or 5554) gets its own profile, and the game itself is never modified. [Installation, update, and removal guide](docs/INSTALLER.en.md)

What changed in each version: [CHANGELOG](CHANGELOG.md).

## Languages

- **Three languages:** Simplified Chinese and Japanese, plus the original English. The translation covers settings, the vehicle editor, common flight windows and HUD, planning, tracking, and save windows; anything not yet translated stays in English.
- **Familiar labels stay recognizable:** abbreviations such as RCS, EVA, Isp, TWR, HUD, and Δv, along with units, key bindings, part model numbers, and names you give your vehicles, are kept as they are.

## Known limits

- Some screens and labels are still in English.
- Long labels and long save names can be cut off.
- Japanese currently uses a shared Chinese font, so some kanji show Chinese-style shapes.
- Not every in-game action or long play session has been tested. [Release notes with details](docs/RELEASE_NOTES_v0.9.0.md)

## How it works

The mod changes the text on screen while the game runs instead of editing game files, so your game folder stays untouched. For build and test details, see [Development and builds](docs/DEVELOPMENT.md).

## Feedback, contributing, and licenses

Found a wrong or awkward translation? [Open an issue](https://github.com/LiX-Works/KsaUiLanguages/issues). To help translate, see [Contribute translations](CONTRIBUTING.md); reference material: [review dictionary (Chinese)](docs/DICTIONARY.md) · [part-name review (Chinese)](docs/PART_NAMING_REVIEW.md) · [versioning (Chinese)](docs/VERSIONING.md).

This is an unofficial community mod. **It includes neither the KSA game nor the Starship vehicle mod.** Independently authored code and translations use **MIT**; the font uses **SIL OFL**. The loader and runtime retain their accompanying licenses. [Licenses and attribution](THIRD_PARTY.md)
