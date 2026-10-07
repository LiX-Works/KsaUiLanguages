# 一键安装包

下载 [离线安装包](https://github.com/LiX-Works/KsaUiLanguages/releases/tag/v0.4.1)。这是 ZIP 格式的安装助手，包含插件 0.4.1、StarMap 0.4.7 和私有 .NET 10.0.12 Runtime。

## 在另一台电脑上安装

1. 先安装并确认 **KSA 2026.10.7.5541** 能正常运行；电脑需满足游戏的 Windows x64 系统要求。
2. 把 KsaUiLanguages-0.4.1-installer4-win-x64.zip 复制到目标电脑，完整解压。
3. 关闭游戏，双击 Install.cmd，选择包含 KSA.dll 的游戏目录，阅读随附许可并确认安装。
4. 从桌面 **KSA 中文版 (5541)** 启动。菜单栏的 Language 可以切换简体中文和英文。

安装本身可离线进行，无需管理员权限、SDK、另装加载器或全局 .NET。下载包不含 KSA 游戏。

已经安装旧版的玩家，关闭游戏后运行新版 Install.cmd，选择同一游戏目录即可更新。独立设置和存档会保留；安装器备份配置和旧插件。

若双击后用记事本打开，请确认点击的是 Install.cmd，而不是 Install.ps1。不要把整个脚本复制到 CMD 中运行。

## 配置和卸载

默认安装根目录：%LOCALAPPDATA%\KsaUiLanguages\Build5541。

独立配置、mod 和存档在根目录下的 instance；重装备份在 backups。安装器不自动复制原玩家目录里的设置或存档。若要迁移存档，先关闭游戏、备份，再按游戏实际的存档目录结构复制。

游戏内切换语言目前只作用于本次会话。下次启动的默认值来自 instance/mods/KsaUiLanguages/language.json。

按 Win+R，输入安装根目录后，双击 Uninstall.cmd。卸载移除桌面快捷方式、加载器和私有运行时等部署文件，保留 instance、backups、脚本和归属标记。彻底删除目录前请先备份存档。

移到另一台电脑时，复制原始 ZIP 并重新安装；安装后的目录和快捷方式记录的是本机路径。

## 验证和范围

13 项安装流程检查在 Windows PowerShell 5.1 中通过，包括中文及空格路径、重装备份、保留其他 manifest 条目、校验损坏文件、拒绝错误版本、拒绝重解析点、快捷方式归属、卸载保留存档。原游戏与原玩家配置哈希保持一致。

插件当前通过 47 项语言对象、方法指令、原生 ImGui 哈希和补丁安装检查，覆盖规划、轨迹、存档、资源等显示文字及中英文切换。

本版插件已在独立实例中实际启动游戏，并查看多个主要窗口及中英文切换。安装助手 GUI 的完整流程和第二台实体电脑尚未测试；这些插件界面检查不能代替安装助手的全流程实机验证。

## 构建安装包

在仓库根目录运行 PowerShell：

~~~powershell
.\tools\make_installer.ps1
~~~

脚本会从官方 URL 下载锁定的三个 ZIP，或复用 work/installer-deps 缓存；校验 SHA-256，.NET Runtime 另校验官方 SHA-512。没有游戏安装或 SDK，也能构建安装助手。

输出为 dist/KsaUiLanguages-0.4.1-installer4-win-x64.zip。脚本为安装包内的 PowerShell 源码写入 UTF-8 BOM，并使用 Windows 换行，以兼容系统自带 PowerShell 5.1。

开发者在已安装 build5541 的机器上可运行：

~~~powershell
.\tools\check_installer.ps1 -GameDir 'C:\Games\Kitten Space Agency'
~~~

这会在仓库 work 内建立临时实例，执行安装 / 重装 / 卸载等检查，使用临时目录模拟快捷方式，不触及真实桌面，也不启动游戏。结果写入 work/run-logs/installer-checks.json。

非交互安装须先阅读随附许可，并显式提供 -AcceptLicenses。正常玩家使用 Install.cmd 的安装窗口即可。
