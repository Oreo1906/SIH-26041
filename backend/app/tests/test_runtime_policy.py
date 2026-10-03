import json
from pathlib import Path

import pytest
from jsonschema import Draft202012Validator

ROOT = Path(__file__).resolve().parents[3]


@pytest.mark.parametrize("name", ["fire", "gas"])
def test_runtime_policy_has_explicit_references(name):
    schema = json.loads((ROOT / "schemas/scenario-runtime-policy.schema.json").read_text())
    policy = json.loads((ROOT / f"demo/runtime-policies/{name}.json").read_text())
    scenario = json.loads((ROOT / f"examples/demo_content/scenarios/{name}_v1.example.json").read_text())
    Draft202012Validator(schema).validate(policy)
    assert policy["scenarioId"] == scenario["scenarioId"]
    assert policy["scenarioVersion"] == scenario["version"]
    steps = {step["stepId"] for step in scenario["steps"]}
    assert policy["entryStepId"] in steps
    assert set(policy["terminalStepIds"]) <= steps
    for step in scenario["steps"]:
        for action in step["allowedActions"]:
            if action.get("scoreDelta", 0):
                category = policy["actionCategories"][step["stepId"] + "/" + action["actionId"]]
                assert category in scenario["scoring"]["categories"]
