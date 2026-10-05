from pathlib import Path
import json
import sys

project = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(project / 'tools' / 'python-libs'))
from fontTools.ttLib import TTFont
from fontTools.varLib.instancer import instantiateVariableFont
from fontTools import subset

texts = ['Language / 语言', 'English', '简体中文']
for path in (project / 'assets' / 'KsaUiLanguages' / 'Locales').glob('*.json'):
    pack = json.loads(path.read_text(encoding='utf-8'))
    texts.append(pack['displayName'])
    for section in ('native', 'literals', 'ui', 'tooltips', 'editor', 'startup', 'parts'):
        texts.extend(pack.get(section, {}).keys())
        texts.extend(pack.get(section, {}).values())
font = TTFont(project / 'assets' / 'NotoSansSC-variable.ttf')
font = instantiateVariableFont(font, {'wght': 400}, inplace=True)
available = set(font.getBestCmap())
unicode_points = {ord(char) for text in texts for char in text if ord(char) >= 0x20}
for lower, upper in ((0x20, 0x100), (0x370, 0x400), (0x2000, 0x2070),
                     (0x2190, 0x2300), (0x2500, 0x2600)):
    unicode_points.update(set(range(lower, upper)) & available)
missing = unicode_points - available
if missing:
    raise ValueError(f'Font does not cover required code points: {sorted(missing)}')
options = subset.Options()
options.layout_features = ['*']
options.name_IDs = ['*']
subsetter = subset.Subsetter(options=options)
subsetter.populate(unicodes=unicode_points)
subsetter.subset(font)
renamed_names = {
    1: 'KSA UI Noto SC Subset', 2: 'Regular',
    3: 'KsaUiNotoSCSubset-Regular-0.2', 4: 'KSA UI Noto SC Subset Regular',
    6: 'KsaUiNotoSCSubset-Regular', 16: 'KSA UI Noto SC Subset', 17: 'Regular',
}
for name_id, value in renamed_names.items():
    for record in list(font['name'].names):
        if record.nameID == name_id:
            font['name'].setName(value, name_id, record.platformID, record.platEncID, record.langID)
    font['name'].setName(value, name_id, 3, 1, 0x409)
output = project / 'assets' / 'KsaUiLanguages' / 'Fonts' / 'KsaUiNotoSC.ttf'
output.parent.mkdir(parents=True, exist_ok=True)
font.save(output)
print(json.dumps({'font': str(output), 'code_points': len(unicode_points),
                  'bytes': output.stat().st_size}, ensure_ascii=False))
