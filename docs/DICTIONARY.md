# 校对字典

字典把游戏原文、现有目标语言译文和使用场景放在一起，方便人工或其他 AI 校对。v0.6.0 提供简体中文和日语两套目录。

从 [v0.6.0 下载页](https://github.com/LiX-Works/KsaUiLanguages/releases/tag/v0.6.0) 获取 **KsaUiLanguages-0.6.0-dictionary.zip**。包内按目标语言分为 `zh-CN/` 和 `ja-JP/`：

- `zh-CN/review-catalog.zh-CN.json`、`ja-JP/review-catalog.ja-JP.json`：各语言的完整校对记录，适合提供给 AI。
- 对应的 `.csv`：相同记录的 UTF-8 BOM 表格视图，方便查看与筛选。
- 每个语言目录的 `校对说明.txt`：注明目标语言及备注来源，可以与该目录一起交给校对者。

本版支持5541和5554。字典的原生英文参考保留5541基线，部件XML按实际导出参数注明版本；支持范围来自独立检查，不能由字典数量推断。

每套目录各有 1625 条记录，其中 748 条附有作者填写的语义备注。日语目录复用 `reference/translation-context.zh-CN.json` 中的中文备注；它们用于说明界面位置、操作对象和容易混淆的意思，不是日语备注，也不是英语原文。JSON 的 `meta.optionalFields.remarks` 明确记录备注的 `language`、`source` 和 `sourceSha256`；本身清楚的条目可以留空。

planning 区段用于轨道规划和轨迹图，utility 区段用于存档、资源和对象清单等常用窗口。

controls 区段用于键位动作和赋值提示，与 ui 菜单区段分开：例如菜单中的 Camera Mode 是“相机模式”，对应键位动作则是“切换相机模式”。hud 区段用于仪表显示条件及布局管理；布局表头 Name 是“名称”，乘员表头的同词则是“姓名”。

## 怎样校对

先读 original、translation 和 remarks。作者备注用于理解语义，修改建议填写 proposed_translation 和 reviewer_notes，并用 entry_id 定位。

校对分两轮会更清楚：一轮核对技术含义，一轮检查目标语言是否自然、顺畅、贴近玩家的说法。日语应按原文含义及功能语境校对，中文语义备注仅辅助理解。分歧最终结合游戏界面和功能判断，不能只按票数决定。

占位符、内部键、部件型号、单位和常用缩写需要保留。部件的 `original_kind` 若是 `internal_id_fallback`，说明所提供 XML 中没有唯一明确的英文显示名，目标语言名称属于按用途拟名。5541 的中文命名依据仍单独注明来源；原生英文来自 5541 英文清单，部件英文显示名从实际提供的 Core XML 读取，不从中文译文或备注倒推。`partsXmlGameVersion` 记录所用 XML 的版本。

这些文件用于校对，不能直接放入 Locales 作为语言包。字典里还包括待接入或待验证的词条；coverage 字段说明当前限制。

## 重新导出

开发者在仓库根目录运行：

~~~powershell
python tools/export_dictionary.py --out-dir work/exports/dictionary/zh-CN
python tools/export_dictionary.py --locale ja-JP --out-dir work/exports/dictionary/ja-JP
~~~

`--locale` 可选 `zh-CN` 或 `ja-JP`，省略时仍导出简体中文。省略 `--out-dir` 时，中文沿用 `work/exports/dictionary-local`，日语使用其 `ja-JP/` 子目录。指定输出目录时，建议按语言分开，避免覆盖同名的校对说明。

需要 Python 和本地游戏安装，但不需要额外 Python 库。游戏不在默认位置时，增加 `--game-dir` 参数；使用 5554 的 Core XML 时，也应明确注明版本：

~~~powershell
python tools/export_dictionary.py --locale ja-JP --game-dir 'C:\Games\Kitten Space Agency' --parts-game-version 2026.10.10.5554 --out-dir work/exports/dictionary/ja-JP
~~~

正式译文来自 `assets/KsaUiLanguages/Locales/<locale>.json`；语义备注来自 `reference/translation-context.zh-CN.json`。导出记录所选词典的 SHA-256，并检查中日词典的 section/key 覆盖一致、条目 ID 唯一、占位符以及 JSON/CSV 往返一致性。英文模板和部件命名依据也保存在 `reference` 中，配合相应游戏 XML 即可重建导出流程。

英文原文与游戏内部 ID 用于定位和兼容；本项目的 MIT 许可不表示对游戏素材重新授权。
