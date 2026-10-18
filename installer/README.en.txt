KSA UI Languages v0.7.0 — English installation guide

Kitten Space Agency (KSA) is a space simulation game still in development.
This unofficial pack adds partial Japanese and Simplified Chinese UI translations,
with an English option. It supports Windows x64 and these exact KSA versions:
2026.10.7.5541 / 2026.10.10.5554. Install the game separately first.

Quick start
1. Extract the entire KsaUiLanguages-0.7.0-installer6-win-x64.zip.
2. Close the game and double-click Install.cmd, not Install.ps1.
3. The folder picker asks for your game folder. Select the folder
   containing KSA.dll and confirm the selection.
4. The next dialog shows the game folder, the installation folder and the
   startup language, and asks you to accept the bundled licenses. Review
   LICENSE.txt and the licenses folder; choose Yes to accept and continue.
5. Launch the desktop shortcut "KSA UI Languages (5554)" or
   "KSA UI Languages (5541)", according to your game version.
6. In the game, choose Language → English / 日本語 / 简体中文.
   Your choice is remembered for the next launch.

A new installation starts in Japanese on Japanese Windows, in Simplified
Chinese on Chinese Windows, and in English otherwise. An update keeps the
language of the existing installation.

The loader and a private runtime are included. Installation works offline
without administrator rights. No KSA game files or Starship mod are included.

Updates and saves
For the same game build, run the new Install.cmd and select the original game
folder. Settings, saves and the startup language are preserved; configuration
and the previous plugin are backed up. Saves are not automatically backed up.
Updating from v0.6.0 or earlier replaces the old "KSA 中文版" desktop shortcut.
Each game build has a separate profile. The installer does not update KSA,
copy existing player settings, or automatically migrate older saves.

Profiles normally live in:
%LOCALAPPDATA%\KsaUiLanguages\Build5554 or Build5541
Settings, mods and saves are in instance; installation backups are in backups.
To remove the installation, close the game and run Uninstall.cmd there.
It retains instance and backups. Back up saves before deleting these yourself.

Translation is partial. Long Japanese resource labels and save names can clip,
and Japanese uses the shared Noto Sans SC font, so some kanji show Chinese
glyph shapes. Full UI coverage, long sessions, another PC, and older-save
migration have not been fully tested. Independent code/translations: MIT;
font: SIL OFL; third-party components retain their own licenses.

Full guide:
https://github.com/LiX-Works/KsaUiLanguages/blob/v0.7.0/README.md
Downloads:
https://github.com/LiX-Works/KsaUiLanguages/releases/tag/v0.7.0
