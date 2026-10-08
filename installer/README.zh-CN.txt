KSA 中文安装说明 — v0.5.0 预览版

Kitten Space Agency（KSA）是一款仍在开发中的太空模拟游戏。
本包提供部分简体中文，支持 Windows x64 和游戏
2026.10.7.5541 / 2026.10.10.5554。先确认对应原版游戏能正常运行。

三步开始
1. 完整解压 KsaUiLanguages-0.5.0-installer5-win-x64.zip。
2. 关闭游戏，双击 Install.cmd，选择包含 KSA.dll 的游戏目录。
3. 从桌面“KSA 中文版 (5554)”或“KSA 中文版 (5541)”启动。

安装包已带加载器和私有运行时，可离线安装，无需管理员权限。
如果双击后用记事本打开，请确认点的是Install.cmd，而不是Install.ps1。

更新与换版本
同游戏版本更新：选择原游戏目录，设置与存档保留，配置/旧插件会备份。
换游戏版本：使用对应的新Build目录，保留旧实例；不允许跨版本覆盖实例。
游戏本体需要自行准备，安装器不升级游戏，不自动导入或迁移存档。

独立目录
%LOCALAPPDATA%\KsaUiLanguages\Build5554 或 Build5541
设置、模组和存档在instance，重装备份在backups。存档不会自动备份。
移到其他电脑：复制原始ZIP，完整解压后重新安装；不要直接复制本机部署。
卸载运行Uninstall.cmd，保留instance和backups；自行删除前先备份存档。

本版新增
5554适配；欢迎和加载提示；降落伞、资源/动力、乘员和编辑器文字。
新增32个储箱名，字典1625条、748条语义备注。
两个游戏版本均通过54项语言/控件检查，5554主要界面与独立保存/载入已抽查。
长存档名在载入确认弹窗可能截断，仍有英文界面，长期稳定性待验证。
Language可即时切换，当前选择只对本次运行生效。

非官方社区汉化，不含游戏或Starship模组。
代码/译文MIT，字体OFL，第三方组件保留各自许可。
项目：https://github.com/LiX-Works/KsaUiLanguages
下载：https://github.com/LiX-Works/KsaUiLanguages/releases/tag/v0.5.0
