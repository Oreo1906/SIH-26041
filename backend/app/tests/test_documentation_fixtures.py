"""Validate supplied JSON contracts without implementing the Phase 1 engine."""

import json
from pathlib import Path

import pytest
from jsonschema import Draft202012Validator, FormatChecker

ROOT = Path(__file__).resolve().parents[3]
SCHEMAS = ROOT / "schemas"
EXAMPLES = ROOT / "examples" / "demo_content"


def load(path):
    return json.loads(path.read_text(encoding="utf-8"))


@pytest.mark.parametrize("path", sorted(SCHEMAS.glob("*.json")), ids=lambda p: p.name)
def test_supplied_schema_is_valid(path):
    Draft202012Validator.check_schema(load(path))


def test_example_modules_conform_to_schema():
    validator = Draft202012Validator(
        load(SCHEMAS / "module.schema.json"), format_checker=FormatChecker()
    )
    for module in load(EXAMPLES / "modules.example.json"):
        validator.validate(module)


@pytest.mark.parametrize("filename", ["fire_v1.example.json", "gas_v1.example.json"])
def test_example_scenario_conforms_to_schema(filename):
    Draft202012Validator(
        load(SCHEMAS / "scenario.schema.json"), format_checker=FormatChecker()
    ).validate(load(EXAMPLES / "scenarios" / filename))
