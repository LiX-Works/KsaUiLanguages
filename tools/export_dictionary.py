#!/usr/bin/env python3
"""Export the current Chinese locale as a review catalog and plain CSV."""

from __future__ import annotations

import argparse
import collections
import csv
import hashlib
import json
import re
import sys
import xml.etree.ElementTree as ET
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
DEFAULT_GAME_DIR = Path(r"C:\Program Files\Kitten Space Agency")
DEFAULT_OUT_DIR = ROOT / "work" / "exports" / "dictionary-local"
LOCALE_FILE = ROOT / "assets" / "KsaUiLanguages" / "Locales" / "zh-CN.json"
INVENTORY_FILE = ROOT / "reference" / "native-source-5541.json"
SEMANTIC_CONTEXT_FILE = ROOT / "reference" / "translation-context.zh-CN.json"
CSPROJ_FILE = ROOT / "src" / "KsaUiLanguages" / "KsaUiLanguages.csproj"
NAMING_NOTES_FILE = ROOT / "reference" / "part-name-evidence-5541.json"

PLACEHOLDER_RE = re.compile(r"\{[^{}]+\}")
VERSION_RE = re.compile(r"<Version>\s*([^<]+?)\s*</Version>")
SECTION_ORDER = ("native", "literals", "ui", "tooltips", "editor", "startup", "parts", "controls", "hud", "planning", "utility")
COVERAGE = {
    "native": "resource mapped, per-entry visual not verified",
    "ui": "limited hooks, per-entry not verified",
    "literals": "limited hooks, per-entry not verified",
    "tooltips": "limited hooks, per-entry not verified",
    "editor": "mixed: some integrated / some candidates; per-entry review needed",
    "startup": "startup screen visually checked generally; per-entry options not all operated",
    "parts": "display-only mappings; fallback original preserved; visual check pending",
    "controls": "settings action captions and binding-popup text; binding values untouched; per-entry visual not all verified",
    "hud": "HUD layout and context-window captions; layout names, canvas IDs and visibility settings untouched; per-entry visual not all verified",
    "planning": "flight and transfer planning window captions; calculations and plan identifiers unchanged; visual coverage recorded separately",
    "utility": "save/load, resources and object-window captions; saved names, storage keys and runtime values unchanged; visual coverage recorded separately",
}
PRESERVE_TERMS = (
    "RCS", "Δv", "TWR", "Isp", "EVA", "HUD", "IVA", "SOI",
    "STAR", "SURF", "ORBT", "OVEL", "TGT", "BURN", "ASMB",
    "X", "Y", "Z", "2 X", "3 X", "4 X", "6 X", "8 X",
)
KNOWN_FRAGMENT_KEYS = {
    "Nozzle on ", "Segment '", "' on ", "Volume - ", "Chutes on '",
    "Tank '", "Decoupler '", "Seat ", "Add ", " s", "Time: ",
    " s\nThrust: ", "\nIsp: ", " s\nChamber Pressure: ",
    " (grain cross-section: ", " (Existing Vehicle)",
}
CSV_COLUMNS = (
    "entry_id", "section", "key", "original", "originalSourceStatus", "original_kind",
    "translation", "context", "coverage", "placeholders", "retain_terms", "known_fragment",
    "placeholder_status", "review_status", "proposed_translation", "reviewer_notes", "notes",
    "remarks",
)
CSV_HEADER_LABELS = {"remarks": "备注"}


def read_json(path: Path):
    with path.open("r", encoding="utf-8-sig") as stream:
        return json.load(stream)


def local_name(tag: str) -> str:
    return tag.rsplit("}", 1)[-1]


def game_core_dir(game_dir: Path) -> Path:
    for candidate in (game_dir / "Content" / "Core", game_dir / "Core", game_dir):
        if candidate.is_dir() and any(candidate.rglob("*.xml")):
            return candidate
    raise FileNotFoundError(f"No Content/Core XML files found under: {game_dir}")


def load_part_names(core_dir: Path):
    """Read direct Assets children; PartGameData DisplayName overrides Part."""
    assets, game_data = {}, {}
    xml_files = sorted(core_dir.rglob("*.xml"))
    for xml_path in xml_files:
        root = ET.parse(xml_path).getroot()
        for element in list(root):
            kind = local_name(element.tag)
            part_id = element.get("Id")
            if not part_id or kind not in {"Part", "PartGameData"}:
                continue
            record = {"display": (element.get("DisplayName") or "").strip(),
                      "file": xml_path.name}
            (assets if kind == "Part" else game_data).setdefault(part_id, []).append(record)

    names = {}
    conflicts = []
    for part_id in sorted(assets.keys() | game_data.keys()):
        # Only nonempty natural-language DisplayName attributes count as explicit names.
        data_names = {r["display"] for r in game_data.get(part_id, []) if r["display"]}
        asset_names = {r["display"] for r in assets.get(part_id, []) if r["display"]}
        candidates = data_names or asset_names
        if len(candidates) > 1:
            conflicts.append(part_id)
            names[part_id] = {"original": part_id, "kind": "internal_id_fallback",
                              "file": "", "conflict": True}
            continue
        if candidates:
            display = next(iter(candidates))
            records = game_data.get(part_id, []) if data_names else assets.get(part_id, [])
            names[part_id] = {"original": display, "kind": "explicit_display_name",
                              "file": records[0]["file"], "conflict": False}
        else:
            records = game_data.get(part_id, []) or assets.get(part_id, [])
            names[part_id] = {"original": part_id, "kind": "internal_id_fallback",
                              "file": records[0]["file"] if records else "", "conflict": False}

    prefab_assets = {key for key in assets if "_Prefab_" in key}
    prefab_data = {key for key in game_data if "_Prefab_" in key}
    prefab_ids = prefab_assets | prefab_data
    return names, {
        "source": "Installed Content/Core/**/*.xml; direct Assets child Part/@Id is the asset definition; matching PartGameData/@Id overlays it, and a nonempty PartGameData/@DisplayName overrides Part/@DisplayName.",
        "xml_file_count": len(xml_files),
        "prefab_part_asset_count": len(prefab_assets),
        "prefab_part_game_data_count": len(prefab_data),
        "prefab_id_intersection_count": len(prefab_assets & prefab_data),
        "prefab_ids_without_game_data": sorted(prefab_assets - prefab_data),
        "prefab_game_data_without_part": sorted(prefab_data - prefab_assets),
        "prefab_explicit_display_name_count": sum(names.get(key, {}).get("kind") == "explicit_display_name" for key in prefab_ids),
        "prefab_internal_id_fallback_count": sum(names.get(key, {}).get("kind") == "internal_id_fallback" for key in prefab_ids),
        "additional_nonprefab_part_records": sorted((assets.keys() | game_data.keys()) - prefab_ids),
        "conflicting_display_name_ids": conflicts,
        "count_note": "These counts describe the installed Core XML naming source, not the number of translated entries or proof of in-game coverage.",
    }


def index_inventory(inventory):
    candidates = collections.defaultdict(set)
    groups = collections.defaultdict(set)
    for item in inventory.get("entries", []):
        ident, english = item.get("id"), item.get("english")
        if ident and isinstance(english, str) and english:
            candidates[ident].add(english)
            if item.get("group"):
                groups[ident].add(item["group"])
    return candidates, groups


def index_naming_notes(path: Path):
    """Best-effort index of human naming evidence; never use it as English source."""
    if not path.is_file():
        return {}
    try:
        data = read_json(path)
    except (OSError, ValueError):
        return {}
    result = {}
    id_fields = ("id", "part_id", "partId", "template_id", "templateId", "key")
    note_fields = ("note", "notes", "rationale", "reason", "evidence", "source", "context", "naming_note", "namingNote")

    def visit(value):
        if isinstance(value, dict):
            ident = next((value.get(k) for k in id_fields if isinstance(value.get(k), str)), None)
            notes = [str(value[k]).strip() for k in note_fields if isinstance(value.get(k), str) and value[k].strip()]
            if ident and notes:
                result.setdefault(ident, " ".join(notes)[:500])
            for child in value.values():
                visit(child)
        elif isinstance(value, list):
            for child in value:
                visit(child)

    visit(data)
    return result


def has_term(text: str, term: str) -> bool:
    if term in {"X", "Y", "Z"}:
        return bool(re.search(rf"(?<![A-Za-z0-9]){re.escape(term)}(?![A-Za-z0-9])", text))
    return bool(re.search(rf"(?<![A-Za-z0-9]){re.escape(term)}(?![A-Za-z0-9])", text))


def context_for(section: str, native_item=None) -> str:
    if section == "native":
        group = (native_item or {}).get("group", "unknown")
        return f"Native game string; inventory group: {group}."
    return {
        "literals": "Fixed UI literal or button caption; hook coverage is limited.",
        "ui": "Menu, window, or HUD label keyed by its English display text.",
        "controls": "Controls settings action name or key-assignment popup caption; actual bindings and config keys unchanged.",
        "hud": "HUD layouts or gauge visibility conditions; names, storage IDs and context values remain original.",
        "planning": "Flight plan, maneuver and transfer planning UI; names and calculation inputs remain original.",
        "utility": "Save/load, resource or universe object UI; persisted identifiers and user names remain original.",
        "tooltips": "Tooltip text keyed by its English source string.",
        "editor": "Vehicle editor or part-editor label/literal; some entries may be fragments.",
        "startup": "Startup/setup screen label or option; options were not all operated individually.",
        "parts": "Part display name keyed by a template ID; see XML name-source status.",
    }.get(section, "Locale dictionary entry; per-entry context and coverage need review.")


def build_entries(locale, inventory, part_names, naming_notes, semantic_context):
    source_candidates, source_groups = index_inventory(inventory)
    entries, placeholder_mismatches = [], []
    sections = [s for s in SECTION_ORDER if isinstance(locale.get(s), dict)]
    sections.extend(s for s, value in locale.items() if isinstance(value, dict) and s not in sections)
    for section in sections:
        for key, translation in locale[section].items():
            key, translation = str(key), str(translation)
            source_item = None
            original_status = "section_key_is_english_source"
            original_kind = None
            notes = []
            if section == "native":
                found = source_candidates.get(key, set())
                if len(found) == 1:
                    original = next(iter(found))
                    original_status = "inventory_match"
                    group = sorted(source_groups.get(key, set()))
                    source_item = {"group": ", ".join(group) if group else "unknown"}
                else:
                    original = ""
                    original_status = "missing"
                    notes.append("No unique English source found in native-source-5541.json; do not infer one from the key.")
            elif section == "parts":
                record = part_names.get(key)
                if record and record["kind"] == "explicit_display_name":
                    original = record["original"]
                    original_status = "explicit_display_name"
                    original_kind = "explicit_display_name"
                    if record.get("file"):
                        notes.append(f"English name source: Content/Core/{record['file']}.")
                else:
                    original = key
                    original_status = "internal_id_fallback"
                    original_kind = "internal_id_fallback"
                    notes.append("Original falls back to the template ID; the Chinese label is an independent proposed name, not a translation from natural-language English.")
                    if record and record.get("conflict"):
                        notes.append("Conflicting XML DisplayName values found; ID fallback used instead of guessing.")
                    elif record and record.get("file"):
                        notes.append(f"No DisplayName found; ID read from Content/Core/{record['file']}.")
                    else:
                        notes.append("Template ID was not found in the installed Core XML corpus.")
                proposal = naming_notes.get(key)
                if proposal:
                    notes.append("Independent naming evidence (not English source): " + proposal)
            else:
                original = key

            tokens = sorted(PLACEHOLDER_RE.findall(original))
            translation_tokens = sorted(PLACEHOLDER_RE.findall(translation))
            placeholder_status = "match" if collections.Counter(tokens) == collections.Counter(translation_tokens) else ("source_missing" if original_status == "missing" else "mismatch")
            if placeholder_status == "mismatch":
                placeholder_mismatches.append({"entry_id": None, "section": section, "key": key,
                                               "original": tokens, "translation": translation_tokens})
                notes.append("Placeholder token multiset differs; preserve each source token exactly.")

            known_fragment = key in KNOWN_FRAGMENT_KEYS or key != key.strip(" \t\r\n")
            if known_fragment:
                notes.append("KNOWN_FRAGMENT: this key is a source string fragment; preserve its adjacent spacing and punctuation.")
            retain_terms = [term for term in PRESERVE_TERMS if has_term(key + " " + original + " " + translation, term)]
            if section == "parts":
                retain_terms.insert(0, key)
            entry_id = f"native.{key}" if section == "native" else f"{section}:{hashlib.sha256(key.encode('utf-8')).hexdigest()[:12]}"
            entry = {
                "entry_id": entry_id,
                "section": section,
                "key": key,
                "original": original,
                "originalSourceStatus": original_status,
                "original_kind": original_kind,
                "translation": translation,
                "context": context_for(section, source_item),
                "coverage": COVERAGE.get(section, "limited hooks, per-entry not verified"),
                "placeholders": tokens,
                "retain_terms": retain_terms,
                "known_fragment": known_fragment,
                "placeholder_status": placeholder_status,
                "review_status": "unreviewed",
                "proposed_translation": None,
                "reviewer_notes": "",
                "notes": " ".join(notes),
                "remarks": semantic_context.get(section, {}).get(key, ""),
            }
            if section == "native" and len(source_candidates.get(key, set())) > 1:
                entry["notes"] += " Multiple different English candidates share this ID."
            entries.append(entry)
    for item in placeholder_mismatches:
        item["entry_id"] = next((e["entry_id"] for e in entries if e["section"] == item["section"] and e["key"] == item["key"]), "")
    return entries, placeholder_mismatches


def csv_value(entry, column):
    value = entry[column]
    if column in {"placeholders", "retain_terms"}:
        return json.dumps(value, ensure_ascii=False, separators=(",", ":"))
    if column == "proposed_translation" and value is None:
        return ""
    if value is None:
        return ""
    if isinstance(value, bool):
        return "true" if value else "false"
    return str(value)


def write_and_check_csv(path: Path, entries):
    with path.open("w", encoding="utf-8-sig", newline="") as stream:
        writer = csv.DictWriter(stream, fieldnames=[CSV_HEADER_LABELS.get(c, c) for c in CSV_COLUMNS], extrasaction="raise", lineterminator="\r\n")
        writer.writeheader()
        for entry in entries:
            writer.writerow({CSV_HEADER_LABELS.get(column, column): csv_value(entry, column) for column in CSV_COLUMNS})
    roundtrip = []
    with path.open("r", encoding="utf-8-sig", newline="") as stream:
        for row in csv.DictReader(stream):
            header_keys = {label: key for key, label in CSV_HEADER_LABELS.items()}
            item = {header_keys.get(key, key): value for key, value in row.items()}
            item["placeholders"] = json.loads(item["placeholders"])
            item["retain_terms"] = json.loads(item["retain_terms"])
            item["known_fragment"] = item["known_fragment"] == "true"
            item["original_kind"] = item["original_kind"] or None
            item["proposed_translation"] = item["proposed_translation"] or None
            roundtrip.append(item)
    if roundtrip != entries:
        raise ValueError("CSV round-trip differs from the canonical JSON entries.")
    return len(roundtrip)


def write_plain_text(path: Path):
    content = """多语言词典校对说明

这份校对目录由 KSA 简体中文源词典导出，JSON 是规范记录，CSV 是相同条目的 UTF-8 BOM 平面视图，方便复制、筛选或供其他 AI 阅读。它们不是可直接安装的插件语言包，也不取代 assets/KsaUiLanguages/Locales/zh-CN.json；源词典仍是唯一正式数据源。导出中的 key、entry_id、original、section、coverage、placeholders、retain_terms 和 notes 用于定位与审计，请勿改写。remarks（CSV 对应“备注”栏）由项目作者按已知界面与功能填写，用于说明出现位置、操作对象和多义词语境；本身清楚的条目可留空。校对者应先阅读这些语义备注，再把修改建议填入 proposed_translation 和 reviewer_notes，逐条按 entry_id 返回；不要直接覆盖 translation 或作者备注。建议技术评审与中文写作评审分开：前者核实原文含义、功能、参数与模型事实，后者检查自然表达、玩家语感、词语选择与语气一致性。争议由主审按源码/界面上下文裁定，不按意见数量投票。建议返回格式：{"changes":[{"entry_id":"...","proposed_translation":"...","reason":"...","confidence":"high|medium|low"}]}。如发现作者备注不足或存在疑问，可在 reviewer_notes 中指出，供主审补核。无需修改的条目可以省略。

“信”是准确呈现技术含义、用途、参数和模型事实；“达”是让中文易懂、顺畅、指向明确；“雅”是地道自然、有玩家语感、选词恰当且语气一致。长度没有固定上限：按界面空间调整，但不要为了短而省掉必要语义。避免生硬直译，也避免为求文采加入原文没有的含义。先判断术语在此处的实际功能，再建议译法。相同英文可能在不同 section 承担不同用途，不能合并。native 的英文原文仅由 5541 文本清单按 ID 对应；originalSourceStatus 为 missing 时不要根据 ID 猜原文。parts 的 key 是 prefab/template ID，不能翻译或重排；若 originalSourceStatus 为 internal_id_fallback，original 只是内部 ID，当前中文名称是独立拟名，不是从自然英文翻译来的。可用随条目提供的命名依据辅助审阅，但不能把它当作英语原文。

所有大括号占位符必须原样、完整、大小写一致，数量也要一致。保留控制标识、X/Y/Z、倍数、部件型号/编号、数字、单位符号和格式；常见缩写 RCS、Δv、TWR、Isp、EVA、HUD、IVA，以及 STAR、SURF、ORBT、OVEL、TGT、BURN 等按 terms 规则保留。ORBIT 要结合上下文：编辑器相机模式表示环绕相机，不是飞行轨道。部件读数的冒号用于两列排版，保留 ASCII 冒号与布局空格；不要改成全角冒号。标记 KNOWN_FRAGMENT 的条目是运行时拼接片段，邻接空格、标点及动态名称/ID 都要保留。

coverage 是谨慎的集成状态说明，不表示每个条目均已在游戏内验证；字典中存在一个 key 也不等于该字符串一定被运行时 hook 覆盖。placeholder_status 或 fragment 标记是校对线索，不代表插件运行失败。构建其他语言时，应基于有来源的 original 翻译；缺少英语原文、功能上下文或术语依据时应标注待核，不要编造。
"""
    path.write_text(content, encoding="utf-8", newline="\n")


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--out-dir", type=Path, default=DEFAULT_OUT_DIR, help="Output directory (default: work/exports/dictionary-local).")
    parser.add_argument("--game-dir", type=Path, default=DEFAULT_GAME_DIR, help="KSA install directory; may also point directly to Content/Core.")
    args = parser.parse_args()

    locale = read_json(LOCALE_FILE)
    inventory = read_json(INVENTORY_FILE)
    game_version = inventory.get("game_version", "unknown")
    version_match = VERSION_RE.search(CSPROJ_FILE.read_text(encoding="utf-8-sig"))
    if not version_match:
        raise ValueError(f"Could not read plugin <Version> from {CSPROJ_FILE.name}.")
    plugin_version = version_match.group(1)
    core_dir = game_core_dir(args.game_dir)
    part_names, parts_basis = load_part_names(core_dir)
    naming_notes = index_naming_notes(NAMING_NOTES_FILE)
    semantic_context = read_json(SEMANTIC_CONTEXT_FILE).get("sections", {}) if SEMANTIC_CONTEXT_FILE.is_file() else {}
    entries, placeholder_mismatches = build_entries(locale, inventory, part_names, naming_notes, semantic_context)

    ids = [entry["entry_id"] for entry in entries]
    if len(ids) != len(set(ids)):
        raise ValueError("Generated entry_id values are not unique.")
    args.out_dir.mkdir(parents=True, exist_ok=True)
    csv_path = args.out_dir / "review-catalog.zh-CN.csv"
    json_path = args.out_dir / "review-catalog.zh-CN.json"
    text_path = args.out_dir / "校对说明.txt"
    csv_rows = write_and_check_csv(csv_path, entries)
    if csv_rows != len(entries):
        raise ValueError("CSV row count does not match JSON entry count.")

    parts_basis["locale_parts_mapped_count"] = len(locale.get("parts", {})) if isinstance(locale.get("parts"), dict) else 0
    parts_basis["locale_part_ids_not_found_in_xml"] = sorted(
        key for key in (locale.get("parts", {}) if isinstance(locale.get("parts"), dict) else {}) if key not in part_names
    )
    source_files = [
        "assets/KsaUiLanguages/Locales/zh-CN.json",
        "reference/native-source-5541.json",
        "src/KsaUiLanguages/KsaUiLanguages.csproj",
        "game install: Content/Core/**/*.xml (resolved from --game-dir)",
    ]
    if NAMING_NOTES_FILE.is_file():
        source_files.append("reference/part-name-evidence-5541.json (naming notes only; not English source)")
    if SEMANTIC_CONTEXT_FILE.is_file():
        source_files.append("reference/translation-context.zh-CN.json (author-supplied semantic notes)")
    missing_native = sum(entry["section"] == "native" and entry["originalSourceStatus"] == "missing" for entry in entries)
    meta = {
        "schemaVersion": "1.0",
        "gameVersion5541": game_version,
        "pluginVersion": plugin_version,
        "locale": locale.get("locale", "zh-CN"),
        "sourceFiles": source_files,
        "optionalFields": {
            "remarks": {
                "label": "备注",
                "default": "",
                "description": "Optional author-supplied UI location, affected object/function and semantic disambiguation; obvious entries may be blank. Review suggestions belong in reviewer_notes.",
            }
        },
        "parts_name_basis": parts_basis,
        "validation": {
            "entry_count": len(entries),
            "entry_ids_unique": True,
            "csv_row_count": csv_rows,
            "csv_round_trip": "passed",
            "native_missing_source_count": missing_native,
            "placeholder_mismatch_count": len(placeholder_mismatches),
            "placeholder_mismatches": placeholder_mismatches,
            "known_fragment_count": sum(entry["known_fragment"] for entry in entries),
            "author_semantic_remarks_count": sum(bool(entry["remarks"]) for entry in entries),
            "note": "Placeholder findings are comparison flags only; they do not establish runtime hook support or failure.",
        },
    }
    terms = {
        "preserve_exact": list(PRESERVE_TERMS),
        "preserve_all_unit_symbols_and_numeric_format_suffixes": True,
        "preserve_placeholders_control_ids_template_ids_part_model_numbers_and_ascii_readout_delimiters": True,
        "context_sensitive": {
            "ORBIT": "Translate according to context; in the editor camera selector it means an orbit/encircling camera (环绕相机), not orbital flight.",
        },
        "review_rule": "Do not normalize capitalization, unit spelling, IDs, or placeholders when they carry a code/control meaning.",
    }
    catalog = {"meta": meta, "terms": terms, "entries": entries}
    json_path.write_text(json.dumps(catalog, ensure_ascii=False, indent=2) + "\n", encoding="utf-8", newline="\n")
    write_plain_text(text_path)
    # Parse the emitted artifact once to catch encoding/serialization errors.
    emitted = read_json(json_path)
    if len(emitted.get("entries", [])) != len(entries) or len({e["entry_id"] for e in emitted["entries"]}) != len(entries):
        raise ValueError("JSON round-trip validation failed.")

    section_counts = collections.Counter(entry["section"] for entry in entries)
    print(f"JSON: {json_path}")
    print(f"CSV: {csv_path}")
    print(f"Instructions: {text_path}")
    print("Entries: " + ", ".join(f"{key}={section_counts[key]}" for key in sorted(section_counts)))
    print(f"Unique IDs: {len(ids)}; native sources missing: {missing_native}; placeholder mismatches: {len(placeholder_mismatches)}")
    print(f"Core prefab XML IDs: {parts_basis['prefab_id_intersection_count']}; explicit names: {parts_basis['prefab_explicit_display_name_count']}; ID fallbacks: {parts_basis['prefab_internal_id_fallback_count']}; locale parts mapped: {parts_basis['locale_parts_mapped_count']}")
    print("CSV round-trip: passed")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except (OSError, ValueError, ET.ParseError) as exc:
        print(f"export_dictionary: {exc}", file=sys.stderr)
        raise SystemExit(2)
