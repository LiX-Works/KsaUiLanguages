KSA UI Languages v0.6.0 — English installation guide

Kitten Space Agency (KSA) is a space simulation game still in development.
This unofficial pack adds partial Japanese and Simplified Chinese UI translations,
with an English option. It supports Windows x64 and these exact KSA versions:
2026.10.7.5541 / 2026.10.10.5554. Install the game separately first.

Quick start
1. Extract the entire KsaUiLanguages-0.6.0-installer5-win-x64.zip.
2. Close the game and double-click Install.cmd, not Install.ps1.
3. The Chinese folder picker asks for your game folder. Select the folder
   containing KSA.dll and confirm the selection.
4. The next Chinese dialog shows the game and installation folders and asks
   you to accept the bundled licenses and install. Review LICENSE.txt and
   the licenses folder; choose Yes / 是 to accept and continue.
5. Launch the desktop shortcut “KSA 中文版 (5554)” or “KSA 中文版 (5541)”,
   according to your game version. The shortcut name stays in Chinese.
6. Startup is in Simplified Chinese. In the game, choose Language → English
   or Language → 日本語. This choice lasts for the current session only.

The loader and a private runtime are included. Installation works offline
without administrator rights. No KSA game files or Starship mod are included.

Updates and saves
For the same game build, run the new Install.cmd and select the original game
folder. Settings and saves are preserved; configuration and the previous plugin
are backed up. Saves are not automatically backed up.
Each game build has a separate profile. The installer does not update KSA,
copy existing player settings, or automatically migrate older saves.

Profiles normally live in:
%LOCALAPPDATA%\KsaUiLanguages\Build5554 or Build5541
Settings, mods and saves are in instance; installation backups are in backups.
To remove the installation, close the game and run Uninstall.cmd there.
It retains instance and backups. Back up saves before deleting these yourself.

Translation is partial. Long Japanese resource labels and save names can clip.
Full UI coverage, long sessions, another PC, and older-save migration have not
been fully tested. Independent code/translations: MIT; font: SIL OFL;
third-party components retain their own licenses.

Full guide, including the optional startup-language setting:
https://github.com/LiX-Works/KsaUiLanguages/blob/v0.6.0/README.en.md
Downloads:
https://github.com/LiX-Works/KsaUiLanguages/releases/tag/v0.6.0
