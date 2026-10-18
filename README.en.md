# KSA UI Languages

[简体中文](README.md) · [日本語](README.ja.md) · **English**

**Kitten Space Agency (KSA) is a space simulation game still in development.** This language pack adds Japanese and Simplified Chinese to settings, the vehicle editor, and common flight windows. You can also switch back to English.

**Current release: v0.6.0 preview. Supports Windows x64 and KSA 2026.10.7.5541 / 2026.10.10.5554.** This is a partial UI translation. Screens outside the translation framework and entries without a translation remain in English.

## Download and install

[Download v0.6.0](https://github.com/LiX-Works/KsaUiLanguages/releases/tag/v0.6.0) and choose **KsaUiLanguages-0.6.0-installer5-win-x64.zip**.

1. Install one of the exact supported KSA versions above and check that the original game runs.
2. Extract the entire ZIP, close the game, and run **Install.cmd**. Select the game folder containing **KSA.dll**.
3. Launch **“KSA 中文版 (5554)”** or **“KSA 中文版 (5541)”** from the desktop. The shortcut name depends on the game build you selected.
4. In the game, choose **Language → 日本語 / 简体中文 / English**.

The installer prompts and desktop shortcut names are currently in Chinese. At the license confirmation, choose **Yes / 是** to accept the bundled licenses and continue. The loader and a private runtime are included, so installation works offline without administrator rights. Use Install.cmd; double-clicking Install.ps1 may only open a text editor.

**Updating the pack for the same game build:** select the original game folder. Settings and saves are preserved; configuration and the previous plugin are backed up. The installer does not automatically back up saves.

**Moving from 5541 to 5554:** the installer creates a separate Build5554 profile and preserves Build5541. Keep both game folders. It does not update or back up the game itself, or automatically migrate older saves. [Installation, save locations, and removal guide (Chinese)](docs/INSTALLER.md)

## Languages and this update

- **Three languages:** Japanese joins Simplified Chinese and the original English. Each Japanese and Chinese dictionary contains **1,625 entries**, covering the existing framework's settings, editor, common HUD, planning, tracking, save windows, and explanations.
- **Japanese welcome text:** improved wrapping for kana and Japanese punctuation, with the required font characters added.
- **Familiar labels stay recognizable:** common abbreviations such as RCS, EVA, Isp, TWR, HUD, and Δv, along with units, key bindings, part model numbers, and player-defined names, are preserved.

Simplified Chinese is the default. **Language menu choices last only for the current session.** To change the startup language, edit `locale` in the installed profile's `instance/mods/KsaUiLanguages/language.json` to `ja-JP`, `zh-CN`, or `en-US`.

## Checks and known limits

Both supported builds passed **59 automated checks each**, and selected Japanese startup, loading, and welcome screens were checked on 5554. Some content remains in English, and long labels or save names may clip. Not all operations have been tested, and long-session testing is incomplete. [Detailed checks and known issues](docs/RELEASE_NOTES_v0.6.0.md)

This is an unofficial community language pack. **It includes neither the KSA game nor the Starship vehicle mod.** Future public versions advance to v0.7.0, v0.8.0, and so on. [Versioning policy (Chinese)](docs/VERSIONING.md)

## Contributing and licenses

[Review dictionary (Chinese)](docs/DICTIONARY.md) · [Contribute translations](CONTRIBUTING.md) · [Development and builds](docs/DEVELOPMENT.md) · [Part-name review (Chinese)](docs/PART_NAMING_REVIEW.md)

Independently authored code and translations use **MIT**; the font uses **SIL OFL**. The loader and runtime retain their accompanying licenses. [Licenses and attribution](THIRD_PARTY.md)
