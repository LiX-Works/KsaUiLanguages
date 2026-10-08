# 离线安装与更新

[下载 v0.5.0](https://github.com/LiX-Works/KsaUiLanguages/releases/tag/v0.5.0)，选择 **KsaUiLanguages-0.5.0-installer5-win-x64.zip**。包内含汉化插件、StarMap 0.4.7 和私有 .NET 10.0.12 Runtime，不含游戏本体。

## 安装

1. 先安装 Windows x64 版 **KSA 2026.10.7.5541 或 2026.10.10.5554**，确认原版能运行。
2. 完整解压ZIP，关闭游戏，双击 **Install.cmd**，选择包含 KSA.dll 的游戏目录。
3. 从 **KSA 中文版 (5554)** 或 **KSA 中文版 (5541)** 桌面入口启动。

无需管理员权限、SDK或全局.NET，可离线安装。双击PS1可能只会打开记事本，请使用CMD入口，勿将整个脚本复制到命令提示符。

## 同版本更新和跨版本迁移

同一游戏版本更新汉化时，运行新 Install.cmd 并选择原来的游戏目录即可。独立设置和存档保留，配置和旧插件有备份；安装器不会自动备份存档。

安装器按实际游戏版本选择独立根目录：

| 游戏版本 | 默认目录 | 桌面入口 |
| --- | --- | --- |
| 2026.10.7.5541 | %LOCALAPPDATA%\KsaUiLanguages\Build5541 | KSA 中文版 (5541) |
| 2026.10.10.5554 | %LOCALAPPDATA%\KsaUiLanguages\Build5554 | KSA 中文版 (5554) |

两个版本可以保留独立入口、设置和存档。已有5541实例不会被5554安装复用；自定义目录也不能跨版本覆盖。旧安装助手1–4的5541实例可以同版本升级。

汉化安装器不负责安装或更新KSA本体。想保留旧版时，先保留旧游戏目录，再另行准备新版目录。实例不会自动导入原玩家设置和存档；旧版存档迁移尚未验证，操作前请另存备份。

## 配置和卸载

根目录下的 **instance** 存放独立设置、mods和存档，**backups** 存放重装备份。Language选择只对当次运行生效，默认语言在instance/mods/KsaUiLanguages/language.json。

打开对应安装根目录，运行 **Uninstall.cmd**。它移除归属本安装的快捷方式与部署文件，保留instance和backups等；彻底删除前先自行备份存档。

换电脑时复制原始ZIP重新安装，部署后的快捷方式和配置包含本机路径。

## 检查边界

安装流程在Windows PowerShell 5.1中检查中文/空格路径、重装、备份、跨版本拒绝、错误版本、重解析点、快捷方式归属和卸载保留存档。5541 通过18项，5554通过16项；额外的5541检查验证旧安装助手状态迁移。使用项目内临时实例，不触及真实桌面，不启动游戏。

插件在两版均通过54项语言和控件检查。5554另有实际启动、主要窗口和独立保存/载入记录；这不代表安装助手GUI、第二台电脑或全部游戏操作已验收。

## 开发者打包

构建插件并更新installer/dependencies.json中的真实插件ZIP哈希后，运行：

~~~powershell
.\tools\make_installer.ps1
.\tools\check_installer.ps1 -GameDir 'C:\Games\KSA-5554' -OtherGameDir 'C:\Games\KSA-5541'
~~~

输出dist/KsaUiLanguages-0.5.0-installer5-win-x64.zip。脚本复用校验过的本地插件及依赖缓存；加载器和运行时来自锁定官方URL，核对SHA-256，运行时另核对SHA-512。包内PS1为UTF-8 BOM，CMD为Windows换行，兼容系统PowerShell5.1。
