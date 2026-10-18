# v0.6.0 预览版

[简体中文说明](https://github.com/LiX-Works/KsaUiLanguages/blob/v0.6.0/README.md) · [日本語](https://github.com/LiX-Works/KsaUiLanguages/blob/v0.6.0/README.ja.md) · [English](https://github.com/LiX-Works/KsaUiLanguages/blob/v0.6.0/README.en.md)

Kitten Space Agency（KSA）是一款仍在开发中的太空模拟游戏。**KSA UI Languages v0.6.0** 在现有部分简体中文界面上新增**日本語**，并保留原版英文切换。

本版支持 **Windows x64、KSA 2026.10.7.5541 和 2026.10.10.5554**。未覆盖的界面仍显示英文；词条数量不代表全游戏翻译完成。后续公开版本按 v0.7.0、v0.8.0 递增，旧版本标签和下载保留。

## 下载与更新

推荐下载 **KsaUiLanguages-0.6.0-installer5-win-x64.zip**。完整解压，关闭游戏，运行 **Install.cmd**，选择包含 **KSA.dll** 的游戏目录，再从桌面的 **KSA 中文版 (5554)** 或 **KSA 中文版 (5541)** 启动。

安装器提示与快捷方式名称目前仍为中文。安装包已包含加载器和私有运行时，可离线安装，无需管理员权限。

游戏内通过 **Language → 日本語 / 简体中文 / English** 切换。默认简体中文；菜单选择只对本次运行生效。修改实例中的 `instance/mods/KsaUiLanguages/language.json`，将 `locale` 设为 `ja-JP`、`zh-CN` 或 `en-US`，可设置启动默认语言。

同一游戏版本更新时选择原游戏目录，独立设置和存档保留，配置与旧插件有备份；安装器不会自动备份存档。换到 5554 时使用独立 Build5554 实例，保留 Build5541；不会升级游戏本体或自动迁移旧存档。[安装、存档与卸载说明](https://github.com/LiX-Works/KsaUiLanguages/blob/v0.6.0/docs/INSTALLER.md)

## 本版更新

- **日文词典**：中、日词典各有 **1,625 条记录**，沿用现有设置、编辑器、常用 HUD、规划、轨迹、存档、乘员与提示等翻译范围。没有声称翻译全部游戏界面。
- **日文显示**：补充共用字体字符，让欢迎页支持假名、长音和日文标点换行；保留原有热键卡片。字体仍为 Noto Sans SC 子集，尚未配置单独的日文字形字体。
- **语义校对**：区分相机环绕与飞行轨道、机动与实际燃烧、推力特性与药柱断面形状；校正欢迎页的相机参考系说明。保留 RCS、EVA、Isp、TWR、HUD、Δv 等常用缩写、单位、型号与玩家名称。
- **三语项目说明**：仓库首页默认中文，并提供完整日文、英文说明，三份说明顶部可以互相切换。
- **校对字典**：新增日文导出，与中文分别提供 JSON、CSV 和校对说明；每套附有 748 条中文语义备注，明确保留英文来源和备注的语言信息。日文资源名称「電気エネルギー」缩短为「電気」，减少资源栏截断。

## 检查与已知问题

同一发行插件在 5541、5554 上各通过 **59 项语言与控件检查**，覆盖中文回归、日文原生文本、英文回退、三语言往返、真实控件 ID、欢迎页热键及假名换行。中、日词典的键集合一致，格式参数、标记顺序和边界空格检查通过。

5554 已实际显示日文启动配置和加载画面；用户提供的截图进一步确认日文欢迎页及飞行场景能够显示。日文飞行操作和游戏内三语言菜单切换尚未完成实机检查；不能将自动检查当成实际操作验收。旧版的中文界面与存档检查记录见 [v0.5.0 说明](https://github.com/LiX-Works/KsaUiLanguages/blob/v0.5.0/docs/RELEASE_NOTES_v0.5.0.md)，不作为本版日文功能的实机证据。

已知限制：

- 日文资源栏的长文字可能被截断；长存档名在载入确认弹窗中可能被截断。
- 版本历史、部分 HUD 短标签、调试和深层界面仍有英文。
- 共用字体尚未提供单独的日文字形；欢迎页换行处理不代表所有窄控件均已适配。
- 所有操作、长时间运行、第二台电脑、旧存档迁移和其他游戏版本尚未全面验证。

## 文件与许可

- **KsaUiLanguages-0.6.0-installer5-win-x64.zip**：推荐的完整离线安装包。
- **KsaUiLanguages-0.6.0-build5541-5554.zip**：仅语言插件，供已配置 StarMap 0.4.7 的玩家。
- **KsaUiLanguages-0.6.0-dictionary.zip**：用于翻译核查的词典与说明。
- **KsaUiLanguages-0.6.0-source.zip**：与本版提交对应的源码和译文。
- **SHA256SUMS.txt / INSTALLER-SHA256.txt**：下载完整性校验值。

这是非官方社区语言包，不包含 KSA 游戏本体或 Starship 模组。独立代码与译文采用 MIT，字体采用 SIL OFL，第三方组件保留各自许可。

## 日本語

v0.6.0 では日本語を追加しました。日本語・簡体字中国語の辞書は各 1,625 項目で、英語にも切り替えられます。Windows x64、KSA 2026.10.7.5541 / 2026.10.10.5554 が対象です。

ZIP をすべて展開して Install.cmd を実行し、KSA.dll のあるフォルダーを選択してください。案内とデスクトップの「KSA 中文版」ショートカット名は中国語です。起動後に **Language → 日本語** を選べます。初期設定は簡体字中国語で、ゲーム内の選択は終了時に保存されません。

両ゲームバージョンで 59 項目の自動チェックを実施し、5554 の起動・読み込み画面と、ユーザーのスクリーンショットによる日本語のようこそ画面・飛行シーンを確認しました。日本語での飛行操作と、画面上のメニューを使った 3 言語の切り替えは未確認です。長いリソース名やセーブ名が切れる場合があります。翻訳は一部のみで、長時間のプレイや別の PC での動作を保証するものではありません。[日本語の説明](https://github.com/LiX-Works/KsaUiLanguages/blob/v0.6.0/README.ja.md)

## English

v0.6.0 adds Japanese alongside Simplified Chinese and English. The Japanese and Chinese dictionaries each contain 1,625 entries. Supported versions are Windows x64 builds KSA 2026.10.7.5541 and 2026.10.10.5554.

Extract the offline installer, run Install.cmd, and select the folder containing KSA.dll. Installer prompts and the “KSA 中文版” desktop shortcut remain in Chinese. Choose your language from **Language → 日本語 / 简体中文 / English** after launch. Simplified Chinese is the default, and in-game choices last for the current session only.

Each game build passed 59 automated language/UI checks. Japanese startup and loading screens were observed on 5554; a user screenshot also confirmed the Japanese welcome screen and flight scene. Flight operations in Japanese and switching all three languages through the live game menu remain unverified. Long resource labels and save names can clip. Translation is partial, and full UI coverage, long sessions, and another PC have not been fully tested. [Full English guide](https://github.com/LiX-Works/KsaUiLanguages/blob/v0.6.0/README.en.md)
