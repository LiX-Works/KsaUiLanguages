KSA UI Languages v0.4.2 — 简体中文预览版

Kitten Space Agency（KSA）是一款仍在开发中的太空模拟游戏。
本包提供部分简体中文与英文回退，支持 Windows x64、
KSA 2026.10.7.5541 和 2026.10.10.5554。

推荐安装
完整解压 KsaUiLanguages-0.4.2-installer5-win-x64.zip，关闭游戏，
运行 Install.cmd，选择包含 KSA.dll 的目录。
从桌面“KSA 中文版 (5554)”或“KSA 中文版 (5541)”启动。
游戏版本不同会使用独立设置与存档，不自动迁移旧存档。

仅插件包
先配置 StarMap 0.4.7，把整个 KsaUiLanguages 文件夹放入实例目录的mods，
保留原有 manifest.toml 条目，并启用：

[[mods]]
id = "KsaUiLanguages"
enabled = true

通过 StarMap.exe 启动。仅插件ZIP不含游戏、加载器或运行时。

本版内容
欢迎/加载提示、降落伞、资源和动力参数、乘员及编辑器零散提示；
新增32个储箱名，保留型号。字典1625条、748条语义备注。
54项检查在两个支持版本通过，5554主要界面及独立保存/载入已抽查。
长存档名在载入确认弹窗可能截断，仍有英文界面，长期稳定性待验证。

Language 可即时切换中英文，当前选择不保存；默认值来自language.json。
卸载前关闭游戏，在manifest中停用或移除插件条目。
代码/译文MIT、字体SIL OFL，许可证在包内。这是非官方社区汉化。
项目：https://github.com/LiX-Works/KsaUiLanguages
下载：https://github.com/LiX-Works/KsaUiLanguages/releases/tag/v0.4.2
