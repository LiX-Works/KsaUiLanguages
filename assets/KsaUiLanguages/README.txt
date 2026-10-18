KSA UI Languages v0.9.0 — 简体中文 / 日本語 / English

Kitten Space Agency（KSA）是一款仍在开发中的太空模拟游戏。
本包提供部分中文与日文界面，并可切回英文。
支持 Windows x64、KSA 2026.10.7.5541 和 2026.10.10.5554。

推荐安装：完整解压 KsaUiLanguages-0.9.0-installer6-win-x64.zip，
关闭游戏，运行 Install.cmd，选择包含 KSA.dll 的游戏目录。
从桌面“KSA UI Languages (5554)”或“KSA UI Languages (5541)”启动。
安装提示为英文，包内附中、日、英说明。无需管理员权限。

仅插件包：先配置 StarMap 0.4.7，将整个 KsaUiLanguages 文件夹放入
实例目录的 mods，保留原 manifest.toml 条目，并启用：

[[mods]]
id = "KsaUiLanguages"
enabled = true

通过 StarMap.exe 启动；本 ZIP 不含游戏、加载器或运行时。
游戏不同版本使用独立设置和存档，不自动迁移旧存档。

Language → 简体中文 / 日本語 / English 即时切换，选择会写入 language.json，
下次启动沿用。language.json 中 zh-CN 为中文，ja-JP 为日文，en-US 为英文；
仅插件包默认为中文。
中、日词典各含 1,625 条记录；尚未接入的文字保留英文。
保留常用模拟器缩写、单位、快捷键、玩家名称和内部 ID。
两个支持版本各通过 60 项自动检查；未全面验证所有操作和长期稳定性。
部分长文字可能截断，字体暂共用 Noto Sans SC 子集。

日本語
KSA は開発中の宇宙シミュレーションゲームです。この非公式 Mod は
一部の UI を日本語・中国語に翻訳し、英語にも戻せます。
対応：Windows x64、KSA 2026.10.7.5541 / 2026.10.10.5554。
StarMap 0.4.7 のインスタンスに上記の mods 設定で導入してください。
プラグインのみの場合、初期表示は中国語です。ゲーム内の Language → 日本語 で
切り替えると、その言語が language.json に保存され、次回もその言語で起動します。
未対応の箇所は英語のままです。長い文字列が切れる場合があります。

English
KSA is a space simulator in development. This unofficial mod adds partial
Chinese and Japanese UI translations, with English available in Language.
Supports Windows x64, KSA 2026.10.7.5541 / 2026.10.10.5554, StarMap 0.4.7.
Use the offline installer for a first installation, or place this folder in
your configured StarMap instance's mods directory and enable the entry above.
The plugin-only package starts in Chinese. Choosing Language → English / 日本語
saves that choice in language.json for future launches. Some UI remains English.
Long text may clip; full gameplay and long-term stability are not verified.

代码/译文 MIT；字体 SIL OFL；许可证和来源在包内。
Code/translations: MIT. Font: SIL OFL. No KSA game or Starship mod is included.
https://github.com/LiX-Works/KsaUiLanguages
https://github.com/LiX-Works/KsaUiLanguages/releases/tag/v0.9.0
