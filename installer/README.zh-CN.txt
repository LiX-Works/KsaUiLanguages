KSA 简体中文安装说明

三步开始
1. 下载并完整解压 KsaUiLanguages-0.2.0-installer1-win-x64.zip，不要直接在压缩包内运行。
2. 双击 Install.cmd，在窗口中选择包含 KSA.dll 的游戏安装目录。
3. 安装完成后，使用桌面快捷方式“KSA 中文版 (5541)”启动。安装前先关闭正在运行的 KSA 和 StarMap。

要求与包含内容
电脑上必须已安装合法的 KSA 2026.10.7.5541 Windows 64 位版本；系统兼容性以 KSA 实际系统要求为准。安装包只适配该版本，不含游戏本体。包内含中文插件 0.2.0、MIT 许可的 StarMap 0.4.7 和私有 .NET 10.0.12 运行时；无需另装 SDK、.NET 或 loader，也无需管理员权限或网络。安装时请阅读并接受包内 MIT、OFL 与 Microsoft .NET 许可，依赖分别适用各自许可。

安装位置、语言与存档
默认安装位置为 %LOCALAPPDATA%\KsaUiLanguages\Build5541。游戏使用独立 instance 配置和存档，不会自动导入原 Documents 下的设置或存档。游戏内从 Language/语言菜单选择 English 或简体中文；选择仅对当前会话生效。默认中文由 instance\mods\KsaUiLanguages\language.json 设置。原版游戏仍从原来的入口启动。

换电脑与卸载
可以把 ZIP 复制到 U 盘，在另一台满足要求的电脑上重新运行 Install.cmd；跨电脑流程尚未在第二台实体电脑实测。已安装目录和桌面快捷方式绑定本机路径，请勿直接复制整个已安装文件夹代替重新安装。若需带入旧存档，请先退出游戏并手动备份，再按实际存档目录结构复制；当前独立 instance 位于 %LOCALAPPDATA%\KsaUiLanguages\Build5541\instance。

卸载时按 Win+R，输入 %LOCALAPPDATA%\KsaUiLanguages\Build5541 打开默认安装目录，再运行 Uninstall.cmd。卸载程序仅移除部署程序和快捷方式，会保留 instance、backups 与 scripts；彻底删除 instance 前请先手动备份存档。

这是基础 UI 汉化，常见缩写会保留；尚未验证全部按钮、长期使用或其他游戏版本。本机代码检查已覆盖安装、重装、卸载、异常路径和私有运行时语言；尚未在第二台实体电脑、游戏 GUI 或实际游戏启动中验证。

源码：https://github.com/LiX-Works/KsaUiLanguages
发行页：https://github.com/LiX-Works/KsaUiLanguages/releases/tag/v0.2.0-installer.1
源码采用 MIT 许可，字体采用 OFL，依赖按各自许可使用。