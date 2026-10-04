KSA UI Languages **离线安装助手 1**，汉化插件仍为 **0.2.0**。

下载 **KsaUiLanguages-0.2.0-installer1-win-x64.zip**，完整解压后双击 **Install.cmd**，选择游戏目录。安装完成后从桌面 **KSA 中文版 (5541)** 启动。

- 仅适配 **KSA 2026.10.7.5541 / Windows x64**；需先自行安装并确认游戏能正常运行。
- 包内包含插件、StarMap 0.4.7、私有 .NET 10.0.12 Runtime 和相应许可。
- 安装可离线完成，无需管理员权限或全局安装 .NET。
- 使用独立配置与存档；重装保留设置并备份，卸载保留存档。
- 提供 payload 哈希校验、版本检查和卸载助手。

[详细安装说明](https://github.com/LiX-Works/KsaUiLanguages/blob/main/docs/INSTALLER.md)。

独立源码 MIT、字体 SIL OFL；StarMap/Harmony 为 MIT，官方 Windows .NET Runtime 使用随附 Microsoft .NET Library License 和第三方声明。

Windows PowerShell 5.1 隔离安装流程的 13 项检查通过，实际安装插件在包内私有运行时上的 11 项语言检查通过。安装助手 GUI、图形游戏启动及第二台实体电脑尚未验证。

installer1-source.zip 是对应源码；INSTALLER-SHA256.txt 提供离线安装包校验值。此包不含 KSA 游戏文件。
