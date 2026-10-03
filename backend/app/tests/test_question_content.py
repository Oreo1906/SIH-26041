import json
from pathlib import Path

import jsonschema

ROOT = Path(__file__).resolve().parents[3]


def test_both_bundled_question_banks_and_translations_are_complete():
    schema = json.loads((ROOT / 'schemas/question-bank.schema.json').read_text(encoding='utf-8'))
    locales = [json.loads((ROOT / f'demo/localization/{locale}.json').read_text(encoding='utf-8')) for locale in ('en', 'hi', 'sat')]
    files = list((ROOT / 'mobile-unity/Assets/StreamingAssets/Content/questions').glob('*.json'))
    assert len(files) == 2
    for file in files:
        bank = json.loads(file.read_text(encoding='utf-8')); jsonschema.validate(bank, schema)
        assert len(bank['questions']) >= 8
        assert len({item['id'] for item in bank['questions']}) == len(bank['questions'])
        for question in bank['questions']:
            assert 3 <= len(question['options']) <= 4
            ids = {item['id'] for item in question['options']}
            assert len(ids) == len(question['options'])
            assert set(question['correctOptionIds']) <= ids
            for key in [question['promptKey'], question['explanationKey'], *[item['textKey'] for item in question['options']]]:
                assert all(key in table for table in locales)


def test_offline_session_and_micro_training_policies_validate_and_reference_real_steps():
    content = ROOT / 'mobile-unity/Assets/StreamingAssets/Content'
    options = json.loads((content / 'session-options.json').read_text())
    jsonschema.validate(options, json.loads((ROOT / 'schemas/session-options.schema.json').read_text()))
    schema = json.loads((ROOT / 'schemas/micro-training-policy.schema.json').read_text())
    for name in ('fire', 'gas'):
        policy = json.loads((content / f'micro/{name}.json').read_text())
        scenario = json.loads((content / f'scenarios/{name}_v1.json').read_text())
        jsonschema.validate(policy, schema)
        assert policy['moduleId'] == scenario['moduleId']
        ids = {step['stepId'] for step in scenario['steps']}
        assert set(policy['generalStepIds']) <= ids
        assert set(policy['topicSteps'].values()) <= ids
