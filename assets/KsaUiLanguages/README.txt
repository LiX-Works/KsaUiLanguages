KSA UI Languages 0.2.0 — 基础 UI 汉化预览版

项目与源码：https://github.com/LiX-Works/KsaUiLanguages
下载：https://github.com/LiX-Works/KsaUiLanguages/releases/tag/v0.2.0

适配：KSA 2026.10.7.5541 / Windows x64 / StarMap 0.4.7
其他游戏版本会停用插件。当前不是完整汉化。

安装
1. 关闭游戏，按 StarMap 项目说明安装并配置 0.4.7 加载器：
   https://github.com/StarMapLoader/StarMap/releases/tag/0.4.7
2. 将本 KsaUiLanguages 文件夹放入“文档/My Games/Kitten Space Agency/mods”。
   使用独立实例时，放到对应实例的 mods 目录。
3. 备份玩家/实例的 manifest.toml，保留原有条目，并追加：
   [[mods]]
   id = "KsaUiLanguages"
   enabled = true
4. 通过 StarMap.exe 启动游戏。

Language / 语言菜单可即时切换 English / 简体中文。
选择目前仅对本次会话生效；下次启动的默认值来自 language.json：
{"locale":"zh-CN"}
如需默认英文，将 zh-CN 改成 en-US。

卸载：先关闭游戏，将 manifest 对应条目的 enabled 改为 false，
或移除条目和本插件文件夹。

覆盖
202条原生语言资源译文；主菜单、暂停菜单、部分设置下拉与面板标题；
资源栏液氢/液氧/电能；78条中文悬停说明。
保留 RCS、Δv、TWR、Isp、EVA、HUD、MMH、NTO 及常见飞行按钮缩写。

验证
11项实际游戏语言对象/ImGui哈希检查通过；若干界面做过实机检查。
未验证所有按钮、长期游戏稳定性或其他游戏版本。

许可证
独立插件与译文：MIT，见 LICENSE.txt。
字体子集：SIL OFL 1.1，见 FONT-LICENSE.txt、FONT-SOURCE.txt。
本包不含游戏或加载器DLL。本项目非官方项目，未获官方背书。
