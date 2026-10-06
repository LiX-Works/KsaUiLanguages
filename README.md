# KSA UI Languages

**Kitten Space Agency（KSA）是一款仍在开发中的太空模拟游戏。** 这个汉化包提供简体中文界面，帮助你看懂设置、搭建飞船和操作说明，也可以切回英文。

**当前版本：v0.4.0 预览版。只支持 Windows 64 位和 KSA 2026.10.7.5541。** 目前是部分汉化，仍有英文内容。

## 下载与安装

[下载 v0.4.0 安装包](https://github.com/LiX-Works/KsaUiLanguages/releases/tag/v0.4.0)，选择 **KsaUiLanguages-0.4.0-installer4-win-x64.zip**。

1. 先安装对应版本的 KSA，并确认原版游戏能正常运行。
2. 完整解压汉化安装包，关闭游戏，双击 **Install.cmd**，选择包含 KSA.dll 的游戏目录。
3. 安装完成后，从桌面 **“KSA 中文版 (5541)”** 启动。

安装包已带所需的加载器和私有运行时，可以离线安装，无需管理员权限。请运行解压后的 Install.cmd；双击 Install.ps1 可能只会打开记事本。

更新时，关闭游戏后运行新版 Install.cmd，选择同一游戏目录即可。独立设置和存档会保留；安装器会备份配置和旧插件，**不会自动备份存档**。

汉化入口使用独立的设置和存档，不会自动导入原版存档。[查看存档位置和卸载方法](docs/INSTALLER.md)。

## v0.4.0 新增内容

- 常用菜单与键位：View、Universe 菜单和调试菜单的固定文字，101 个默认键位动作及按键赋值提示。
- 信息窗口：乘员名单、纪念名单与天体信息。
- HUD：布局保存、管理和确认提示，仪表显示条件及 14 个默认面板名称。
- EVA：5 个出舱操控按钮的悬停说明，区分直接操控、视角操控与自动姿态稳定。
- 中英对照字典扩展至 1,016 条记录，其中 415 条附有界面位置或功能含义备注。

此前已覆盖多个设置页面、启动配置、编辑器工具栏、部件分类、发射设置、部分部件参数和首批 20 个部件名。RCS、EVA、Δv、TWR、Isp、HUD、导航球短缩写和单位保留。

## 使用与限制

游戏内的 **Language** 菜单可以切换中英文，菜单标题保留英文。当前选择只对这次运行生效；默认语言由汉化目录中的 language.json 设置。没有译文的内容继续显示英文。

只支持上面列出的游戏版本。深层编辑器、一些部件名和零散界面仍在完善。新增界面的完整截图验收、长时间游戏稳定性和其他版本尚未验证。[查看本版范围和验证记录](docs/RELEASE_NOTES_v0.4.0.md)。

这是非官方社区汉化。安装包不包含 KSA 游戏本体，也不包含 Starship 飞船模组。

## 校对与开源

[校对字典与说明](docs/DICTIONARY.md) · [贡献翻译](CONTRIBUTING.md) · [开发与构建](docs/DEVELOPMENT.md)

独立代码和译文采用 **MIT**，字体采用 **SIL OFL**。加载器和运行时适用各自的许可，详见 [许可证与来源](THIRD_PARTY.md)。

## English

KSA UI Languages is a partial Simplified Chinese UI mod for Kitten Space Agency, with English fallback. **v0.4.0 supports KSA 2026.10.7.5541 on Windows x64 only.**

Fully extract the offline installer ZIP, run Install.cmd, select your game folder, and launch through the new desktop shortcut. The package includes the loader and a private runtime, but no game binaries or Starship vessel mod. Code and original translations are MIT; the font is SIL OFL.
