KSA UI Languages — v0.6.0 预览版

Kitten Space Agency（KSA）是一款仍在开发中的太空模拟游戏。
本包提供部分中文和日文界面，也能切回英文。支持 Windows x64 和
KSA 2026.10.7.5541 / 2026.10.10.5554。先确认对应原版能正常运行。

三步开始
1. 完整解压 KsaUiLanguages-0.6.0-installer5-win-x64.zip。
2. 关闭游戏，双击 Install.cmd，选择包含 KSA.dll 的游戏目录。
3. 从桌面“KSA 中文版 (5554)”或“KSA 中文版 (5541)”启动。

游戏默认中文，Language 菜单可以切换 日本語 / 简体中文 / English。
菜单选择只对本次运行生效。默认语言在
instance\mods\KsaUiLanguages\language.json，locale 可用 zh-CN / ja-JP / en-US。
安装助手的提示和桌面入口名称目前仍为中文；包内另有日文、英文说明。

加载器和私有运行时已包含，可离线安装，无需管理员权限。
若双击后用记事本打开，请确认点的是 Install.cmd，而不是 Install.ps1。

更新与换版本
同一游戏版本更新：选择原游戏目录，设置与存档保留，配置/旧插件会备份。
不同游戏版本使用各自的 Build 目录；不允许跨版本覆盖实例。
本包不升级游戏，不自动导入或迁移存档。存档不会自动备份。

安装目录
%LOCALAPPDATA%\KsaUiLanguages\Build5554 或 Build5541
设置、模组和存档在 instance，重装备份在 backups。
换电脑时复制原始 ZIP、完整解压并重新安装，不要直接复制本机部署目录。
卸载运行 Uninstall.cmd，保留 instance 和 backups；自行删除前先备份存档。

本版新增日文：中、日各 1,625 条词典记录，并补充日文字符和欢迎页换行。
两个支持版本各通过 59 项语言/控件自动检查；部分长文字可能截断。
仍有英文内容，完整飞行操作、第二台电脑和长期稳定性尚未全面验证。

非官方社区项目，不含游戏或 Starship 模组。
代码/译文 MIT，字体 SIL OFL，第三方组件保留各自许可。
项目：https://github.com/LiX-Works/KsaUiLanguages
下载：https://github.com/LiX-Works/KsaUiLanguages/releases/tag/v0.6.0
