# 贡献指南 / Contributing

欢迎通过 Issue 报告漏译或用 Pull Request 贡献翻译、文档及小范围修复。请附游戏版本、插件版本、原文、出现位置和复现步骤；截图请先检查是否含个人信息。

## 译文

编辑 assets/KsaUiLanguages/Locales/zh-CN.json，保留 JSON 键和所有占位符，例如 {Target}、{0} 和格式说明。优先使用准确、简短的中文。术语策略见 reference/terminology-policy.json。

| 字段 | 用途 |
| --- | --- |
| native | 游戏原生 LString ID 对应的译文 |
| literals | 在已限定的方法中匹配的字符串 |
| ui | 已限定的菜单、设置选项与资源名称 |
| tooltips | 悬停说明 |

加入 JSON 键并不会自动覆盖每个游戏界面。代码补丁还需命中对应的绘制位置；不要扩大为对所有 ImGui 文本或飞船名称的全局替换。

## 新语言

复制语言包结构，使用新的 locale 和 displayName，保留四个词典，按需增加条目。缺失译文会回退到原文。新增字符后须重建字体子集，或提供有再分发许可的新字体，并更新 fontFile、许可证和来源说明。

当前框架只以简体中文和英文进行验证；字体切换、复杂文字整形、从右向左布局还需要单独评估。不要直接宣称某个新语言已完整兼容。

## 代码与验证

按照 docs/DEVELOPMENT.md 构建。涉及语言行为、控件 ID 或占位符的修改应运行 -Check，并在游戏里检查相应界面；记录实际验证范围。

提交自己的原创内容或具有兼容许可的内容，保留第三方署名。不要提交游戏 DLL、反编译文件、安装器、玩家配置、存档、认证信息或个人日志。

By contributing original code/translations, you agree to license your contribution under this project's MIT license. Fonts and other third-party assets retain their own licenses.
