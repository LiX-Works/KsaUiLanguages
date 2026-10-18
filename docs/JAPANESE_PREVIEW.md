# 日文测试版

在现有汉化框架上增加日本語，覆盖现有词典的 **1,625 条记录**。原包尚未接入的英文界面仍然保留。这些译文已纳入 [v0.6.0 发布版](https://github.com/LiX-Works/KsaUiLanguages/releases/tag/v0.6.0)。下文保留本地测试入口和验证范围；其他用户请按仓库 README 下载正式包。

## 这台电脑怎么用

关闭其他 KSA 实例后，从桌面的 **KSA 日文测试（本地）** 启动。它使用独立的 5554 游戏测试目录、设置和存档。

启动后默认显示日文。**Language → 日本語 / 简体中文 / English** 可以即时切换；菜单里的选择只对本次运行生效。

## 使用插件 ZIP

本地生成文件位于 `work/japanese-preview/KsaUiLanguages-japanese-preview-build5541-5554.zip`，适用于已配置 **StarMap 0.4.7** 的 Windows x64 用户。支持的游戏版本仍是 **2026.10.7.5541、2026.10.10.5554**。

1. 关闭游戏，备份实例目录中的 `mods/KsaUiLanguages` 文件夹。
2. 用 ZIP 中的整个 `KsaUiLanguages` 文件夹替换它，保留原 `manifest.toml` 中启用插件的条目。
3. 从原来的 StarMap 入口启动。

ZIP 默认语言是 `ja-JP`。仓库的 `assets/KsaUiLanguages/language.json` 仍默认中文；日文词典位于 `assets/KsaUiLanguages/Locales/ja-JP.json`。直接修改词典后，需要重新构建字体，确保新增字符能够显示。

## 实现与检查

复用已有语言加载、显示替换和语言菜单。新增日文词典、补充共用字体字符，并让欢迎页支持假名换行和日文标点；没有增加游戏行为补丁。

- 11 个词典区段与中文键集合一致，格式参数、标记顺序和边界空格检查通过。
- 共用 Noto Sans SC 子集包含 1,677 个码位，556,308 字节，覆盖目前三个语言包需要的字符。暂未单独配置日文字形字体。
- 同一份插件在 5541、5554 上各通过 59 项检查，包括原有中文回归、日文原生文本、三语言往返、真实 ImGui 控件 ID、欢迎页热键和换行。
- 5554 实际显示了日文启动配置和加载画面，日志到达 `Application Initialization Complete` 和 `Controlling 'Rocket'`，标准错误为空，未发现异常堆栈。用户随后提供的截图进一步确认：飞行场景和日文欢迎页已经显示，欢迎段落能够换行。初版资源栏中的「電気エネルギー」较长，v0.6.0 已缩短为「電気」；缩短后尚未重新实机抽查。
- Computer Use 截图两次返回 `FrameArrived timed out: timed out waiting on channel`；未收到用户主动停止提示。欢迎页和飞行场景的显示证据来自用户截图，飞行操作和游戏内三语言菜单切换尚未完成实机检查。

这是初步日文翻译，仍需结合实机反馈校对措辞和窄控件中的长文字；检查通过不代表所有界面或长期稳定性已经全面验证。
