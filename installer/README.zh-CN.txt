KSA UI Languages — v0.8.0 预览版

Kitten Space Agency（KSA）是一款仍在开发中的太空模拟游戏。
本包提供部分中文和日文界面，也能切回英文。支持 Windows x64 和
KSA 2026.10.7.5541 / 2026.10.10.5554。先确认对应原版能正常运行。

三步开始
1. 完整解压 KsaUiLanguages-0.8.0-installer6-win-x64.zip。
2. 关闭游戏，双击 Install.cmd，选择包含 KSA.dll 的游戏目录。
   安装助手的提示为英文，确认许可时选 Yes。
3. 从桌面“KSA UI Languages (5554)”或“KSA UI Languages (5541)”启动。

游戏内 Language 菜单可以切换 English / 日本語 / 简体中文，选择会保留到下次启动。
新安装时按 Windows 显示语言决定启动语言：中文系统为简体中文，日文系统为日文，
其他为英文；更新安装会保留原来的语言。启动语言记录在
instance\mods\KsaUiLanguages\language.json，locale 可用 zh-CN / ja-JP / en-US。

加载器和私有运行时已包含，可离线安装，无需管理员权限。
若双击后用记事本打开，请确认点的是 Install.cmd，而不是 Install.ps1。

更新与换版本
同一游戏版本更新：选择原游戏目录，设置、存档与启动语言保留，配置/旧插件会备份。
从 v0.6.0 或更早版本更新时，旧的“KSA 中文版”桌面快捷方式会换成新名称。
不同游戏版本使用各自的 Build 目录；不允许跨版本覆盖实例。
本包不升级游戏，不自动导入或迁移存档。存档不会自动备份。

安装目录
%LOCALAPPDATA%\KsaUiLanguages\Build5554 或 Build5541
设置、模组和存档在 instance，重装备份在 backups。
换电脑时复制原始 ZIP、完整解压并重新安装，不要直接复制本机部署目录。
卸载运行 Uninstall.cmd，保留 instance 和 backups；自行删除前先备份存档。

本版更新：新增英文安装指南。v0.7.0 起：记住游戏内的语言选择；
安装、启动、卸载提示为英文；桌面快捷方式改名；安装时按系统语言设置启动语言。
同一发行插件在两个支持版本各通过 60 项语言/控件自动检查，
安装器通过 25 项（5541）和 23 项（5554）隔离的安装、更新与卸载检查。
仍有英文内容，部分长文字可能截断；日文暂用 Noto Sans SC 字体。
完整飞行操作、第二台电脑和长期稳定性尚未全面验证。

非官方社区项目，不含游戏或 Starship 模组。
代码/译文 MIT，字体 SIL OFL，第三方组件保留各自许可。
项目：https://github.com/LiX-Works/KsaUiLanguages
下载：https://github.com/LiX-Works/KsaUiLanguages/releases/tag/v0.8.0
