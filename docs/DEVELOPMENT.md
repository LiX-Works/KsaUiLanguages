# 构建、检查与字体

## 插件构建

需要 Windows x64、.NET 10 SDK、KSA **2026.10.7.5541** 的合法本地安装，以及解压后的 **StarMap 0.4.7**。本仓库不会下载或分发游戏 DLL。

在仓库根目录运行 PowerShell：

~~~powershell
.\tools\build.ps1 -GameDir 'C:\Games\Kitten Space Agency' -LoaderDir 'C:\Tools\StarMap-0.4.7'
~~~

上面路径是示例，请换成自己的路径。脚本优先使用本仓库 tools/dotnet-sdk/dotnet.exe（若存在），否则使用 PATH 中的 dotnet；也可通过 -Dotnet 指定可执行文件。

构建输出位于 work/build/KsaUiLanguages。游戏和加载器引用均为 Private=false，不复制到产物中。Release 插件不生成 PDB，避免在发行 DLL 中保留开发机路径。

## 检查

~~~powershell
.\tools\build.ps1 -GameDir 'C:\Games\Kitten Space Agency' -LoaderDir 'C:\Tools\StarMap-0.4.7' -Check
~~~

脚本构建并运行 LanguageChecks，结果写到 work/run-logs/language-checks.json。检查使用实际游戏 LString 对象，以及游戏 imgui.dll 的 ImHashStr 导出，涵盖中英切换、占位符、按钮标题与 ID、设置下拉的作用范围、动态对象名称及卸载回退。

检查程序通过独立目录重定向玩家文件位置，不启动图形游戏。它不是完整的游戏操作测试，也不能证明长时间稳定性。

检查程序支持 KSA_PROJECT_ROOT、KSA_GAME_DIR、KSA_LOADER_DIR 环境变量；build.ps1 在调用时设置并在完成后恢复这些进程变量。可另用 KSA_PLUGIN_DIR 指定实际安装的插件目录，验证其语言包。

## 打包

完成构建后：

~~~powershell
.\tools\package.ps1
~~~

生成 dist/KsaUiLanguages-0.3.0-build5541.zip 和 SHA256SUMS.txt。打包使用新的 staging 目录与文件白名单，拒绝游戏/加载器 DLL。

若需要与当前 Git 提交严格对应的源码包，先提交所有准备发行的更改，再运行：

~~~powershell
.\tools\package.ps1 -IncludeSource
~~~

这会使用 git archive HEAD 导出源码；有已跟踪但未提交的修改时拒绝源码打包。GitHub 标签应指向同一提交。

## 可选：重建字体

普通插件构建直接使用仓库内已生成的字体，不需要 Python。增加语言字符时再进行此步骤。

1. 从 [Google Fonts / Noto Sans SC](https://github.com/google/fonts/tree/main/ofl/notosanssc) 取得 NotoSansSC[wght].ttf，保存为 assets/NotoSansSC-variable.ttf。该路径被 Git 忽略。
2. 为复现本次字体，使用 SHA-256 为 a3041811a78c361b1de50f953c805e0244951c21c5bd412f7232ef0d899af0da 的源文件。上游更新后的文件应重新验证并更新来源记录。
3. 使用项目虚拟环境或本地依赖目录安装 fontTools，不必全局安装：

~~~powershell
python -m pip install --target tools/python-libs -r tools/requirements-font.txt
python tools/build_ui_font.py
~~~

生成脚本读取各语言包，实例化 weight 400，选取所需字符，更新家族、样式和 PostScript 名称，同时保留上游版权及 OFL 元数据。提交新子集时保留 FONT-LICENSE.txt 和 FONT-SOURCE.txt。

## 当前验证记录

v0.3.0 的 16 项检查通过，包括实际对象、原始方法指令、原生 ImGui 哈希和所有 Harmony 补丁的安装。用户实机截图确认了初版编辑器工具栏、分类、发射字段与部件读数；最后的树状符号、发射预览和推进剂译名修正待视觉复核。

此前检查过主菜单、暂停菜单、图形设置、显示模式下拉、资源栏、中英文往返及 ORBT 中文悬停说明。没有验证所有按钮和长时间游戏行为。
