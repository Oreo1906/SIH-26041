"""Package unmodified demo fixtures with explicit runtime policies, without network."""

import json
from pathlib import Path
import shutil

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "examples/demo_content"
OUTPUT = ROOT / "mobile-unity/Assets/StreamingAssets/Content"

# Author decisions keyed by stable IDs; never derive scoring from array positions.
POLICIES = {
    "fire": {
        "entry": "fire_01_recognize", "terminal": "fire_04_evacuate",
        "categories": {
            "fire_01_recognize": "hazard_recognition",
            "fire_02_alarm": "alarm_notification",
            "fire_03_decision": "decision_equipment",
            "fire_04_evacuate": "evacuation_sequence",
        },
    },
    "gas": {
        "entry": "gas_01_alarm", "terminal": "gas_05_evacuate",
        "categories": {
            "gas_01_alarm": "alarm_recognition", "gas_02_zone": "hazard_zone",
            "gas_03_ppe": "ppe", "gas_04_buddy": "buddy_system",
            "gas_05_evacuate": "evacuation_reporting",
        },
    },
}


def main():
    for folder in (OUTPUT / "scenarios", OUTPUT / "policies", ROOT / "demo/runtime-policies"):
        folder.mkdir(parents=True, exist_ok=True)
    shutil.copyfile(SOURCE / "modules.example.json", OUTPUT / "modules.json")
    for name, rules in POLICIES.items():
        source = SOURCE / f"scenarios/{name}_v1.example.json"
        definition = json.loads(source.read_text(encoding="utf-8"))
        policy = {
            "scenarioId": definition["scenarioId"], "scenarioVersion": definition["version"],
            "entryStepId": rules["entry"], "terminalStepIds": [rules["terminal"]],
            "actionCategories": {
                step["stepId"] + "/" + action["actionId"]: rules["categories"][step["stepId"]]
                for step in definition["steps"] for action in step["allowedActions"] if action.get("scoreDelta", 0) != 0
            },
        }
        text = json.dumps(policy, indent=2, ensure_ascii=False) + "\n"
        (ROOT / f"demo/runtime-policies/{name}.json").write_text(text, encoding="utf-8")
        (OUTPUT / f"policies/{name}.json").write_text(text, encoding="utf-8")
        shutil.copyfile(source, OUTPUT / f"scenarios/{name}_v1.json")
    print("Bundled two unvalidated demo scenarios and explicit runtime policies.")


if __name__ == "__main__":
    main()
