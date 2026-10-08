# 校对字典

字典把游戏原文、现有中文和使用场景放在一起，方便人工或其他 AI 校对，也方便以后添加语言。

从 [v0.4.2 下载页](https://github.com/LiX-Works/KsaUiLanguages/releases/tag/v0.4.2) 获取 **KsaUiLanguages-0.4.2-dictionary.zip**。包内包含：

- review-catalog.zh-CN.json：完整校对记录，适合提供给 AI。
- review-catalog.zh-CN.csv：相同记录的表格视图，方便查看与筛选。
- 校对说明.txt：可以与字典一起交给校对者。

本版支持5541和5554。字典的原生英文参考保留5541基线，部件XML按实际导出参数注明版本；支持范围来自独立检查，不能由字典数量推断。

字典有 1625 条记录，其中 748 条有作者填写的语义备注。备注说明界面位置、操作对象或容易混淆的意思；本身清楚的条目可以留空。

planning 区段用于轨道规划和轨迹图，utility 区段用于存档、资源和对象清单等常用窗口。

controls 区段用于键位动作和赋值提示，与 ui 菜单区段分开：例如菜单中的 Camera Mode 是“相机模式”，对应键位动作则是“切换相机模式”。hud 区段用于仪表显示条件及布局管理；布局表头 Name 是“名称”，乘员表头的同词则是“姓名”。

## 怎样校对

先读 original、translation 和 remarks。作者备注用于理解语义，修改建议填写 proposed_translation 和 reviewer_notes，并用 entry_id 定位。

校对分两轮会更清楚：一轮核对技术含义，一轮检查中文是否自然、顺畅、贴近玩家的说法。分歧最终结合游戏界面和功能判断，不能只按票数决定。

占位符、内部键、部件型号、单位和常用缩写需要保留。部件的 original_kind 若是 internal_id_fallback，说明原版没有明确英文显示名，中文属于按用途拟名。

这些文件用于校对，不能直接放入 Locales 作为语言包。字典里还包括待接入或待验证的词条；coverage 字段说明当前限制。

## 重新导出

开发者在仓库根目录运行：

~~~powershell
python tools/export_dictionary.py --out-dir work/exports/dictionary
~~~

需要 Python 和本地游戏安装，但不需要额外 Python 库。游戏不在默认位置时，增加 --game-dir 参数：

~~~powershell
python tools/export_dictionary.py --game-dir 'C:\Games\Kitten Space Agency' --out-dir work/exports/dictionary
~~~

正式译文来自 assets/KsaUiLanguages/Locales/zh-CN.json；语义备注来自 reference/translation-context.zh-CN.json。英文模板和部件命名依据也保存在 reference 中，公开源码即可重建导出流程。

英文原文与游戏内部 ID 用于定位和兼容；本项目的 MIT 许可不表示对游戏素材重新授权。
