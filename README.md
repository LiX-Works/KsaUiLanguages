# KSA UI Languages

为 **Kitten Space Agency (KSA)** 提供独立的界面语言插件。目前以简体中文验证多语言框架，支持游戏内切换 English / 简体中文。

**0.3.0 预览版 · 仅适配 KSA 2026.10.7.5541 · Windows x64 · StarMap 0.4.7**

[下载预览版](https://github.com/LiX-Works/KsaUiLanguages/releases/tag/v0.3.0) · [开发与检查](docs/DEVELOPMENT.md) · [贡献翻译](CONTRIBUTING.md) · [许可证与来源](THIRD_PARTY.md)

## 推荐：一键安装包

在其他电脑上安装，可以使用 [离线安装助手](https://github.com/LiX-Works/KsaUiLanguages/releases/tag/v0.3.0)：完整解压 ZIP、双击 Install.cmd、选择游戏目录，再从桌面“KSA 中文版 (5541)”启动。

该包已包含加载器和私有 .NET 运行时，无需另行准备；需先自行安装能正常运行的 KSA 2026.10.7.5541。它使用独立配置和存档。详见 [安装与卸载说明](docs/INSTALLER.md)。

## 已覆盖的界面

- 202 条原生语言资源译文，涉及图形、显示、声音、控制、模拟等设置和提示。
- 主菜单、暂停菜单、设置下拉选项及部分 HUD 面板标题。
- 资源栏中的液氢、液氧和电能。
- 中文悬停说明；语言包中共 116 条，包括新增编辑器提示。
- 飞船编辑器的放置 / 平移 / 旋转 / 缩放、部件分类、相机工具、对称工具和发射设置。
- 部件提示卡中的质量、容积、电力、推力、比冲和分离力等参数说明。
- 英文回退、占位符验证和语言切换。

保留 RCS、Δv、TWR、Isp、EVA、HUD、MMH、NTO，以及 STAR / SURF / ORBT / OVEL / TGT / BURN 等常见飞行缩写和单位。大量仪表短按钮、深层编辑器和部件说明仍待补充；这是一份基础 UI 汉化。

## 安装

1. 确认游戏版本是 **2026.10.7.5541**。插件会在其他版本停用，不承诺跨版本兼容。
2. 安装并配置 [StarMap 0.4.7](https://github.com/StarMapLoader/StarMap/releases/tag/0.4.7)。按照其说明配置 StarMapConfig.json 的 GameLocation，然后通过 StarMap 启动游戏。
3. 从本项目 Release 下载 **KsaUiLanguages-0.3.0-build5541.zip**，解压其中的 KsaUiLanguages 文件夹。
4. 将该文件夹放入你的“文档 / My Games / Kitten Space Agency / mods”目录。如果使用 StarMap 独立实例，放入该实例的 mods 目录。
5. 备份玩家或实例目录下的 manifest.toml，在现有内容后添加以下条目。保留已有 Core 和其他 mod 条目：

~~~toml
[[mods]]
id = "KsaUiLanguages"
enabled = true
~~~

目录应包含 mods/KsaUiLanguages/KsaUiLanguages.dll、mod.toml、Locales、Fonts 等文件，避免多套一层文件夹。关闭游戏后安装，通过 StarMap.exe 启动。

菜单栏会出现 **Language / 语言**。游戏内选择即时生效，但当前不会保存选择；如需修改下次启动的默认语言，编辑 mod 目录中的 language.json：

~~~json
{"locale": "zh-CN"}
~~~

使用英文则改成 en-US。卸载时先关闭游戏，将 manifest 中本插件的 enabled 改为 false；也可以移除对应条目和本插件文件夹。

## 验证范围与限制

主菜单、暂停菜单、图形设置、显示模式下拉列表、资源栏、中英往返切换，以及 ORBT 中文悬停说明有此前实机检查记录。v0.3 的初版编辑器工具栏、分类、发射字段和部件参数也由用户提供的实机截图确认。最后补入的树状字形、发射天体 / 地点预览和双基推进剂译名尚待视觉复核。

当前 16 项检查使用实际游戏语言对象、原始方法指令及原生 ImGui 哈希，验证回退、占位符、控件 ID 和全部补丁安装。编辑器字典还保留部分待接入的深层词条，不代表所有界面均已覆盖。

这些检查不涵盖所有按钮操作或长时间游戏稳定性。开发测试出现过桌面操作工具超时，原因未确认，不能据此判断游戏或插件整体稳定性。

当前没有为自动更新、任意游戏版本或从右向左书写的语言提供支持。新语言可以复用 JSON 结构；新增字符需要更新字体子集。

## 开源与后续方向

独立编写的插件、译文和工具采用 **MIT**。随附字体子集采用 **SIL Open Font License 1.1**，详见 [THIRD_PARTY.md](THIRD_PARTY.md)。

本项目是非官方社区项目，未获官方背书或建立合作。仓库和下载包不分发游戏本体、游戏 DLL 或反编译材料。独立插件包需另配 StarMap；离线安装助手则附带官方加载器和私有 .NET Runtime，适用各自许可证。

接下来优先补齐基础 UI 和易懂的悬停说明，完善语言包贡献流程，再考虑其他语言和新版兼容。

## English

An independent, partial UI localization mod for Kitten Space Agency. Simplified Chinese is the first language pack; English fallback and in-game language switching are supported. Version 0.3.0 targets **KSA 2026.10.7.5541 / StarMap 0.4.7 / Windows x64** only.

Use the offline installer ZIP, extract it fully, run Install.cmd and select your existing game folder. The installer includes StarMap and a private .NET Runtime under their respective licenses. Alternatively, install the standalone build5541 mod ZIP manually as described above. Code and original translations are MIT; the font subset is SIL OFL 1.1. Game binaries are not distributed.
