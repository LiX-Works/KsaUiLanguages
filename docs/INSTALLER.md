# 离线安装与更新

[English](INSTALLER.en.md) · **简体中文**

[下载 v0.8.0](https://github.com/LiX-Works/KsaUiLanguages/releases/tag/v0.8.0)，选择 **KsaUiLanguages-0.8.0-installer6-win-x64.zip**。包内含汉化插件、StarMap 0.4.7 和私有 .NET 10.0.12 Runtime，不含游戏本体。

## 安装

1. 先安装 Windows x64 版 **KSA 2026.10.7.5541 或 2026.10.10.5554**，确认原版能运行。
2. 完整解压ZIP，关闭游戏，双击 **Install.cmd**，选择包含 KSA.dll 的游戏目录。
3. 从 **KSA UI Languages (5554)** 或 **KSA UI Languages (5541)** 桌面入口启动。

无需管理员权限、SDK或全局.NET，可离线安装。双击PS1可能只会打开记事本，请使用CMD入口，勿将整个脚本复制到命令提示符。

安装提示为英文，包内附中、日、英安装说明。新安装按 Windows 显示语言决定启动语言（中文系统为简体中文，日文系统为日文，其他为英文），更新安装保留原来的语言；脚本安装可用 `-Locale zh-CN|ja-JP|en-US` 指定。游戏内在 **Language → English / 日本語 / 简体中文** 切换，选择会保留到下次启动。

## 同版本更新和跨版本迁移

同一游戏版本更新汉化时，运行新 Install.cmd 并选择原来的游戏目录即可。独立设置、存档和启动语言保留，配置和旧插件有备份；安装器不会自动备份存档。从 v0.6.0 或更早版本更新时，旧的“KSA 中文版”桌面入口会在安装完成后换成新名称。

安装器按实际游戏版本选择独立根目录：

| 游戏版本 | 默认目录 | 桌面入口 |
| --- | --- | --- |
| 2026.10.7.5541 | %LOCALAPPDATA%\KsaUiLanguages\Build5541 | KSA UI Languages (5541) |
| 2026.10.10.5554 | %LOCALAPPDATA%\KsaUiLanguages\Build5554 | KSA UI Languages (5554) |

两个版本可以保留独立入口、设置和存档。已有5541实例不会被5554安装复用；自定义目录也不能跨版本覆盖。旧安装助手1–4的5541实例可以同版本升级。

汉化安装器不负责安装或更新KSA本体。想保留旧版时，先保留旧游戏目录，再另行准备新版目录。实例不会自动导入原玩家设置和存档；旧版存档迁移尚未验证，操作前请另存备份。

## 配置和卸载

根目录下的 **instance** 存放独立设置、mods和存档，**backups** 存放重装备份。启动语言记录在instance/mods/KsaUiLanguages/language.json，游戏内用Language菜单切换时会自动更新。

打开对应安装根目录，运行 **Uninstall.cmd**。它移除归属本安装的快捷方式与部署文件，保留instance和backups等；彻底删除前先自行备份存档。

换电脑时复制原始ZIP重新安装，部署后的快捷方式和配置包含本机路径。

## 检查边界

安装流程在Windows PowerShell 5.1中检查中文/空格路径、重装、备份、跨版本拒绝、错误版本、重解析点、快捷方式归属和卸载保留存档。v0.7.0 另检查 -Locale 指定、重装保留游戏内语言、拒绝不支持的语言以及三语说明的安装。5541 通过25项，5554通过23项；5541多出的两项验证旧安装助手状态迁移。使用项目内临时实例，不触及真实桌面，不启动游戏。

同一发行插件在两版均通过60项语言和控件检查，包括菜单语言选择的保存。5554另有实际启动、主要窗口和独立保存/载入记录；这不代表安装助手GUI、第二台电脑或全部游戏操作已验收。

## 开发者打包

构建插件并更新installer/dependencies.json中的真实插件ZIP哈希后，运行：

~~~powershell
.\tools\make_installer.ps1
.\tools\check_installer.ps1 -GameDir 'C:\Games\KSA-5554' -OtherGameDir 'C:\Games\KSA-5541'
~~~

输出dist/KsaUiLanguages-0.8.0-installer6-win-x64.zip。脚本复用校验过的本地插件及依赖缓存；加载器和运行时来自锁定官方URL，核对SHA-256，运行时另核对SHA-512。包内PS1为UTF-8 BOM，CMD为Windows换行，兼容系统PowerShell5.1。
