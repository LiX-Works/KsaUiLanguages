# KSA UI Languages

**Kitten Space Agency（KSA）是一款仍在开发中的太空模拟游戏。** 这个汉化包为设置、飞船编辑和常用飞行窗口提供简体中文，也能切回英文。

**当前版本：v0.4.2 预览版。支持 Windows 64 位及 KSA 2026.10.7.5541、2026.10.10.5554。** 目前是部分汉化，仍有英文内容。

## 下载与安装

[下载 v0.4.2](https://github.com/LiX-Works/KsaUiLanguages/releases/tag/v0.4.2)，推荐选择 **KsaUiLanguages-0.4.2-installer5-win-x64.zip**。

1. 先安装上述对应版本的 KSA，并确认原版能正常运行。
2. 完整解压汉化安装包，关闭游戏，双击 **Install.cmd**，选择包含 KSA.dll 的游戏目录。
3. 从桌面 **“KSA 中文版 (5554)”** 或 **“KSA 中文版 (5541)”** 启动，名称取决于所选游戏版本。

加载器和私有运行时已包含在包内，可离线安装，无需管理员权限。请运行 Install.cmd；双击 Install.ps1 可能只会打开记事本。

**同一游戏版本更新汉化**：选择原来的游戏目录，设置和存档会保留。配置和旧插件会备份，安装器不会自动备份存档。

**从 5541 换到 5554**：安装器使用新的 Build5554 实例，保留旧 Build5541。请保留两个游戏目录；汉化安装器不会替你更新或备份游戏本体，也不会自动迁移旧存档。[详细安装与存档说明](docs/INSTALLER.md)

## v0.4.2 新增内容

- **新版适配**：验证 5554，加入可重复的补丁目标与指令差异预检工具。
- **入门与提示**：欢迎页、加载阶段、显存提示、乘员分配、头像相机、EVA 动作和天体悬停说明。
- **部件详情**：降落伞操作与状态、储箱和发动机参数、资源流动图；补齐编辑器的补满消耗品、自动分组和资源管理。
- **储箱名称**：新增 32 个储箱名，保留型号和原模板 ID。后续更直观的命名方案仍在核查，本版没有应用新的昵称方案。
- **校对字典**：共 1,625 条记录，其中 748 条带有界面和功能语义备注。

已有的规划、轨迹图、存档、对象清单、设置、键位、乘员名单和 HUD 汉化继续保留。常用模拟器缩写、单位、快捷键及玩家名称保留。

## 使用与限制

**Language** 菜单可即时切换中英文。当前选择只对本次运行生效，默认值由 language.json 设置。缺少译文时显示原文。

两版均通过 54 项语言与控件检查。5554 已实机抽查欢迎页、飞行计划、转移规划、中英文往返、编辑器及资源分组，并实际保存、载入独立测试存档。

已知问题：长存档名在载入确认弹窗中可能被截断；版本历史、部分 HUD 短标签和深层界面仍有英文。所有按钮、长时间运行、第二台电脑和旧存档迁移尚未全面验证。[本版范围与检查记录](docs/RELEASE_NOTES_v0.4.2.md)

这是非官方社区汉化，安装包不含 KSA 游戏本体或 Starship 飞船模组。

## 校对与开源

[校对字典](docs/DICTIONARY.md) · [贡献翻译](CONTRIBUTING.md) · [开发与构建](docs/DEVELOPMENT.md) · [部件命名核查](docs/PART_NAMING_REVIEW.md)

独立代码和译文采用 **MIT**，字体采用 **SIL OFL**；加载器和运行时适用随附许可。[许可证与来源](THIRD_PARTY.md)

## English

Partial Simplified Chinese UI for **KSA 2026.10.7.5541 and 2026.10.10.5554, Windows x64**. Extract the offline installer, run Install.cmd, and select your game folder. Each game build uses its own profile and desktop shortcut. This update adds welcome/loading, crew, parachute, resource and editor captions, plus 5554 compatibility checks. English fallback and user names/IDs are preserved. Game binaries and the Starship mod are excluded. Long save names can clip in the load confirmation; complete UI and long-run validation remain pending.
