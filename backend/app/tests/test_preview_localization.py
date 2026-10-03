"""Build-source localization coverage; placeholders remain explicit release gaps."""
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]


def test_every_preview_and_scenario_key_exists_in_all_three_locales():
    tables = {code: json.loads((ROOT / f'demo/localization/{code}.json').read_text(encoding='utf-8')) for code in ('en', 'hi', 'sat')}
    assert set(tables['en']) == set(tables['hi']) == set(tables['sat'])
    required = set()
    source = (ROOT / 'mobile-unity/Assets/SurakshaXR/Presentation/PreviewApp.cs').read_text(encoding='utf-8')
    required.update(re.findall(r'(?:T|Title)\("([^"\n]+)"\)', source))
    for file in (ROOT / 'examples/demo_content/scenarios').glob('*.json'):
        scenario = json.loads(file.read_text(encoding='utf-8'))
        required.update((scenario['titleKey'], scenario['descriptionKey']))
        required.update(scenario['objectives'])
        required.update(entity['labelKey'] for entity in scenario['entities'])
        required.update('category.' + category for category in scenario['scoring']['categories'])
        for step in scenario['steps']:
            required.update((step['objectiveKey'], step['narrationKey']))
            for action in step['allowedActions']:
                required.update((action['feedbackKey'], 'action.' + action['actionId']))
                required.update('topic.' + tag for tag in action['weakTopicTags'])
    for locale, table in tables.items():
        assert required <= table.keys(), (locale, required - table.keys())
        assert all(isinstance(value, str) and value.strip() for value in table.values())


def test_unreviewed_santali_copy_is_explicitly_labeled():
    table = json.loads((ROOT / 'demo/localization/sat.json').read_text(encoding='utf-8'))
    assert all(value.startswith('[SAT REVIEW] ') for value in table.values())


def test_hindi_draft_covers_training_quizzes_and_shell_without_review_placeholders():
    table = json.loads((ROOT / 'demo/localization/hi.json').read_text(encoding='utf-8'))
    assert all('[HI REVIEW]' not in value for value in table.values())
    for key, value in table.items():
        assert re.search(r'[\u0900-\u097f]', value), key
    # A filled translation table does not mean expert approval.
    assert 'पुनरीक्षण' in table['ui.translation_notice']
