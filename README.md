# KSA UI Languages

**Kitten Space Agency（KSA）是一款仍在开发中的太空模拟游戏。** 这个汉化包提供简体中文界面，帮助你看懂设置、搭建飞船和操作说明，也可以切回英文。

**当前版本：v0.3.1 预览版。支持 Windows 64 位和 KSA 2026.10.7.5541。** 目前是部分汉化，仍有英文内容。

## 下载与安装

[下载最新版安装包](https://github.com/LiX-Works/KsaUiLanguages/releases/tag/v0.3.1)，选择 **KsaUiLanguages-0.3.1-installer3-win-x64.zip**。

1. 先安装对应版本的 KSA，并确认原版游戏能正常运行。
2. 完整解压汉化安装包，关闭游戏，双击 Install.cmd，选择包含 KSA.dll 的游戏目录。
3. 安装完成后，从桌面 **“KSA 中文版 (5541)”** 启动。

安装包已带所需的加载器和运行时，可以离线安装，无需管理员权限。

已经安装旧版汉化的玩家，关闭游戏后运行新版 Install.cmd，选择同一游戏目录即可更新。独立设置和存档会保留；安装器会备份配置和旧插件。

汉化入口使用独立的设置和存档，不会自动导入原版存档。[查看存档位置和卸载方法](docs/INSTALLER.md)。

## v0.3.1 新增内容

- 启动配置页：天体系统、游戏模式、纹理、阴影、显存提示等项目中文显示。
- 首批 20 个部件名：乘员舱、固体助推器、分离器、推进剂箱等更容易辨认，同时保留型号。
- 改善部分提示的中文表达。
- 提供中英对照字典，备注说明词句出现在哪个界面、菜单或功能中，方便校对和增加语言。

之前已覆盖主菜单、多个设置页面、编辑器工具栏、部件分类、发射设置和部分部件参数。RCS、Δv、TWR、Isp、EVA、HUD 等常用缩写和单位保留。

## 使用与限制

游戏内的 **Language / 语言** 菜单可以切换中英文。当前选择只对这次运行生效；默认语言由汉化目录中的 language.json 设置。

只支持上面列出的游戏版本。深层编辑器、一些部件名和零散界面仍在完善；长时间游戏稳定性和其他版本尚未验证。

这是非官方社区汉化。安装包不包含 KSA 游戏本体。

## 校对与开源

[校对字典与说明](docs/DICTIONARY.md) · [贡献翻译](CONTRIBUTING.md) · [开发与构建](docs/DEVELOPMENT.md)

独立代码和译文采用 **MIT**，字体采用 **SIL OFL**。加载器和运行时适用各自的许可，详见 [许可证与来源](THIRD_PARTY.md)。

## English

KSA UI Languages is a partial Simplified Chinese UI mod for Kitten Space Agency, with English fallback. **v0.3.1 supports KSA 2026.10.7.5541 on Windows x64.**

Download the offline installer ZIP from Releases, extract it fully, run Install.cmd and select your game folder. Launch through the new desktop shortcut. The installer includes StarMap 0.4.7 and a private .NET 10 Runtime.

Code and original translations are MIT; the font is SIL OFL. Game binaries are not distributed.
