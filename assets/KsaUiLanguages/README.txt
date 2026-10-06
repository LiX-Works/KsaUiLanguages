KSA UI Languages v0.4.0 — 简体中文界面包（预览版）

Kitten Space Agency（KSA）是一款仍在开发中的太空模拟游戏。
本包提供部分简体中文界面，帮助你看懂设置、搭建飞船和操作说明。

支持：Windows 64 位 / KSA 2026.10.7.5541 / StarMap 0.4.7
下载与源码：https://github.com/LiX-Works/KsaUiLanguages/releases/tag/v0.4.0

推荐使用离线安装包
完整解压 KsaUiLanguages-0.4.0-installer4-win-x64.zip，关闭游戏，双击 Install.cmd，
选择包含 KSA.dll 的游戏目录，再从桌面“KSA 中文版 (5541)”启动。
安装包已带加载器和运行时。旧版用户用同样步骤更新，保留独立设置与存档。

如果下载的是仅插件包
先按 StarMap 0.4.7 的说明配置加载器：
https://github.com/StarMapLoader/StarMap/releases/tag/0.4.7
将本 KsaUiLanguages 文件夹放入玩家/实例目录下的 mods。
备份 manifest.toml，保留原有条目，并添加：

[[mods]]
id = "KsaUiLanguages"
enabled = true

通过 StarMap.exe 启动。

这次新增
补齐视图和宇宙菜单、101个默认键位名、乘员名单、天体信息、
HUD布局和仪表显示条件，以及小猫出舱操控提示。Language标题只用英文。
保留之前的启动配置页中文、首批20个部件中文名。
常见缩写、型号和单位保留；仍有英文内容。
34项语言/控件检查通过；新增界面的完整实机显示验收尚未完成。
部分部件名、长期稳定性和其他游戏版本尚未完成验证。

语言与卸载
Language 菜单可即时切换中英文；当前不会保存本次选择。
下次启动的默认语言来自 language.json，zh-CN 是中文，en-US 是英文。
卸载前关闭游戏，将 manifest 中本插件 enabled 改为 false，或移除插件条目。

许可证
独立代码与译文 MIT；字体 SIL OFL，许可证在包内。
本插件包不含游戏或加载器。本项目是非官方社区汉化。
