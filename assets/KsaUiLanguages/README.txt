KSA UI Languages 0.4.1 — 简体中文预览版

Kitten Space Agency（KSA）是一款仍在开发中的太空模拟游戏。
本包为设置、飞船编辑和常用飞行窗口提供部分简体中文，并支持切回英文。
支持：Windows 64 位 / KSA 2026.10.7.5541 / StarMap 0.4.7。

推荐安装方法
下载并完整解压 KsaUiLanguages-0.4.1-installer4-win-x64.zip。
关闭游戏，双击 Install.cmd，选择包含 KSA.dll 的游戏目录。
随后从桌面“KSA 中文版 (5541)”启动。旧版用户按同样步骤更新。

手动安装仅插件包
先配置 StarMap 0.4.7，将整个 KsaUiLanguages 文件夹放入玩家/实例目录的 mods。
备份 manifest.toml，保留已有条目，并添加：

[[mods]]
id = "KsaUiLanguages"
enabled = true

通过 StarMap.exe 启动。更新时先关闭游戏，再替换同名插件文件夹。
仅插件包不含游戏、加载器或运行时。

这次新增
飞行计划、机动编辑、转移规划、地面与目标轨迹、部件与对接菜单，
以及资源详情、存档列表、对象清单和常用弹窗。
校对字典有1480条记录，602条附语义备注。
47项语言与控件检查通过；已实机查看部分主要窗口并验证中英文切换。
仍是部分汉化，调试、高级调参和一些深层界面尚未完善。

语言与卸载
Language 菜单可即时切换中英文，当前不会保存本次选择。
下次启动的默认语言来自 language.json：zh-CN为中文，en-US为英文。
卸载前关闭游戏，将 manifest 中本插件 enabled 改为 false，或移除插件条目。

独立代码与译文 MIT；字体 SIL OFL，许可证在包内。
这是非官方社区汉化，不含 Starship 飞船模组。
源码：https://github.com/LiX-Works/KsaUiLanguages
下载：https://github.com/LiX-Works/KsaUiLanguages/releases/tag/v0.4.1
