# Licenses and attribution

## Independently authored project

The plugin in src/KsaUiLanguages, checks in src/LanguageChecks, original translations, documentation and project tools are licensed under [MIT](LICENSE).

The MIT license does not relicense the font, game or external dependencies.

## Bundled font

- Upstream: [Google Fonts / Noto Sans SC](https://github.com/google/fonts/tree/main/ofl/notosanssc).
- Source file: NotoSansSC[wght].ttf.
- Copyright notice: Copyright 2014–2021 Adobe, with Reserved Font Name “Source”.
- License: [SIL Open Font License 1.1](assets/KsaUiLanguages/FONT-LICENSE.txt).
- Modification: weight 400 instantiation, subset for the language packs and supporting UI symbols, renamed family/style/PostScript names.
- Generated file: assets/KsaUiLanguages/Fonts/KsaUiNotoSC.ttf.
- Generator: tools/build_ui_font.py.
- SHA-256 of the upstream file used for this release: a3041811a78c361b1de50f953c805e0244951c21c5bd412f7232ef0d899af0da.

The original font file is not bundled. Its copyright and license metadata are retained in the generated font, and the full license accompanies the release.

## External dependencies, not bundled

- [Kitten Space Agency](https://ahwoo.com/app/100000/kitten-space-agency), by RocketWerkz: game binaries and assets remain subject to their own terms. This repository does not claim that the game is open source.
- [StarMap 0.4.7](https://github.com/StarMapLoader/StarMap/tree/0.4.7): mod loader and API, MIT. Install it separately and retain its license.
- [Harmony](https://github.com/pardeike/Harmony): patching dependency supplied by StarMap, MIT. The project references the loader's 0Harmony.dll and does not redistribute it.
- [.NET](https://dotnet.microsoft.com/): a .NET 10 SDK is required for development.
- [fontTools](https://github.com/fonttools/fonttools): optional font-generation tool, MIT; not bundled with the project.

StarMap's public loader code and [KSA-ZhHans](https://github.com/GaiusCassiusLonginus/KSA-ZhHans) were consulted during initial investigation. This plugin and its translations were independently authored; neither project's source is included.

This is an unofficial community project. Product names identify compatibility and do not imply endorsement.
