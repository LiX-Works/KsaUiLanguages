# v0.4.0 预览版

Kitten Space Agency（KSA）是一款仍在开发中的太空模拟游戏。**KSA UI Languages** 为它提供简体中文界面，帮助你看懂设置、搭建飞船和操作说明。

**本版是部分汉化预览版，只支持 Windows 64 位和 KSA 2026.10.7.5541。** 没有译文的内容继续显示英文。

## 下载与更新

推荐下载 **KsaUiLanguages-0.4.0-installer4-win-x64.zip**：

1. 完整解压安装包，关闭游戏。
2. 双击 **Install.cmd**，选择包含 KSA.dll 的游戏目录。
3. 从桌面 **“KSA 中文版 (5541)”** 启动。

请运行解压后的 Install.cmd；双击 Install.ps1 可能只会打开记事本。加载器和私有运行时已包含在包内，安装过程可离线完成，无需管理员权限。

旧版用户按同样步骤更新，选择原来的游戏目录即可。独立设置和存档会保留，配置和旧插件会备份；安装器**不会自动备份存档**。汉化入口不会自动导入原版存档。[查看安装、存档位置与卸载说明](INSTALLER.md)。

## 新增汉化

- **菜单与键位**：补充 View、Universe 常用菜单和调试菜单的固定文字；覆盖 101 个默认键位动作及按键赋值提示。实际键位和自定义绑定不改写。
- **乘员与天体信息**：乘员名单、纪念名单、任务统计和天体轨道信息。小猫姓名、飞船名称等动态内容保留原值。
- **HUD 布局**：保存、管理、应用、覆盖和删除提示；补充仪表显示条件及 14 个默认面板名称。玩家自定义布局名和内部 ID 保持原值。
- **EVA 提示**：补齐本版实际存在的 5 个出舱操控按钮悬停说明，解释直接操控、视角操控、RCS 和自动姿态稳定。
- **校对字典**：共 1,016 条记录，其中 415 条附有界面位置或功能含义备注，方便复核和增加语言。记录数量不代表全部游戏界面已汉化。

RCS、EVA、Δv、TWR、Isp、HUD、导航球短缩写和单位保留。**Language** 菜单标题保留英文；语言选择只对本次运行生效，下次启动的默认语言由 language.json 设置。

## 其他下载

- **KsaUiLanguages-0.4.0-build5541.zip**：仅汉化插件，供已配置加载器的玩家手动安装。
- **KsaUiLanguages-0.4.0-dictionary.zip**：中英对照字典和校对材料。
- **KsaUiLanguages-0.4.0-source.zip**：与本版提交对应的开源代码和译文。
- 随发布文件提供的 SHA-256 校验值可用于检查下载完整性。

## 验证与限制

本版已通过 **34 项使用实际游戏程序集的无图形检查**，覆盖翻译对象、显示文字与内部 ID 的区分、补丁目标和补丁安装机制；这些检查没有操作游戏画面。

新版安装包已通过 **13 项 Windows PowerShell 5.1 流程检查**，包含安装、重装、卸载、备份和文件校验。原游戏与原玩家配置的哈希保持不变。

新增界面的完整截图验收尚未完成，截图工具出现等待画面超时。因此目前不能据此确认所有文字都没有截断或遮挡。长时间游戏稳定性、第二台实体电脑和其他游戏版本尚未验证。

本次只发布汉化更新，不包含 Starship 飞船模组。安装包不含 KSA 游戏本体，不修改原版游戏文件。

独立代码与译文采用 MIT；字体采用 SIL OFL；加载器、Harmony 和私有 .NET Runtime 适用各自随附许可，详见 [许可证与来源](../THIRD_PARTY.md)。

## English

Partial Simplified Chinese UI preview for **KSA 2026.10.7.5541 on Windows x64**. Fully extract the offline installer and run Install.cmd. English fallback and original user names/IDs are preserved. All 34 language checks and 13 installer checks passed; complete screenshot verification of the new UI is pending. This release excludes game binaries and the Starship vessel mod.
