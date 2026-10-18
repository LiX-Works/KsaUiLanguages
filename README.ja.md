# KSA UI Languages

[![Release](https://img.shields.io/github/v/release/LiX-Works/KsaUiLanguages?include_prereleases&label=release)](https://github.com/LiX-Works/KsaUiLanguages/releases) ![KSA builds 5541 and 5554](https://img.shields.io/badge/KSA-5541%20%7C%205554-blue) ![Windows x64](https://img.shields.io/badge/platform-Windows%20x64-lightgrey) [![License: MIT](https://img.shields.io/badge/license-MIT-green)](LICENSE)

[English](README.md) · [简体中文](README.zh-CN.md) · **日本語**

**Kitten Space Agency の多言語 UI MOD。日本語や簡体字中国語でプレイでき、ゲーム内でいつでも言語を切り替えられます。**

Kitten Space Agency は、実際の物理に基づいたハードコアな宇宙飛行シミュレーターです。ロケットを自分で設計し、本物の軌道力学に従って飛ばします。始めるのは難しくありませんが、挑戦を好むプレイヤーにとって上限はほとんどありません。

![KSA の飛行画面。簡体字中国語 UI で、English・日本語・简体中文 を並べた Language メニューを開いた状態](docs/images/language-menu-zh-CN.webp)

*飛行中の画面（簡体字中国語 UI）。Language メニューから English・日本語・简体中文 を切り替えられます。*

## 特長

- **ワンクリックで言語を切り替え**: ゲーム内のメニューで English・日本語・简体中文 を選べます。選んだ言語は記憶されます。
- **オフラインで簡単にインストール**: インストーラー 1 つで完了し、管理者権限も追加のダウンロードも不要です。
- **セーブデータは安全**: 更新しても設定とセーブデータは保持され、設定ファイルは自動でバックアップされます。
- **丁寧な翻訳**: 各言語 1,625 項目を意味に沿って校正。RCS や Δv など、プレイヤーになじみのある表記はそのまま残しています。
- **リリースごとに**、対応する各ゲームバージョンでテストしています。

## ダウンロードとインストール

**最新版: v0.9.0 プレビュー版**（Windows x64、KSA 2026.10.7.5541 / 2026.10.10.5554 対応）。[v0.9.0 をダウンロード](https://github.com/LiX-Works/KsaUiLanguages/releases/tag/v0.9.0)し、**KsaUiLanguages-0.9.0-installer6-win-x64.zip** を選んでください。

1. 上記の対応バージョンの KSA を用意し、通常どおり起動できることを確認します。
2. ZIP をすべて展開し、ゲームを終了してから **Install.cmd** を実行します。**KSA.dll** があるゲームフォルダーを選択してください。
3. デスクトップの **「KSA UI Languages (5554)」** または **「KSA UI Languages (5541)」** から起動します。名前は選択したゲームのバージョンによって変わります。
4. ゲーム内で **Language → English / 日本語 / 简体中文** から言語を選択します。選んだ言語は次回の起動時にも使われます。

インストーラーの案内は英語です。同梱ライセンスに同意する場合は、確認画面で **Yes** を選んでください。Install.ps1 をダブルクリックするとテキストエディターで開く場合があるため、Install.cmd を使ってください。

**更新**: 新しいインストーラーを実行し、元のゲームフォルダーを選びます。設定、セーブデータ、起動時の言語は保持されます。設定ファイルと旧プラグインはバックアップされますが、セーブデータは自動でバックアップされません。ゲームのバージョン（5541 または 5554）ごとに専用の環境を作成し、ゲーム本体は変更しません。[インストール・更新・アンインストールの詳細（英語）](docs/INSTALLER.en.md)

各バージョンの変更点は [CHANGELOG](CHANGELOG.md) をご覧ください。

## 対応言語

- **3 言語**: 日本語と簡体字中国語、そして元の英語。設定、機体エディター、主な飛行ウィンドウと HUD、計画、軌跡、セーブ画面が翻訳の対象です。まだ翻訳されていない部分は英語のままです。
- **なじみのある表記を保持**: RCS、EVA、Isp、TWR、HUD、Δv などの略語、単位、キー割り当て、パーツの型番、機体に付けた名前はそのまま残します。

## 既知の制限

- 一部の画面やラベルは英語のままです。
- 長いラベルやセーブ名は途中で切れる場合があります。
- 日本語は中国語と共通のフォントで表示するため、一部の漢字が中国語の字形になります。
- すべての操作や長時間のプレイはまだ検証していません。[リリースノートの詳細（英語）](docs/RELEASE_NOTES_v0.9.0.md)

## 仕組み

この MOD はゲームファイルを書き換えず、ゲームの実行中に画面上の文字を差し替えます。そのため、ゲームフォルダーはそのまま保たれます。ビルドとテストの詳細は [開発・ビルド](docs/DEVELOPMENT.md) をご覧ください。

## フィードバック・参加・ライセンス

誤訳や不自然な訳を見つけたら、[issue を作成](https://github.com/LiX-Works/KsaUiLanguages/issues)してください。翻訳への参加は [翻訳への参加](CONTRIBUTING.md) をご覧ください。参考資料：[校正用辞書（中国語）](docs/DICTIONARY.md) · [パーツ名の検討記録（中国語）](docs/PART_NAMING_REVIEW.md) · [バージョン番号の方針（中国語）](docs/VERSIONING.md)。

非公式のコミュニティ製 MOD です。**KSA 本体と Starship 機体 MOD は含みません。** 独自のコードと訳文は **MIT**、フォントは **SIL OFL** です。ローダーとランタイムには、それぞれ同梱のライセンスが適用されます。[ライセンスと出典](THIRD_PARTY.md)
